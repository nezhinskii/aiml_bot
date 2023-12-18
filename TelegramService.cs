using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using AForge;

namespace AIMLTGBot
{
    public class TelegramService : IDisposable
    {
        private readonly TelegramBotClient client;
        private readonly AIMLService aiml;
        // CancellationToken - инструмент для отмены задач, запущенных в отдельном потоке
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        public string Username { get; }
        private DataHolder dataHolder = new DataHolder();
        private MagicEye processor = new MagicEye();
        private BaseNetwork network = new AccordNet(new int[] { 2400, 500, 100, 10 });
        string currentPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        public TelegramService(string token, AIMLService aimlService)
        {
            dataHolder.loadData(
                $"{currentPath}\\old_data\\train",
                $"{currentPath}\\old_data\\test",
                $"{currentPath}\\old_data\\train.txt",
                $"{currentPath}\\old_data\\test.txt"
            );
            network.TrainOnDataSet(dataHolder.trainData, 35, 1e-9, true, 1e-2);
            double accuracy = dataHolder.testData.TestNeuralNetwork(network);
            Console.WriteLine(accuracy.ToString());
            aiml = aimlService;
            client = new TelegramBotClient(token);
            client.StartReceiving(HandleUpdateMessageAsync, HandleErrorAsync, new ReceiverOptions
            {   // Подписываемся только на сообщения
                AllowedUpdates = new[] { UpdateType.Message }
            },
            cancellationToken: cts.Token);
            // Пробуем получить логин бота - тестируем соединение и токен
            Username = client.GetMeAsync().Result.Username;
        }

        async Task HandleUpdateMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;
            var chatId = message.Chat.Id;
            var username = message.Chat.FirstName;
            if (message.Type == MessageType.Text)
            {
                var messageText = update.Message.Text;

                Console.WriteLine($"Received a '{messageText}' message in chat {chatId} with {username}.");

                string answer = aiml.Talk(chatId, username, messageText);
                if (answer.Trim() == "")
                {
                    answer = "Не совсем уловил мысль";
                }
                // Echo received message text
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: answer,
                    cancellationToken: cancellationToken);
                return;
            }
            // Загрузка изображений пригодится для соединения с нейросетью
            if (message.Type == MessageType.Photo)
            {
                var photoId = message.Photo.Last().FileId;
                Telegram.Bot.Types.File fl = await client.GetFileAsync(photoId, cancellationToken: cancellationToken);
                var imageStream = new MemoryStream();
                await client.DownloadFileAsync(fl.FilePath, imageStream, cancellationToken: cancellationToken);
                // Если бы мы хотели получить битмап, то могли бы использовать new Bitmap(Image.FromStream(imageStream))
                // Но вместо этого пошлём картинку назад
                // Стрим помнит последнее место записи, мы же хотим теперь прочитать с самого начала
                imageStream.Seek(0, 0);
                var image = new Bitmap(Image.FromStream(imageStream));
                processor.ProcessImage(image, false);
                var processedImage = processor.processed;
                var sample = bitmapToSample(processedImage);
                network.Predict(sample);
                string prediction = "undef";
                processedImage.Save($"{currentPath}//predicted.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine(sample.recognizedClass);
                switch (sample.recognizedClass)
                {
                    case SymbolType.Alpha:
                        prediction = "alpha";
                        break;
                    case SymbolType.Beta:
                        prediction = "beta";
                        break;
                    case SymbolType.Gamma:
                        prediction = "gamma";
                        break;
                    case SymbolType.Epsilon:
                        prediction = "epsilon";
                        break;
                    case SymbolType.Iota:
                        prediction = "iota";
                        break;
                    case SymbolType.Lambda:
                        prediction = "lambda";
                        break;
                    case SymbolType.Pi:
                        prediction = "pi";
                        break;
                    case SymbolType.Sigma:
                        prediction = "sigma";
                        break;
                    case SymbolType.Tau:
                        prediction = "tau";
                        break;
                    case SymbolType.Psi:
                        prediction = "psi";
                        break;
                    case SymbolType.Undef:
                        prediction = "undef";
                        break;
                }
                await client.SendTextMessageAsync(
                    message.Chat.Id,
                    aiml.Talk(chatId, username, $"predicted {prediction}"),
                    cancellationToken: cancellationToken
                );
                return;
            }
            // Можно обрабатывать разные виды сообщений, просто для примера пробросим реакцию на них в AIML
            if (message.Type == MessageType.Video)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Видео"), cancellationToken: cancellationToken);
                return;
            }
            if (message.Type == MessageType.Audio)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Аудио"), cancellationToken: cancellationToken);
                return;
            }
            if (message.Type == MessageType.Voice)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Войс"), cancellationToken: cancellationToken);
                return;
            }
        }

        Sample bitmapToSample(Bitmap processed)
        {
            Rectangle rect = new Rectangle(0, 0, processed.Width, processed.Height);
            BitmapData bmpData = processed.LockBits(rect, ImageLockMode.ReadOnly, processed.PixelFormat);
            double[] input = new double[48 * 48];
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int heightInPixels = bmpData.Height;
                int widthInBytes = bmpData.Stride;
                for (int y = 0; y < heightInPixels; y++)
                {
                    for (int x = 0; x < widthInBytes; x = x + 1)
                    {
                        double grayValue = ptr[(y * bmpData.Stride) + x] / 255.0;
                        input[(y * bmpData.Stride) + x] = grayValue;

                    }
                }
            }
            Sample sample = new Sample(input, 10);
            return sample;
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var apiRequestException = exception as ApiRequestException;
            if (apiRequestException != null)
                Console.WriteLine($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
            else
                Console.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Заканчиваем работу - корректно отменяем задачи в других потоках
            // Отменяем токен - завершатся все асинхронные таски
            cts.Cancel();
        }
    }
}
