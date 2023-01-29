using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Test_SaveData
{
    internal class ListSymbol : List<Symbol>
    {
        public async Task<int> loadFromFile(string maskFile)
        {
            Console.WriteLine("Загрузка сохраненных данных...");
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
                Console.WriteLine(file);
            }
            stopwatch.Stop();
            for (int I =0; I < this.Count; I++)
            {
                this[I].m1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
            }
            Console.WriteLine("Загрузка завершена за {0:f3} с", stopwatch.ElapsedMilliseconds/1000.0);


            return 0;
        }

        public async Task<int> loadHistoryFromServer(string symbolStr)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine("Загрузка данных с сервера");
            string respBody = string.Empty;
            HuobiHistoryRoot HistoryKline;

            char[] delimiter = { ',', ' ' };
            string[] SubStr = symbolStr.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string subStr in SubStr)
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage respInfo = await client.GetAsync("https://api.huobi.pro/market/history/kline?period=1min&size=2000&symbol=" + subStr);
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
            Console.WriteLine("Загрузка завершена за {0:F3} с", stopwatch.ElapsedMilliseconds / 1000.0);

            return 0;
        }
    }
}
