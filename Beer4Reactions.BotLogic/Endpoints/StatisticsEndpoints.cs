using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Beer4Reactions.BotLogic.DTOs;
using Beer4Reactions.BotLogic.Services;
using Beer4Reactions.BotLogic.BackgroundServices;
using Beer4Reactions.BotLogic.Data;

namespace Beer4Reactions.BotLogic.Endpoints;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/statistics").WithTags("Statistics");

        group.MapGet("/monthly/{chatId:long}", GetMonthlyStatisticsAsync)
            .WithName("GetMonthlyStatistics")
            .WithSummary("Get stored monthly statistics");

        group.MapGet("/current/{chatId:long}", GetCurrentStatisticsAsync)
            .WithName("GetCurrentStatistics")
            .WithSummary("Generate current statistics snapshot");

        group.MapPost("/monthly/test", SendMonthlyWinnersTestAsync)
            .WithName("SendMonthlyWinnersTest")
            .WithSummary("Trigger test announcement for monthly winners");
    }

    
    private static async Task<IResult> GetMonthlyStatisticsAsync(
        long chatId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromServices] AppDbContext context,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(chatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            var statistics = await context.MonthlyStatistics
                .Include(ms => ms.TopPhoto)
                    .ThenInclude(p => p!.User)
                .Include(ms => ms.TopMediaGroup)
                .Include(ms => ms.TopUser)
                .Where(ms => ms.ChatId == chatId && ms.Year == targetYear && ms.Month == targetMonth)
                .OrderByDescending(ms => ms.CreatedAt)
                .ToListAsync();

            return Results.Ok(ApiResponse<object>.Ok(statistics.Select(s => new 
            { 
                Id = s.Id,
                ChatId = s.ChatId,
                Year = s.Year,
                Month = s.Month,
                TopPhoto = s.TopPhoto != null ? new 
                {
                    Id = s.TopPhoto.Id,
                    FileId = s.TopPhoto.FileId,
                    Author = s.TopPhoto.User.FirstName ?? s.TopPhoto.User.Username,
                    ReactionCount = s.TopPhotoReactionCount
                } : null,
                TopUser = s.TopUser != null ? new 
                {
                    Id = s.TopUser.Id,
                    Name = s.TopUser.FirstName ?? s.TopUser.Username,
                    ReactionCount = s.TopUserReactionCount
                } : null,
                TopReaction = !string.IsNullOrEmpty(s.TopReactionType) ? new 
                {
                    Type = s.TopReactionType,
                    UsageCount = s.TopReactionUsageCount
                } : null,
                TotalPhotos = s.TotalPhotos,
                TotalMediaGroups = s.TotalMediaGroups,
                TotalReactions = s.TotalReactions,
                TotalActiveUsers = s.TotalActiveUsers,
                CreatedAt = s.CreatedAt
            })));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error getting statistics: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetCurrentStatisticsAsync(
        long chatId,
        [FromServices] StatisticsService statisticsService,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(chatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            var statisticsText = await statisticsService.GenerateCurrentStatisticsAsync(chatId);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                ChatId = chatId,
                StatisticsText = statisticsText,
                GeneratedAt = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error getting current statistics: {ex.Message}"));
        }
    }

    private static async Task<IResult> SendMonthlyWinnersTestAsync(
        [FromBody] TestMonthlyWinnersRequest request,
        [FromServices] MonthlyStatisticsService monthlyStatisticsService,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(request.SourceChatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Source chat not allowed"));
            }

            if (!chatValidation.IsChatAllowed(request.TargetChatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Target chat not allowed"));
            }

            var result = await monthlyStatisticsService.SendWinnersTestAsync(
                request.SourceChatId,
                request.TargetChatId,
                request.Year,
                request.Month,
                request.StartDateUtc,
                request.EndDateUtc);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                result.SourceChatId,
                result.TargetChatId,
                result.PeriodStart,
                result.PeriodEnd,
                result.TopPhoto,
                result.TopPublisher,
                result.TopReactionReceiver,
                result.HasErrors,
                result.ErrorMessage
            }));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error sending test winners: {ex.Message}"));
        }
    }

}
