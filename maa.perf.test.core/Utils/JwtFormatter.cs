using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace maa.perf.test.core.Utils
{
    class JwtFormatter
    {
        public static string FormatJwt(string raw)
        {
            StringBuilder sb = new StringBuilder();
            string[] stringTokens = raw.Split(new char[] { '.', '\t', ' ', '\n', '\r' });

            foreach (var tf in stringTokens)
            {
                try
                {
                    // We'll assume a base 64 URL encoded string that's a JSON structure.  If not, it's
                    // OK to thrown an exception and move along.
                    byte[] theBytes = Convert.FromBase64String(Pad(tf.Replace('-', '+').Replace('_', '/')));
                    var obj = JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(theBytes));
                    sb.AppendFormat("{0}\n", obj.ToString());
                }
                catch (Exception)
                {
                    //sb.AppendFormat("Non JSON Field ignored\n");
                }
            }

            return sb.ToString();
        }

        private static string Pad(string input)
        {
            var count = 3 - ((input.Length + 3) % 4);

            if (count == 0)
            {
                return input;
            }

            return input + new string('=', count);
        }
    }
}
