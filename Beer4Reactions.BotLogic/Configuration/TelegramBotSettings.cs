namespace Beer4Reactions.BotLogic.Configuration;

public class TelegramBotSettings
{
    public string ApiToken { get; set; } = string.Empty;
    public List<long> AllowedChatIds { get; set; } = [];
}
