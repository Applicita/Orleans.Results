namespace Orleans.Results.Tests;

[CollectionDefinition(Name)]
public class TestCluster : ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}
