using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Market_Analizer
{
    public class Candle
    {
        public long UnixTimeGMT;    // время начала свечи (от 00:00:00 до 00:00:59 и т.д.)
        public decimal Open;         // цена открытия
        public decimal Lo;           // минимальная цена
        public decimal Hi;           // максимальная цена
        public decimal Close;        // цена закрытия
        public decimal Amount;        // объем сделок (xrpusdt в xrp)
        public decimal Vol;           // объем сделкк (xrpusdt в usdt)
        public int Count;           // количество сделок
        
        public Candle(string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.UnixTimeGMT = Convert.ToInt64(SubStr[0]);
            this.Open = decimal.Parse(SubStr[2]);
            this.Lo = decimal.Parse(SubStr[3]);
            this.Hi = decimal.Parse(SubStr[4]);
            this.Close = decimal.Parse(SubStr[5]);
            this.Amount = decimal.Parse(SubStr[6]);
            this.Vol = decimal.Parse(SubStr[7]);
            this.Count = int.Parse(SubStr[8]);
        }

        public Candle(long ts, decimal open, decimal hight, decimal low, decimal close, decimal amount, decimal vol, int count)
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

        public Candle(string ch, long ts, decimal open, decimal hight, decimal low, decimal close, decimal amount, decimal vol, int count)
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
            this.Lo = Math.Min(this.Lo, decimal.Parse(SubStr[3]));
            this.Hi = Math.Max(this.Hi, decimal.Parse(SubStr[4]));
            this.Close = decimal.Parse(SubStr[5]);
            this.Amount = decimal.Parse(SubStr[6]);
            this.Vol = decimal.Parse(SubStr[7]);
            this.Count += int.Parse(SubStr[8]);
        }
        public int Multiple()
        {
            // возвращает порядок, на который надо умножить цены, чтоб небыло
            // значимых цифр после точки
            // не работает с типом decimal
            // ==============================================================
            int Digits = Math.Max(BitConverter.GetBytes(decimal.GetBits((decimal)this.Hi)[3])[2],
                BitConverter.GetBytes(decimal.GetBits((decimal)this.Open)[3])[2]
                );
            Digits = Math.Max(Digits, BitConverter.GetBytes(decimal.GetBits((decimal)this.Lo)[3])[2]);
            Digits = Math.Max(Digits, BitConverter.GetBytes(decimal.GetBits((decimal)this.Close)[3])[2]);
            return (int)Math.Pow(10, Digits);
        }

    }
}
