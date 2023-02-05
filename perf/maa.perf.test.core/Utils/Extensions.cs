using maa.perf.test.core.Model;
using System;
using System.Collections.Generic;

namespace maa.perf.test.core.Utils
{
    public static class Extensions
    {
        static Random _rnd = new Random();

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime;
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static void Trace(this Exception x)
        {
            Tracer.TraceWarning(x.ToString());
        }

        public static T GetRandomWeightedSample<T>(this List<T> allSamples) where T : IWeightedObject
        {
            var randomValue = _rnd.NextDouble();
            var currentSum = 0.0d;

            foreach (var s in allSamples)
            {
                currentSum += s.Percentage;
                if (currentSum > randomValue)
                {
                    return s;
                }
            }

            return allSamples[^1];
        }


        public static T GetRandomSample<T>(this List<T> allSamples)
        {
            return allSamples[_rnd.Next(0, allSamples.Count)];
        }
    }
}
