using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq;

namespace maa.perf.test.core.Utils
{
    public class IntervalMetrics
    {
        // ID field
        //[JsonProperty(PropertyName = "id")]
        public DateTime EndTime { get; private set; }

        // Non-calculated attributes
        public TimeSpan Duration { get; private set; }
        public string TestDescription { get; private set; }
        public string MachineName { get; private set; }
        public int ProcessId { get; private set; }
        public string ResourceDescription { get; private set; }

        // Calculated attributes from latency distribution times
        public long Count { get; private set; }
        public double RPS { get; private set; }
        public int AverageLatencyMS { get; private set; }
        public int Min { get; private set; }
        public int Max { get; private set; }
        public int Percentile50 { get; private set; }
        public int Percentile90 { get; private set; }
        public int Percentile95 { get; private set; }
        public int Percentile99 { get; private set; }
        public int Percentile995 { get; private set; }
        public int Percentile999 { get; private set; }

        // NON seriliazed attributes
        [JsonIgnore]
        public ConcurrentDictionary<int, int> LatencyDistributionMS { get; private set; } = new ConcurrentDictionary<int, int>();

        public IntervalMetrics(DateTime endTime, TimeSpan duration, string testDescription, string machineName, int processId, string resourceDescription, ConcurrentDictionary<int, int> latencyDistributionMS)
        {
            EndTime = endTime;
            Duration = duration;
            TestDescription = testDescription;
            MachineName = machineName;
            ProcessId = processId;
            ResourceDescription = resourceDescription;

            foreach (var latencyTime in latencyDistributionMS.Keys)
            {
                int otherValue = latencyDistributionMS[latencyTime];
                LatencyDistributionMS.AddOrUpdate(latencyTime, otherValue, (key, currentValue) => currentValue + otherValue);
            }

            ComputeCalculatedAttributes();
        }

        public IntervalMetrics(IntervalMetrics other, DateTime endTime, TimeSpan duration)
            : this(endTime, duration, other.TestDescription, other.MachineName, other.ProcessId, other.ResourceDescription, other.LatencyDistributionMS)
        {
        }

        public void Aggregate(IntervalMetrics other)
        {
            // Aggregate latency times
            foreach (var latencyTime in other.LatencyDistributionMS.Keys)
            {
                int otherValue = other.LatencyDistributionMS[latencyTime];
                LatencyDistributionMS.AddOrUpdate(latencyTime, otherValue, (key, currentValue) => currentValue + otherValue);
            }

            // Recompute other values
            ComputeCalculatedAttributes();
        }

        private void ComputeCalculatedAttributes()
        {
            // Perform calculations first
            int intervalRequestCount = 0;
            long intervalTotalRequestDurationMs = 0;
            foreach (var kvp in LatencyDistributionMS)
            {
                intervalTotalRequestDurationMs += (kvp.Key * kvp.Value);
                intervalRequestCount += kvp.Value;
            }

            double rps = ((double)(1000 * intervalRequestCount)) / ((double)Duration.TotalMilliseconds);
            int averageLatencyMS = (int)(0 != intervalRequestCount ? intervalTotalRequestDurationMs / intervalRequestCount : 0);

            // Calculate percentiles
            var orderedLatencyTimes = LatencyDistributionMS.OrderBy((p) => p.Key);
            Dictionary<int, int> percentileIndex = new Dictionary<int, int>
            {
                {  50, GetPercentileIndex(50, intervalRequestCount) } ,
                {  90, GetPercentileIndex(90, intervalRequestCount) } ,
                {  95, GetPercentileIndex(95, intervalRequestCount) } ,
                {  99, GetPercentileIndex(99, intervalRequestCount) },
                {  995, GetPercentileIndex(99.5, intervalRequestCount) },
                {  999, GetPercentileIndex(99.9, intervalRequestCount) },
            };
            Dictionary<int, int> percentileValues = new Dictionary<int, int>();
            foreach (var x in percentileIndex) { percentileValues.Add(x.Key, -1); }

            int currCount = 0;
            Min = int.MaxValue;
            Max = int.MinValue;
            foreach (var x in orderedLatencyTimes)
            {
                Min = Math.Min(Min, x.Key);
                Max = Math.Max(Max, x.Key);
                int currentMax = currCount + x.Value;
                currCount += x.Value;
                foreach (var p in percentileIndex)
                {
                    if ((-1 == percentileValues[p.Key]) && (p.Value <= currentMax))
                        percentileValues[p.Key] = x.Key;
                }
            }

            // Record values second
            Count = intervalRequestCount;
            RPS = rps;
            AverageLatencyMS = averageLatencyMS;
            Percentile50 = percentileValues[50];
            Percentile90 = percentileValues[90];
            Percentile95 = percentileValues[95];
            Percentile99 = percentileValues[99];
            Percentile995 = percentileValues[995];
            Percentile999 = percentileValues[999];
        }

        public void SetAdditionalValues(string vault, string resource)
        {
            ResourceDescription = resource;
        }

        private int GetPercentileIndex(double percentile, int totalCount)
        {
            return (int)Math.Ceiling((double)totalCount * (double)percentile / 100.0);
        }

    }
}
