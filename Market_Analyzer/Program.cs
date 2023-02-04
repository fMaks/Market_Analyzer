using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;

namespace Test_SaveData
{
    internal class Program
    {
        static async Task Main(string[] args) {
            Console.CursorVisible = false;
            ConsoleKeyInfo kkey = new ConsoleKeyInfo();
            ArraySegment<byte> msg1 = new ArraySegment<byte>(new byte[1024]);

            Huobi Birga1 = new Huobi("btcusdt,xrpusdt,htusdt,ethusdt,trxusdt");
            //object value = await Huobi.GetSymbolAsync();

            
            while (!(Console.KeyAvailable && (kkey = Console.ReadKey(true)).Key == ConsoleKey.Escape))
            {
            
            }

            Birga1.Dispose();
            Console.ReadKey();
        }
    }
}