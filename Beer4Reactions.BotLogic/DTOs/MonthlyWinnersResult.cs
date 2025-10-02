using System;

namespace Beer4Reactions.BotLogic.DTOs;

public class MonthlyWinnersResult
{
    public long SourceChatId { get; set; }
    public long TargetChatId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public TopPhotoSummary? TopPhoto { get; set; }
    public TopPublisherSummary? TopPublisher { get; set; }
    public TopReactionReceiverSummary? TopReactionReceiver { get; set; }
    public string? ErrorMessage { get; set; }
    public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);

    public sealed class TopPhotoSummary
    {
        public int PhotoId { get; set; }
        public long MessageId { get; set; }
        public string FileId { get; set; } = string.Empty;
        public int ReactionCount { get; set; }
        public bool IsAlbum { get; set; }
        public string? AuthorUsername { get; set; }
        public string? AuthorFirstName { get; set; }
    }

    public sealed class TopPublisherSummary
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public int PhotoCount { get; set; }
    }

    public sealed class TopReactionReceiverSummary
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public int ReactionCount { get; set; }
        public int PhotoCount { get; set; }
    }
}
