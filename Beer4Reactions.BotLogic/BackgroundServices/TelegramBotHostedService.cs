using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Beer4Reactions.BotLogic.Configuration;
using Beer4Reactions.BotLogic.Handlers;

namespace Beer4Reactions.BotLogic.BackgroundServices;

public class TelegramBotHostedService(
    ITelegramBotClient botClient,
    IServiceProvider serviceProvider,
    IOptions<TelegramBotSettings> botSettings,
    ILogger<TelegramBotHostedService> logger) : BackgroundService
{
    private readonly TelegramBotSettings _botSettings = botSettings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await botClient.GetMe(stoppingToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates =
            [
                Telegram.Bot.Types.Enums.UpdateType.Message,
                Telegram.Bot.Types.Enums.UpdateType.MessageReaction
            ]
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        logger.LogInformation("TELEGRAM BOT | STARTED | Username[@{BotUsername}] | Chats[{ChatCount}] | Polling[ACTIVE]", 
            me.Username, _botSettings.AllowedChatIds.Count);
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var updateHandler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();
        await updateHandler.HandleUpdateAsync(update, cancellationToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Polling error occurred");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Telegram bot...");
        await base.StopAsync(cancellationToken);
    }
}
