using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.Models;

namespace Beer4Reactions.BotLogic.Services;

public class TopMessageService(
    AppDbContext context,
    ITelegramBotClient botClient,
    StatisticsService statisticsService,
    ILogger<TopMessageService> logger)
{
    public async Task UpdateTopMessageAsync(long chatId)
    {
        var activeMessage = await context.TopMessages
            .FirstOrDefaultAsync(tm => tm.ChatId == chatId && tm.IsActive);

        if (activeMessage == null)
        {
            logger.LogWarning("No active TopMessage found for chat {ChatId}", chatId);
            return;
        }

        try
        {
            var newStatisticsText = await statisticsService.GenerateCurrentStatisticsAsync(chatId);

            // Проверяем, изменился ли контент
            if (activeMessage.LastMessageContent != null
                && activeMessage.LastMessageContent.Equals(newStatisticsText, StringComparison.InvariantCulture))
            {
                return;
            }

            await botClient.EditMessageText(
                chatId: chatId,
                messageId: (int)activeMessage.MessageId,
                text: newStatisticsText,
                parseMode: ParseMode.Html);

            // Обновляем запись в БД
            activeMessage.LastMessageContent = newStatisticsText;
            activeMessage.LastUpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("TopMessage updated: {MessageId}", activeMessage.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update TopMessage {MessageId} in chat {ChatId}.", 
                activeMessage.MessageId, chatId);
        }
    }
    
    public async Task<TopMessage> CreateTopMessageAsync(long chatId)
    {
        // Деактивируем существующее активное сообщение
        var existingActiveMessage = await context.TopMessages
            .FirstOrDefaultAsync(tm => tm.ChatId == chatId && tm.IsActive);

        if (existingActiveMessage != null)
        {
            await DeactivateTopMessageAsync(existingActiveMessage);
        }

        // Создаем новое TopMessage
        var statisticsText = await statisticsService.GenerateCurrentStatisticsAsync(chatId);
        
        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: statisticsText,
            parseMode: ParseMode.Html,
            disableNotification: true);
        
        await botClient.PinChatMessage(chatId, sentMessage.MessageId, disableNotification: true);
        
        var topMessage = new TopMessage
        {
            ChatId = chatId,
            MessageId = sentMessage.MessageId,
            IsActive = true,
            LastMessageContent = statisticsText,
            StatisticsPeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        context.TopMessages.Add(topMessage);
        await context.SaveChangesAsync();

                        logger.LogInformation("CHAT[{ChatId}] | TOP MESSAGE CREATED | Message[{MessageId}] | Period[{StartDate:yyyy-MM-dd} to Current]",
                    chatId, sentMessage.MessageId, topMessage.StatisticsPeriodStart);

        return topMessage;
    }

    private async Task DeactivateTopMessageAsync(TopMessage topMessage)
    {
        try
        {
            await botClient.DeleteMessage(topMessage.ChatId, (int)topMessage.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete TopMessage {MessageId}", topMessage.MessageId);
        }
        topMessage.IsActive = false;
        topMessage.IsDeleted = true;
        await context.SaveChangesAsync();
    }

    public async Task<TopMessage?> GetActiveTopMessageAsync(long chatId)
    {
        return await context.TopMessages
            .FirstOrDefaultAsync(tm => tm.ChatId == chatId && tm.IsActive);
    }
}
