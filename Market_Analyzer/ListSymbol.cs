using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Market_Analizer
{
    internal class ListSymbol : List<Symbol>
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public async Task<int> loadFromFile(string maskFile)
        {
            Console.Write("Загрузка сохраненных данных");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<string> files = new List<string>(Directory.GetFiles("Data\\", maskFile));
            files.Sort();
            foreach (string file in files)
            {
                StreamReader sr = new StreamReader(file);
                string[] ReadStr = File.ReadAllLines(file);
                foreach (string Str in ReadStr)
                {
                    string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    Int32 tempFindIndex = this.FindIndex(item => item.SymbolName == SubStr[1]);
                    if (tempFindIndex == -1)
                    {
                        this.Add(new Symbol(Str));
                    }
                    else
                    {
                        this[tempFindIndex].AddTick(Str);
                    }
                }
                sr.Close();
                logger.Debug($"Загрузка из файла {file}");
            }
            stopwatch.Stop();
            for (int I =0; I < this.Count; I++)
            {
                this[I].m1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
            }
            Console.WriteLine(" завершена за {0:f3} с", stopwatch.ElapsedMilliseconds/1000.0);
            logger.Debug("Загрузка завершена за {0:f3} с", stopwatch.ElapsedMilliseconds / 1000.0);

            return 0;
        }

        public async Task<int> loadHistoryFromServer(string symbolStr)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            logger.Debug("Загрузка данных с сервера");
            Console.Write("Загрузка данных с сервера");
            string respBody = string.Empty;
            HuobiHistoryRoot HistoryKline;

            char[] delimiter = { ',', ' ' };
            string[] SubStr = symbolStr.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string subStr in SubStr)
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage respInfo;

                // определяем сколько нужно подгрузить свечей из истории
                // в m1[0] - последняя сохраненная свеча в файле
                Int32 FindIndex = this.FindIndex(item => item.SymbolName == subStr);
                if (FindIndex != -1)
                {
                    long lastTime = this[FindIndex].m1[0].UnixTimeGMT;
                    DateTime date = DateTime.Now;
                    long currentUnixTime = (uint)(date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    int countLoad = Math.Min(2000, (int)Math.Ceiling(((currentUnixTime - lastTime)/60.0)));
                    respInfo = await client.GetAsync("https://api.huobi.pro/market/history/kline?period=1min&size=" + countLoad.ToString() + "&symbol=" + subStr);
                }
                else
                {
                    respInfo = await client.GetAsync("https://api.huobi.pro/market/history/kline?period=1min&size=2000&symbol=" + subStr);
                }
                respInfo.EnsureSuccessStatusCode();
                respBody = await respInfo.Content.ReadAsStringAsync();
                HistoryKline = JsonConvert.DeserializeObject<HuobiHistoryRoot>(respBody);
                
                foreach (HuobiHistoryDatum datum in HistoryKline.data)
                {
                    Int32 tempFindIndex = this.FindIndex(item => item.SymbolName == subStr);
                    if (tempFindIndex == -1)
                    {
                        this.Add(new Symbol(subStr, HistoryKline.data[0]));
                    }
                    else
                    {
                        Int32 tempFindDateIndex = this[tempFindIndex].m1.FindIndex(item => item.UnixTimeGMT == datum.id);
                        if (tempFindDateIndex == -1)
                        {
                            this[tempFindIndex].AddTick(datum);
                            this[tempFindIndex].SaveCandleToFile(datum);
                        }
                    }
                }
                Int32 tempFindSymbIndex = this.FindIndex(item => item.SymbolName == subStr);
                this[tempFindSymbIndex].m1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
            }
            
            stopwatch.Stop();
            //(int consoleLeft, int consoleTop) = Console.GetCursorPosition();
            //Console.SetCursorPosition(0, consoleTop);
            Console.WriteLine(" завершена за {0:F3} с", stopwatch.ElapsedMilliseconds / 1000.0);
            logger.Debug("загрузка завершена за {0:F3} с", stopwatch.ElapsedMilliseconds / 1000.0);
            return 1;
        }
    }
}
