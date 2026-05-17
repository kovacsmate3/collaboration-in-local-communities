using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Conversations;

[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed partial class ConversationsController(
    AppDbContext db,
    IHubContext<ChatHub> chatHub,
    CosmosMessageService cosmosMessages) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> StartAsync(
        StartConversationRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .Include(t => t.SeekerProfile)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.SeekerProfileId == profile.Id)
        {
            return Problem(
                title: "Cannot Message Own Task",
                detail: "You cannot start a conversation on a task you posted.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var existing = await db.TaskConversations
            .Include(c => c.SeekerProfile)
            .Include(c => c.HelperProfile)
            .FirstOrDefaultAsync(
                c => c.TaskId == request.TaskId && c.HelperProfileId == profile.Id,
                cancellationToken);

        if (existing is not null)
        {
            return Ok(ToConversationResponse(existing, task.Title, profile.Id));
        }

        var conversation = new TaskConversation
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            SeekerProfileId = task.SeekerProfileId,
            HelperProfileId = profile.Id,
            CosmosConversationId = Guid.NewGuid().ToString(),
        };

        db.TaskConversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);

        conversation.SeekerProfile = task.SeekerProfile;
        conversation.HelperProfile = profile;

        return Ok(ToConversationResponse(conversation, task.Title, profile.Id));
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var rows = await db.TaskConversations
            .Where(c => c.SeekerProfileId == profile.Id || c.HelperProfileId == profile.Id)
            .Select(c => new
            {
                c.Id,
                c.TaskId,
                TaskTitle = c.Task.Title,
                c.SeekerProfileId,
                SeekerDisplayName = c.SeekerProfile.DisplayName,
                SeekerPhotoUrl = c.SeekerProfile.PhotoUrl,
                c.HelperProfileId,
                HelperDisplayName = c.HelperProfile.DisplayName,
                HelperPhotoUrl = c.HelperProfile.PhotoUrl,
                c.LastMessageContent,
                c.LastMessageAt,
                c.SeekerLastReadAt,
                c.HelperLastReadAt,
                c.CreatedAt,
            })
            .OrderByDescending(x => x.LastMessageAt.HasValue ? x.LastMessageAt.Value : x.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = rows.Select(r =>
        {
            var isSeeker = r.SeekerProfileId == profile.Id;
            var other = isSeeker
                ? new ParticipantInfo(r.HelperProfileId, r.HelperDisplayName, r.HelperPhotoUrl)
                : new ParticipantInfo(r.SeekerProfileId, r.SeekerDisplayName, r.SeekerPhotoUrl);

            var lastReadAt = isSeeker ? r.SeekerLastReadAt : r.HelperLastReadAt;
            var hasUnread = r.LastMessageAt.HasValue &&
                            (lastReadAt is null || r.LastMessageAt.Value > lastReadAt.Value);

            return new ConversationPreviewResponse(
                r.Id,
                r.TaskId,
                r.TaskTitle,
                other,
                r.LastMessageContent,
                r.LastMessageAt,
                hasUnread);
        });

        return Ok(result);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessagesAsync(
        Guid id,
        [FromQuery] DateTimeOffset? before,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        limit = limit <= 0 ? 50 : Math.Min(limit, 100);

        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var conversation = await db.TaskConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        if (conversation.SeekerProfileId != profile.Id && conversation.HelperProfileId != profile.Id)
        {
            return Forbid();
        }

        var (documents, hasMore) = await cosmosMessages.GetByConversationAsync(
            id.ToString(),
            before,
            limit,
            cancellationToken);

        var messages = documents.Select(d => new MessageResponse(
            Guid.Parse(d.Id),
            d.Content,
            d.SentAt,
            Guid.Parse(d.SenderProfileId),
            d.SenderDisplayName,
            IsMine: d.SenderProfileId == profile.Id.ToString()));

        return Ok(new MessagesPageResponse(messages, hasMore));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendAsync(
        Guid id,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var conversation = await db.TaskConversations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        if (conversation.SeekerProfileId != profile.Id && conversation.HelperProfileId != profile.Id)
        {
            return Forbid();
        }

        var document = new MessageDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = id.ToString(),
            SenderProfileId = profile.Id.ToString(),
            SenderDisplayName = profile.DisplayName,
            Content = request.Content.Trim(),
            SentAt = DateTimeOffset.UtcNow,
        };

        await cosmosMessages.AddAsync(document, cancellationToken);

        conversation.LastMessageContent = document.Content.Length > 500
            ? document.Content[..500]
            : document.Content;
        conversation.LastMessageAt = document.SentAt;

        if (conversation.SeekerProfileId == profile.Id)
        {
            conversation.SeekerLastReadAt = document.SentAt;
        }
        else
        {
            conversation.HelperLastReadAt = document.SentAt;
        }

        db.AddActivityEvent(
            profile.UserId,
            profile.Id,
            ActivityEventType.MessageSent,
            nameof(TaskConversation),
            conversation.Id,
            new { conversation.TaskId, MessageId = document.Id });
        await db.SaveChangesAsync(cancellationToken);

        var response = new MessageResponse(
            Guid.Parse(document.Id),
            document.Content,
            document.SentAt,
            profile.Id,
            profile.DisplayName,
            IsMine: true);

        var hubEvent = new HubMessageEvent(
            response.Id,
            response.Content,
            response.SentAt,
            response.SenderProfileId,
            response.SenderDisplayName);

        await chatHub.Clients
            .Group($"conversation-{id}")
            .SendAsync("ReceiveMessage", hubEvent, cancellationToken);

        var recipientProfileId = conversation.SeekerProfileId == profile.Id
            ? conversation.HelperProfileId
            : conversation.SeekerProfileId;

        var notification = new NewMessageNotification(
            id,
            profile.DisplayName,
            document.Content.Length > 100 ? document.Content[..100] : document.Content);

        await chatHub.Clients
            .Group($"user-{recipientProfileId}")
            .SendAsync("NewMessageNotification", notification, cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var conversation = await db.TaskConversations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        if (conversation.SeekerProfileId != profile.Id && conversation.HelperProfileId != profile.Id)
        {
            return Forbid();
        }

        var now = DateTimeOffset.UtcNow;
        if (conversation.SeekerProfileId == profile.Id)
        {
            conversation.SeekerLastReadAt = now;
        }
        else
        {
            conversation.HelperLastReadAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
