using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using FlickrNet;

class Program
{
    static void Main(string[] args)
    {
        var bot = new Bot("6078566710:AAGhu3MMCK3ijpSLrUNHRTtkOY5DG1jzIdM");
        bot.CreateCommands();
        bot.StartReceiving();
        Console.ReadLine();
    }
}

public class FlickrAPI
{
    private static readonly Flickr _flickr = new Flickr("91c4230533049eee9265538f9f2ca2a6");
    private static readonly Random _random = new Random();

    public static async Task<string> GetPhotoUrlAsync(string request)
    {
        var photoSearchOptions = new PhotoSearchOptions
        {
            Text = request,
            SortOrder = PhotoSearchSortOrder.Relevance
        };
        PhotoCollection photos = await _flickr.PhotosSearchAsync(photoSearchOptions);
        var listPhotos = photos.ToList();
        if (listPhotos.Count == 0)
        {
            return null;
        }


        var randomPhotos = _random.Next(0, listPhotos.Count);
        return listPhotos[randomPhotos].LargeUrl;
    }
}

public class Bot
{
    private readonly TelegramBotClient _botClient;

    public Bot(string token)
    {
        _botClient = new TelegramBotClient(token);
    }

    public void CreateCommands()
    {
        _botClient.SetMyCommandsAsync(new List<BotCommand>()
        {
            new()
            {
                Command = CustomBotCommands.START,
                Description = "Запустить бота."
            },
            new()
            {
                Command = CustomBotCommands.ABOUT,
                Description = "Что делает бот и как им пользоваться?"
            }
        });
    }

    public void StartReceiving()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new UpdateType[] { UpdateType.Message }
        };
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleError,
            receiverOptions,
            cancellationToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var chatId = update.Message.Chat.Id;
        if (string.IsNullOrEmpty(update.Message.Text))
        {
            await _botClient.SendTextMessageAsync(chatId,
                text: "Данный бот принимает только текстовые сообщения.\n" +
                      "Введите ваш запрос правильно.",
                cancellationToken: cancellationToken);
            return;
        }

        var messageText = update.Message.Text;

        if (IsStartCommand(messageText))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Привет, я бот по поиску картинок. Введите ваш запрос.",
                cancellationToken: cancellationToken);
            return;
        }

        if (IsAboutCommand(messageText))
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Данный бот возвращает 1 картинку по запросу пользователя. \n" +
                      "Чтобы получить картинку, введите текстовый запрос.",
                cancellationToken: cancellationToken);
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            await SendPhotoAsync(chatId, messageText, cancellationToken);
        }
    }

    private Task HandleError(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    private bool IsStartCommand(string messageText)
    {
        return messageText.ToLower() == CustomBotCommands.START;
    }

    private bool IsAboutCommand(string messageText)
    {
        return messageText.ToLower() == CustomBotCommands.ABOUT;
    }

    private async Task SendPhotoAsync(long chatId, string request, CancellationToken cancellationToken)
    {
        var photoUrl = await FlickrAPI.GetPhotoUrlAsync(request);

        if (photoUrl == null)
        {
            await _botClient.SendTextMessageAsync(chatId,
                "Изображений не найдено.",
                cancellationToken: cancellationToken);
            return;
        }

        await _botClient.SendPhotoAsync(chatId: chatId,
            photo: new InputFileUrl(photoUrl),
            cancellationToken: cancellationToken);
    }
}

public static class CustomBotCommands
{
    public const string START = "/start";
    public const string ABOUT = "/about";
}