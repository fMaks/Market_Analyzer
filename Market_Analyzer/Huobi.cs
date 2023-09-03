using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Reflection.Metadata;
using NLog;
using NLog.Fluent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using IniFiles;

namespace Market_Analizer
{
    public class HuobiRootSymbolInfo
    {
        public string status { get; set; }
        public List<HuobySymbolDataInfo> data { get; set; }
        public string ts { get; set; }
        public int full { get; set; }
    }
    public class HuobySymbolDataInfo
    {
        public string tags { get; set; }
        public string state { get; set; }
        public string wr { get; set; }
        public List<P1> p1 { get; set; }
        public int sm { get; set; }
        public string sc { get; set; }
        public object lr { get; set; }
        public int tpp { get; set; }        // точность торговой цены
        public bool te { get; set; }
        public string bcdn { get; set; }
        public string qcdn { get; set; }
        public string elr { get; set; }
        public int fp { get; set; }
        public string bc { get; set; }
        public string qc { get; set; }
        public int tap { get; set; }
        public int ttp { get; set; }
        public object smlr { get; set; }
        public object flr { get; set; }
        public string sp { get; set; }
        public object d { get; set; }
        public bool whe { get; set; }
        public bool cd { get; set; }
        public object toa { get; set; }
        public int w { get; set; }
        public List<P> p { get; set; }
        public string dn { get; set; }
        public string suspend_desc { get; set; }
    }
    public class P1
    {
        public int id { get; set; }
        public string name { get; set; }
        public int weight { get; set; }
    }
    public class P
    {
        public int id { get; set; }
        public string name { get; set; }
        public int weight { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    // https://api.huobi.pro/market/history/kline?period=1min&size=2000&symbol=btcusdt
    public class HuobiHistoryDatum
    {
        public int id { get; set; }
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal low { get; set; }
        public decimal high { get; set; }
        public decimal amount { get; set; }
        public decimal vol { get; set; }
        public int count { get; set; }
    }

    public class HuobiHistoryRoot
    {
        public string ch { get; set; }
        public string status { get; set; }
        public long ts { get; set; }
        public List<HuobiHistoryDatum> data { get; set; }
    }

    public class HuobiRootTickCandle
    {
        public string ch { get; set; }
        public long ts { get; set; }
        public HuobiTickCandle tick { get; set; }
    }

    public class HuobiTickCandle
    {
        public long id { get; set; }
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal low { get; set; }
        public decimal high { get; set; }
        public decimal amount { get; set; }
        public decimal vol { get; set; }
        public int count { get; set; }
    }


    internal class Huobi : IDisposable
    {
        private Int64 PingPongCount = 0;
        private string wsUrl = "wss://api.huobi.pro/ws";
        public static HuobiRootSymbolInfo? SymbolInfo;
        public ClientWebSocket? ClientWS = null;
        private CancellationTokenSource? CTS;
        private string SubscribeStr;
        public ListSymbol DataSymbol = new ListSymbol();
        static int flagEndLoadServer = 0;
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public Huobi(string Str)
        {
            this.SubscribeStr = Str;
            Task<int> statusLoad = Task.FromResult(0);
            statusLoad = GetSymbolAsync();
            while (statusLoad.Result == 0)
            {
                Thread.Sleep(1000);
            }
            statusLoad = Task.FromResult(1);
            statusLoad = DataSymbol.loadFromFile("Huobi*");
            while (statusLoad.Result != 0)
            {
                Thread.Sleep(1000);
            }
            statusLoad = Task.FromResult(1);
            statusLoad = DataSymbol.loadHistoryFromServer(this.SubscribeStr);
            while (statusLoad.Result != 0)
            {
                Thread.Sleep(1000);
            }

            flagEndLoadServer = 1;
            //добавить в массив значение tpp
            foreach (Symbol Symb in DataSymbol)
            {
                Int32 tempFindIndex = SymbolInfo.data.FindIndex(item => item.sc == Symb.SymbolName);
                if (tempFindIndex != -1)
                {
                    Symb.tpp = SymbolInfo.data[tempFindIndex].tpp;
                }
            }
            DataSymbol.resizeTF();
            ConnectAsync();
        }

        public static async Task<string> GetSymbolHistoryAsync(string symbol, string tf, int count)
        {
            // получает историю свечей
            // symbol - имя символа
            // count - количество свечей
            // tf - таймфрейм
            HttpClient client = new HttpClient();
            HttpResponseMessage respInfo;
            respInfo = await client.GetAsync("https://api.huobi.pro/market/history/kline?period=" + tf + "&size=" + count.ToString() + "&symbol=" + symbol);
            respInfo.EnsureSuccessStatusCode();
            return await respInfo.Content.ReadAsStringAsync();
        }

        public async Task ConnectAsync()
        {
            logger.Debug("Start connect");
            if (ClientWS != null)
            {
                logger.Debug("Connect - StartWS != null");
                if (ClientWS.State == WebSocketState.Open)
                {
                    logger.Debug("StartWS.State = Open");
                    return;
                }
                ClientWS.Dispose();
                //ClientWS = new ClientWebSocket();
            }
            ClientWS = new ClientWebSocket();
            if (CTS != null)
            {
                logger.Debug("Connect - CTS != null");
                CTS.Dispose();
            }
            CTS = new CancellationTokenSource();
            await ClientWS.ConnectAsync(new Uri(wsUrl), CTS.Token);
            SubscribeCandle();

            await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task ReceiveLoop()
        {
            logger.Debug($"{nameof(ReceiveLoop)}");
            int BUFSIZE = 16384;
            var loopToken = CTS.Token;
            logger.Debug(loopToken);
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[BUFSIZE];
            // === переделать try-except
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(BUFSIZE);
                    do
                    {
                        receiveResult = await ClientWS.ReceiveAsync(buffer, CTS.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    ResponseReceived(outputStream);
                }
            }
            catch (TaskCanceledException e)
            {
                logger.Info("TaskCancelException");
                logger.Info(e);
                Console.WriteLine("Exit");
                return;
            }
            catch (WebSocketException e)
            {
                //Console.WriteLine($"Websocket exception : {e.Message}");
                logger.Info(e);
            }
            catch (Exception e)
            {
                logger.Warn(e);
            }
            finally
            {
                logger.Debug("Try-catch-Finally");
                //outputStream?.Dispose();
                logger.Info("Reconnect");
                Console.WriteLine("\nReconnect");
                ConnectAsync();
            }
        }

        private async Task ResponseReceived(Stream inputStream)
        {
            var buffer = new byte[8192];
            try
            {
                int countByte = inputStream.Read(buffer);
                Stream st = new MemoryStream(buffer);
                var decompressor = new GZipStream(st, CompressionMode.Decompress);
                var resultStream = new MemoryStream();
                decompressor.CopyTo(resultStream);
                String StrIn = new string(Encoding.UTF8.GetString(resultStream.ToArray()));
                if (StrIn.Contains("ping"))
                {
                    String StrOut = StrIn.Replace("ping", "pong");
                    ArraySegment<byte> arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(StrOut));
                    await ClientWS.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                    (int consoleLeft, int consoleTop) = Console.GetCursorPosition();
                    Console.SetCursorPosition(0, consoleTop);
                    PingPongCount++;
                    Console.Write($"Ping-Pong count: {PingPongCount:D6}");
                }
                else
                {
                    if (StrIn.Contains("subbed"))
                    {
                        logger.Info(StrIn);
                    }
                    else
                    {
                        if (StrIn.Contains("\"ch\""))
                        {
                            HuobiRootTickCandle HRTick = JsonConvert.DeserializeObject<HuobiRootTickCandle>(StrIn);
                            Int32 tempFindIndex = DataSymbol.FindIndex(item => item.SymbolName == HRTick.ch.Split('.')[1]);
                            if (tempFindIndex == -1)
                            {
                                Console.WriteLine("BBBBBB");
                                DataSymbol.Add(new Symbol(HRTick));
                            }
                            else
                            {
                                if (HRTick.tick.id == DataSymbol[tempFindIndex].m1[0].UnixTimeGMT)
                                {
                                    DataSymbol[tempFindIndex].m1[0].Lo = HRTick.tick.low;
                                    DataSymbol[tempFindIndex].m1[0].Hi = HRTick.tick.high;
                                    DataSymbol[tempFindIndex].m1[0].Close = HRTick.tick.close;
                                    DataSymbol[tempFindIndex].m1[0].Amount = HRTick.tick.amount;
                                    DataSymbol[tempFindIndex].m1[0].Vol = HRTick.tick.vol;
                                    DataSymbol[tempFindIndex].m1[0].Count = HRTick.tick.count;
                                }
                                else
                                {
                                    // новая свеча
                                    DataSymbol[tempFindIndex].AddTick(HRTick.tick.id, HRTick.tick.open, HRTick.tick.high, HRTick.tick.low,
                                        HRTick.tick.close, HRTick.tick.amount, HRTick.tick.vol, HRTick.tick.count);

                                    DateTime tDate = DateTime.Now;
                                    DateTime FileDate = new DateTime(tDate.Year, tDate.Month, tDate.Day, tDate.Hour, 0, 0);
                                    StreamWriter swLogS1 = new StreamWriter($"Data\\Huobi_{FileDate.Year:d4}_{FileDate.Month:d2}_{FileDate.Day:d2}_m1.txt", true, System.Text.Encoding.UTF8);
                                    swLogS1.WriteLine($"{DataSymbol[tempFindIndex].m1[1].UnixTimeGMT} " +
                                        $"{DataSymbol[tempFindIndex].SymbolName} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Open} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Hi} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Lo} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Close} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Amount} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Vol} " +
                                        $"{DataSymbol[tempFindIndex].m1[1].Count} "
                                        );
                                    swLogS1.Close();
                                }
                            }
                        }
                        else
                        {
                            logger.Debug("Не обработанный ответ от сервера");
                            logger.Debug(StrIn);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
                Console.WriteLine("AAAAA");
                Console.WriteLine(e.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("\nDisconnectAsync()");
            if (ClientWS is null) return;
            if (ClientWS.State == WebSocketState.Open)
            {
                CTS.CancelAfter(TimeSpan.FromSeconds(3));
                await ClientWS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await ClientWS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            ClientWS.Dispose();
            ClientWS = null;
            CTS.Dispose();
            CTS = null;
            NLog.LogManager.Shutdown();
        }
        //public void Dispose() => DisconnectAsync();.Wait()
        public void Dispose() => DisconnectAsync();

        public int SaveData()
        {
            return 0;
        }

        public static async Task<int> GetSymbolAsync()
        {
            // получить все торговые пары
            // data[].sate = "online" - пара торгуется
            // tpp - точность торговой цены (?)
            string respBody = string.Empty;
            bool statusReqest = false;
            Console.WriteLine("Получение информации о торгуемых парах");

            HttpClient client = new HttpClient();
            while (!statusReqest)
            {
                try
                {
                    HttpResponseMessage respInfo = await client.GetAsync("https://api.huobi.pro/v2/settings/common/symbols");
                    respInfo.EnsureSuccessStatusCode();
                    respBody = await respInfo.Content.ReadAsStringAsync();
                    SymbolInfo = JsonConvert.DeserializeObject<HuobiRootSymbolInfo>(respBody);
                    client.Dispose();
                    statusReqest = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    logger.Debug($"Загрузка информации не удалась: {ex.Message}");
                }
            }
            Console.WriteLine("Загрузка информации завершена");
            logger.Info("Загрузка информации завершена");
            return SymbolInfo.data.Count;
        }

        public async Task SubscribeCandle()
        {
            // подписывается через вебсокет на получение сигналов
            // по указанным парам (через запятую
            // ALLONLINE - все торгуемые пары (SymbolInfo[].state = "online"

            //ClientWS = new ClientWebSocket();
            //await ClientWS.ConnectAsync(new Uri("wss://api.huobi.pro/ws"), CancellationToken.None);
            //ClientWS.Options.KeepAliveInterval = TimeSpan.Zero;

            logger.Debug("Subscribe");
            if (SubscribeStr.Contains("ALLONLINE"))
            {
                Console.WriteLine("11111111111111");
                return;
            }
            else
            {
                char[] delimiter = { ',', ' ' };
                string[] SubStr = SubscribeStr.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                foreach (var Symbol in SubStr)
                {
                    //string Subscribe = "{\"sub\":\"market." + Symbol + ".ticker\"}";
                    string Subscribe = "{\"sub\":\"market." + Symbol + ".kline.1min\",\"id\":\"55555\"}";
                    byte[] SubAsBytes = Encoding.UTF8.GetBytes(Subscribe);
                    await ClientWS.SendAsync(SubAsBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                    logger.Debug($"Subscribe: {Subscribe}");
                }
            }
            return;
        }
    }
}
