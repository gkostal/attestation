using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core.Utils
{
    class CsvAggregatingMetricsHandler : IDisposable
    {
        private readonly object _lock = new object();
        private readonly string _uberDescription;
        private Dictionary<string, List<IntervalMetrics>> _testMetricDictionary = new Dictionary<string, List<IntervalMetrics>>();

        public CsvAggregatingMetricsHandler(string uberDescription)
        {
            _uberDescription = uberDescription;
        }

        public void MetricsAvailableHandler(IntervalMetrics metrics)
        {
            List<IntervalMetrics> testMetricsList;

            lock (_lock)
            {
                if (!_testMetricDictionary.ContainsKey(metrics.TestDescription))
                {
                    testMetricsList = new List<IntervalMetrics>();
                    _testMetricDictionary.Add(metrics.TestDescription, testMetricsList);
                }
                else
                {
                    testMetricsList = _testMetricDictionary[metrics.TestDescription];
                }

                testMetricsList.Add(metrics);
            }
        }

        public void Dispose()
        {
            var startTime = DateTime.Now;
            var filePath = string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}.csv",
                Environment.MachineName,
                startTime.Year,
                startTime.Month,
                startTime.Day,
                startTime.Hour,
                startTime.Minute,
                startTime.Second,
                _uberDescription);
            
            using (var fileWriter = File.AppendText(filePath))
            {
                fileWriter.WriteLine("\"ResourceDescription\",\"TestDescription\",\"IntervalTime\",\"Count\",\"RPS\",\"AverageLatency\",\"P50\",\"P90\",\"P95\",\"P99\",\"P99.5\",\"P99.9\"");

                var sortedKeys = _testMetricDictionary.Keys.ToArray();
                Array.Sort(sortedKeys);

                for (int i = 0; i < sortedKeys.Length; i++)
                {
                    var metrics = _testMetricDictionary[sortedKeys[i]];
                    var endTime = metrics[^1].EndTime;
                    var duration = metrics[^1].EndTime - metrics[0].EndTime + TimeSpan.FromSeconds(1);
                    var currentAggregation = new IntervalMetrics(metrics[0], endTime, duration);

                    for (int j = 1; j < metrics.Count; j++)
                    {
                        currentAggregation.Aggregate(metrics[j]);
                    }

                    var csvLine = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}",
                        currentAggregation.ResourceDescription,
                        currentAggregation.TestDescription,
                        currentAggregation.EndTime.ToString(),
                        currentAggregation.Count,
                        currentAggregation.RPS,
                        currentAggregation.AverageLatencyMS,
                        currentAggregation.Percentile50,
                        currentAggregation.Percentile90,
                        currentAggregation.Percentile95,
                        currentAggregation.Percentile99,
                        currentAggregation.Percentile995,
                        currentAggregation.Percentile999);

                    fileWriter.WriteLine(csvLine);
                }

                fileWriter.Close();
            }
        }
    }
}
