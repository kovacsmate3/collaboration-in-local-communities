using Newtonsoft.Json;

namespace Backend.Features.Conversations;

public sealed class MessageDocument
{
    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty("conversationId")]
    public string ConversationId { get; init; } = string.Empty;

    [JsonProperty("senderProfileId")]
    public string SenderProfileId { get; init; } = string.Empty;

    [JsonProperty("senderDisplayName")]
    public string SenderDisplayName { get; init; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; init; } = string.Empty;

    [JsonProperty("sentAt")]
    public DateTimeOffset SentAt { get; init; }
}
