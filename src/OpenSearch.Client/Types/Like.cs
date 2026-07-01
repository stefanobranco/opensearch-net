using System.Text.Json.Serialization;

namespace OpenSearch.Client;

/// <summary>
/// A <c>more_like_this</c> input item: either free text to find similar documents for, or a
/// <see cref="LikeDocument"/> pointing at (or supplying) a document. Construct implicitly from a string
/// or a document, e.g. <c>Like l = "some text";</c> or <c>Like l = new LikeDocument { Index = "i", Id = "1" };</c>.
/// </summary>
[JsonConverter(typeof(LikeConverter))]
public sealed class Like
{
	/// <summary>The free-text form, or <c>null</c> when a document is used.</summary>
	public string? Text { get; }

	/// <summary>The document form, or <c>null</c> when free text is used.</summary>
	public LikeDocument? Document { get; }

	private Like(string text) => Text = text;
	private Like(LikeDocument document) => Document = document;

	public static implicit operator Like(string text) => new(text);
	public static implicit operator Like(LikeDocument document) => new(document);
}

public sealed class LikeConverter : StringOrDefinitionConverter<Like, LikeDocument>
{
	protected override Like Create(string name) => name;
	protected override Like Create(LikeDocument definition) => definition;
	protected override string? NameOf(Like value) => value.Text;
	protected override LikeDocument? DefinitionOf(Like value) => value.Document;
}
