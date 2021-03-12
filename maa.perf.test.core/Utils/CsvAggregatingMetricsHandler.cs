using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace maa.perf.test.core.Utils
{
    class CsvAggregatingMetricsHandler : IDisposable
    {
        private class TestRunMetrics
        {
            public IntervalMetrics TheIntervalMetrics { get; set; }
            public DateTime MinTime { get; set; }
            public DateTime MaxTime { get; set; }
        }

        private ConcurrentDictionary<(double, long, string), TestRunMetrics> _testRunMetrics = new ConcurrentDictionary<(double, long, string), TestRunMetrics>();
        private double _currentRps;
        private long _currentConnections;

        public void SetRpsAndConnections(double rps, long connections)
        {
            _currentRps = rps;
            _currentConnections = connections;
        }

        public void MetricsAvailableHandler(IntervalMetrics metrics)
        {
            TestRunMetrics testRunMetrics = null;
            (double, long, string) key = (_currentRps, _currentConnections, metrics.TestDescription);

            if (_testRunMetrics.ContainsKey(key))
            {
                testRunMetrics = _testRunMetrics[key];
                lock (testRunMetrics)
                {
                    testRunMetrics.TheIntervalMetrics.Aggregate(metrics);
                    if (metrics.EndTime < testRunMetrics.MinTime)
                    {
                        testRunMetrics.MinTime = metrics.EndTime;
                    }
                    if (metrics.EndTime > testRunMetrics.MaxTime)
                    {
                        testRunMetrics.MaxTime = metrics.EndTime;
                    }
                }
            }
            else
            {
                testRunMetrics = new TestRunMetrics()
                {
                    TheIntervalMetrics = new IntervalMetrics(metrics, metrics.EndTime, metrics.Duration),
                    MinTime = metrics.EndTime,
                    MaxTime = metrics.EndTime
                };

                _testRunMetrics.TryAdd(key, testRunMetrics);
            }
        }

        public void Dispose()
        {
            if (_testRunMetrics.Count > 0)
            {
                var startTime = DateTime.Now;
                var filePath = string.Format("{0}-{1}-{2:d2}-{3:d2}-{4:d2}-{5:d2}-{6:d2}-{7}.csv",
                    Environment.MachineName,
                    startTime.Year,
                    startTime.Month,
                    startTime.Day,
                    startTime.Hour,
                    startTime.Minute,
                    startTime.Second,
                    "uber");

                using (var fileWriter = File.AppendText(filePath))
                {
                    fileWriter.WriteLine("\"TotalRPS\"," +
                                         "\"Connections\"," +
                                         "\"ResourceDescription\"," +
                                         "\"TestDescription\"," +
                                         "\"DateTime\"," +
                                         "\"DurationSeconds\"," +
                                         "\"Count\"," +
                                         "\"RPS\"," +
                                         "\"AverageLatency\"," +
                                         "\"P50\"," +
                                         "\"P90\"," +
                                         "\"P95\"," +
                                         "\"P99\"," +
                                         "\"P99.5\"," +
                                         "\"P99.9\"");

                    var sortedKeys = _testRunMetrics.Keys.ToArray();
                    Array.Sort(sortedKeys);

                    for (int i = 0; i < sortedKeys.Length; i++)
                    {
                        var key = sortedKeys[i];
                        var metrics = _testRunMetrics[key];
                        var finalMetric = new IntervalMetrics(metrics.TheIntervalMetrics, metrics.MinTime, metrics.MaxTime - metrics.MinTime + TimeSpan.FromSeconds(1));

                        var csvLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                            key.Item1,
                            key.Item2,
                            finalMetric.ResourceDescription,
                            finalMetric.TestDescription,
                            finalMetric.EndTime.ToString(),
                            finalMetric.Duration.TotalSeconds,
                            finalMetric.Count,
                            finalMetric.RPS,
                            finalMetric.AverageLatencyMS,
                            finalMetric.Percentile50,
                            finalMetric.Percentile90,
                            finalMetric.Percentile95,
                            finalMetric.Percentile99,
                            finalMetric.Percentile995,
                            finalMetric.Percentile999);

                        fileWriter.WriteLine(csvLine);
                    }

                    fileWriter.Close();
                }
            }
        }
    }
}
