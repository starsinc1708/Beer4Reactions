namespace Beer4Reactions.BotLogic.DTOs;

public class TopReactionReceiver
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public int PhotoCount { get; set; }
    public int SinglePhotoReactionCount { get; set; }
    public int ReactionCount { get; set; }
}