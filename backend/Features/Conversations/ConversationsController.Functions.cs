using System.Security.Claims;
using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Conversations;

public sealed partial class ConversationsController
{
    private static ConversationResponse ToConversationResponse(
        TaskConversation conversation,
        string taskTitle,
        Guid currentProfileId)
    {
        var isSeeker = conversation.SeekerProfileId == currentProfileId;
        var other = isSeeker ? conversation.HelperProfile : conversation.SeekerProfile;
        var otherId = isSeeker ? conversation.HelperProfileId : conversation.SeekerProfileId;

        return new ConversationResponse(
            conversation.Id,
            conversation.TaskId,
            taskTitle,
            new ParticipantInfo(otherId, other.DisplayName, other.PhotoUrl),
            conversation.CreatedAt);
    }

    private async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            return null;
        }

        return await db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
