using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Conversations;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class ConversationsControllerTests
{
    // ── HasUnread calculation ────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsHasUnreadTrue_WhenLastReadAtIsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        (UserProfile seeker, _, TaskConversation conversation) = await SeedConversationAsync(db, ct);
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        conversation.SeekerLastReadAt = null;
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, seeker.UserId);
        var result = await controller.ListAsync(ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var previews = Assert.IsAssignableFrom<IEnumerable<ConversationPreviewResponse>>(ok.Value);
        var preview = Assert.Single(previews);
        Assert.True(preview.HasUnread);
    }

    [Fact]
    public async Task ListAsync_ReturnsHasUnreadTrue_WhenLastMessageAtIsAfterLastReadAt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        // ReSharper disable once InconsistentNaming
        var base_ = DateTimeOffset.UtcNow;
        (UserProfile seeker, _, TaskConversation conversation) = await SeedConversationAsync(db, ct);
        conversation.LastMessageAt = base_.AddMinutes(1);
        conversation.SeekerLastReadAt = base_;
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, seeker.UserId);
        var result = await controller.ListAsync(ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var previews = Assert.IsType<IEnumerable<ConversationPreviewResponse>>(ok.Value, exactMatch: false);
        Assert.True(Assert.Single(previews).HasUnread);
    }

    [Fact]
    public async Task ListAsync_ReturnsHasUnreadFalse_WhenLastReadAtIsAfterLastMessageAt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        // ReSharper disable once InconsistentNaming
        var base_ = DateTimeOffset.UtcNow;
        (UserProfile seeker, _, TaskConversation conversation) = await SeedConversationAsync(db, ct);
        conversation.LastMessageAt = base_;
        conversation.SeekerLastReadAt = base_.AddSeconds(1);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, seeker.UserId);
        var result = await controller.ListAsync(ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var previews = Assert.IsType<IEnumerable<ConversationPreviewResponse>>(ok.Value, exactMatch: false);
        Assert.False(Assert.Single(previews).HasUnread);
    }

    [Fact]
    public async Task ListAsync_ReturnsHasUnreadFalse_WhenNoMessagesSent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var (seeker, _, _) = await SeedConversationAsync(db, ct);

        var controller = CreateController(db, seeker.UserId);
        var result = await controller.ListAsync(ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var previews = Assert.IsAssignableFrom<IEnumerable<ConversationPreviewResponse>>(ok.Value);
        Assert.False(Assert.Single(previews).HasUnread);
    }

    [Fact]
    public async Task ListAsync_ComputesHasUnreadPerUser_SeekerAndHelperIndependently()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        // ReSharper disable once InconsistentNaming
        var base_ = DateTimeOffset.UtcNow;
        (UserProfile seeker, UserProfile helper, TaskConversation conversation) = await SeedConversationAsync(db, ct);
        conversation.LastMessageAt = base_.AddMinutes(1);
        conversation.SeekerLastReadAt = base_.AddMinutes(2); // seeker already read it
        conversation.HelperLastReadAt = null;                // helper has not
        await db.SaveChangesAsync(ct);

        var seekerController = CreateController(db, seeker.UserId);
        var seekerResult = await seekerController.ListAsync(ct);
        var seekerOk = Assert.IsType<OkObjectResult>(seekerResult);
        var seekerPreviews = Assert.IsAssignableFrom<IEnumerable<ConversationPreviewResponse>>(seekerOk.Value);
        Assert.False(Assert.Single(seekerPreviews).HasUnread);

        var helperController = CreateController(db, helper.UserId);
        var helperResult = await helperController.ListAsync(ct);
        var helperOk = Assert.IsType<OkObjectResult>(helperResult);
        var helperPreviews = Assert.IsType<IEnumerable<ConversationPreviewResponse>>(helperOk.Value, exactMatch: false);
        Assert.True(Assert.Single(helperPreviews).HasUnread);
    }

    // ── MarkReadAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_UpdatesSeekerLastReadAt_WhenCallerIsSeeker()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        (UserProfile seeker, _, TaskConversation conversation) = await SeedConversationAsync(db, ct);

        var controller = CreateController(db, seeker.UserId);
        var result = await controller.MarkReadAsync(conversation.Id, ct);

        Assert.IsType<NoContentResult>(result);

        var updated = await db.TaskConversations.FindAsync([conversation.Id], ct);
        Assert.NotNull(updated!.SeekerLastReadAt);
        Assert.Null(updated.HelperLastReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_UpdatesHelperLastReadAt_WhenCallerIsHelper()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        (_, UserProfile helper, TaskConversation conversation) = await SeedConversationAsync(db, ct);

        var controller = CreateController(db, helper.UserId);
        var result = await controller.MarkReadAsync(conversation.Id, ct);

        Assert.IsType<NoContentResult>(result);

        var updated = await db.TaskConversations.FindAsync([conversation.Id], ct);
        Assert.NotNull(updated!.HelperLastReadAt);
        Assert.Null(updated.SeekerLastReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_ReturnsForbid_WhenCallerIsNotParticipant()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        (_, _, TaskConversation conversation) = await SeedConversationAsync(db, ct);

        var outsider = CreateProfile("Outsider");
        db.Profiles.Add(outsider);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, outsider.UserId);
        var result = await controller.MarkReadAsync(conversation.Id, ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MarkReadAsync_ReturnsNotFound_WhenConversationDoesNotExist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var profile = CreateProfile("User");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, profile.UserId);
        var result = await controller.MarkReadAsync(Guid.NewGuid(), ct);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetMessagesAsync access control ──────────────────────────────────────

    [Fact]
    public async Task GetMessagesAsync_ReturnsForbid_WhenCallerIsNotParticipant()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        (_, _, TaskConversation conversation) = await SeedConversationAsync(db, ct);

        var outsider = CreateProfile("Outsider");
        db.Profiles.Add(outsider);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, outsider.UserId);
        var result = await controller.GetMessagesAsync(conversation.Id, null, 0, ct);

        Assert.IsType<ForbidResult>(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false))
            .Options;
        return new AppDbContext(options);
    }

    private static ConversationsController CreateController(AppDbContext db, Guid userId) =>
        new(db, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    ], "TestAuth")),
                },
            },
        };

    private static UserProfile CreateProfile(string displayName) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        DisplayName = displayName,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static async Task<(UserProfile Seeker, UserProfile Helper, TaskConversation Conversation)>
        SeedConversationAsync(AppDbContext db, CancellationToken ct)
    {
        var seeker = CreateProfile("Seeker");
        var helper = CreateProfile("Helper");

        var categoryId = Guid.NewGuid();
        db.Categories.Add(new Category
        {
            Id = categoryId,
            Code = "test",
            Name = "Test",
            Icon = Category.DefaultIcon,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        db.Profiles.Add(seeker);
        db.Profiles.Add(helper);
        await db.SaveChangesAsync(ct);

        var task = new CommunityTask
        {
            Id = Guid.NewGuid(),
            SeekerProfileId = seeker.Id,
            CategoryId = categoryId,
            Title = "Test task",
            Description = "Description",
            CompensationType = CompensationType.Voluntary,
            Status = Backend.Domain.Enums.TaskStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.Tasks.Add(task);

        var conversation = new TaskConversation
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            SeekerProfileId = seeker.Id,
            HelperProfileId = helper.Id,
            CosmosConversationId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.TaskConversations.Add(conversation);

        await db.SaveChangesAsync(ct);
        return (seeker, helper, conversation);
    }
}
