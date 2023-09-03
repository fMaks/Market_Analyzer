using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                sr.Close();
                Array.Sort(ReadStr);
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
                logger.Debug($"Загрузка из файла {file}");
            }
            stopwatch.Stop();
            for (int I =0; I < this.Count; I++)
            {
                this[I].m1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
                this[I].m15.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
                this[I].h1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
                this[I].h4.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
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
            try
            {
                foreach (string subStr in SubStr)
                {
                    bool statusRequest = false;
                    logger.Debug($"загрузка с сервера {subStr}");

                    while (!statusRequest)
                    {
                        //Int32 FindIndex = this.FindIndex(item => item.SymbolName == subStr);
                        try
                        {
                            string response = await Huobi.GetSymbolHistoryAsync(subStr, "1min", 2000);
                            HistoryKline = JsonConvert.DeserializeObject<HuobiHistoryRoot>(response);
                            Console.WriteLine($"Получено {subStr} - {HistoryKline.data.Count}");
                            foreach (HuobiHistoryDatum datum in HistoryKline.data)
                            {
                                Int32 tempFindIndex = this.FindIndex(item => item.SymbolName == subStr);
                                if (tempFindIndex == -1)
                                {
                                    //this.Add(new Symbol(subStr, HistoryKline.data[0]));
                                    this.Add(new Symbol(subStr, datum));
                                }
                                else
                                {
                                    Int32 tempFindDateIndex = this[tempFindIndex].m1.FindIndex(item => item.UnixTimeGMT == datum.id);
                                    if (tempFindDateIndex == -1)
                                    {
                                        this[tempFindIndex].AddTick(datum);

                                        DateTime FileDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(datum.id);
                                        StreamWriter sw = new StreamWriter($"Data\\Huobi_{FileDate.Year:d4}_{FileDate.Month:d2}_{FileDate.Day:d2}_m1.txt", true, System.Text.Encoding.UTF8);
                                        sw.WriteLine($"{datum.id} " +
                                            $"{this[tempFindIndex].SymbolName} " +
                                            $"{datum.open} " +
                                            $"{datum.high} " +
                                            $"{datum.low} " +
                                            $"{datum.close} " +
                                            $"{datum.amount} " +
                                            $"{datum.vol} " +
                                            $"{datum.count} "
                                        );
                                        sw.Close();
                                    }
                                    else
                                    {
                                        //if (this[tempFindIndex].m1[tempFindDateIndex].Count == 0 && datum.count != 0)
                                        if (this[tempFindIndex].m1[tempFindDateIndex].Open == this[tempFindIndex].m1[tempFindDateIndex].Close &&
                                            this[tempFindIndex].m1[tempFindDateIndex].Open == this[tempFindIndex].m1[tempFindDateIndex].Hi &&
                                            this[tempFindIndex].m1[tempFindDateIndex].Open == this[tempFindIndex].m1[tempFindDateIndex].Lo)
                                        {
                                            DateTime FileDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(datum.id);
                                            StreamWriter sw = new StreamWriter($"Data\\Huobi_{FileDate.Year:d4}_{FileDate.Month:d2}_{FileDate.Day:d2}_m1.txt", true, System.Text.Encoding.UTF8);
                                            sw.WriteLine($"{datum.id} " +
                                                $"{this[tempFindIndex].SymbolName} " +
                                                $"{datum.open} " +
                                                $"{datum.high} " +
                                                $"{datum.low} " +
                                                $"{datum.close} " +
                                                $"{datum.amount} " +
                                                $"{datum.vol} " +
                                                $"{datum.count} "
                                            );
                                            sw.Close();

                                        }
                                        this[tempFindIndex].m1[tempFindDateIndex].Open = datum.open;
                                        this[tempFindIndex].m1[tempFindDateIndex].Lo = datum.low;
                                        this[tempFindIndex].m1[tempFindDateIndex].Hi = datum.high;
                                        this[tempFindIndex].m1[tempFindDateIndex].Close = datum.close;
                                        this[tempFindIndex].m1[tempFindDateIndex].Amount = datum.amount;
                                        this[tempFindIndex].m1[tempFindDateIndex].Vol = datum.vol;
                                        this[tempFindIndex].m1[tempFindDateIndex].Count = datum.count;
                                    }
                                }
                            }
                            Int32 tempFindSymbIndex = this.FindIndex(item => item.SymbolName == subStr);
                            this[tempFindSymbIndex].m1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
                            this[tempFindSymbIndex].m15.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
                            this[tempFindSymbIndex].h1.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));
                            this[tempFindSymbIndex].h4.Sort((a, b) => b.UnixTimeGMT.CompareTo(a.UnixTimeGMT));

                            logger.Debug($"загрузка с сервера {subStr} завершена");
                            statusRequest = true;
                        }
                        catch (TaskCanceledException ex)
                        {
                            logger.Debug(ex.Message);
                            logger.Debug("Превышен интервал ожидания");
                            logger.Debug("Повторный запрос");
                        }
                        catch (HttpRequestException ex)
                        {
                            logger.Debug(ex.Message);
                            logger.Debug($"Не удалось получить для {subStr}");
                            logger.Debug("Повторный запрос");
                        }
                        catch (Exception e)
                        {
                            logger.Error(e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e); 
            }

            stopwatch.Stop();
            //(int consoleLeft, int consoleTop) = Console.GetCursorPosition();
            //Console.SetCursorPosition(0, consoleTop);
            Console.WriteLine(" завершена за {0:F3} с", stopwatch.ElapsedMilliseconds / 1000.0);
            logger.Debug("загрузка завершена за {0:F3} с", stopwatch.ElapsedMilliseconds / 1000.0);

            return 0;
        }

        public async Task<int> resizeTF ()
        {
            foreach (var item in this)
            {

            }
            return 0;
        }
    }
}
