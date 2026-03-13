namespace OpenSearch.Net;

/// <summary>
/// Manages HTTP handler rotation for DNS refresh. Creates new <see cref="HttpMessageHandler"/>
/// instances periodically to ensure stale DNS entries don't persist. Thread-safe.
/// </summary>
internal sealed class HttpClientFactory : IDisposable
{
	private readonly TimeSpan _handlerLifetime;
	private readonly Func<HttpMessageHandler> _handlerFactory;
	private volatile HandlerEntry _currentEntry;
	private readonly object _rotationLock = new();
	private int _disposed;

	private readonly TimeSpan _requestTimeout;

	public HttpClientFactory(
		TimeSpan? handlerLifetime = null,
		Func<HttpMessageHandler>? handlerFactory = null,
		TimeSpan? requestTimeout = null)
	{
		_handlerLifetime = handlerLifetime ?? TimeSpan.FromMinutes(5);
		_handlerFactory = handlerFactory ?? CreateDefaultHandler;
		_requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(60);
		_currentEntry = new HandlerEntry(_handlerFactory(), _requestTimeout);
	}

	/// <summary>
	/// Returns the current (possibly rotated) <see cref="HttpClient"/>.
	/// The client is reused across requests for the lifetime of its handler.
	/// </summary>
	public HttpClient GetClient()
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

		var entry = _currentEntry;
		if (entry.IsExpired(_handlerLifetime))
			entry = RotateHandler(entry);

		return entry.Client;
	}

	private HandlerEntry RotateHandler(HandlerEntry staleEntry)
	{
		lock (_rotationLock)
		{
			// Double-check: another thread may have already rotated.
			var current = _currentEntry;
			if (current.CreatedAtTicks != staleEntry.CreatedAtTicks)
				return current;

			var oldEntry = _currentEntry;
			_currentEntry = new HandlerEntry(_handlerFactory(), _requestTimeout);

			// Dispose the old handler on a background thread to allow
			// in-flight requests to complete.
			_ = DisposeAfterDelay(oldEntry, TimeSpan.FromSeconds(20));

			return _currentEntry;
		}
	}

	private static async Task DisposeAfterDelay(HandlerEntry entry, TimeSpan delay)
	{
		try
		{
			await Task.Delay(delay).ConfigureAwait(false);
		}
		finally
		{
			entry.Client.Dispose();
			entry.Handler.Dispose();
		}
	}

	private static HttpMessageHandler CreateDefaultHandler() =>
		new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(5),
			AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
		};

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		var entry = _currentEntry;
		entry.Client.Dispose();
		entry.Handler.Dispose();
	}

	private sealed class HandlerEntry
	{
		public HttpMessageHandler Handler { get; }
		public HttpClient Client { get; }
		public long CreatedAtTicks { get; }

		public HandlerEntry(HttpMessageHandler handler, TimeSpan requestTimeout)
		{
			Handler = handler;
			Client = new HttpClient(handler, disposeHandler: false) { Timeout = requestTimeout };
			CreatedAtTicks = Environment.TickCount64;
		}

		public bool IsExpired(TimeSpan lifetime) =>
			Environment.TickCount64 - CreatedAtTicks >= (long)lifetime.TotalMilliseconds;
	}
}
