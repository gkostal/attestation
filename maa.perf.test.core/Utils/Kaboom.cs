using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core.Utils
{
    public class Kaboom
    {
        private Action<string> _traceMessage;

        public Kaboom (TimeSpan waitTime, bool traceCountdown)
        {
            _traceMessage = traceCountdown ? TraceMessage : IgnoreMessage;
            Task.Run(async () => await Countdown(waitTime));
        }

        private async Task Countdown(TimeSpan waitTime)
        {
            var tickInterval = TimeSpan.FromSeconds(1);
            
            while (waitTime >= TimeSpan.FromSeconds(0))
            {
                _traceMessage($"Shutdown countdown: Remaining seconds = {waitTime.TotalSeconds}");
                await Task.Delay(tickInterval);
                waitTime -= tickInterval;
            }

            _traceMessage($"Shutdown countdown: Exiting process now");
            Environment.Exit(-1);
        }

        private void TraceMessage(string message)
        {
            Tracer.TraceInfo(message);
        }

        private void IgnoreMessage(string _)
        {
        }
    }
}
