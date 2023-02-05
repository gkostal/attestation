using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using maa.perf.test.core.Model;

namespace maa.perf.test.core.Utils
{
    public class AsyncFor
    {
        #region Public

        /// <summary>
        /// Asynchronously call function 'asyncWorkerFunction' a total  of 'totalIterations' times
        /// running ',axSimultaneousAwaits' Tasks in parallel.
        /// </summary>
        /// <param name="totalIterations">Total number of times to execute asyncWorkerFunction</param>
        /// <param name="maxSimultanousAwaits">Maximum number of simultaneous awaits (i.e. number of simultaneous Tasks)</param>
        /// <param name="asyncWorkerFunction">Async worker function that performs desired action(s)</param>
        /// <param name="maxTPS">Maximum number of transactions per second, 0 means no limit</param>
        /// <returns></returns>

        public delegate void IntervalMetricsNotification(IntervalMetrics metrics);
        public event IntervalMetricsNotification PopulateExtendedMetrics;
        public event IntervalMetricsNotification PerMinuteMetricsAvailable;
        public event IntervalMetricsNotification PerSecondMetricsAvailable;

        public AsyncFor(Func<double> getTps, string resourceDescription, string testDescription, bool measureServerSideTime)
        {
            _getTps = getTps;
            _resourceDescription = resourceDescription;
            _testDescription = testDescription;
            _measureServerSideTime = measureServerSideTime;

            _runningTaskCount = 0;
            _currentCount = 0;
            _warnedOfMissingServerSideTime = false;
            _throttlingCurrentCount = 0;
            _throttlingTimer = new Stopwatch();
            _timer = new Stopwatch();
            _rnd = new Random();
            _lastReportTime = DateTime.Now;
            _intervalLatencyTimes = new ConcurrentDictionary<int, int>();
            _totalCpuPercentageReported = 0.0;
            _perMinuteMetrics = null;
        }

        public AsyncFor(double maxTPS, string resourceDescription, string testDescription, bool measureServerSideTime)
            : this(() => maxTPS, resourceDescription, testDescription, measureServerSideTime)
        {
        }

        public async Task ForAsync(TimeSpan totalDuration, long maxSimultaneousAwaits, Func<Task<PerformanceInformation>> asyncWorkerFunction, CancellationToken cancellationToken)
        {
            await ForAsync(() => _timer.Elapsed <= totalDuration, maxSimultaneousAwaits, asyncWorkerFunction, cancellationToken);
        }

        public async Task ForAsync(long totalIterations, long maxSimultaneousAwaits, Func<Task<PerformanceInformation>> asyncWorkerFunction, CancellationToken cancellationToken)
        {
            await ForAsync(() => _currentCount <= totalIterations, maxSimultaneousAwaits, asyncWorkerFunction, cancellationToken);
        }

        public async Task ForAsync(Func<bool> shouldContinue, long maxSimultaneousAwaits, Func<Task<PerformanceInformation>> asyncWorkerFunction, CancellationToken cancellationToken)
        {
            _timer.Start();
            _throttlingTimer.Start();
            _testDescription ??= asyncWorkerFunction.Method.Name;
            var reportCancellationTokenSource = new CancellationTokenSource();
            var reportLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(reportCancellationTokenSource.Token, cancellationToken);

            // Run maxSimultaneousAwaits tasks to perform the work
            List<Task> asyncForTasks = new List<Task>();
            for (long i = 0; i < maxSimultaneousAwaits; i++)
            {
                asyncForTasks.Add(Task.Run(() =>InternalForAsync(shouldContinue, asyncWorkerFunction, cancellationToken), cancellationToken));
            }

            // Run one task to report on status every second
            var reportBackgroundTask = Task.Run(() => ReportObservedTpsAsync(reportLinkedCancellationTokenSource.Token), reportLinkedCancellationTokenSource.Token);

            // Wait on all asyncfor tasks to complete
            await Task.WhenAll(asyncForTasks.ToArray());

            // Wait on report background task to complete, signaling it to cancel now
            reportCancellationTokenSource.Cancel();
            await Task.WhenAll(new Task[] { reportBackgroundTask });
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

        private async Task ReportObservedTpsAsync(CancellationToken cancellationToken)
        {
            _lastReportTime = DateTime.Now;

            // Write TPS periodically
            while (true)
            {
                // Bail if cancelled
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // Wait for start of next interval
                    await Task.Delay(GetMillisecondsToStartOfNextInterval(), cancellationToken);

                    // Bail if cancelled
                    cancellationToken.ThrowIfCancellationRequested();

                    // Note info for current interval, resetting interval aggretation data members
                    DateTime currentTimeSnapshot = DateTime.Now;
                    var recentLatencyTimes = Interlocked.Exchange(ref _intervalLatencyTimes,
                        new ConcurrentDictionary<int, int>());
                    var recentCpuPercentageReported = Interlocked.Exchange(ref _totalCpuPercentageReported, 0.0);

                    // Calculate one second interval metrics
                    IntervalMetrics m = InitPerSecondIntervalMetric(currentTimeSnapshot,
                        currentTimeSnapshot - _lastReportTime, recentLatencyTimes, recentCpuPercentageReported);
                    PopulateExtendedMetrics?.Invoke(m);
                    PerSecondMetricsAvailable?.Invoke(m);

                    // Update one minute interval metrics & post if needed
                    UpdatePerMinuteIntervalMetrics(m);

                    // Remember this interval
                    _lastReportTime = currentTimeSnapshot;
                    
                    // Don't run too fast & report more than once per interval
                    await Task.Delay(10, cancellationToken); 
                }
                catch (OperationCanceledException)
                {
                    // Don't throw exception when cancelled, just exit silently
                    break;
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

        private IntervalMetrics InitPerSecondIntervalMetric(DateTime currentTimeSnapshot, TimeSpan duration, ConcurrentDictionary<int, int> recentLatencyTimes, double recentCpuPercentageReported)
        {
            return new IntervalMetrics
            (
                currentTimeSnapshot.Truncate(TimeSpan.FromSeconds(1)),    // Truncate to current second
                duration,
                _testDescription,
                Environment.MachineName,
                Process.GetCurrentProcess().Id,
                _resourceDescription,
                recentLatencyTimes,
                recentCpuPercentageReported
            );
        }

        private async Task InternalForAsync(Func<bool> shouldContinue, Func<Task<PerformanceInformation>> asyncWorkerFunction, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _runningTaskCount);

            try
            {
                for (long curCount = Interlocked.Increment(ref _currentCount);
                    shouldContinue();
                    curCount = Interlocked.Increment(ref _currentCount))
                {
                    // *******************************
                    // Bail if cancellation requested
                    // *******************************
                    cancellationToken.ThrowIfCancellationRequested();

                    // *******************************
                    // Throttling data bookkeeping
                    // *******************************
                    if (_throttlingTimer.Elapsed > _throttlingInterval)
                    {
                        lock (_throttlingTimer)
                        {
                            if (_throttlingTimer.Elapsed > _throttlingInterval)
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
                    var perfInfo = await asyncWorkerFunction();
                    var clientMeasuredTime = (int) (DateTime.Now - start).TotalMilliseconds;
                    var serverMeasuredTime = (int) perfInfo.Request.DurationMs;
                    int latencyTime = (_measureServerSideTime && (serverMeasuredTime > 0))
                        ? serverMeasuredTime
                        : clientMeasuredTime;

                    // *******************************
                    // Warn one time if server side 
                    // time is missing
                    // *******************************
                    if (_measureServerSideTime && !_warnedOfMissingServerSideTime && (serverMeasuredTime <= 0))
                    {
                        Tracer.TraceWarning(
                            $"Server side time not available.  Measuring client side time instead of server side time.");
                        _warnedOfMissingServerSideTime = true;
                    }

                    // *******************************
                    // Record CPU percentage (if known)
                    // *******************************
                    AddToDoubleThreadSafe(perfInfo.Machine.Cpu.Total, ref _totalCpuPercentageReported);

                    // *******************************
                    // Record latency time
                    // *******************************
                    _intervalLatencyTimes.AddOrUpdate(latencyTime, 1, (key, oldvalue) => oldvalue + 1);

                    // *******************************
                    // Slow down if needed
                    // *******************************
                    if (shouldContinue())
                    {
                        if ((_getTps() > 0) && (_throttlingTimer.Elapsed.Milliseconds > 0))
                        {
                            // Calculate if we need to delay
                            double minMilliseconds = ((double) _throttlingCurrentCount / (double) _getTps()) * 1000.0;
                            long delayMilliseconds =
                                (long) (minMilliseconds - (double) _throttlingTimer.Elapsed.TotalMilliseconds);

                            // Delay if running too fast
                            while (delayMilliseconds > 0 && shouldContinue())
                            {
                                // Delay with a bit of jitter and no longer than 100ms (so that we exit gracefully at the stop time if set)
                                int currentDelay = (int) (delayMilliseconds) + _rnd.Next(0, 25);
                                await Task.Delay(Math.Min(currentDelay, 100), cancellationToken);

                                // Determine if we still need to delay
                                minMilliseconds = ((double) _throttlingCurrentCount / (double) _getTps()) * 1000.0;
                                delayMilliseconds =
                                    (long) (minMilliseconds - (double) _throttlingTimer.Elapsed.TotalMilliseconds);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Don't throw exception when cancelled, just exit silently
                Tracer.TraceInfo($"Organized shutdown in progress.  An async connection is exiting gracefully now.  {_runningTaskCount - 1} remaining.");
            }
            finally
            {
                Interlocked.Decrement(ref _runningTaskCount);
            }
        }

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
        private long _runningTaskCount;
        private long _currentCount;
        private string _testDescription;
        private readonly string _resourceDescription;
        private readonly bool _measureServerSideTime;
        private bool _warnedOfMissingServerSideTime;

        // Throttling state - periodically resets to avoid long running "catch up" work
        private long _throttlingCurrentCount;
        private readonly Stopwatch _throttlingTimer;
        private readonly TimeSpan _throttlingInterval = TimeSpan.FromSeconds(15);

        // Helper objects
        private readonly Stopwatch _timer;
        private readonly Random _rnd;

        // Interval state
        private DateTime _lastReportTime;
        private ConcurrentDictionary<int, int> _intervalLatencyTimes;
        private double _totalCpuPercentageReported;
        private IntervalMetrics _perMinuteMetrics;

        #endregion
    }

}
