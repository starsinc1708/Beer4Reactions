using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.Models;

public sealed class MediaGroup
{
    public int Id { get; set; }
    
    [Required]
    public string MediaGroupId { get; set; } = string.Empty;
    
    [Required]
    public long ChatId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Reaction> GroupReactions { get; set; } = new List<Reaction>();
}
