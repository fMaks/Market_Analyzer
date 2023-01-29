using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Test_SaveData
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
        public int tpp { get; set; }
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
        public float open { get; set; }
        public float close { get; set; }
        public float low { get; set; }
        public float high { get; set; }
        public float amount { get; set; }
        public float vol { get; set; }
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
        public float open { get; set; }
        public float close { get; set; }
        public float low { get; set; }
        public float high { get; set; }
        public float amount { get; set; }
        public float vol { get; set; }
        public int count { get; set; }
    }


    internal class Huobi
    {
        // public static async Task<int> GetSymbolAsync()
        // получить информацию о торговых символах

        public static HuobiRootSymbolInfo? SymbolInfo;
        public ClientWebSocket ClientWS = null;
        private string SubscribeStr;
        public ListSymbol DataSymbol = new ListSymbol();


        public Huobi (string Str)
        {
            this.SubscribeStr = Str;
            DataSymbol.loadFromFile("Huobi*");
            DataSymbol.loadHistoryFromServer(this.SubscribeStr);
            SubscribeCandle();

        }

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

            HttpClient client = new HttpClient();
            HttpResponseMessage respInfo = await client.GetAsync("https://api.huobi.pro/v2/settings/common/symbols");
            respInfo.EnsureSuccessStatusCode();
            respBody = await respInfo.Content.ReadAsStringAsync();
            SymbolInfo = JsonConvert.DeserializeObject<HuobiRootSymbolInfo>(respBody);
            client.Dispose();
            return SymbolInfo.data.Count;
        }
        public async Task<int> SubscribeCandle()
        {
            // подписывается через вебсокет на получение сигналов
            // по указанным парам (через запятую
            // ALLONLINE - все торгуемые пары (SymbolInfo[].state = "online"
            // Out:
            // 0 - если запрос в Str выполнен удачно
            // 1 - если ошибка
            ClientWS = new ClientWebSocket();
            await ClientWS.ConnectAsync(new Uri("wss://api.huobi.pro/ws"), CancellationToken.None);
            //ClientWS.Options.KeepAliveInterval = TimeSpan.Zero;

            if (SubscribeStr.Contains("ALLONLINE"))
            {
                Console.WriteLine("11111111111111");
                return 1;
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
                }
            }
            return 0;

        }

    }
}
