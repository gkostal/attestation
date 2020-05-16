using System;

namespace validatequotes
{
    public class Logger
    {
        public static void WriteLine(string message)
        {
            Console.WriteLine($"{DateTime.Now} : {message}");
        }
    }
}
