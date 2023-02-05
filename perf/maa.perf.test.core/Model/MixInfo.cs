namespace maa.perf.test.core.Model
{
    using maa.perf.test.core.Utils;
    using System.Collections.Generic;
    using System.Linq;

    public class MixInfo
    {
        public MixInfo()
        {
            TestRuns = new List<TestRunInfo>();
            ApiMix = new List<WeightedApiInfo>();
            ProviderMix = new List<WeightedAttestationProvidersInfo>();
        }

        public List<TestRunInfo> TestRuns { get; set; }

        public List<WeightedApiInfo> ApiMix { get; set; }

        public List<WeightedAttestationProvidersInfo> ProviderMix { get; set; }

        public static MixInfo ReadMixInfo(string mixFileName)
        {
            var mixFileContents = default(MixInfo);

            if (!string.IsNullOrEmpty(mixFileName))
            {
                mixFileContents = SerializationHelper.ReadFromFileCached<MixInfo>(mixFileName);
                var totalApiWeight = mixFileContents.ApiMix?.Sum(a => a.Weight);
                var totalProviderWeight = mixFileContents.ProviderMix?.Sum(p => p.Weight);

                mixFileContents.ApiMix?.ForEach(a => a.Percentage = a.Weight / totalApiWeight.Value);
                mixFileContents.ProviderMix?.ForEach(p => p.Percentage = p.Weight / totalProviderWeight.Value);
            }

            return mixFileContents;
        }

        public void Trace()
        {
            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"Number of test runs      : {TestRuns.Count}");
            Tracer.TraceInfo($"Number of APIs called    : {ApiMix.Count}");
            Tracer.TraceInfo($"Number of provider infos : {ProviderMix.Count}");
            Tracer.TraceInfo($"");

            Tracer.TraceInfo($"**** Test Runs");
            for (int i = 0; i < TestRuns.Count; i++)
            {
                var tr = TestRuns[i];
                var timeDescription = tr.TestTimeSeconds == int.MaxValue ? "Infinite" : tr.TestTimeSeconds.ToString();
                Tracer.TraceInfo($"    Test run #{i,-5}  RPS: {tr.TargetRPS,-5}  Connections: {tr.SimultaneousConnections,-5}  Reconnect: {tr.ForceReconnects}   ServerTime: {tr.MeasureServerSideTime}   Time: {timeDescription}");
            }

            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"**** APIs Called");
            for (int i = 0; i < ApiMix.Count; i++)
            {
                var ai = ApiMix[i];
                Tracer.TraceInfo($"    API called #{i,-5}  Percentage: {ai.Percentage * 100.0d,-5}  Value: {(string.IsNullOrEmpty(ai.Url) ? ai.ApiName.ToString() : ai.Url)}  Headers: {ai.Headers}");
            }

            Tracer.TraceInfo($"");
            Tracer.TraceInfo($"**** Provider Infos");
            for (int i = 0; i < ProviderMix.Count; i++)
            {
                var pi = ProviderMix[i];
                Tracer.TraceInfo($"    Provider info #{i,-5}  Percentage: {pi.Percentage * 100.0d,-5}  Name: {pi.DnsName,-44}   IpAddress: {pi.IpAddress,-18}   OverrideName: {pi.TenantNameOverride,-20}   Count: {pi.ProviderCount}");
            }

            Tracer.TraceInfo($"");

        }
    }
}
