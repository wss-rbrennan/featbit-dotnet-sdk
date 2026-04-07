using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FeatBit.Sdk.Server.DataSynchronizer;
using FeatBit.Sdk.Server.Events;
using FeatBit.Sdk.Server.Model;
using FeatBit.Sdk.Server.Options;
using FeatBit.Sdk.Server.Store;
using FeatBit.Sdk.Server.Telemetry;

namespace FeatBit.Sdk.Server;

[Collection(nameof(TestApp))]
public class EvaluationTelemetryTests
{
    private readonly TestApp _app;

    public EvaluationTelemetryTests(TestApp app)
    {
        _app = app;
    }

    private FbClient CreateClient(bool trackDuration)
    {
        var options = new FbOptionsBuilder("test-secret")
            .Streaming(new Uri("ws://localhost/"))
            .TrackEvaluationDuration(trackDuration)
            .Build();

        var store = new DefaultMemoryStore();
        var synchronizer = new WebSocketDataSynchronizer(options, store, op => _app.CreateFbWebSocket(op));
        var client = new FbClient(options, store, synchronizer, new NullEventProcessor());
        return client;
    }

    [Fact]
    public void MetricsHistogram_RecordsEvaluationDuration_WhenTrackingEnabled()
    {
        var client = CreateClient(trackDuration: true);
        var user = FbUser.Builder("u1").Build();

        var measurements = new List<double>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == FeatBitSdkMeter.MeterName &&
                instrument.Name == "featbit.flag.evaluation.duration")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((_, value, _, _) => measurements.Add(value));
        meterListener.Start();

        client.BoolVariation("returns-true", user);

        meterListener.RecordObservableInstruments();

        Assert.Single(measurements);
        Assert.True(measurements[0] >= 0, "Duration should be non-negative");
    }

    [Fact]
    public void MetricsHistogram_NotRecorded_WhenTrackingDisabled()
    {
        var client = CreateClient(trackDuration: false);
        var user = FbUser.Builder("u1").Build();

        var measurements = new List<double>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == FeatBitSdkMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((_, value, _, _) => measurements.Add(value));
        meterListener.Start();

        client.BoolVariation("returns-true", user);

        Assert.Empty(measurements);
    }

    [Fact]
    public void ActivitySource_StartsAndStopsActivity_WhenTrackingEnabled()
    {
        var client = CreateClient(trackDuration: true);
        var user = FbUser.Builder("u1").Build();

        Activity recordedActivity = null;

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeatBitSdkActivitySource.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => recordedActivity = activity
        };
        ActivitySource.AddActivityListener(activityListener);

        client.BoolVariation("returns-true", user);

        Assert.NotNull(recordedActivity);
        Assert.Equal(FeatBitSdkActivitySource.EvaluationActivityName, recordedActivity.OperationName);
        Assert.Equal("returns-true", recordedActivity.GetTagItem("flag.key"));
        Assert.NotNull(recordedActivity.GetTagItem("result.kind"));
        Assert.NotNull(recordedActivity.GetTagItem("result.reason"));
    }

    [Fact]
    public void ActivitySource_NoActivity_WhenTrackingDisabled()
    {
        var client = CreateClient(trackDuration: false);
        var user = FbUser.Builder("u1").Build();

        var activitiesStarted = 0;

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeatBitSdkActivitySource.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => activitiesStarted++
        };
        ActivitySource.AddActivityListener(activityListener);

        client.BoolVariation("returns-true", user);

        Assert.Equal(0, activitiesStarted);
    }
}
