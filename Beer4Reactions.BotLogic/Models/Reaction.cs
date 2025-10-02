using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.Models;

public sealed class Reaction
{
    public int Id { get; set; }
    
    [Required]
    public string Type { get; set; } = string.Empty; // 👍, ❤️, 😂, 😮, 😢, 😡, etc.
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    // Реакция может быть на отдельное фото или на всю группу
    public int? PhotoId { get; set; }
    public int? MediaGroupId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Photo? Photo { get; set; }
    public MediaGroup? MediaGroup { get; set; }
}
