using Beer4Reactions.BotLogic.Models;

namespace Beer4Reactions.BotLogic.DTOs;

public class TopPhotoResult
{
    public Photo Photo { get; set; }
    public int ReactionCount { get; set; }
}