using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Market_Analizer;

using IniFiles;
using Telegram.Bot;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Drawing.Imaging;
using Patern;

namespace Market_Analizer
{
    public class Symbol
    {
        public string SymbolName { get; set; }
        public int tpp;
        public List<Candle> m1;
        public List<Candle> m5;
        public List<Candle> m15;
        public List<Candle> m30;
        public List<Candle> h1;
        public List<Candle> h4;

        public Symbol()
        {
            m1 = new List<Candle>();
            m5 = new List<Candle>();
            m15 = new List<Candle>();
            m30 = new List<Candle>();
            h1 = new List<Candle>();
            h4 = new List<Candle>();
        }

        public Symbol(string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.SymbolName = SubStr[1];
            long candleTime = long.Parse(SubStr[0]);
            long candleStartTime = candleTime - candleTime % 60;
            m1 = new List<Candle>();
            m1.Insert(0, new Candle(candleStartTime, decimal.Parse(SubStr[2]), decimal.Parse(SubStr[3]), decimal.Parse(SubStr[4]),
                decimal.Parse(SubStr[5]), decimal.Parse(SubStr[6]), decimal.Parse(SubStr[7]), int.Parse(SubStr[8])));

            m5 = new List<Candle>();
            m5.Insert(0, new Candle(Str));
            m15 = new List<Candle>();
            m15.Insert(0, new Candle(Str));
            m30 = new List<Candle>();
            m30.Insert(0, new Candle(Str));

            candleStartTime = candleTime - candleTime % 3600;
            h1 = new List<Candle>();
            h1.Insert(0, new Candle(candleStartTime, decimal.Parse(SubStr[2]), decimal.Parse(SubStr[3]), decimal.Parse(SubStr[4]),
                decimal.Parse(SubStr[5]), decimal.Parse(SubStr[6]), decimal.Parse(SubStr[7]), int.Parse(SubStr[8])));
            candleStartTime = candleTime - candleTime % 14400;
            h4 = new List<Candle>();
            h4.Insert(0, new Candle(candleStartTime, decimal.Parse(SubStr[2]), decimal.Parse(SubStr[3]), decimal.Parse(SubStr[4]),
                decimal.Parse(SubStr[5]), decimal.Parse(SubStr[6]), decimal.Parse(SubStr[7]), int.Parse(SubStr[8])));

        }

        public Symbol (string Name, HuobiHistoryDatum datum)
        {
            this.SymbolName = Name;
            m1 = new List<Candle>();
            m5 = new List<Candle>();
            m15 = new List<Candle>();
            m30 = new List<Candle>();
            h1 = new List<Candle>();
            h4 = new List<Candle>();
        }

        public Symbol (HuobiRootTickCandle HRTick)
        {
            this.SymbolName = HRTick.ch.Split('.')[1];
            m1 = new List<Candle>();
            m1.Insert(0, new Candle(HRTick));
            m5 = new List<Candle>();
            m5.Insert(0, new Candle(HRTick));
            m15 = new List<Candle> ();
            m15.Insert(0, new Candle(HRTick));
            m30 = new List<Candle>();
            m30.Insert(0, new Candle(HRTick));
            h1 = new List<Candle>();
            h1.Insert(0, new Candle(HRTick));
            h4= new List<Candle>();
            h4.Insert(0, new Candle(HRTick));
        }

        public Symbol(string ch, long ts, decimal open, decimal hight, decimal low, decimal close, decimal amount, decimal vol, int count)
        {
            this.SymbolName = ch.Split('.')[1];
            m1 = new List<Candle>();
            m1.Add(new Candle(ch, ts, open, hight, low, close, amount, vol, count));

            //m5 = new List<Candle>();
            //m5.Add(new Candle(ts - ts % 300, close));
        }

        public void AddTick(long ts, decimal open, decimal hight, decimal low, decimal close, decimal amount, decimal vol, int count)
        {
            if (this.m1.Count > 0)
            {
                if (this.m1[0].UnixTimeGMT + 59 > ts)
                {
                    this.m1[0].Hi = Math.Max(this.m1[0].Hi, hight);
                    this.m1[0].Lo = Math.Min(this.m1[0].Lo, low);
                    this.m1[0].Close = close;
                    this.m1[0].Amount = amount;
                    this.m1[0].Vol = vol;
                    this.m1[0].Count = count;

                    // проверка на импульс
                    long currentTimeOffset = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                    if (this.tpp != 0)   // нет инфы о разрядности данных, значит еще идет загрузка
                    {
                        //if (currentTimeOffset - this.m1[0].UnixTimeGMT < 60)
                        {
                            if ((this.m1[0].Hi - this.m1[0].Lo) / this.m1[0].Lo > 0.05m) // * 100 = 1%
                            {
                                string strChat = "-1001523852940";
                                var test = new Telegram.Bot.TelegramBotClient("5551814200:AAG4EBN6XrhlIb20Tl4P3PjfDciSA38dhGE");
                                var GetMe = test.GetMeAsync();
                                DateTime signalTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.m1[1].UnixTimeGMT).ToLocalTime();
                                CandleImageStyle imgStyle = new CandleImageStyle();
                                imgStyle.width = 1024;
                                imgStyle.hight = 768;
                                imgStyle.background = System.Drawing.Color.White;
                                var img = GetImage(this.m1, 0, 100, imgStyle);
                                // шлем сигнал в телегу
                                MemoryStream ms = new MemoryStream();
                                img.Save(ms, ImageFormat.Jpeg);
                                ms.Position = 0;
                                var SendImage = test.SendPhotoAsync(strChat, photo: new Telegram.Bot.Types.InputFile(ms), caption: $"Huobi M1 импульс " +
                                    $"{signalTime.ToString("HH:mm")}");
                            }
                        }
                    }
                }
                else
                {
                    while (this.m1[0].UnixTimeGMT + 60 < ts)
                    {
                        long tts = this.m1[0].UnixTimeGMT + 60;
                        decimal pPrice = this.m1[0].Close;
                        this.m1.Insert(0, new Candle(tts, pPrice, pPrice, pPrice, pPrice, 0, 0, 0));
                    }
                    this.m1.Insert(0, new Candle(ts, open, hight, low, close, amount, vol, count));
                }
            }
            else
            {
                //decimal tHight = Math.Max(open, Math.Max(hight, close));
                //decimal tLow = Math.Min(open, Math.Min(low, close));
                this.m1.Insert(0, new Candle(ts, open, hight, low, close, amount, vol, count));
            }
            // m15
            if (this.m15.Count > 0)
            {
                if ((this.m15[0].UnixTimeGMT + 899) > ts)
                {
                    this.m15[0].Hi = Math.Max(this.m15[0].Hi, hight);
                    this.m15[0].Lo = Math.Min(this.m15[0].Lo, low);
                    this.m15[0].Close = close;
                    this.m15[0].Amount += amount;
                    this.m15[0].Vol += vol;
                    this.m15[0].Count++;     // ????
                }
                else
                {
                    while (this.m15[0].UnixTimeGMT + 900 < ts)
                    {
                        long tts = this.m15[0].UnixTimeGMT + 900;
                        decimal pPrice = this.m15[0].Close;
                        this.m15.Insert(0, new Candle(tts, pPrice, pPrice, pPrice, pPrice, 0, 0, 0));
                    }
                    long tsStartCandle = ts - ts % 900;
                    this.m15.Insert(0, new Candle(tsStartCandle, open, hight, low, close, amount, vol, count));
                }
            }
            else
            {
                long tsStartCandle = ts - ts % 900;
                this.m15.Insert(0, new Candle(tsStartCandle, open, hight, low, close, amount, vol, count));
            }
            // h1
            if (this.h1.Count > 0)
            {
                //Console.WriteLine($"{this.SymbolName} {this.h1[0].UnixTimeGMT} {ts}");
                if ((this.h1[0].UnixTimeGMT + 3599) > ts)
                {
                    this.h1[0].Hi = Math.Max(this.h1[0].Hi, hight);
                    this.h1[0].Lo = Math.Min(this.h1[0].Lo, low);
                    this.h1[0].Close = close;
                    this.h1[0].Amount += amount;
                    this.h1[0].Vol += vol;
                    this.h1[0].Count++;     // ????
                }
                else
                {
                    while (this.h1[0].UnixTimeGMT + 3600 < ts)
                    {
                        long tts = this.h1[0].UnixTimeGMT + 3600;
                        decimal pPrice = this.h1[0].Close;
                        this.h1.Insert(0, new Candle(tts, pPrice, pPrice, pPrice, pPrice, 0, 0, 0));
                    }
                    long tsStartCandle = ts - ts % 3600;
                    this.h1.Insert(0, new Candle(tsStartCandle, open, hight, low, close, amount, vol, count));

                    // проверка паттернов
                    //long currentTimeOffset = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                    //if (this.tpp != 0)
                    //{
                    //    if (h1.ifHammer(1) && h1.Trend(2, 2) == 2)
                    //    {
                    //        //Console.WriteLine($"Hammer: {this.SymbolName} ");
                    //        string strChat = "-1001523852940";
                    //        var test = new Telegram.Bot.TelegramBotClient("5551814200:AAG4EBN6XrhlIb20Tl4P3PjfDciSA38dhGE");
                    //        var GetMe = test.GetMeAsync();
                    //        DateTime signalTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.h1[1].UnixTimeGMT).ToLocalTime();
                    //        CandleImageStyle imgStyle = new CandleImageStyle();
                    //        imgStyle.width = 2560;
                    //        imgStyle.hight = 1440;
                    //        imgStyle.background = System.Drawing.Color.White;
                    //        var img = GetImage(this.h1, 0, 200, imgStyle);
                    //        // шлем сигнал в телегу
                    //        MemoryStream ms = new MemoryStream();
                    //        img.Save(ms, ImageFormat.Jpeg);
                    //        ms.Position = 0;
                    //        var SendImage = test.SendPhotoAsync(strChat, photo: new Telegram.Bot.Types.InputFile(ms),
                    //            caption: $"Huobi H1 молот " +
                    //            $"{signalTime.ToString("HH:mm")}");
                    //    }
                    //}

                    // каждые 4 часа график 1H битка
                    if (this.h1[0].UnixTimeGMT % 14400 == 0 && this.SymbolName.Contains("btcusdt") && this.tpp != 0)
                    {
                        string strChat = "-1001523852940";
                        var test = new Telegram.Bot.TelegramBotClient("5551814200:AAG4EBN6XrhlIb20Tl4P3PjfDciSA38dhGE");
                        var GetMe = test.GetMeAsync();
                        DateTime signalLocTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.h1[0].UnixTimeGMT).ToLocalTime();
                        DateTime signalUTCTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.h1[0].UnixTimeGMT);
                        DateTime signalHuobiTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.h1[0].UnixTimeGMT).AddHours(8);
                        CandleImageStyle imgStyle = new CandleImageStyle();
                        imgStyle.width = 2560;
                        imgStyle.hight = 1440;
                        imgStyle.background = System.Drawing.Color.White;
                        var img = GetImage(this.h1, 0, 450, imgStyle);
                        // шлем сигнал в телегу
                        MemoryStream ms = new MemoryStream();
                        img.Save(ms, ImageFormat.Jpeg);
                        ms.Position = 0;
                        var SendImage = test.SendPhotoAsync(strChat, photo: new Telegram.Bot.Types.InputFile(ms), caption: $"Huobi BTC info " +
                            $"Local time: {signalLocTime.ToString("HH:mm")} UTC time: {signalUTCTime.ToString("HH:mm")} " +
                            $"Huobi Time {signalHuobiTime.ToString("HH:mm")}");
                    }
                }
            }
            else    // если в h1 еще пусто
            {
                long tsStartCandle = ts - ts % 3600;
                //decimal tHight = Math.Max(open, Math.Max(hight, close));
                //decimal tLow = Math.Min(open, Math.Min(low, close));
                //this.h1.Insert(0, new Candle(tsStartCandle, open, tHight, tLow, close, amount, vol, count));
                this.h1.Insert(0, new Candle(tsStartCandle, open, hight, low, close, amount, vol, count));
            }

            //h4
            if (this.h4.Count > 0)
            {
                if ((this.h4[0].UnixTimeGMT + 14399) > ts)
                {
                    this.h4[0].Hi = Math.Max(this.h4[0].Hi, hight);
                    this.h4[0].Lo = Math.Min(this.h4[0].Lo, low);
                    this.h4[0].Close = close;
                    this.h4[0].Amount += amount;
                    this.h4[0].Vol += vol;
                    this.h4[0].Count++;     // ????
                }
                else
                {
                    while (this.h4[0].UnixTimeGMT + 14400 < ts)
                    {
                        long tts = this.h4[0].UnixTimeGMT + 14400;
                        decimal pPrice = this.h4[0].Close;
                        this.h4.Insert(0, new Candle(tts, pPrice, pPrice, pPrice, pPrice, 0, 0, 0));
                    }
                    long tsStartCandle = ts - ts % 14400;
                    this.h4.Insert(0, new Candle(tsStartCandle, open, hight, low, close, amount, vol, count));
                    if (this.tpp != 0)
                    {
                        if (h4.ifHammer(1) && h4.Trend(2, 2) == 2)
                        {
                            //Console.WriteLine($"Hammer: {this.SymbolName} ");
                            StreamReader fRead = new StreamReader("TBot\\token.ini");
                            string TBot_token = fRead.ReadLine();
                            string TChannel = fRead.ReadLine();
                            fRead.Close();
                            ITelegramBotClient bot = new TelegramBotClient(TBot_token);

                            var GetMe = bot.GetMeAsync();
                            DateTime signalTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.h1[1].UnixTimeGMT).ToLocalTime();
                            CandleImageStyle imgStyle = new CandleImageStyle();
                            imgStyle.width = 2560;
                            imgStyle.hight = 1440;
                            imgStyle.background = System.Drawing.Color.White;
                            var img = GetImage(this.h4, 0, 200, imgStyle);
                            // шлем сигнал в телегу
                            MemoryStream ms = new MemoryStream();
                            img.Save(ms, ImageFormat.Jpeg);
                            ms.Position = 0;
                            var SendImage = bot.SendPhotoAsync(TChannel, photo: new Telegram.Bot.Types.InputFile(ms),
                                caption: $"Huobi H4 молот " +
                                $"{signalTime.ToString("HH:mm")}");
                        }
                    }
                }
            }
            else
            {
                long tsStartCandle = ts - ts % 14400;
                this.h4.Insert(0, new Candle(tsStartCandle, open, hight, low, close, amount, vol, count));
            }
        }

        public void AddTick(string Str)
        {
            string[] SubStr = Str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            AddTick(Convert.ToInt64(SubStr[0]), decimal.Parse(SubStr[2]),
                decimal.Parse(SubStr[3]), decimal.Parse(SubStr[4]), decimal.Parse(SubStr[5]),
                decimal.Parse(SubStr[6]), decimal.Parse(SubStr[7]), int.Parse(SubStr[8]));
        }

        public void AddTick(HuobiHistoryDatum datum)
        {
            //this.m1.Add(new Candle("X", datum.id, datum.open, datum.low, datum.high, datum.close, datum.amount, datum.vol, datum.count));
            AddTick(datum.id, datum.open, datum.high, datum.low, datum.close, datum.amount, datum.vol, datum.count);
        }

        public class CandleImageStyle
        {
            public int width = 800;     // ширина картинки
            public int hight = 600;     // высота
            public System.Drawing.Color background = System.Drawing.Color.GreenYellow; // цвет фона
            public int thickets = 5;    // толщина свечи
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]
        public Bitmap GetImage (List<Candle> tf, int startCandle, int len, CandleImageStyle style)
        {
            // делает картинку по свечам
            // In:
            //  startCandle - с какой свечи начинать рисовать (0 - ткущая)
            //  len - сколько свечей рисовать
            // Если len меньше общего количества, рисует текущее количество

            const int HEAD = 25;    // сколько пикселей отступить для верхней подписи
            const int DOWN = 15;    // отсуп снизу
            const int RIGHT = 70;   // ширина области справа

            Bitmap b = new Bitmap(style.width, style.hight);
            //int Multiple = tf[startCandle].Multiple();
            long Multiple = (long)Math.Pow(10, this.tpp);
            using (Graphics g = Graphics.FromImage(b))
            {
                decimal gHi = tf[startCandle].Hi;
                decimal gLo = tf[startCandle].Lo;
                // определим мин. и макс. из свечей
                for (int count = startCandle + 1; count < Math.Min(tf.Count, len); count++)
                {
                    gLo = Math.Min(gLo, tf[count].Lo != 0 ? tf[count].Lo : gLo);
                    gHi = Math.Max(gHi, tf[count].Hi);
                }
                // высота графика в пунктах (от Lo самой низкой до Hi самой высокой свечи)
                long LongGrid = (long)((gHi - gLo) * Multiple);
                // сколько пунктов на 1 пиксель
                decimal ScalingGrid = LongGrid / (decimal)(style.hight - HEAD - DOWN);
                // толщина свечи
                int thickest = Math.Max(5, (style.width - RIGHT) / (Math.Min(len, tf.Count)));
                g.Clear(style.background);
                Pen GreenPen = new Pen(System.Drawing.Color.Green);
                Pen RedPen = new Pen(System.Drawing.Color.Red);
                Pen BlackPen = new Pen(System.Drawing.Color.Black);
                Pen GrayPen = new Pen(System.Drawing.Color.LightGray);
                // рисуем оконтовку
                g.DrawLine(new Pen(System.Drawing.Color.Black), 0, style.hight - DOWN, style.width - RIGHT, style.hight - DOWN);
                g.DrawLine(new Pen(System.Drawing.Color.Black), style.width - RIGHT, style.hight - DOWN, style.width - RIGHT, HEAD);
                Font drawFont = new Font("Arial", 8);
                SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Black);
                StringFormat drawFormat = new StringFormat();
                
                Font nameFont = new Font("Arial", 12, FontStyle.Bold);
                // рисуем горизонтальную сетку подписи (снизу вверх)
                for (int i = 0; i <= (style.hight - HEAD - DOWN) / 50; i++)
                {
                    //g.DrawLine(GrayPen, 0, HEAD + i * 50, style.width - RIGHT + 2, HEAD + 10 + i * 50);
                    //g.DrawString(Math.Round(gHi - (decimal)i * LongGrid / (style.hight / 50) / Multiple, this.tpp).ToString(), drawFont, drawBrush, (float)(style.width - RIGHT + 3), (float)(HEAD + 10 + i * 50 - 5));
                    g.DrawLine(GrayPen, 0, style.hight - DOWN - i * 50, style.width - RIGHT + 2, style.hight - DOWN - i * 50);
                    g.DrawString(Math.Round(gLo + ((decimal)i * 50 * ScalingGrid) / Multiple, this.tpp).ToString(), drawFont, drawBrush, (float)(style.width - RIGHT + 3), (float)(style.hight - DOWN - i * 50 - 4));
                }
                // верхняя линия с gHi, если сверху слишком много пустого места
                if ((style.hight - DOWN - HEAD) % 50 >= 20)
                {
                    g.DrawLine(GrayPen, 0, HEAD, style.width - RIGHT + 2, HEAD);
                    g.DrawString(Math.Round(gHi, this.tpp).ToString(), drawFont, drawBrush, (float)(style.width - RIGHT + 3), (float)(HEAD - 4));
                }
                for (int CountBar = 0; CountBar < Math.Min(tf.Count, (style.width - RIGHT) / thickest); CountBar++)
                {
                    // рисуем вертикальную сетку и нижние подписи
                    //if (CountBar == 1 || CountBar % 4 == 0)
                    if ((CountBar * thickest) % 100 < thickest)
                    {
                        // выводим время свечи в нижней части графика
                        int CordX = style.width - RIGHT - (CountBar + 1) * thickest + thickest / 2;
                        g.DrawLine(BlackPen, CordX, style.hight - DOWN, CordX, style.hight - DOWN + 2);
                        DateTime BarTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(tf[CountBar].UnixTimeGMT).ToLocalTime();
                        g.DrawString(BarTime.ToString("HH:mm"), drawFont, drawBrush, CordX - 15, style.hight - 13);
                        g.DrawLine(GrayPen, CordX, HEAD, CordX, style.hight - DOWN);
                    }
                    // нижняя и верхняя точки теней
                    Point PointLo = new Point(style.width - RIGHT - (CountBar + 1) * (thickest) + thickest / 2 - 1, (int)(style.hight - DOWN - (tf[CountBar].Lo - gLo) / ScalingGrid * Multiple));
                    Point PointHi = new Point(style.width - RIGHT - (CountBar + 1) * (thickest) + thickest / 2 - 1,  (int)(style.hight - DOWN - (tf[CountBar].Hi - gLo) / ScalingGrid * Multiple));
                    // тело свечи
                    int tBody = (int)(Math.Abs(tf[CountBar].Close - tf[CountBar].Open) / ScalingGrid * Multiple);
                    Rectangle rect = new Rectangle(style.width - RIGHT - (CountBar + 1) * thickest,
                        (int)(style.hight - DOWN - (Math.Max(tf[CountBar].Open, tf[CountBar].Close) - gLo) / ScalingGrid * Multiple),
                        thickest - 1,
                        tBody == 0 ? 1 : tBody
                        ); ;

                    if ((tf[CountBar].Open < tf[CountBar].Close) ||
                        (tf[CountBar].Open == tf[CountBar].Close))
                    {
                        // зеленая (ростущая) свеча
                        g.DrawLine(GreenPen, PointLo, PointHi);
                        g.FillRectangle(new SolidBrush(System.Drawing.Color.Green), rect);
                    }
                    else
                    {
                        // красная (падающая) свеча
                        g.DrawLine(RedPen, PointLo, PointHi);
                        g.FillRectangle(new SolidBrush(System.Drawing.Color.Red), rect);
                    }
                }
                // имя пары
                g.DrawString(this.SymbolName, nameFont, drawBrush, 1, 1);

                DateTime fTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(this.h1[1].UnixTimeGMT).ToLocalTime();
                String NameSave = "Data\\Img\\" + this.SymbolName + "_" + fTime.ToString("yyyyMMdd_HHmm") + ".jpg";
                b.Save(NameSave, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            return b;
        }
        void Add(string ch, long ts, double close)
        {

        }
    }
}
