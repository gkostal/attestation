using Newtonsoft.Json;
using System;
using System.Net.Http;

namespace maa.perf.test.core.Utils
{
    public class PerfMetrics
    {
        public string SourceId;
        public DateTime TimeStamp;
        public double RequestUnits;
        public double RPS;
    }

    class BroadcastMetricsHandler
    {
        public BroadcastMetricsHandler()
        {
        }

        private HttpClient myClient = null;
        private string mySourceId = $"{Environment.MachineName} ({Utilities.GetLocalIpAddress()}) : {System.Diagnostics.Process.GetCurrentProcess().Id}";

        public void MetricsAvailableHandler(IntervalMetrics metrics)
        {
            if (null == myClient)
            {
                myClient = new HttpClient();
            }

            PerfMetrics p = new PerfMetrics()
            {
                RequestUnits = 0.0,
                RPS = metrics.RPS,
                SourceId = mySourceId,
                TimeStamp = DateTime.Now
            };
            string jp = JsonConvert.SerializeObject(p);
            StringContent content = new StringContent(jp, System.Text.Encoding.UTF8, "application/json");

            try
            {
                // Fire and forget ... best effort ... async ... 
                myClient.PostAsync("http://10.0.0.8:5000/api/PerfMetrics", content);
            }
            catch (Exception x)
            {
                Tracer.TraceVerbose($"Exception: {x.Message}");
            }
        }
    }
}
