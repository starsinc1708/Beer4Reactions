using Microsoft.EntityFrameworkCore;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.Models;
using Telegram.Bot.Types;

namespace Beer4Reactions.BotLogic.Services;

public class ReactionService(AppDbContext context, IServiceProvider serviceProvider, ILogger<ReactionService> logger)
{
    public async Task HandleReactionAsync(MessageReactionUpdated reactionUpdate, CancellationToken cancellationToken)
    {
        var chatId = reactionUpdate.Chat.Id;
        var messageId = reactionUpdate.MessageId;
        var user = reactionUpdate.User;

        if (user == null) return;

        // Находим фото по сообщению
        var photo = await context.Photos
            .Include(p => p.MediaGroup)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.MessageId == messageId, 
                cancellationToken: cancellationToken);

        if (photo == null) return;

        // Получаем или создаем пользователя
        using var scope = serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        
        var dbUser = await userService.GetOrCreateUserAsync(user, chatId, cancellationToken);

        // Обрабатываем новые и старые реакции
        await ProcessReactionChangesAsync(reactionUpdate.OldReaction, reactionUpdate.NewReaction, 
            dbUser, photo, chatId);
    }

    private async Task ProcessReactionChangesAsync(
        IEnumerable<ReactionType> oldReactions,
        IEnumerable<ReactionType> newReactions,
        Models.User user,
        Photo photo,
        long chatId)
    {
        var oldReactionTypes = oldReactions.Select(GetReactionTypeString).ToHashSet();
        var newReactionTypes = newReactions.Select(GetReactionTypeString).ToHashSet();

        // Удаляем старые реакции
        var reactionsToRemove = oldReactionTypes.Except(newReactionTypes);
        foreach (var reactionType in reactionsToRemove)
        {
            await RemoveReactionAsync(user, photo, reactionType, chatId);
        }

        // Добавляем новые реакции
        var reactionsToAdd = newReactionTypes.Except(oldReactionTypes);
        foreach (var reactionType in reactionsToAdd)
        {
            await AddReactionAsync(user, photo, reactionType, chatId);
        }
    }

    private async Task AddReactionAsync(Models.User user, Photo photo, string reactionType, long chatId)
    {
        // Если фото является частью группы, реакция применяется к группе
        if (photo.MediaGroupId.HasValue)
        {
            var existingGroupReaction = await context.Reactions
                .FirstOrDefaultAsync(r => r.UserId == user.Id && 
                                         r.MediaGroupId == photo.MediaGroupId && 
                                         r.Type == reactionType);

            if (existingGroupReaction == null)
            {
                var groupReaction = new Reaction
                {
                    Type = reactionType,
                    UserId = user.Id,
                    ChatId = chatId,
                    MediaGroupId = photo.MediaGroupId
                };
                context.Reactions.Add(groupReaction);
            }
        }
        // Реакция на отдельное фото
        else
        {
            
            var existingPhotoReaction = await context.Reactions
                .FirstOrDefaultAsync(r => r.UserId == user.Id && 
                                         r.PhotoId == photo.Id && 
                                         r.Type == reactionType);

            if (existingPhotoReaction == null)
            {
                var photoReaction = new Reaction
                {
                    Type = reactionType,
                    UserId = user.Id,
                    ChatId = chatId,
                    PhotoId = photo.Id
                };
                context.Reactions.Add(photoReaction);
            }
        }

        await context.SaveChangesAsync();
        
        var mediaGroupInfo = photo.MediaGroupId.HasValue ? $" | MediaGroup[{photo.MediaGroupId}]" : "";
        logger.LogInformation("CHAT[{ChatId}] | REACTION SAVED | [{ReactionType}] from [{Username}] to message [{MessageId}] | Photo by [{PhotoAuthor}]{MediaGroupInfo}",
            chatId, reactionType, user.Username ?? user.FirstName, photo.MessageId, photo.User.Username ?? photo.User.FirstName ?? "Unknown", mediaGroupInfo);
    }

    private async Task RemoveReactionAsync(Models.User user, Photo photo, string reactionType, long chatId)
    {
        Reaction? reactionToRemove;

        if (photo.MediaGroupId.HasValue)
        {
            reactionToRemove = await context.Reactions
                .FirstOrDefaultAsync(r => r.UserId == user.Id && 
                                         r.MediaGroupId == photo.MediaGroupId && 
                                         r.Type == reactionType);
        }
        else
        {
            reactionToRemove = await context.Reactions
                .FirstOrDefaultAsync(r => r.UserId == user.Id && 
                                         r.PhotoId == photo.Id && 
                                         r.Type == reactionType);
        }

        if (reactionToRemove != null)
        {
            context.Reactions.Remove(reactionToRemove);
            await context.SaveChangesAsync();
            
            var mediaGroupInfo = photo.MediaGroupId.HasValue ? $" | MediaGroup[{photo.MediaGroupId}]" : "";
            logger.LogInformation("CHAT[{ChatId}] | REACTION REMOVED | [{ReactionType}] from [{Username}] to message [{MessageId}] | Photo by [{PhotoAuthor}]{MediaGroupInfo}",
                chatId, reactionType, user.Username ?? user.FirstName, photo.MessageId, photo.User.Username ?? photo.User.FirstName ?? "Unknown", mediaGroupInfo);
        }
    }

    private static string GetReactionTypeString(ReactionType reactionType)
    {
        return reactionType switch
        {
            ReactionTypeEmoji emoji => emoji.Emoji,
            ReactionTypeCustomEmoji customEmoji => customEmoji.CustomEmojiId,
            _ => "unknown"
        };
    }
    
    public async Task<(string ReactionType, int UsageCount)?> GetTopReactionAsync(long chatId, DateTime startDate, DateTime endDate)
    {
        return await context.Reactions
            .Where(r => r.ChatId == chatId && r.CreatedAt >= startDate && r.CreatedAt < endDate)
            .GroupBy(r => r.Type)
            .Select(g => new
            {
                ReactionType = g.Key,
                UsageCount = g.Count()
            })
            .OrderByDescending(x => x.UsageCount)
            .Select(x => new { x.ReactionType, x.UsageCount })
            .FirstOrDefaultAsync()
            .ContinueWith(t => t.Result != null ? ((string ReactionType, int UsageCount)?)(t.Result.ReactionType, t.Result.UsageCount) : null);
    }
}
