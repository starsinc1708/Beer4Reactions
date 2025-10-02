using System;
using System.ComponentModel.DataAnnotations;

namespace Beer4Reactions.BotLogic.DTOs;

public class TestMonthlyWinnersRequest
{
    [Required]
    public long SourceChatId { get; set; }

    [Required]
    public long TargetChatId { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }
}
