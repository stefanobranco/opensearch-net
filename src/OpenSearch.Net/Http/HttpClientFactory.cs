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
	private bool _disposed;

	public HttpClientFactory(TimeSpan? handlerLifetime = null, Func<HttpMessageHandler>? handlerFactory = null)
	{
		_handlerLifetime = handlerLifetime ?? TimeSpan.FromMinutes(5);
		_handlerFactory = handlerFactory ?? CreateDefaultHandler;
		_currentEntry = new HandlerEntry(_handlerFactory(), Environment.TickCount64);
	}

	/// <summary>
	/// Returns an <see cref="HttpClient"/> backed by the current (possibly rotated) handler.
	/// The returned client must NOT dispose the handler.
	/// </summary>
	public HttpClient CreateClient()
	{
		var entry = _currentEntry;
		MaybeRotateHandler(entry);
		return new HttpClient(_currentEntry.Handler, disposeHandler: false);
	}

	private void MaybeRotateHandler(HandlerEntry entry)
	{
		var elapsed = Environment.TickCount64 - entry.CreatedAtTicks;
		if (elapsed < (long)_handlerLifetime.TotalMilliseconds)
			return;

		lock (_rotationLock)
		{
			// Double-check: another thread may have already rotated.
			if (_currentEntry.CreatedAtTicks != entry.CreatedAtTicks)
				return;

			var oldHandler = _currentEntry.Handler;
			_currentEntry = new HandlerEntry(_handlerFactory(), Environment.TickCount64);

			// Dispose the old handler on a background thread to avoid blocking callers
			// that may still have in-flight requests completing on the old handler.
			// A small delay gives those requests time to finish.
			_ = DisposeHandlerAfterDelay(oldHandler, TimeSpan.FromSeconds(20));
		}
	}

	private static async Task DisposeHandlerAfterDelay(HttpMessageHandler handler, TimeSpan delay)
	{
		try
		{
			await Task.Delay(delay).ConfigureAwait(false);
		}
		finally
		{
			handler.Dispose();
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
		if (_disposed)
			return;

		_disposed = true;
		_currentEntry.Handler.Dispose();
	}

	private sealed record HandlerEntry(HttpMessageHandler Handler, long CreatedAtTicks);
}
