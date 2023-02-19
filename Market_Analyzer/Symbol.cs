using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Market_Analizer;

namespace Market_Analizer
{
    internal class Symbol
    {
        public string SymbolName { get; set; }
        public List<Candle> m1;
        public List<Candle> m5;
        public List<Candle> m15;

        public Symbol(string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.SymbolName = SubStr[1];
            m1 = new List<Candle>();
            m1.Insert(0, new Candle(Str));
            // тут проверить, возможно для свечей m5 и старше надо округлять время на начало свечи
            m5 = new List<Candle>();
            m5.Insert(0, new Candle(Str));
            m15 = new List<Candle>();
            m15.Insert(0, new Candle(Str));
        }

        public Symbol (string Name, HuobiHistoryDatum datum)
        {
            this.SymbolName = Name;
            m1 = new List<Candle>();
        }

        public Symbol (HuobiRootTickCandle HRTick)
        {
            this.SymbolName = HRTick.ch.Split('.')[1];
            m1 = new List<Candle>();
            m1.Insert(0, new Candle(HRTick));
        }

        public void AddTick(string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.m1.Insert(0, new Candle(Str));
            // m5
            if (Convert.ToInt64(SubStr[0]) - this.m5[0].UnixTimeGMT > 300)
            {
                this.m5.Insert(0, new Candle(Str));
            }
            else
            {
                this.m5[0].UpdateCandle(Str);
            }
            // m15
            if (Convert.ToInt64(SubStr[0]) - this.m15[0].UnixTimeGMT > 900)
            {
                this.m15.Insert(0, new Candle(Str));
            }
            else
            {
                this.m15[0].UpdateCandle(Str);
            }
        }

        public void AddTick(HuobiHistoryDatum datum)
        {
            this.m1.Add(new Candle("X", datum.id, datum.open, datum.low, datum.high, datum.close, datum.amount, datum.vol, datum.count));
        }

        public void SaveCandleToFile(HuobiHistoryDatum datum)
        {
            DateTime FileDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(datum.id);
            StreamWriter sw = new StreamWriter($"Data\\Huobi_{FileDate.Year:d4}_{FileDate.Month:d2}_{FileDate.Day:d2}_m1.txt", true, System.Text.Encoding.UTF8);
            sw.WriteLine($"{datum.id} " +
                $"{SymbolName} " +
                $"{datum.open:F10} " +
                $"{datum.high:F10} " +
                $"{datum.low:F10} " +
                $"{datum.close:F10} " +
                $"{datum.amount:F10} " +
                $"{datum.vol:F10} " +
                $"{datum.count} "
            );
            sw.Close();
        }

        public Symbol(string ch, long ts, float open, float hight, float low, float close, float amount, float vol, int count)
        {
            this.SymbolName = ch.Split('.')[1];
            m1 = new List<Candle>();
            m1.Add(new Candle(ch, ts, open, hight, low, close, amount, vol, count));

            //m5 = new List<Candle>();
            //m5.Add(new Candle(ts - ts % 300, close));
        }


        void Add(string ch, long ts, double close)
        {

        }
    }
}
