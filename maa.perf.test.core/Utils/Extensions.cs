using System;

namespace maa.perf.test.core.Utils
{
    public static class Extensions
    {
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime;
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static void Trace(this Exception x)
        {
            Tracer.TraceWarning(x.ToString());
        }
    }
}
