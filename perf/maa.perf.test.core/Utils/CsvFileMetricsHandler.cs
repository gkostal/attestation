﻿using System;
using System.IO;

namespace maa.perf.test.core.Utils
{
    class CsvFileMetricsHandler
    {
        public CsvFileMetricsHandler()
        {
        }

        public void MetricsAvailableHandler(IntervalMetrics metrics)
        {
            OpenFileIfNeeded(metrics);

            string csvLine = string.Format("{0}, {1}, {2,22}, {3,8}, {4,5:f0}, {13,5:f0}, {5,5:f1}, {6,5}, {7,5}, {8,5}, {9,5}, {10,5}, {11,5}, {12,5}",
                metrics.ResourceDescription,      // 0
                metrics.TestDescription,          // 1
                metrics.EndTime.ToString(),       // 2
                metrics.Count,                    // 3
                metrics.RPS,                      // 4
                metrics.CpuPercentage,            // 5
                metrics.AverageLatencyMS,         // 6
                metrics.Percentile50,             // 7
                metrics.Percentile90,             // 8
                metrics.Percentile95,             // 9
                metrics.Percentile99,             // 10
                metrics.Percentile995,            // 11
                metrics.Percentile999,            // 12
                metrics.TotalThrottledRequests);  // 13

            lock (_lock)
            {
                _fileWriter.WriteLine(csvLine);
                _fileWriter.Close();
                _fileWriter = File.AppendText(_filePath);
            }
        }

        private void OpenFileIfNeeded(IntervalMetrics metrics)
        {
            if (null == _fileWriter)
            {
                lock (_lock)
                {
                    if (null == _fileWriter)
                    {
                        DateTime startTime = DateTime.Now;
                        _filePath = string.Format("{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}-{8}.csv",
                            Environment.MachineName,
                            metrics.ProcessId,
                            startTime.Year,
                            startTime.Month,
                            startTime.Day,
                            startTime.Hour,
                            startTime.Minute,
                            startTime.Second,
                            metrics.TestDescription);
                        _fileWriter = File.AppendText(_filePath);

                        _fileWriter.WriteLine("\"ResourceDescription\"," +
                                              "\"TestDescription\"," +
                                              "\"IntervalTime\"," +
                                              "\"Count\"," +
                                              "\"RPS\"," +
                                              "\"TRPS\"," +
                                              "\"CPU\"," +
                                              "\"AverageLatency\"," +
                                              "\"P50\"," +
                                              "\"P90\"," +
                                              "\"P95\"," +
                                              "\"P99\"," +
                                              "\"P99.5\"," +
                                              "\"P99.9\"");
                    }
                }
            }
        }

        private readonly object _lock = new object();
        private string _filePath = null;
        private StreamWriter _fileWriter = null;
    }
}
