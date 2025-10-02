using Microsoft.Extensions.Options;
using Beer4Reactions.BotLogic.Configuration;

namespace Beer4Reactions.BotLogic.Services;

public class ChatValidationService(IOptions<TelegramBotSettings> botSettings)
{
    private readonly TelegramBotSettings _botSettings = botSettings.Value;

    public bool IsChatAllowed(long chatId)
    {
        return _botSettings.AllowedChatIds.Contains(chatId);
    }

    public bool IsAnyChatAllowed()
    {
        return _botSettings.AllowedChatIds.Count != 0;
    }
}
