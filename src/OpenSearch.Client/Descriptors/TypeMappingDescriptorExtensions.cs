namespace OpenSearch.Client;

/// <summary>
/// Fluent <c>properties</c> building for the generated <see cref="TypeMappingDescriptor"/>. The generated
/// <c>Properties(Dictionary&lt;string, Property&gt;)</c> takes a pre-built map; these add a fluent
/// <see cref="PropertiesDescriptor{TDocument}"/> so fields can be declared by name or by member expression.
/// </summary>
public static class TypeMappingDescriptorExtensions
{
	/// <summary>Builds the mapping's <c>properties</c> by field name.</summary>
	public static TypeMappingDescriptor Properties(this TypeMappingDescriptor d, Action<PropertiesDescriptor<object>> configure)
	{
		var descriptor = new PropertiesDescriptor<object>();
		configure(descriptor);
		d._value.Properties = descriptor._dict;
		return d;
	}

	/// <summary>Builds the mapping's <c>properties</c>, allowing <typeparamref name="TDocument"/> member
	/// expressions to name fields (e.g. <c>.Properties&lt;MyDoc&gt;(p =&gt; p.Text(x =&gt; x.Title, t =&gt; ...))</c>).</summary>
	public static TypeMappingDescriptor Properties<TDocument>(this TypeMappingDescriptor d, Action<PropertiesDescriptor<TDocument>> configure)
	{
		var descriptor = new PropertiesDescriptor<TDocument>();
		configure(descriptor);
		d._value.Properties = descriptor._dict;
		return d;
	}
}
