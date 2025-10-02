using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Beer4Reactions.BotLogic.DTOs;
using Beer4Reactions.BotLogic.Services;

namespace Beer4Reactions.BotLogic.Endpoints;

public static class MessagesEndpoints
{
    public static void MapMessagesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/messages").WithTags("Messages");

        group.MapPost("/send", SendMessageAsync)
            .WithName("SendMessage")
            .WithSummary("Отправить сообщение в чат");

        group.MapPut("/edit/{messageId:long}", EditMessageAsync)
            .WithName("EditMessage")
            .WithSummary("Отредактировать сообщение");

        group.MapPost("/pin/{messageId:long}", PinMessageAsync)
            .WithName("PinMessage")
            .WithSummary("Закрепить сообщение");
    }

    private static async Task<IResult> SendMessageAsync(
        [FromBody] SendMessageRequest request,
        [FromServices] ITelegramBotClient botClient,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(request.ChatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            var message = await botClient.SendMessage(
                chatId: request.ChatId,
                text: request.Text,
                parseMode: ParseMode.Html,
                linkPreviewOptions: request.DisableWebPagePreview ? 
                    new Telegram.Bot.Types.LinkPreviewOptions { IsDisabled = true } : null,
                disableNotification: request.DisableNotification);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                MessageId = message.MessageId,
                ChatId = message.Chat.Id,
                Date = message.Date
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error sending message: {ex.Message}"));
        }
    }

    private static async Task<IResult> EditMessageAsync(
        long messageId,
        [FromBody] EditMessageRequest request,
        [FromServices] ITelegramBotClient botClient,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(request.ChatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            var editedMessage = await botClient.EditMessageText(
                chatId: request.ChatId,
                messageId: (int)messageId,
                text: request.Text,
                parseMode: ParseMode.Html,
                linkPreviewOptions: request.DisableWebPagePreview ? 
                    new Telegram.Bot.Types.LinkPreviewOptions { IsDisabled = true } : null);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                MessageId = editedMessage.MessageId,
                ChatId = editedMessage.Chat.Id,
                EditDate = editedMessage.EditDate
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error editing message: {ex.Message}"));
        }
    }

    private static async Task<IResult> PinMessageAsync(
        long messageId,
        [FromBody] PinMessageRequest request,
        [FromServices] ITelegramBotClient botClient,
        [FromServices] ChatValidationService chatValidation)
    {
        try
        {
            if (!chatValidation.IsChatAllowed(request.ChatId))
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Chat not allowed"));
            }

            await botClient.PinChatMessage(
                chatId: request.ChatId,
                messageId: (int)messageId,
                disableNotification: request.DisableNotification);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                MessageId = messageId,
                ChatId = request.ChatId,
                Pinned = true
            }));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Error pinning message: {ex.Message}"));
        }
    }
}
