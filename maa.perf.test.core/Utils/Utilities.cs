using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace maa.perf.test.core.Utils
{
    public class Utilities
    {
        public static void RunAtFrequency(TimeSpan frequency, Action method, bool ignoreExceptions = false)
        {
            Task.Run(() =>
            {
                var intervalEnd = DateTime.Now + frequency;
                while (true)
                {
                    // Wait for scheduled time and set next scheduled time
                    // If current time is after scheduled time, continue immediately noting the new interval end time
                    var waitTime = intervalEnd - DateTime.Now;
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        Thread.Sleep(waitTime);
                    }
                    else
                    {
                        intervalEnd = DateTime.Now;
                    }
                    intervalEnd += frequency;

                    // Perform operation
                    try
                    {
                        method();
                    }
                    catch (Exception)
                    {
                        if (!ignoreExceptions) throw;
                    }
                }
            });
        }

        public static void MeasureFunctionRps(Action testMethod)
        {
            long count = 0;
            var intervalStart = DateTime.Now;
            var intervalLength = TimeSpan.FromSeconds(1);
            while (true)
            {
                if (DateTime.Now > intervalStart + intervalLength)
                {
                    Console.WriteLine($"Method : {testMethod.Method.Name}");
                    Console.WriteLine($"Count  : {count}");
                    Console.WriteLine($"RPS    : {count / (DateTime.Now - intervalStart).TotalSeconds}");
                    Console.WriteLine();

                    count = 0;
                    intervalStart = DateTime.Now;
                }

                testMethod();
                count++;
            }
        }

        public static string GetLocalIpAddress()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("1.1.1.1", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }

            return localIP;
        }
    }
}