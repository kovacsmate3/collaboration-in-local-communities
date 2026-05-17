using System.Security.Claims;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Conversations;

[Authorize]
public sealed class ChatHub(AppDbContext db) : Hub
{
    public async Task JoinConversationAsync(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var id))
        {
            return;
        }

        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            return;
        }

        var conversation = await db.TaskConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation is null ||
            (conversation.SeekerProfileId != profile.Id && conversation.HelperProfileId != profile.Id))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{id}");
    }

    public async Task JoinUserGroupAsync()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{profile.Id}");
    }
}
