using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace maa.perf.test.core.Utils
{
    public class AsyncFor
    {
        #region Public

        /// <summary>
        /// On one thread asynchronously call function 'asyncWorkerFunction' a total 
        /// of 'totalIterations' times asynchronously waiting a maximum of 
        /// 'maxSimultaneousAwaits' times.
        /// </summary>
        /// <param name="totalIterations">Total number of times to execute asyncWorkerFunction</param>
        /// <param name="maxSimultanousAwaits">Maximum number of simultaneous awaits</param>
        /// <param name="asyncWorkerFunction">Async worker function that performs desired action(s)</param>
        /// <param name="maxTPS">Maximum number of transactions per second, 0 means no limit</param>
        /// <returns></returns>
        //public static async Task StaticFor(long totalIterations, long maxSimultanousAwaits, Func<Task> asyncWorkerFunction, long maxTPS = 0)
        //{
        //    AsyncFor asyncFor = new AsyncFor(maxTPS);
        //    await asyncFor.For(totalIterations, maxSimultanousAwaits, asyncWorkerFunction);
        //}

        public delegate void IntervalMetricsNotification(IntervalMetrics metrics);
        public event IntervalMetricsNotification PopulateExtendedMetrics;
        public event IntervalMetricsNotification PerMinuteMetricsAvailable;
        public event IntervalMetricsNotification PerSecondMetricsAvailable;

        public AsyncFor(Func<double> getTps, string resourceDescription, string testDescription)
        {
            _getTps = getTps;
            _resourceDescription = resourceDescription;
            _testDescription = testDescription;
            _currentCount = 0;
            _timer = new Stopwatch();
            _throttlingCurrentCount = 0;
            _throttlingTimer = new Stopwatch();
        }

        public AsyncFor(double maxTPS, string resourceDescription, string testDescription)
            : this(() => maxTPS, resourceDescription, testDescription)
        {
        }

        public async Task For(TimeSpan totalDuration, long maxSimultanousAwaits, Func<Task<double>> asyncWorkerFunction)
        {
            await For(() => _timer.Elapsed <= totalDuration, maxSimultanousAwaits, asyncWorkerFunction);
        }

        public async Task For(long totalIterations, long maxSimultanousAwaits, Func<Task<double>> asyncWorkerFunction)
        {
            await For(() => _currentCount <= totalIterations, maxSimultanousAwaits, asyncWorkerFunction);
        }

        public async Task For(Func<bool> shouldContinue, long maxSimultanousAwaits, Func<Task<double>> asyncWorkerFunction)
        {
            _timer.Start();
            _throttlingTimer.Start();
            _enabled = true;
            _testDescription ??= asyncWorkerFunction.Method.Name;

            List<Task> tasks = new List<Task>();
            for (long i = 0; i < maxSimultanousAwaits; i++)
            {
                tasks.Add(InternalFor(shouldContinue, asyncWorkerFunction));
            }

            // On purpose do NOT await the loop that prints status forever ...
            // Let it run forever ... 
#pragma warning disable 4014
            ReportObservedTPSAsync();

            // Wait on all 
            await Task.WhenAll(tasks.ToArray());
            _enabled = false;
        }

        public void Terminate()
        {
            _enabled = false;
        }

        #endregion

        #region Private

        private int GetMillisecondsToStartOfNextInterval()
        {
            int currentMillisecondOffset = DateTime.Now.Millisecond;
            return (currentMillisecondOffset == 0 ?
                0 :
                1000 - currentMillisecondOffset);
        }

        private async Task ReportObservedTPSAsync()
        {
            _lastReportTime = DateTime.Now;

            // Write TPS periodically
            while (_enabled)
            {
                try
                {
                    // Wait for start of next interval
                    await Task.Delay(GetMillisecondsToStartOfNextInterval());

                    // Note info for current interval, resetting interval aggretation data members
                    DateTime currentTimeSnapshot = DateTime.Now;
                    var recentLatencyTimes = Interlocked.Exchange(ref _intervalLatencyTimes, new ConcurrentDictionary<int, int>());
                    var recentTotalRequestCharge = Interlocked.Exchange(ref _totalRequestCharge, 0.0);

                    // Calculate one second interval metrics
                    IntervalMetrics m = InitPerSecondIntervalMetric(currentTimeSnapshot, currentTimeSnapshot - _lastReportTime, recentLatencyTimes, recentTotalRequestCharge);
                    PopulateExtendedMetrics?.Invoke(m);
                    PerSecondMetricsAvailable?.Invoke(m);

                    // Update one minute interval metrics & post if needed
                    UpdatePerMinuteIntervalMetrics(m);

                    // Remember this interval
                    _lastReportTime = currentTimeSnapshot;
                    await Task.Delay(10);  // Don't run too fast & report more than once per interval
                }
                catch (Exception x)
                {
                    Tracer.TraceWarning($"Ignoring exception on background metrics reporting thread.  Exception = {x.ToString()}");
                }
            }
        }

        private void UpdatePerMinuteIntervalMetrics(IntervalMetrics currentSecondMetrics)
        {
            // Create per minute metric at program start
            if (null == _perMinuteMetrics)
            {
                _perMinuteMetrics = InitPerMinuteIntervalMetric(currentSecondMetrics);
            }
            // If transitioned to a new minute, notify & start new minute 
            else if (currentSecondMetrics.EndTime.Minute != _perMinuteMetrics.EndTime.Minute)
            {
                PerMinuteMetricsAvailable?.Invoke(_perMinuteMetrics);
                _perMinuteMetrics = InitPerMinuteIntervalMetric(currentSecondMetrics);
            }
            // If in same minute as last time aggregate this second with current minute
            else
            {
                _perMinuteMetrics.Aggregate(currentSecondMetrics);
            }
        }

        private IntervalMetrics InitPerMinuteIntervalMetric(IntervalMetrics currentSecondMetrics)
        {
            return new IntervalMetrics
            (
                currentSecondMetrics,
                currentSecondMetrics.EndTime.Truncate(TimeSpan.FromMinutes(1)),
                TimeSpan.FromMinutes(1)
            );
        }

        private IntervalMetrics InitPerSecondIntervalMetric(DateTime currentTimeSnapshot, TimeSpan duration, ConcurrentDictionary<int, int> recentLatencyTimes, double recentTotalRequestCharge)
        {
            return new IntervalMetrics
            (
                currentTimeSnapshot.Truncate(TimeSpan.FromSeconds(1)),    // Truncate to current second
                duration,
                _testDescription,
                Environment.MachineName,
                Process.GetCurrentProcess().Id,
                _resourceDescription,
                recentLatencyTimes
            );
        }

        private async Task InternalFor(Func<bool> shouldContinue, Func<Task<double>> asyncWorkerFunction)
        {
            for (long curCount = Interlocked.Increment(ref _currentCount);
                shouldContinue() && _enabled;
                curCount = Interlocked.Increment(ref _currentCount))
            {
                // *******************************
                // Throttling data bookkeeping
                // *******************************
                if (_throttlingTimer.Elapsed > THROTTLING_INTERVAL)
                {
                    lock (_throttlingTimer)
                    {
                        if (_throttlingTimer.Elapsed > THROTTLING_INTERVAL)
                        {
                            _throttlingTimer.Restart();
                            _throttlingCurrentCount = 0;
                        }
                    }
                }
                Interlocked.Increment(ref _throttlingCurrentCount);

                // *******************************
                // Do work measuring latency time
                // *******************************
                DateTime start = DateTime.Now;
                double requestCharge = await asyncWorkerFunction();
                int latencyTime = (int)(DateTime.Now - start).TotalMilliseconds;

                // *******************************
                // Record request charge
                // *******************************
                AddToDoubleThreadSafe(requestCharge, ref _totalRequestCharge);

                // *******************************
                // Record latency time
                // *******************************
                _intervalLatencyTimes.AddOrUpdate(latencyTime, 1, (key, oldvalue) => oldvalue + 1);

                // *******************************
                // Slow down if needed
                // *******************************
                if (shouldContinue() && _enabled)
                {
                    if ((_getTps() > 0) && (_throttlingTimer.Elapsed.Milliseconds > 0))
                    {
                        // Calculate if we need to delay
                        double minMilliseconds = ((double)_throttlingCurrentCount / (double)_getTps()) * 1000.0;
                        long delayMilliseconds = (long)(minMilliseconds - (double)_throttlingTimer.Elapsed.TotalMilliseconds);

                        // Delay if running too fast
                        while (delayMilliseconds > 0 && _enabled && shouldContinue())
                        {
                            // Delay with a bit of jitter and no longer than 100ms (so that we exit gracefully at the stop time if set)
                            int currentDelay = (int)(delayMilliseconds) + _rnd.Next(0, 25);
                            await Task.Delay(Math.Min(currentDelay, 100));

                            // Determine if we still need to delay
                            minMilliseconds = ((double)_throttlingCurrentCount / (double)_getTps()) * 1000.0;
                            delayMilliseconds = (long)(minMilliseconds - (double)_throttlingTimer.Elapsed.TotalMilliseconds);
                        }
                    }
                }
            }
        }

        // AddToTotal safely adds a value to the running total.
        public double AddToDoubleThreadSafe(double addend, ref double totalValue)
        {
            double initialValue, computedValue;
            do
            {
                // Save the current running total in a local variable.
                initialValue = totalValue;

                // Add the new value to the running total.
                computedValue = initialValue + addend;

                // CompareExchange compares totalValue to initialValue. If
                // they are not equal, then another thread has updated the
                // running total since this loop started. CompareExchange
                // does not update totalValue. CompareExchange returns the
                // contents of totalValue, which do not equal initialValue,
                // so the loop executes again.
            }
            while (initialValue != Interlocked.CompareExchange(ref totalValue,
                computedValue, initialValue));
            // If no other thread updated the running total, then 
            // totalValue and initialValue are equal when CompareExchange
            // compares them, and computedValue is stored in totalValue.
            // CompareExchange returns the value that was in totalValue
            // before the update, which is equal to initialValue, so the 
            // loop ends.

            // The function returns computedValue, not totalValue, because
            // totalValue could be changed by another thread between
            // the time the loop ends and the function returns.
            return computedValue;
        }

        // Desired RPS
        private Func<double> _getTps;

        // Global static state
        private bool _enabled = false;
        private long _currentCount;
        private string _testDescription;
        private string _resourceDescription;

        // Throttling state - periodically resets to avoid long running "catch up" work
        private long _throttlingCurrentCount;
        private Stopwatch _throttlingTimer;
        readonly TimeSpan THROTTLING_INTERVAL = TimeSpan.FromSeconds(15);

        // Helper objects
        private Stopwatch _timer;
        private Random _rnd = new Random();

        // Interval state
        private DateTime _lastReportTime = DateTime.Now;
        private ConcurrentDictionary<int, int> _intervalLatencyTimes = new ConcurrentDictionary<int, int>();
        private double _totalRequestCharge = 0.0;
        private IntervalMetrics _perMinuteMetrics = null;

        #endregion
    }

}
