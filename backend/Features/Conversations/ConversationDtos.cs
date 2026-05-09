using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Conversations;

public sealed record StartConversationRequest([Required] Guid TaskId);

public sealed record SendMessageRequest(
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    string Content);

public sealed record ParticipantInfo(Guid ProfileId, string DisplayName, string? PhotoUrl);

public sealed record ConversationResponse(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    ParticipantInfo OtherParticipant,
    DateTimeOffset CreatedAt);

public sealed record ConversationPreviewResponse(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    ParticipantInfo OtherParticipant,
    string? LastMessageContent,
    DateTimeOffset? LastMessageAt);

public sealed record MessageResponse(
    Guid Id,
    string Content,
    DateTimeOffset SentAt,
    Guid SenderProfileId,
    string SenderDisplayName,
    bool IsMine);

// Sent over SignalR — omits IsMine so each client computes it from SenderProfileId.
public sealed record HubMessageEvent(
    Guid Id,
    string Content,
    DateTimeOffset SentAt,
    Guid SenderProfileId,
    string SenderDisplayName);
