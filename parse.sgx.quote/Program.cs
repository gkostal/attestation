using System.Text.Json;

namespace ParseSgxQuote
{
    internal class Program
    {
        static void Main(string[] _)
        {
            new Program().Run();
        }

        public void Run()
        {
            var b = ReadAllBytes("./sample.quotes/sgx.successful.quote.bin");
            var q = new SgxQuote(b);
            Console.WriteLine($"{q}");

            b = ReadAllBytes("./sample.quotes/sgx.failed.quote.bin");
            q = new SgxQuote(b);
            Console.WriteLine($"{q}");

            b = ReadAllBytes("./sample.quotes/oe.maa.scus.test.quote.bin");
            q = new SgxQuote(b);
            Console.WriteLine($"{q}");
        }

        public byte[] ReadAllBytes(string path)
        {
            using (var f = File.OpenRead(path))
            {
                var bytes = new byte[f.Length];
                f.Read(bytes, 0, bytes.Length);
                return bytes;
            }
        }
    }
}