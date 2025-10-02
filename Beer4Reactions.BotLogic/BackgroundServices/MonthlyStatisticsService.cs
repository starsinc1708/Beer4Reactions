using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.Models;
using Beer4Reactions.BotLogic.Configuration;
using Beer4Reactions.BotLogic.DTOs;
using Beer4Reactions.BotLogic.Services;

namespace Beer4Reactions.BotLogic.BackgroundServices;

public class MonthlyStatisticsService(
    IServiceProvider serviceProvider,
    ITelegramBotClient botClient,
    IOptions<TelegramBotSettings> botSettings,
    IOptions<BotSettings> settings,
    ILogger<MonthlyStatisticsService> logger) : BackgroundService
{
    private readonly TelegramBotSettings _botSettings = botSettings.Value;
    private readonly BotSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MONTHLY STATS SERVICE | STARTED | Chats[{ChatCount}]",
            _botSettings.AllowedChatIds.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var localNow = utcNow.AddHours(_settings.TimezoneOffsetHours);

                // --- вычисляем ближайшее время запуска (1-е число, 09:00 по локальному времени) ---
                var nextRunLocal = new DateTime(localNow.Year, localNow.Month, 1, 9, 0, 0);

                if (localNow >= nextRunLocal)
                {
                    nextRunLocal = nextRunLocal.AddMonths(1);
                }

                var nextRunUtc = nextRunLocal.AddHours(-_settings.TimezoneOffsetHours);

                var delay = nextRunUtc - utcNow;
                
                logger.LogInformation("MONTHLY STATS SERVICE | Next run scheduled at Local[{Local}] | UTC[{Utc}] | Delay {Delay}",
                    nextRunLocal, nextRunUtc, delay);
                
                await Task.Delay(delay, stoppingToken);
                
                await CheckAndProcessMonthlyStatisticsAsync();
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Monthly statistics service cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in monthly statistics service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task CheckAndProcessMonthlyStatisticsAsync()
    {
        var utcNow = DateTime.UtcNow;
        var localTime = utcNow.AddHours(_settings.TimezoneOffsetHours);

        var prevMonth = localTime.AddMonths(-1);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var photoService = scope.ServiceProvider.GetRequiredService<PhotoService>();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        var reactionService = scope.ServiceProvider.GetRequiredService<ReactionService>();
        
        var startDate = new DateTime(prevMonth.Year, prevMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        foreach (var chatId in _botSettings.AllowedChatIds)
        {
            _ = await SendWinnersCongratulationsAsync(
                context,
                photoService,
                userService,
                chatId,
                chatId,
                startDate,
                endDate);

            await AggregateStatisticsForChatAsync(
                context, 
                photoService, 
                reactionService, 
                userService,
                chatId, 
                prevMonth.Year, 
                prevMonth.Month);
        }

        logger.LogInformation("Monthly stats completed for {Year}-{Month:D2}", prevMonth.Year, prevMonth.Month);
    }

    private static async Task AggregateStatisticsForChatAsync(
        AppDbContext context, PhotoService photoService, ReactionService reactionService, UserService userService,
        long chatId, int year, int month)
    {
        // Проверяем, не создана ли уже статистика для этого месяца
        var existingStats = await context.MonthlyStatistics
            .FirstOrDefaultAsync(ms => ms.ChatId == chatId && ms.Year == year && ms.Month == month);

        if (existingStats != null)  return;

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        // Агрегируем данные
        var monthlyStats = new MonthlyStatistic
        {
            ChatId = chatId,
            Year = year,
            Month = month
        };

        // Топ фото месяца
        var topPhoto = await photoService.GetTopPhotoAsync(chatId, startDate, endDate);
        if (topPhoto.HasValue)
        {
            monthlyStats.TopPhotoId = topPhoto.Value.PhotoId;
            monthlyStats.TopPhotoReactionCount = topPhoto.Value.ReactionCount;
        }

        // Топ альбом месяца
        var topAlbum = await photoService.GetTopAlbumAsync(chatId, startDate, endDate);
        if (topAlbum.HasValue)
        {
            monthlyStats.TopMediaGroupId = topAlbum.Value.MediaGroupId;
            monthlyStats.TopMediaGroupReactionCount = topAlbum.Value.ReactionCount;
        }

        // Топ пользователь месяца
        var topUser = await userService.GetTopUserAsync(chatId, startDate, endDate);
        if (topUser.HasValue)
        {
            monthlyStats.TopUserId = topUser.Value.UserId;
            monthlyStats.TopUserReactionCount = topUser.Value.ReactionCount;
        }

        // Топ реакция месяца
        var topReaction = await reactionService.GetTopReactionAsync(chatId, startDate, endDate);
        if (topReaction.HasValue)
        {
            monthlyStats.TopReactionType = topReaction.Value.ReactionType;
            monthlyStats.TopReactionUsageCount = topReaction.Value.UsageCount;
        }

        // Общая статистика
        monthlyStats.TotalPhotos = await context.Photos
            .CountAsync(p => p.ChatId == chatId && p.CreatedAt >= startDate && p.CreatedAt < endDate);

        monthlyStats.TotalMediaGroups = await context.MediaGroups
            .CountAsync(mg => mg.ChatId == chatId && mg.CreatedAt >= startDate && mg.CreatedAt < endDate);

        monthlyStats.TotalReactions = await context.Reactions
            .CountAsync(r => r.ChatId == chatId && r.CreatedAt >= startDate && r.CreatedAt < endDate);

        monthlyStats.TotalActiveUsers = await context.Users
            .CountAsync(u => u.ChatId == chatId && u.LastActiveAt >= startDate && u.LastActiveAt < endDate);
        
        context.MonthlyStatistics.Add(monthlyStats);
        await context.SaveChangesAsync();
    }
    
    public async Task<MonthlyWinnersResult> SendWinnersTestAsync(
        long statisticsChatId,
        long sendToChatId,
        int? year = null,
        int? month = null,
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null)
    {
        if (year.HasValue ^ month.HasValue)
        {
            throw new ArgumentException("Both year and month must be provided together for a custom period.");
        }

        if (startDateUtc.HasValue ^ endDateUtc.HasValue)
        {
            throw new ArgumentException("Both start and end dates must be provided together for a custom period.");
        }

        if ((year.HasValue || month.HasValue) && (startDateUtc.HasValue || endDateUtc.HasValue))
        {
            throw new ArgumentException("Specify either year/month or start/end dates, not both.");
        }

        if (month is int monthValue && (monthValue < 1 || monthValue > 12))
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        DateTime periodStart;
        DateTime periodEnd;

        if (startDateUtc is DateTime start && endDateUtc is DateTime end)
        {
            periodStart = NormalizeToUtc(start);
            periodEnd = NormalizeToUtc(end);

            if (periodEnd <= periodStart)
            {
                throw new ArgumentException("End date must be greater than start date.");
            }
        }
        else if (year.HasValue && month.HasValue)
        {
            periodStart = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            periodEnd = periodStart.AddMonths(1);
        }
        else
        {
            var utcNow = DateTime.UtcNow;
            var localTime = utcNow.AddHours(_settings.TimezoneOffsetHours);
            var prevMonth = localTime.AddMonths(-1);

            periodStart = new DateTime(prevMonth.Year, prevMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            periodEnd = periodStart.AddMonths(1);
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var photoService = scope.ServiceProvider.GetRequiredService<PhotoService>();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();

        logger.LogInformation("MONTHLY STATS TEST | SourceChat[{SourceChatId}] -> TargetChat[{TargetChatId}] | Period {Start:yyyy-MM-dd} - {End:yyyy-MM-dd}",
            statisticsChatId, sendToChatId, periodStart, periodEnd);

        return await SendWinnersCongratulationsAsync(
            context,
            photoService,
            userService,
            sendToChatId,
            statisticsChatId,
            periodStart,
            periodEnd);
    }


    private async Task<MonthlyWinnersResult> SendWinnersCongratulationsAsync(
        AppDbContext context,
        PhotoService photoService,
        UserService userService,
        long sendToId,
        long chatId,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new MonthlyWinnersResult
        {
            SourceChatId = chatId,
            TargetChatId = sendToId,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        try
        {
            var monthName = startDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
            
            var topPhoto = await photoService.GetWinningPhotoAsync(chatId, startDate, endDate);
            var topPublisher = await userService.GetTopPublisherAsync(chatId, startDate, endDate);
            var topReactionReceiver = await userService.GetTopReactionReceiverAsync(chatId, startDate, endDate);
            
            if (topPhoto != null)
            {
                result.TopPhoto = new MonthlyWinnersResult.TopPhotoSummary
                {
                    PhotoId = topPhoto.Photo.Id,
                    MessageId = topPhoto.Photo.MessageId,
                    FileId = topPhoto.Photo.FileId,
                    ReactionCount = topPhoto.ReactionCount,
                    IsAlbum = topPhoto.Photo.MediaGroupId.HasValue,
                    AuthorUsername = topPhoto.Photo.User.Username,
                    AuthorFirstName = topPhoto.Photo.User.FirstName
                };

                await SendWinningPhotoAsync(context, sendToId, chatId, topPhoto, monthName);
            }

            if (topPublisher != null)
            {
                result.TopPublisher = new MonthlyWinnersResult.TopPublisherSummary
                {
                    Username = topPublisher.Username,
                    FirstName = topPublisher.FirstName,
                    PhotoCount = topPublisher.PhotoCount
                };

                await SendTopPublisherAsync(sendToId, topPublisher, monthName);
            }

            if (topReactionReceiver != null)
            {
                result.TopReactionReceiver = new MonthlyWinnersResult.TopReactionReceiverSummary
                {
                    Username = topReactionReceiver.Username,
                    FirstName = topReactionReceiver.FirstName,
                    ReactionCount = topReactionReceiver.ReactionCount,
                    PhotoCount = topReactionReceiver.PhotoCount
                };

                await SendTopReactionReceiverAsync(sendToId, topReactionReceiver, monthName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send winners congratulations for chat {ChatId} | SENT TO {sendToId}", chatId, sendToId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task SendWinningPhotoAsync(AppDbContext context, long sendToId, long chatId, TopPhotoResult topPhoto, string monthName)
    {
        try
        {
            var photo = topPhoto.Photo;
            
            var authorName = !string.IsNullOrEmpty(photo.User.Username) ? $"@{photo.User.Username}" : 
                            !string.IsNullOrEmpty(photo.User.FirstName) ? photo.User.FirstName : "Пользователь";

            var chatIdForLink = GenerateChatIdFromExtended(chatId);
            
            var congratsMessage = $"🏆 <b><a href=\"https://t.me/c/{chatIdForLink}/{photo.MessageId}\">ФОТО МЕСЯЦА</a></b> <i>{monthName}</i>\n\n" +
                                 $"Автор: {authorName}\n" +
                                 $"Это фото получило больше всего реакций! А именно - <b>{topPhoto.ReactionCount}!</b>";

            if (photo.MediaGroupId.HasValue)
            {
                // Если это часть альбома, отправляем все фото из группы
                var mediaGroupPhotos = await context.Photos
                    .Where(p => p.MediaGroupId == photo.MediaGroupId)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                if (mediaGroupPhotos.Count > 1)
                {
                    congratsMessage = $"🏆 <b><a href=\"https://t.me/c/{chatIdForLink}/{photo.MessageId}\">ФОТО МЕСЯЦА</a></b> <i>{monthName}</i>\n\n" +
                                      $"Автор: {authorName}\n" +
                                      $"Эти фото получили больше всего реакций! А именно - <b>{topPhoto.ReactionCount}!</b>";
                }
                
                // Затем отправляем все фото медиа-группы одним сообщением
                var mediaGroup = new List<IAlbumInputMedia>();

                var mediaGroupIndex = 0;
                
                foreach (var groupPhoto in mediaGroupPhotos)
                {
                    if (mediaGroupIndex == 0)
                    {
                        mediaGroup.Add(new InputMediaPhoto(groupPhoto.FileId)
                        {
                            Caption = congratsMessage,
                            ParseMode = ParseMode.Html
                        });
                    }
                    else
                    {
                        mediaGroup.Add(new InputMediaPhoto(groupPhoto.FileId));
                    }
                    mediaGroupIndex++;
                }
                
                await botClient.SendMediaGroup(sendToId, mediaGroup);
            }
            else
            {
                // Отправляем одиночное фото
                await botClient.SendPhoto(sendToId, photo.FileId, caption: congratsMessage, parseMode: ParseMode.Html);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send winning photo for chat {ChatId}", chatId);
        }
    }
    
    private async Task SendTopPublisherAsync(long sendToId, TopPublisherResult topPublisher, string monthName)
    {
        var publisherName = !string.IsNullOrEmpty(topPublisher.Username) ? $"@{topPublisher.Username}" : 
            !string.IsNullOrEmpty(topPublisher.FirstName) ? topPublisher.FirstName : "Пользователь";
                
        var congratsMessage = $"📷 <b>ФОТОПУЛЕМЁТ - {publisherName}!</b>\n<i>{monthName}</i>\n\n" +
                              $"Опубликовал <b>{topPublisher.PhotoCount} фотографий</b>\n" +
                              $"<i>Спасибо за активность!</i>";
                
        await botClient.SendMessage(sendToId, congratsMessage, parseMode: ParseMode.Html);
    }
    
    private async Task SendTopReactionReceiverAsync(long sendToId, TopReactionReceiver topReactionReceiver, string monthName)
    {
        var receiverName = !string.IsNullOrEmpty(topReactionReceiver.Username) ? $"@{topReactionReceiver.Username}" : 
            !string.IsNullOrEmpty(topReactionReceiver.FirstName) ? topReactionReceiver.FirstName : "Пользователь";

        var congratsMessage = $"❤️ <b>КОЛЛЕКЦИОНЕР РЕАКЦИЙ - {receiverName}!</b>\n<i>{monthName}</i>\n\n" +
                              $"Получил больше всего реакций : <b>{topReactionReceiver.ReactionCount} шт.</b>\n" +
                              $"Опубликовав <b>{topReactionReceiver.PhotoCount}</b> фотографий!";
                
        await botClient.SendMessage(sendToId, congratsMessage, parseMode: ParseMode.Html);
    }
    
    private static string GenerateChatIdFromExtended(long chatId)
    {
        var linkChatId = Math.Abs(chatId).ToString();
        if (linkChatId.StartsWith("100"))
        {
            linkChatId = linkChatId[3..];
        }
        return linkChatId;
    }
}
