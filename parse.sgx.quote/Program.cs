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
            DumpSgxQuote("./sample.quotes/sgx.successful.quote.bin");
            DumpSgxQuote("./sample.quotes/sgx.failed.quote.bin");
            DumpSgxQuote("./sample.quotes/oe.maa.scus.test.quote.bin");
            DumpSgxQuote("./sample.quotes/sgx.quote.2022.07.12.11.46.35.bin");
        }

        public void Run2()
        {
            Console.WriteLine(GenerateCString("./sample.quotes/sgx.successful.quote.bin", "goodQuote"));
            Console.WriteLine(GenerateCString("./sample.quotes/sgx.failed.quote.bin", "badQuote"));
        }

        private string GenerateCString (string filePath, string variableName)
        {
            var q = ReadAllBytes(filePath);
            var qq = string.Join(",", q);

            return $"static unsigned char {variableName}[] = {{{qq}}};";
        }

        private void DumpSgxQuote(string filePath)
        {
            var b = ReadAllBytes(filePath);
            var q = new SgxQuote(b);

            Console.WriteLine();
            Console.WriteLine("**************************************************************************************************");
            Console.WriteLine();
            Console.WriteLine($"FileName: {filePath}");
            Console.WriteLine($"{q}");
            Console.WriteLine("**************************************************************************************************");
            Console.WriteLine();
        }

        private byte[] ReadAllBytes(string path)
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