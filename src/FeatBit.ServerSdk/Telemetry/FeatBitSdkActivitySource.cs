using System.Diagnostics;

namespace FeatBit.Sdk.Server.Telemetry
{
    internal static class FeatBitSdkActivitySource
    {
        internal const string ActivitySourceName = "FeatBit.ServerSdk";

        internal static readonly ActivitySource ActivitySource =
            new ActivitySource(ActivitySourceName);

        internal const string EvaluationActivityName = "featbit.flag.evaluation";
    }
}
