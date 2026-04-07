using System.Diagnostics.Metrics;

namespace FeatBit.Sdk.Server.Telemetry
{
    internal static class FeatBitSdkMeter
    {
        internal const string MeterName = "FeatBit.ServerSdk";

        private static readonly Meter Meter = new Meter(MeterName);

        /// <summary>
        /// Records the duration of a flag evaluation in milliseconds.
        /// Instrument name: <c>featbit.flag.evaluation.duration</c>
        /// Tags: <c>flag.key</c>, <c>result.kind</c>
        /// </summary>
        internal static readonly Histogram<double> EvaluationDuration =
            Meter.CreateHistogram<double>(
                name: "featbit.flag.evaluation.duration",
                unit: "ms",
                description: "Duration of a feature flag evaluation."
            );
    }
}
