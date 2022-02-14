using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jodit_Bot_.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
//using JsonSerializer = System.Text.Json.JsonSerializer;


namespace Jodit_Bot_.Services
{
    public class HandleUpdateService
    {
        
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<HandleUpdateService> _logger;

        public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
        {
            _botClient = botClient;
            _logger = logger;
            
         
        }

        private ReplyKeyboardMarkup KeyboardGroups;

        public async Task EchoAsync(Update update)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                UpdateType.Message => BotOnMessageReceived(update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
                UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery!),
                UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
                _ => UnknownUpdateHandlerAsync(update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(exception);
            }
        }

       

        
        private async Task BotOnMessageReceived(Message message)
        {
            
            _logger.LogInformation("Receive message type: {messageType}", message.Type);
            if (message.Type != MessageType.Text)
                return;

            var action = message.Text!.Split(' ')[0] switch
            {
                "/start" => SendHello(_botClient, message),
                "/key" => SendEnterKey(_botClient, message),
                "/who" => Who(_botClient, message),
                _ => Usage(_botClient, message)
            };
            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);

            
           
           
            static async Task<Message> SendEnterKey(ITelegramBotClient bot, Message message)
            {
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Enter the key as a reply message");
            }
            
            static async Task<Message> SendHello(ITelegramBotClient bot, Message message)
            {
                return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Hello, im duty bot");
                
            }
            
            static async Task<Message> Who(ITelegramBotClient bot, Message message)
            {
                
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender,
                    cert, chain, sslPolicyErrors) => { return true; };
            
                using (var client = new HttpClient(clientHandler))
                {
                    try
                    {
                        var result = await client.GetStringAsync(
                            $"https://localhost:5000/api/GetGroupsByUser?idChat={message.Chat.Id}");

                        List<GroupBot> list = JsonConvert.DeserializeObject<List<GroupBot>>(result);


                        if (list.Count > 0)
                        {
                            InlineKeyboardButton[] l = new InlineKeyboardButton[list.Count];
                            for (int i = 0; i < list.Count; i++)
                            {
                                l[i] = InlineKeyboardButton.WithCallbackData(list[i].NameGroup,
                                    list[i].IdGroup.ToString());
                            }

                            InlineKeyboardMarkup inlineKeyboard = new(l);

                            return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                                text: "Choose group",
                                replyMarkup: inlineKeyboard);
                        }
                        
                        return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                            text: "You don't have groups");
                       

                    }
                    catch (Exception e)
                    {
                        
                    }
                    return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                        text: "Oops, you are not logged in. Use /key");
                }
                  
            }
            
            static async Task<Message> Usage(ITelegramBotClient bot, Message message)
            {
                
                if (message.ReplyToMessage != null && message.ReplyToMessage.Text.Contains("Enter key"))
                {
                    var key = message.Text;
                    
                    HttpClientHandler clientHandler = new HttpClientHandler();
                    clientHandler.ServerCertificateCustomValidationCallback = (sender,
                        cert, chain, sslPolicyErrors) => { return true; };
            
                    using (var client = new HttpClient(clientHandler))
                    {
                        var result = await client.GetStringAsync(
                            $"https://localhost:5000/api/RegUserChat?idChat={message.Chat.Id}&key={key}");

                        if (Convert.ToBoolean(result))
                        {
                            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Authorization successful",  
                                replyMarkup: new ReplyKeyboardRemove());
                        }
                        else
                        {
                            return await bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Authorization failed",  
                                replyMarkup: new ReplyKeyboardRemove());
                        }
                      
                    }
                    
                }

                const string usage = "Usage:\n" +
                                     "/start - start bot\n" +
                                     "/key - authorization\n" +
                                     "/who - find out who is on duty\n";

                return await bot.SendTextMessageAsync(chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove());
            }
            
        }
        
        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}");

            try
            {
                int idGroup = Convert.ToInt32(callbackQuery.Data);
                
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender,
                    cert, chain, sslPolicyErrors) => { return true; };
                using (var client = new HttpClient(clientHandler))
                {
                    var result = await client.GetStringAsync(
                        $"https://localhost:5000/api/getUserByDate?idGroup={idGroup}");
                    await _botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: result);
                }
            }catch(Exception e){}
        }

        #region Inline Mode

        private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery)
        {
            _logger.LogInformation("Received inline query from: {inlineQueryFromId}", inlineQuery.From.Id);

            InlineQueryResult[] results =
            {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };

            await _botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0);
        }

        private Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
        {
            _logger.LogInformation("Received inline result: {chosenInlineResultId}", chosenInlineResult.ResultId);
            return Task.CompletedTask;
        }

        #endregion

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            _logger.LogInformation("Unknown update type: {updateType}", update.Type);
            return Task.CompletedTask;
        }

        public Task HandleErrorAsync(Exception exception)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
            return Task.CompletedTask;
        }
    }
}