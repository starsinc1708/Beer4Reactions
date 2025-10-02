using Microsoft.EntityFrameworkCore;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.DTOs;
using User = Telegram.Bot.Types.User;

namespace Beer4Reactions.BotLogic.Services;

public class UserService(AppDbContext context)
{
    public async Task<Models.User> GetOrCreateUserAsync(User telegramUser, long chatId,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUser.Id 
                                      && u.ChatId == chatId, 
                cancellationToken: cancellationToken);

        if (user == null)
        {
            user = new Models.User
            {
                TelegramUserId = telegramUser.Id,
                ChatId = chatId,
                Username = telegramUser.Username,
                FirstName = telegramUser.FirstName,
                LastName = telegramUser.LastName
            };
            context.Users.Add(user);
        }
        else
        {
            user.Username = telegramUser.Username;
            user.FirstName = telegramUser.FirstName;
            user.LastName = telegramUser.LastName;
            user.LastActiveAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return user;
    }
    
    public async Task<TopPublisherResult?> GetTopPublisherAsync(long chatId, DateTime startDate, DateTime endDate)
    {
        var result = await context.Users
            .Where(u => u.ChatId == chatId)
            .Select(u => new TopPublisherResult
            {
                Username = u.Username ?? "username",
                FirstName = u.FirstName ?? "unknown",
                PhotoCount = u.Photos.Count(p => p.CreatedAt >= startDate && p.CreatedAt < endDate)
            })
            .Where(x => x.PhotoCount > 0)
            .OrderByDescending(x => x.PhotoCount)
            .FirstOrDefaultAsync();

        return result ?? null;
    }

    public async Task<TopReactionReceiver?> GetTopReactionReceiverAsync(
        long chatId, 
        DateTime startDate,
        DateTime endDate)
    {
        // Получаем реакции на отдельные фото (не в группах)
        var singlePhotoReactions = await context.Users
            .Where(u => u.ChatId == chatId)
            .Select(u => new
            {
                UserId = u.Id,
                Username = u.Username ?? "",
                FirstName = u.FirstName ?? "",
                PhotoCount = u.Photos.Count(p => p.CreatedAt >= startDate && p.CreatedAt < endDate),
                SinglePhotoReactionCount = u.Photos
                    .Where(p => p.CreatedAt >= startDate && p.CreatedAt < endDate && p.MediaGroupId == null)
                    .SelectMany(p => p.Reactions.Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate && r.User.TelegramUserId != u.TelegramUserId))
                    .Count()
            })
            .ToListAsync();

        // Получаем реакции на MediaGroup отдельно
        var mediaGroupReactions = await context.MediaGroups
            .Where(mg => mg.ChatId == chatId && mg.CreatedAt >= startDate && mg.CreatedAt < endDate)
            .Select(mg => new
            {
                UserId = mg.Photos.First().UserId,
                GroupReactionCount = mg.GroupReactions
                    .Count(r => r.CreatedAt >= startDate && r.CreatedAt < endDate && r.User.TelegramUserId != mg.Photos.First().User.TelegramUserId)
            })
            .ToListAsync();

        // Объединяем данные
        var result = singlePhotoReactions
            .GroupJoin(mediaGroupReactions, 
                single => single.UserId, 
                group => group.UserId, 
                (single, groups) => new TopReactionReceiver
                {
                    UserId = single.UserId,
                    Username = single.Username,
                    FirstName = single.FirstName,
                    PhotoCount = single.PhotoCount,
                    ReactionCount = single.SinglePhotoReactionCount + groups.Sum(g => g.GroupReactionCount)
                })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .FirstOrDefault();

        return result ?? null;
    }

    public async Task<(int UserId, int ReactionCount)?> GetTopUserAsync(long chatId, DateTime startDate, DateTime endDate)
    {
        return await context.Users
            .Where(u => u.ChatId == chatId)
            .Select(u => new
            {
                UserId = u.Id,
                ReactionCount = u.Photos
                                    .Where(p => p.CreatedAt >= startDate && p.CreatedAt < endDate)
                                    .SelectMany(p => p.Reactions)
                                    .Count(r => r.CreatedAt >= startDate && r.CreatedAt < endDate) +
                                context.Reactions
                                    .Count(r => r.MediaGroup != null && 
                                                r.MediaGroup.Photos.Any(p => p.UserId == u.Id) &&
                                                r.CreatedAt >= startDate && r.CreatedAt < endDate)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Select(x => new { x.UserId, x.ReactionCount })
            .FirstOrDefaultAsync()
            .ContinueWith(t => t.Result != null ? ((int UserId, int ReactionCount)?)(t.Result.UserId, t.Result.ReactionCount) : null);
    }
}

