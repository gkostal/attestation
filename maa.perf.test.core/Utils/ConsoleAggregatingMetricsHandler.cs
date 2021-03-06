using System;
using System.Collections.Generic;

namespace maa.perf.test.core.Utils
{
    class ConsoleAggregattingMetricsHandler
    {
        private int _lookbackSeconds;
        private List<IntervalMetrics> _lookbackData = new List<IntervalMetrics>();
        private int _simultaneousCount;
        static private List<string> _bufferedOutput = new List<string>();

        public ConsoleAggregattingMetricsHandler(int simultaneousCount, int lookbackSeconds = 15)
        {
            _simultaneousCount = simultaneousCount;
            _lookbackSeconds = lookbackSeconds;
        }

        public void MetricsAvailableHandler(IntervalMetrics metrics)
        {
            _lookbackData.Add(metrics);
            if (_lookbackData.Count > _lookbackSeconds)
            {
                _lookbackData.RemoveAt(0);
            }

            var endTime = metrics.EndTime;
            var duration = metrics.EndTime - _lookbackData[0].EndTime + TimeSpan.FromSeconds(1);
            var currentAggregation = new IntervalMetrics(_lookbackData[0], endTime, duration);
            for (int i=1; i<_lookbackData.Count; i++)
            {
                currentAggregation.Aggregate(_lookbackData[i]);
            }
            //Tracer.TraceInfo($"endTime: {currentAggregation.EndTime}    duration: {currentAggregation.Duration}    count: {currentAggregation.Count}    rps: {currentAggregation.RPS}");
            MetricsAvailableHandlerSingleLine(currentAggregation);
        }

        public void MetricsAvailableHandlerSingleLine(IntervalMetrics metrics)
        {
            List<string> messagesToFlush = null;

            var line = string.Format(
                "Resource: {0}  , Test: {1,-36}  , Time: {2,-22}, RPS: {3,7:f2}, Average:{4,5}, P50:{5,5}, P90:{6,5}, P95:{7,5}, P99:{8,5}",
                metrics.ResourceDescription, // 0
                metrics.TestDescription,     // 1
                metrics.EndTime.ToString(),  // 2
                metrics.RPS,                 // 3
                metrics.AverageLatencyMS,    // 4
                metrics.Percentile50,        // 5
                metrics.Percentile90,        // 6
                metrics.Percentile95,        // 7
                metrics.Percentile99);       // 8

            lock (_bufferedOutput)
            {
                _bufferedOutput.Add(line);
                if (_bufferedOutput.Count >= _simultaneousCount)
                {
                    messagesToFlush = _bufferedOutput;
                    _bufferedOutput = new List<string>();
                }
            }

            if (messagesToFlush != null)
            {
                messagesToFlush.Sort();
                foreach (var m in messagesToFlush)
                {
                    Console.WriteLine(m);
                }
                Console.WriteLine();
            }
        }
    }
}
