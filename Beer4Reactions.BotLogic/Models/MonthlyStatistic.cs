using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.Models;

public sealed class MonthlyStatistic
{
    public int Id { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    public int Year { get; set; }
    
    [Required]
    public int Month { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Топ фото месяца
    public int? TopPhotoId { get; set; }
    public int TopPhotoReactionCount { get; set; }
    
    // Топ альбом месяца
    public int? TopMediaGroupId { get; set; }
    public int TopMediaGroupReactionCount { get; set; }
    
    // Топ пользователь месяца (по количеству реакций на его контент)
    public int? TopUserId { get; set; }
    public int TopUserReactionCount { get; set; }
    
    // Топ реакция месяца (самая используемая)
    public string? TopReactionType { get; set; }
    public int TopReactionUsageCount { get; set; }
    
    // Общая статистика
    public int TotalPhotos { get; set; }
    public int TotalMediaGroups { get; set; }
    public int TotalReactions { get; set; }
    public int TotalActiveUsers { get; set; }
    
    // Navigation properties
    public Photo? TopPhoto { get; set; }
    public MediaGroup? TopMediaGroup { get; set; }
    public User? TopUser { get; set; }
}
