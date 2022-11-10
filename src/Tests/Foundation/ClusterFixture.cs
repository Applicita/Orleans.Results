using Orleans.TestingHost;

namespace Orleans.Results.Tests;

public sealed class ClusterFixture : IDisposable
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder().AddSiloBuilderConfigurator<SiloConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.StopAllSilos();

    public TestingHost.TestCluster Cluster { get; }

    sealed class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) => siloBuilder.AddMemoryGrainStorageAsDefault();
    }
}
