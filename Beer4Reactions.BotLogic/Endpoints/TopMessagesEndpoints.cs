using Microsoft.AspNetCore.Mvc;
using Beer4Reactions.BotLogic.DTOs;
using Beer4Reactions.BotLogic.Services;

namespace Beer4Reactions.BotLogic.Endpoints;

public static class TopMessagesEndpoints
{
    public static void MapTopMessagesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/topmessages").WithTags("TopMessages");

        group.MapPost("/create/{chatId:long}", CreateTopMessageAsync)
            .WithName("CreateTopMessage")
            .WithSummary("Создать новое TopMessage в чате");

        group.MapPut("/update/{chatId:long}", UpdateTopMessageAsync)
            .WithName("UpdateTopMessage")
            .WithSummary("Обновить активное TopMessage в чате");

        group.MapGet("/active/{chatId:long}", GetActiveTopMessageAsync)
            .WithName("GetActiveTopMessage")
            .WithSummary("Получить активное TopMessage в чате");
    }

    private static async Task<IResult> CreateTopMessageAsync(
        long chatId,
        [FromServices] TopMessageService topMessageService,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(chatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            var topMessage = await topMessageService.CreateTopMessageAsync(chatId);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                Id = topMessage.Id,
                ChatId = topMessage.ChatId,
                MessageId = topMessage.MessageId,
                IsActive = topMessage.IsActive,
                CreatedAt = topMessage.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error creating top message: {ex.Message}"));
        }
    }

    private static async Task<IResult> UpdateTopMessageAsync(
        long chatId,
        [FromServices] TopMessageService topMessageService,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(chatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            await topMessageService.UpdateTopMessageAsync(chatId);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                ChatId = chatId,
                Updated = true,
                UpdatedAt = DateTime.UtcNow
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error updating top message: {ex.Message}"));
        }
    }

    private static async Task<IResult> GetActiveTopMessageAsync(
        long chatId,
        [FromServices] TopMessageService topMessageService,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(chatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            var topMessage = await topMessageService.GetActiveTopMessageAsync(chatId);

            if (topMessage == null)
            {
                return Results.NotFound(ApiResponse<object>.Fail("No active top message found"));
            }

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                Id = topMessage.Id,
                ChatId = topMessage.ChatId,
                MessageId = topMessage.MessageId,
                IsActive = topMessage.IsActive,
                CreatedAt = topMessage.CreatedAt,
                LastUpdatedAt = topMessage.LastUpdatedAt
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error getting top message: {ex.Message}"));
        }
    }
}
