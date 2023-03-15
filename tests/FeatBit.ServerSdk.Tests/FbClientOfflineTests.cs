using FeatBit.Sdk.Server.DataSynchronizer;
using FeatBit.Sdk.Server.Events;
using FeatBit.Sdk.Server.Model;
using FeatBit.Sdk.Server.Options;

namespace FeatBit.Sdk.Server;

public class FbClientOfflineTests
{
    [Fact]
    public async Task CreateAndClose()
    {
        var options = new FbOptionsBuilder()
            .Offline(true)
            .Build();

        var client = new FbClient(options);
        await client.CloseAsync();
    }

    [Fact]
    public void UseNullEventProcessor()
    {
        var options = new FbOptionsBuilder()
            .Offline(true)
            .Build();

        var client = new FbClient(options);

        Assert.IsType<NullDataSynchronizer>(client._dataSynchronizer);
    }

    [Fact]
    public void UseNullDataSource()
    {
        var options = new FbOptionsBuilder()
            .Offline(true)
            .Build();

        var client = new FbClient(options);

        Assert.IsType<NullEventProcessor>(client._eventProcessor);
    }

    [Fact]
    public void ClientIsInitialized()
    {
        var options = new FbOptionsBuilder()
            .Offline(true)
            .Build();

        var client = new FbClient(options);

        Assert.True(client.Initialized);
    }

    [Fact]
    public void ReturnsDefaultValue()
    {
        var options = new FbOptionsBuilder()
            .Offline(true)
            .Build();

        var client = new FbClient(options);

        var user = FbUser.Builder("tester").Build();

        var variation = client.StringVariation("hello", user, "fallback-value");
        Assert.Equal("fallback-value", variation);
    }
}