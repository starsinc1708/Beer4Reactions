namespace Beer4Reactions.BotLogic.Configuration;

public class BotSettings
{
    public int TopMessageUpdateIntervalMinutes { get; set; } = 5;
    public int StatisticsUpdateIntervalMinutes { get; set; } = 5;
    public int TimezoneOffsetHours { get; set; } = 4; // Часовой пояс +4 (по умолчанию)
}
