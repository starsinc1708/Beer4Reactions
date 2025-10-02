using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.Models;

public class TopMessage
{
    public int Id { get; set; }
    
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    public long MessageId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Содержимое сообщения (кешируем для отслеживания изменений)
    public string? LastMessageContent { get; set; }
    
    // Статистика на момент последнего обновления
    public DateTime StatisticsPeriodStart { get; set; }
    public DateTime StatisticsPeriodEnd { get; set; }
    
    // Флаг для отслеживания, было ли сообщение удалено
    public bool IsDeleted { get; set; } = false;
}
