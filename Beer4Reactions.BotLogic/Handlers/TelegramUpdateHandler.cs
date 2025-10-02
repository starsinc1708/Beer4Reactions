using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Beer4Reactions.BotLogic.Configuration;
using Beer4Reactions.BotLogic.Services;

namespace Beer4Reactions.BotLogic.Handlers;

public class TelegramUpdateHandler(
    ITelegramBotClient botClient,
    PhotoService photoService,
    ReactionService reactionService,
    UserService userService,
    ChatValidationService chatValidationService,
    ILogger<TelegramUpdateHandler> logger)
{
    private readonly ITelegramBotClient _botClient = botClient;

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            var chatId = GetChatId(update);
            
            if (chatId.HasValue && !chatValidationService.IsChatAllowed(chatId.Value))
            {
                logger.LogWarning("Received update from unauthorized chat: {ChatId}", chatId);
                return;
            }

            switch (update.Type)
            {
                case UpdateType.Message:
                    await HandleMessageAsync(update.Message!, cancellationToken);
                    break;
                
                case UpdateType.MessageReaction:
                    await HandleMessageReactionAsync(update.MessageReaction!, cancellationToken);
                    break;

                default:
                    logger.LogDebug("Received update of type {UpdateType}, ignoring", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling update: {Update}", update);
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.From == null) return;

        var chatId = message.Chat.Id;
        
        // Обрабатываем только фото
        if (message.Type == MessageType.Photo)
        {
            var user = await userService.GetOrCreateUserAsync(message.From, chatId, cancellationToken);
            
            await photoService.SavePhotoAsync(message, user, cancellationToken);

            logger.LogInformation("CHAT[{ChatId}] | PHOTO SAVED | Message[{MessageId}] from [{Username}]",
                chatId, message.MessageId, user.Username ?? user.FirstName);
        }
    }

    private async Task HandleMessageReactionAsync(MessageReactionUpdated reactionUpdate, CancellationToken cancellationToken)
    {
        if (reactionUpdate.User == null) return;

        await reactionService.HandleReactionAsync(reactionUpdate, cancellationToken);
    }

    private static long? GetChatId(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message?.Chat.Id,
            UpdateType.MessageReaction => update.MessageReaction?.Chat.Id,
            UpdateType.EditedMessage => update.EditedMessage?.Chat.Id,
            UpdateType.CallbackQuery => update.CallbackQuery?.Message?.Chat.Id,
            _ => null
        };
    }
}
