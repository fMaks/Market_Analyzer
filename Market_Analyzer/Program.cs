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

            Huobi Birga1 = new Huobi("btcusdt,xrpusdt,htusdt");
            object value = await Huobi.GetSymbolAsync();

            while (!(Console.KeyAvailable && (kkey = Console.ReadKey(true)).Key == ConsoleKey.Escape)) {
                try
                {
                    if (Birga1.ClientWS != null && Birga1.ClientWS.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result = await Birga1.ClientWS.ReceiveAsync(msg1, CancellationToken.None);
                        var response = Encoding.UTF8.GetString(msg1.Array, 0, result.Count);
                        Stream st = new MemoryStream(msg1.Array);
                        var decompressor = new GZipStream(st, CompressionMode.Decompress);
                        var resultStream = new MemoryStream();
                        decompressor.CopyTo(resultStream);
                        String StrIn = new string(Encoding.UTF8.GetString(resultStream.ToArray()));
                        if (StrIn.Contains("ping"))
                        {
                            String StrOut = StrIn.Replace("ping", "pong");
                            ArraySegment<byte> arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(StrOut));
                            await Birga1.ClientWS.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        if (!StrIn.Contains("ch") && !StrIn.Contains("ping"))
                        {
                            Console.WriteLine(StrIn);
                        }
                        else
                        {
                            if (StrIn.Contains("ch"))
                            {
                                HuobiRootTickCandle HRTick = JsonConvert.DeserializeObject<HuobiRootTickCandle>(StrIn);
                                Int32 tempFindIndex = Birga1.DataSymbol.FindIndex(item => item.SymbolName == HRTick.ch.Split('.')[1]);
                                if (HRTick.tick.id == Birga1.DataSymbol[tempFindIndex].m1[0].UnixTimeGMT)
                                {
                                    Birga1.DataSymbol[tempFindIndex].m1[0].Lo = HRTick.tick.low;
                                    Birga1.DataSymbol[tempFindIndex].m1[0].Hi = HRTick.tick.high;
                                    Birga1.DataSymbol[tempFindIndex].m1[0].Close = HRTick.tick.close;
                                    Birga1.DataSymbol[tempFindIndex].m1[0].Amount = HRTick.tick.amount;
                                    Birga1.DataSymbol[tempFindIndex].m1[0].Vol = HRTick.tick.vol;
                                    Birga1.DataSymbol[tempFindIndex].m1[0].Count = HRTick.tick.count;
                                }
                                else
                                {
                                    // новая свеча
                                    Birga1.DataSymbol[tempFindIndex].m1.Insert(0, new Candle(HRTick.ch, HRTick.tick.id, HRTick.tick.open, HRTick.tick.high, HRTick.tick.low,
                                        HRTick.tick.close, HRTick.tick.amount, HRTick.tick.vol, HRTick.tick.count));

                                    DateTime tDate = DateTime.Now;
                                    DateTime FileDate = new DateTime(tDate.Year, tDate.Month, tDate.Day, tDate.Hour, 0, 0);
                                    StreamWriter swLogS1 = new StreamWriter($"Data\\Huobi_{FileDate.Year:d4}_{FileDate.Month:d2}_{FileDate.Day:d2}_m1.txt", true, System.Text.Encoding.UTF8);
                                    swLogS1.WriteLine($"{Birga1.DataSymbol[tempFindIndex].m1[1].UnixTimeGMT} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].SymbolName} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Open:F10} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Hi:F10} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Lo:F10} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Close:F10} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Amount:F10} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Vol:F10} " +
                                        $"{Birga1.DataSymbol[tempFindIndex].m1[1].Count} "
                                        );
                                    swLogS1.Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        //Console.WriteLine("Websocket.Status = {0}", Birga1.ClientWS.State);
                        //Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected exception : {0}", ex.ToString());
                    //Console.ReadKey();
                }
            }
            await Birga1.ClientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "Exit", CancellationToken.None);
            Console.ReadKey();
        }
    }
}