using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Market_Analizer
{
    internal class Candle
    {
        public long UnixTimeGMT;    // время начала свечи (от 00:00:00 до 00:00:59 и т.д.)
        public float Open;         // цена открытия
        public float Lo;           // минимальная цена
        public float Hi;           // максимальная цена
        public float Close;        // цена закрытия
        public float Amount;        // объем сделок (xrpusdt в xrp)
        public float Vol;           // объем сделкк (xrpusdt в usdt)
        public int Count;           // количество сделок

        public Candle(string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.UnixTimeGMT = Convert.ToInt64(SubStr[0]);
            this.Open = float.Parse(SubStr[2]);
            this.Lo = float.Parse(SubStr[3]);
            this.Hi = float.Parse(SubStr[4]);
            this.Close = float.Parse(SubStr[5]);
            this.Amount = float.Parse(SubStr[6]);
            this.Vol = float.Parse(SubStr[7]);
            this.Count = int.Parse(SubStr[8]);
        }

        public Candle(string ch, long ts, float open, float hight, float low, float close, float amount, float vol, int count)
        {
            this.UnixTimeGMT = ts;
            this.Open = open;
            this.Lo = low;
            this.Hi = hight;
            this.Close = close;
            this.Count = count;
            this.Amount = amount;
            this.Vol = vol;
        }

        public Candle(HuobiRootTickCandle HRTick)
        {
            this.UnixTimeGMT = HRTick.ts;
            this.Open = HRTick.tick.open;
            this.Lo = HRTick.tick.low;
            this.Hi = HRTick.tick.high;
            this.Close = HRTick.tick.close;
            this.Count = HRTick.tick.count;
            this.Amount = HRTick.tick.amount;
            this.Vol = HRTick.tick.vol;
        }

        public void UpdateCandle (string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.Lo = Math.Min(this.Lo, float.Parse(SubStr[3]));
            this.Hi = Math.Max(this.Hi, float.Parse(SubStr[4]));
            this.Close = float.Parse(SubStr[5]);
            this.Amount = float.Parse(SubStr[6]);
            this.Vol = float.Parse(SubStr[7]);
            this.Count += int.Parse(SubStr[8]);
        }
    }
}
