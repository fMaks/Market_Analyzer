using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Reflection;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;

using IniFiles;
using NLog;

using MyTBot;

namespace Market_Analizer
{
    internal class Program
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            try
            {
                logger.Info("==========================");
                logger.Info("Старт");
                Console.CursorVisible = false;
                String strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Console.WriteLine($"Версия: {strVersion}");
                logger.Info($"Версия: {strVersion}");

                TBot tBot = new TBot("TBot\\token.ini");

                IniFile INI = new IniFile("config.ini");
                string WhiteList = INI.ReadINI("GlobalWhiteList", "WhiteList");
                ArraySegment<byte> msg1 = new ArraySegment<byte>(new byte[1024]);
                Huobi Birga1 = new Huobi(WhiteList);

                ConsoleKeyInfo kkey = new ConsoleKeyInfo();
                while (!(Console.KeyAvailable && (kkey = Console.ReadKey(true)).Key == ConsoleKey.Escape))
                {
                }
                Birga1.Dispose();
                logger.Info("Выход");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
            finally
            {
            }
            NLog.LogManager.Shutdown();
        }
    }
}