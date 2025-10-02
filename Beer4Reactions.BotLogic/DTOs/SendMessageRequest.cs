using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.DTOs;

public class SendMessageRequest
{
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    [StringLength(4096, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;

    public bool DisableWebPagePreview { get; set; } = false;
    
    public bool DisableNotification { get; set; } = false;
}

public class EditMessageRequest
{
    [Required]
    public long ChatId { get; set; }
    
    [Required]
    [StringLength(4096, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;

    public bool DisableWebPagePreview { get; set; } = false;
}

public class PinMessageRequest
{
    [Required]
    public long ChatId { get; set; }
    
    public bool DisableNotification { get; set; } = false;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    
    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}
