using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Reflection;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

using IniFiles;
using NLog;
using Telegram.Bot.Polling;

namespace Market_Analizer
{
    internal class Program
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text.ToLower() == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать!");
                    //Console.WriteLine($"\n{message.From.Id}");
                    return;
                }
                if (message.Text.ToLower() == "/help")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Список команд:\n /list - список активных сигналов");

                    return;
                }
                if (message.Text.ToLower() == "/list")
                {

                }
                await botClient.SendTextMessageAsync(message.Chat, "Не понял запрос, для получения справки по работе с ботом - наберите /help");
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            Console.WriteLine("\nTB exception");
        }

        static async Task Main(string[] args) {
            try
            {
                StreamReader fRead = new StreamReader("TBot\\token.ini");
                string TBot_token = fRead.ReadLine();
                fRead.Close();
                ITelegramBotClient bot = new TelegramBotClient(TBot_token);

                Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { }, // receive all update types
                };
                bot.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken
                );
                
                logger.Info("==========================");
                logger.Info("Старт");
                Console.CursorVisible = false;
                String strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Console.WriteLine($"Версия: {strVersion}");
                logger.Info($"Версия: {strVersion}");

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