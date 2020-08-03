using System;

namespace maa.perf.test.core.Utils
{
    public enum TracingLevel
    {
        Verbose = 0,
        Info,
        Warning,
        Error
    }

    public class Tracer
    {
        public static TracingLevel CurrentTracingLevel { get; set; } = TracingLevel.Verbose;

        public static void TraceVerbose(string format = "", params object[] args) { Trace(TracingLevel.Verbose, format, args); }
        public static void TraceInfo(string format = "", params object[] args) { Trace(TracingLevel.Info, format, args); }
        public static void TraceWarning(string format = "", params object[] args) { Trace(TracingLevel.Warning, format, args); }
        public static void TraceError(string format = "", params object[] args) { Trace(TracingLevel.Error, format, args); }
        public static void TraceRaw(string message) { TraceImpl(message); }

        private static void Trace(TracingLevel tracingLevel, string format, params object[] args)
        {
            if (tracingLevel >= CurrentTracingLevel)
            {
                string message = string.Format(format, args);
                TraceImpl(string.Format("{0}: {1}", tracingLevel.ToString(), message));
            }
        }

        private static void TraceImpl(string message)
        {
            Console.WriteLine(message);
        }
    }
}
