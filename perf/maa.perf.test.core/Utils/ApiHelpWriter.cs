using maa.perf.test.core.Model;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Linq;

namespace maa.perf.test.core.Utils
{
    public class ApiHelpWriter : TextWriter
    {
        private static IEnumerable<Api> apiArray = Enum.GetValues<Api>().Where((a) => a != Api.None);
        private static string joinedApiArray = "    " + string.Join(",\n                                 ", apiArray);

        private string _currentLine = string.Empty;

        public override Encoding Encoding
        {
            get { return Console.Out.Encoding; }
        }

        public override void Write(char value)
        {
            _currentLine += value;
            if ((value == '\n') || (value == '\r'))
            {
                var filteredLine = _currentLine.Replace("%API%", joinedApiArray);
                foreach (var c in filteredLine)
                {
                    Console.Out.Write(c);
                }
                _currentLine = string.Empty;
            }
        }
    }
}
