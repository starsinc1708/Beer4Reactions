using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.Models;
using System.Text;
using Beer4Reactions.BotLogic.Configuration;
using Microsoft.Extensions.Options;

namespace Beer4Reactions.BotLogic.Services;

public class StatisticsService(
    AppDbContext context, 
    IOptions<BotSettings> botSettings,
    ILogger<StatisticsService> logger)
{
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(2);
    private readonly BotSettings _settings = botSettings.Value;
    
    public async Task<string> GenerateCurrentStatisticsAsync(long chatId)
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        logger.LogDebug("Generating fresh statistics for chat {ChatId} for period {Start} - {End}", 
            chatId, startDate, now);

        // Топ фото по реакциям
        var topPhotos = await GetTopPhotosWithLinksAsync(chatId, startDate, now, 10);
        
        // Топ пользователи по количеству реакций на их контент
        var topUsers = await GetTopUsersByReactionsAsync(chatId, startDate, now, 15);
        
        // Топ реакции по использованию
        var topReactions = await GetTopReactionsAsync(chatId, startDate, now, 25);

        var result = FormatCurrentStatisticsMessage(topPhotos, topUsers, topReactions, chatId);

        return result;
    }

    private async Task<List<(Photo Photo, int ReactionCount)>> GetTopPhotosAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        return await context.Photos
            .Where(p => p.ChatId == chatId && p.MediaGroupId == null)
            .Select(p => new
            {
                Photo = p,
                ReactionCount = p.Reactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Take(limit)
            .Select(x => new ValueTuple<Photo, int>(x.Photo, x.ReactionCount))
            .ToListAsync();
    }

    private async Task<List<(MediaGroup MediaGroup, int ReactionCount)>> GetTopAlbumsAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        return await context.MediaGroups
            .Where(mg => mg.ChatId == chatId)
            .Select(mg => new
            {
                MediaGroup = mg,
                ReactionCount = mg.GroupReactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Take(limit)
            .Select(x => new ValueTuple<MediaGroup, int>(x.MediaGroup, x.ReactionCount))
            .ToListAsync();
    }

    private async Task<List<(User User, int ReactionCount)>> GetTopUsersAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        return await context.Users
            .Where(u => u.ChatId == chatId)
            .Select(u => new
            {
                User = u,
                ReactionCount = u.Photos
                    .SelectMany(p => p.Reactions)
                    .Count(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate) +
                                context.Reactions
                                    .Count(r => r.MediaGroup != null && 
                                                r.MediaGroup.Photos.Any(p => p.UserId == u.Id) &&
                                                r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Take(limit)
            .Select(x => new ValueTuple<User, int>(x.User, x.ReactionCount))
            .ToListAsync();
    }

    private async Task<List<(string ReactionType, int UsageCount)>> GetTopReactionTypesAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        return await context.Reactions
            .Where(r => r.ChatId == chatId && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .GroupBy(r => r.Type)
            .Select(g => new
            {
                ReactionType = g.Key,
                UsageCount = g.Count()
            })
            .OrderByDescending(x => x.UsageCount)
            .Take(limit)
            .Select(x => new ValueTuple<string, int>(x.ReactionType, x.UsageCount))
            .ToListAsync();
    }

    private string FormatCurrentStatisticsMessage(
        List<(long MessageId, int ReactionCount)> topPhotos,
        List<(string Username, string FirstName, int ReactionCount, int PhotoCount)> topUsers,
        List<(string Type, int Count)> topReactions,
        long chatId)
    {
        var now = DateTime.UtcNow;
        var currentMonth = now.AddHours(_settings.TimezoneOffsetHours).ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
        var header = $"<b>СТАТИСТИКА ЧАТА</b> ❗️\n<i>за {currentMonth}</i>\n";
        var footerText = $"<i>Последнее обновление: {now + TimeSpan.FromHours(_settings.TimezoneOffsetHours):HH:mm dd/MM/yyyy}</i>";

        var messageBuilder = new StringBuilder(header);
        
        messageBuilder.AppendLine("<blockquote expandable><b>Эти фото</b> <s>почти</s> <b>никого не оставили равнодушным:</b>");
        if (topPhotos.Count > 0)
        {
            for (var i = 0; i < topPhotos.Count; i++)
            {
                var photo = topPhotos[i];
                var chatIdForLink = GenerateChatIdForLink(chatId);
                messageBuilder.AppendLine(
                    $"{i + 1}. <a href=\"https://t.me/c/{chatIdForLink}/{photo.MessageId}\">Фото</a> - {photo.ReactionCount} шт.");
            }
        }
        else
        {
            messageBuilder.AppendLine("Нет данных о фотографиях");
        }
        messageBuilder.AppendLine("</blockquote>");
        
        messageBuilder.AppendLine("<blockquote expandable><b>На их фото реагировали больше всего:</b>");
        var filteredUsers = topUsers.Where(us => us.PhotoCount > 0).ToList();
        if (filteredUsers.Count > 0)
        {
            for (var i = 0; i < filteredUsers.Count; i++)
            {
                var user = filteredUsers[i];
                var displayName = !string.IsNullOrEmpty(user.Username) ? $"@{user.Username}" : 
                                 !string.IsNullOrEmpty(user.FirstName) ? user.FirstName : "Пользователь";
                
                messageBuilder.AppendLine(
                    $"{i + 1}. {displayName} - {user.ReactionCount} шт. ({user.PhotoCount} фото)");
            }
        }
        else
        {
            messageBuilder.AppendLine("Нет данных о пользователях");
        }
        messageBuilder.AppendLine("</blockquote>");
        
        messageBuilder.AppendLine("<blockquote expandable><b>Самые популярные реакции:</b>");
        if (topReactions.Count > 0)
        {
            for (var i = 0; i < topReactions.Count; i++)
            {
                var reaction = topReactions[i];
                messageBuilder.AppendLine($"{i + 1}. {reaction.Type} - {reaction.Count} шт.");
            }
        }
        else
        {
            messageBuilder.AppendLine("Нет данных о реакциях");
        }
        messageBuilder.AppendLine("</blockquote>");
        
        messageBuilder.AppendLine(footerText);        
        
        return messageBuilder.ToString();
    }

    private async Task<List<(long MessageId, int ReactionCount)>> GetTopPhotosWithLinksAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        // Сначала получаем топ MediaGroup (если есть)
        var topMediaGroups = await context.MediaGroups
            .Where(mg => mg.ChatId == chatId && mg.CreatedAt >= startDate && mg.CreatedAt <= endDate)
            .Select(mg => new
            {
                MessageId = mg.Photos.OrderBy(p => p.CreatedAt).First().MessageId,
                MediaGroupId = (int?)mg.Id,
                ReactionCount = mg.GroupReactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.User.TelegramUserId != mg.Photos.First().User.TelegramUserId)
            })
            .Where(x => x.ReactionCount > 0)
            .ToListAsync();

        // Затем получаем топ отдельных фото (не в группах)
        var topSinglePhotos = await context.Photos
            .Where(p => p.ChatId == chatId && p.MediaGroupId == null && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .Select(p => new
            {
                MessageId = p.MessageId,
                MediaGroupId = (int?)null,
                ReactionCount = p.Reactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.User.TelegramUserId != p.User.TelegramUserId)
            })
            .Where(x => x.ReactionCount > 0)
            .ToListAsync();

        // Объединяем и сортируем
        var allResults = topMediaGroups.Concat(topSinglePhotos)
            .OrderByDescending(x => x.ReactionCount)
            .Take(limit)
            .Select(x => (x.MessageId, x.ReactionCount))
            .ToList();

        return allResults;
    }

    private async Task<List<(string Username, string FirstName, int ReactionCount, int PhotoCount)>> GetTopUsersByReactionsAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        // Получаем реакции на отдельные фото (не в группах)
        var singlePhotoReactions = await context.Users
            .Where(u => u.ChatId == chatId)
            .Select(u => new
            {
                UserId = u.Id,
                Username = u.Username ?? "",
                FirstName = u.FirstName ?? "",
                PhotoCount = u.Photos.Count(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate),
                SinglePhotoReactionCount = u.Photos
                    .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate && p.MediaGroupId == null)
                    .SelectMany(p => p.Reactions.Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.User.TelegramUserId != u.TelegramUserId))
                    .Count()
            })
            .ToListAsync();

        // Получаем реакции на MediaGroup отдельно
        var mediaGroupReactions = await context.MediaGroups
            .Where(mg => mg.ChatId == chatId && mg.CreatedAt >= startDate && mg.CreatedAt <= endDate)
            .Select(mg => new
            {
                UserId = mg.Photos.First().UserId,
                GroupReactionCount = mg.GroupReactions
                    .Count(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.User.TelegramUserId != mg.Photos.First().User.TelegramUserId)
            })
            .ToListAsync();

        // Объединяем данные
        var userStats = singlePhotoReactions
            .GroupJoin(mediaGroupReactions, 
                single => single.UserId, 
                group => group.UserId, 
                (single, groups) => new
                {
                    single.Username,
                    single.FirstName,
                    single.PhotoCount,
                    ReactionCount = single.SinglePhotoReactionCount + groups.Sum(g => g.GroupReactionCount)
                })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Take(limit)
            .Select(x => (x.Username, x.FirstName, x.ReactionCount, x.PhotoCount))
            .ToList();

        return userStats;
    }

    private async Task<List<(string Type, int Count)>> GetTopReactionsAsync(
        long chatId, DateTime startDate, DateTime endDate, int limit)
    {
        var results = await context.Reactions
            .Where(r => r.ChatId == chatId 
                        && r.CreatedAt >= startDate 
                        && r.CreatedAt <= endDate
                        && r.Type.Length < 4)
            .GroupBy(r => r.Type)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();
            
        return results.Select(x => (x.Type, x.Count)).ToList();
    }

    private static string GenerateChatIdForLink(long chatId)
    {
        // Убираем префикс -100 из chatId для ссылки
        var linkChatId = Math.Abs(chatId).ToString();
        if (linkChatId.StartsWith("100"))
        {
            linkChatId = linkChatId[3..];
        }
        return linkChatId;
    }
}
