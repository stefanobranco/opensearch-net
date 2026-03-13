using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that smuggles a context object into
/// <see cref="JsonSerializerOptions"/>. It never actually converts any type; instead,
/// it acts as a carrier so that other converters can retrieve the context via
/// <see cref="Get(JsonSerializerOptions)"/>.
/// </summary>
/// <remarks>
/// This pattern is borrowed from the Elastic .NET v8 client and is necessary because
/// <see cref="JsonSerializerOptions"/> doesn't have a general-purpose "services" bag.
/// By adding a <see cref="ContextProvider{TContext}"/> to the converters list, any
/// converter that needs <typeparamref name="TContext"/> can look it up at runtime.
/// </remarks>
/// <typeparam name="TContext">The type of context to carry. Typically <see cref="IOpenSearchClientSettings"/>.</typeparam>
public sealed class ContextProvider<TContext> : JsonConverterFactory where TContext : class
{
	private static readonly ConditionalWeakTable<JsonSerializerOptions, Box> Cache = new();

	private readonly TContext _context;

	/// <summary>
	/// Creates a new context provider carrying the given <paramref name="context"/>.
	/// </summary>
	public ContextProvider(TContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		_context = context;
	}

	/// <inheritdoc />
	/// <remarks>Always returns <c>false</c> -- this factory never converts any type.</remarks>
	public override bool CanConvert(Type typeToConvert) => false;

	/// <inheritdoc />
	/// <remarks>Always returns <c>null</c> -- this factory never creates converters.</remarks>
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => null;

	/// <summary>
	/// Retrieves the <typeparamref name="TContext"/> from the given <paramref name="options"/>,
	/// or <c>null</c> if no <see cref="ContextProvider{TContext}"/> has been registered.
	/// </summary>
	public static TContext? Get(JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (Cache.TryGetValue(options, out var cached))
			return cached.Value;

		foreach (var converter in options.Converters)
		{
			if (converter is ContextProvider<TContext> provider)
			{
				Cache.AddOrUpdate(options, new Box(provider._context));
				return provider._context;
			}
		}

		return null;
	}

	// ConditionalWeakTable requires a reference-type value.
	private sealed class Box(TContext value)
	{
		public TContext Value { get; } = value;
	}
}
