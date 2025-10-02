using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.Models;

public sealed class User
{
    public int Id { get; set; }
    
    [Required]
    public long TelegramUserId { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    public string? Username { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}
