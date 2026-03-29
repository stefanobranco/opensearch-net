namespace OpenSearch.Client.Core;

public partial class SearchResponse<TDocument>
{
	public override bool IsValid =>
		base.IsValid && (Shards is null || Shards.Failed == 0);
}
