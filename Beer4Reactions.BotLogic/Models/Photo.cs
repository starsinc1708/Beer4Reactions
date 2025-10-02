using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.Models;

public class Photo
{
    public int Id { get; set; }
    
    [Required]
    public string FileId { get; set; } = string.Empty;
    
    public string? FileUniqueId { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    public long MessageId { get; set; }
    
    public int? MediaGroupId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public string? Caption { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Размеры фото
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual MediaGroup? MediaGroup { get; set; }
    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}
