using Microsoft.EntityFrameworkCore;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.DTOs;
using Beer4Reactions.BotLogic.Models;
using Telegram.Bot.Types;
using User = Beer4Reactions.BotLogic.Models.User;

namespace Beer4Reactions.BotLogic.Services;

public class PhotoService(AppDbContext context, ILogger<PhotoService> logger)
{
    public async Task SavePhotoAsync(Message message, User user, CancellationToken cancellationToken)
    {
        if (message.Photo == null || message.Photo.Length == 0)
        {
            logger.LogError("Message does not contain photo");
            return;
        }

        var telegramPhoto = message.Photo.Last();
        
        var photo = new Photo
        {
            FileId = telegramPhoto.FileId,
            FileUniqueId = telegramPhoto.FileUniqueId,
            ChatId = message.Chat.Id,
            MessageId = message.MessageId,
            UserId = user.Id,
            Caption = message.Caption,
            Width = telegramPhoto.Width,
            Height = telegramPhoto.Height,
            FileSize = telegramPhoto.FileSize ?? 0L
        };

        if (!string.IsNullOrEmpty(message.MediaGroupId))
        {
            var mediaGroup = await GetOrCreateMediaGroupAsync(message.MediaGroupId, message.Chat.Id, cancellationToken);
            photo.MediaGroupId = mediaGroup.Id;
        }

        context.Photos.Add(photo);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<MediaGroup> GetOrCreateMediaGroupAsync(string mediaGroupId, long chatId,
        CancellationToken cancellationToken)
    {
        var mediaGroup = await context.MediaGroups
            .FirstOrDefaultAsync(mg => mg.MediaGroupId == mediaGroupId
                                       && mg.ChatId == chatId, 
                cancellationToken: cancellationToken);

        if (mediaGroup != null) return mediaGroup;
        
        mediaGroup = new MediaGroup
        {
            MediaGroupId = mediaGroupId,
            ChatId = chatId
        };
        
        context.MediaGroups.Add(mediaGroup);
        await context.SaveChangesAsync(cancellationToken);

        return mediaGroup;
    }

    public async Task<TopPhotoResult?> GetWinningPhotoAsync(long chatId, DateTime startDate, DateTime endDate)
    {
        return await context.Photos
            .Include(p => p.User)
            .Include(p => p.MediaGroup)
            .Where(p => p.ChatId == chatId && p.CreatedAt >= startDate && p.CreatedAt < endDate)
            .Select(p => new
            {
                Photo = p,
                ReactionCount = p.Reactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt < endDate && r.User.TelegramUserId != p.User.TelegramUserId) +
                                (p.MediaGroupId.HasValue ? 
                                    p.MediaGroup!.GroupReactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt < endDate && r.User.TelegramUserId != p.User.TelegramUserId) : 0)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Select(x => new TopPhotoResult
            {
                Photo = x.Photo,
                ReactionCount = x.ReactionCount,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(int PhotoId, int ReactionCount)?> GetTopPhotoAsync(long chatId, DateTime startDate, DateTime endDate)
    {
        return await context.Photos
            .Where(p => p.ChatId == chatId && p.MediaGroupId == null && 
                        p.CreatedAt >= startDate && p.CreatedAt < endDate)
            .Select(p => new
            {
                PhotoId = p.Id,
                ReactionCount = p.Reactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Select(x => new { x.PhotoId, x.ReactionCount })
            .FirstOrDefaultAsync()
            .ContinueWith(t => t.Result != null ? ((int PhotoId, int ReactionCount)?)(t.Result.PhotoId, t.Result.ReactionCount) : null);
    }
    
    public async Task<(int MediaGroupId, int ReactionCount)?> GetTopAlbumAsync(long chatId, DateTime startDate, DateTime endDate)
    {
        return await context.MediaGroups
            .Where(mg => mg.ChatId == chatId && mg.CreatedAt >= startDate && mg.CreatedAt < endDate)
            .Select(mg => new
            {
                MediaGroupId = mg.Id,
                ReactionCount = mg.GroupReactions.Count(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
            })
            .Where(x => x.ReactionCount > 0)
            .OrderByDescending(x => x.ReactionCount)
            .Select(x => new { x.MediaGroupId, x.ReactionCount })
            .FirstOrDefaultAsync()
            .ContinueWith(t => t.Result != null ? ((int MediaGroupId, int ReactionCount)?)(t.Result.MediaGroupId, t.Result.ReactionCount) : null);
    }
}
