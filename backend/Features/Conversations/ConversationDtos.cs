using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Backend.Features.Conversations;

[PublicAPI]
public sealed record StartConversationRequest([Required] Guid TaskId);

[PublicAPI]
public sealed record SendMessageRequest(
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    string Content);

[PublicAPI]
public sealed record ParticipantInfo(Guid ProfileId, string DisplayName, string? PhotoUrl);

[PublicAPI]
public sealed record ConversationResponse(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    ParticipantInfo OtherParticipant,
    DateTimeOffset CreatedAt);

[PublicAPI]
public sealed record ConversationPreviewResponse(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    ParticipantInfo OtherParticipant,
    string? LastMessageContent,
    DateTimeOffset? LastMessageAt);

[PublicAPI]
public sealed record MessageResponse(
    Guid Id,
    string Content,
    DateTimeOffset SentAt,
    Guid SenderProfileId,
    string SenderDisplayName,
    bool IsMine);

// Sent over SignalR — omits IsMine so each client computes it from SenderProfileId.
[PublicAPI]
public sealed record HubMessageEvent(
    Guid Id,
    string Content,
    DateTimeOffset SentAt,
    Guid SenderProfileId,
    string SenderDisplayName);
