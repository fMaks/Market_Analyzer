using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using NLog;

namespace MyTBot
{
    internal class TBot
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public TBot(string FileName)
        {
            StreamReader fRead = new StreamReader(FileName);
            string TBot_token = fRead.ReadLine();
            fRead.Close();
            ITelegramBotClient bot = new TelegramBotClient(TBot_token);
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
            logger.Info("Telegram Bot Start");
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine("\nTB exception");
            logger.Error(exception.ToString());
            //logger.Error(exception.Message);
            //logger.Error(exception.StackTrace);
            //logger.Error(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (Directory.Exists("TBot\\Users\\" + message.From.Id.ToString()) == false)
                {
                    Directory.CreateDirectory("TBot\\Users\\" + message.From.Id.ToString());
                }
                StreamWriter chatlog = new StreamWriter($"TBot\\Users\\{message.From.Id}\\chatlog.txt", true);
                DateTime dateTime = DateTime.Now;
                chatlog.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                chatlog.WriteLine(" " + message.Text);
                chatlog.Close();
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
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat, "Не понял запрос, для получения справки по работе с ботом - наберите /help");
            }
        }
    }
}
