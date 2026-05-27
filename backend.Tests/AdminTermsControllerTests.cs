using System.Globalization;
using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Admin.Terms;
using Backend.Features.Terms;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Xunit;

namespace backend.Tests;

public sealed class AdminTermsControllerTests
{
    [Fact]
    public async Task ListAsync_ActivatesDueScheduledVersion_AndCountsLatestAcceptancePerUser()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var oldActive = CreateTerms("0.1.0", now.AddDays(-10), isActive: true, publishedAt: now.AddDays(-10));
        var dueScheduled = CreateTerms("0.2.0", now.AddHours(-1), publishedAt: now.AddDays(-2));
        var userWithNewLatest = Guid.NewGuid();
        var userWithOldLatest = Guid.NewGuid();

        db.TermsVersions.AddRange(oldActive, dueScheduled);
        db.UserTermsAcceptances.AddRange(
            CreateAcceptance(userWithNewLatest, oldActive.Id, now.AddDays(-6)),
            CreateAcceptance(userWithNewLatest, dueScheduled.Id, now.AddDays(-1)),
            CreateAcceptance(userWithOldLatest, oldActive.Id, now.AddHours(-2)));
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.ListAsync(ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<AdminTermsVersionListItem>>(ok.Value).ToList();
        var current = Assert.Single(items, item => item.Id == dueScheduled.Id);
        var previous = Assert.Single(items, item => item.Id == oldActive.Id);
        Assert.True(current.IsActive);
        Assert.False(previous.IsActive);
        Assert.Equal(1, current.AcceptanceCount);
        Assert.Equal(1, previous.AcceptanceCount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetailWithAcceptanceCount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var terms = CreateTerms("0.1.0", now.AddDays(-1), isActive: true, publishedAt: now.AddDays(-1));
        db.TermsVersions.Add(terms);
        db.UserTermsAcceptances.Add(CreateAcceptance(Guid.NewGuid(), terms.Id, now));
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.GetByIdAsync(terms.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminTermsVersionDetail>(ok.Value);
        Assert.Equal(terms.Id, detail.Id);
        Assert.Equal("0.1.0", detail.Version);
        Assert.Equal(1, detail.AcceptanceCount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_ForUnknownVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.GetByIdAsync(Guid.NewGuid(), ct);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateAsync_CreatesSanitizedDraft_AndAuditEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        var effectiveFrom = DateTimeOffset.UtcNow.AddDays(1);
        var controller = CreateController(db, actorId);

        var result = await controller.CreateAsync(
            new CreateTermsVersionRequest(
                " 1.2.3 ",
                " Updated Terms ",
                "<p>Hello</p><script>alert(1)</script>",
                " https://example.test/terms ",
                effectiveFrom),
            ct);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var detail = Assert.IsType<AdminTermsVersionDetail>(created.Value);
        Assert.Equal("GetById", created.ActionName);
        Assert.Equal("1.2.3", detail.Version);
        Assert.Equal("Updated Terms", detail.Title);
        Assert.Contains("<p>Hello</p>", detail.Content);
        Assert.DoesNotContain("script", detail.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://example.test/terms", detail.ContentUrl);
        Assert.False(detail.IsActive);

        var audit = await db.AuditEvents.SingleAsync(ct);
        Assert.Equal(actorId, audit.ActorUserId);
        Assert.Equal("admin.terms_created", audit.EventType);
        Assert.Equal(detail.Id, audit.EntityId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationProblem_ForMissingEffectiveFrom()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.CreateAsync(
            new CreateTermsVersionRequest("1.0.0", "Terms", "<p>Terms</p>", null, null),
            ct);

        var problem = AssertValidationProblem(result);
        Assert.Contains(nameof(CreateTermsVersionRequest.EffectiveFrom), problem.Errors.Keys);
    }

    [Theory]
    [InlineData("1.0", "Terms", nameof(CreateTermsVersionRequest.Version))]
    [InlineData("1.-1.0", "Terms", nameof(CreateTermsVersionRequest.Version))]
    [InlineData("1.0.0", "   ", nameof(CreateTermsVersionRequest.Title))]
    public async Task CreateAsync_ReturnsValidationProblem_ForInvalidRequest(
        string version,
        string title,
        string expectedField)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.CreateAsync(
            new CreateTermsVersionRequest(version, title, "<p>Terms</p>", null, DateTimeOffset.UtcNow),
            ct);

        var problem = AssertValidationProblem(result);
        Assert.Contains(expectedField, problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_ForDuplicateVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        db.TermsVersions.Add(CreateTerms("1.0.0", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.CreateAsync(
            new CreateTermsVersionRequest("1.0.0", "Duplicate", "<p>Terms</p>", null, DateTimeOffset.UtcNow),
            ct);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal("Duplicate version", problem.Title);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDraft_AndAuditEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        var terms = CreateTerms("1.0.0", DateTimeOffset.UtcNow.AddDays(1));
        db.TermsVersions.Add(terms);
        db.UserTermsAcceptances.Add(CreateAcceptance(Guid.NewGuid(), terms.Id, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, actorId);
        var result = await controller.UpdateAsync(
            terms.Id,
            new UpdateTermsVersionRequest(
                "1.1.0",
                " Revised ",
                "<strong>Safe</strong><script>bad()</script>",
                " https://example.test/revised ",
                DateTimeOffset.UtcNow.AddDays(2)),
            ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminTermsVersionDetail>(ok.Value);
        Assert.Equal("1.1.0", detail.Version);
        Assert.Equal("Revised", detail.Title);
        Assert.Contains("<strong>Safe</strong>", detail.Content);
        Assert.DoesNotContain("script", detail.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://example.test/revised", detail.ContentUrl);
        Assert.Equal(1, detail.AcceptanceCount);

        var audit = await db.AuditEvents.SingleAsync(ct);
        Assert.Equal(actorId, audit.ActorUserId);
        Assert.Equal("admin.terms_updated", audit.EventType);
        Assert.Equal(terms.Id, audit.EntityId);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_WhenVersionIsActive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var terms = CreateTerms("1.0.0", DateTimeOffset.UtcNow.AddDays(-1), isActive: true);
        db.TermsVersions.Add(terms);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.UpdateAsync(
            terms.Id,
            new UpdateTermsVersionRequest("1.0.1", "Edit", "<p>Edit</p>", null, DateTimeOffset.UtcNow),
            ct);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal("Cannot edit published version", problem.Title);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflict_ForDuplicateVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var existing = CreateTerms("1.0.0", DateTimeOffset.UtcNow);
        var draft = CreateTerms("1.1.0", DateTimeOffset.UtcNow);
        db.TermsVersions.AddRange(existing, draft);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.UpdateAsync(
            draft.Id,
            new UpdateTermsVersionRequest("1.0.0", "Duplicate", "<p>Duplicate</p>", null, DateTimeOffset.UtcNow),
            ct);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Theory]
    [InlineData("1.0.0", "Terms", null, nameof(UpdateTermsVersionRequest.EffectiveFrom))]
    [InlineData("1.0", "Terms", "2026-05-27T00:00:00+00:00", nameof(UpdateTermsVersionRequest.Version))]
    [InlineData("1.0.0", "   ", "2026-05-27T00:00:00+00:00", nameof(UpdateTermsVersionRequest.Title))]
    public async Task UpdateAsync_ReturnsValidationProblem_ForInvalidRequest(
        string version,
        string title,
        string? effectiveFromValue,
        string expectedField)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var draft = CreateTerms("0.9.0", DateTimeOffset.UtcNow);
        db.TermsVersions.Add(draft);
        await db.SaveChangesAsync(ct);

        DateTimeOffset? effectiveFrom = effectiveFromValue is null
            ? null
            : DateTimeOffset.Parse(effectiveFromValue, CultureInfo.InvariantCulture);
        var controller = CreateController(db);
        var result = await controller.UpdateAsync(
            draft.Id,
            new UpdateTermsVersionRequest(version, title, "<p>Terms</p>", null, effectiveFrom),
            ct);

        var problem = AssertValidationProblem(result);
        Assert.Contains(expectedField, problem.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_ForUnknownVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.UpdateAsync(
            Guid.NewGuid(),
            new UpdateTermsVersionRequest("1.0.0", "Terms", "<p>Terms</p>", null, DateTimeOffset.UtcNow),
            ct);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PublishAsync_ImmediatelyActivatesDraft_AndDeactivatesExistingActiveVersions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var active1 = CreateTerms("0.1.0", now.AddDays(-10), isActive: true, publishedAt: now.AddDays(-10));
        var active2 = CreateTerms("0.1.1", now.AddDays(-9), isActive: true, publishedAt: now.AddDays(-9));
        var draft = CreateTerms("0.2.0", now.AddDays(-1));
        db.TermsVersions.AddRange(active1, active2, draft);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.PublishAsync(draft.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminTermsVersionDetail>(ok.Value);
        Assert.Equal(draft.Id, detail.Id);
        Assert.True(detail.IsActive);
        Assert.NotNull(detail.PublishedAt);
        Assert.False(active1.IsActive);
        Assert.False(active2.IsActive);
        Assert.Equal("admin.terms_published", (await db.AuditEvents.SingleAsync(ct)).EventType);
    }

    [Fact]
    public async Task PublishAsync_SchedulesFutureVersion_AndCancelsOtherScheduledVersions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var active = CreateTerms("0.1.0", now.AddDays(-1), isActive: true, publishedAt: now.AddDays(-1));
        var scheduled = CreateTerms("0.2.0", now.AddDays(5), publishedAt: now.AddHours(-2));
        var draft = CreateTerms("0.3.0", now.AddDays(10));
        db.TermsVersions.AddRange(active, scheduled, draft);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.PublishAsync(draft.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminTermsVersionDetail>(ok.Value);
        Assert.False(detail.IsActive);
        Assert.NotNull(detail.PublishedAt);
        Assert.True(active.IsActive);
        Assert.Null(scheduled.PublishedAt);
    }

    [Fact]
    public async Task PublishAsync_AllowsMostRecentlySupersededVersionToBeRepublished()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var old = CreateTerms("0.1.0", now.AddDays(-20), publishedAt: now.AddDays(-20));
        var active = CreateTerms("0.2.0", now.AddDays(-10), isActive: true, publishedAt: now.AddDays(-10));
        db.TermsVersions.AddRange(old, active);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.PublishAsync(old.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<AdminTermsVersionDetail>(ok.Value);
        Assert.True(detail.IsActive);
        Assert.False(active.IsActive);
        Assert.Null(active.PublishedAt);
    }

    [Fact]
    public async Task PublishAsync_ReturnsConflict_WhenRepublishingOlderSupersededVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var older = CreateTerms("0.1.0", now.AddDays(-30), publishedAt: now.AddDays(-30));
        var latestSuperseded = CreateTerms("0.2.0", now.AddDays(-20), publishedAt: now.AddDays(-20));
        var active = CreateTerms("0.3.0", now.AddDays(-10), isActive: true, publishedAt: now.AddDays(-10));
        db.TermsVersions.AddRange(older, latestSuperseded, active);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.PublishAsync(older.Id, ct);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal("Republish not allowed", problem.Title);
    }

    [Fact]
    public async Task PublishAsync_ReturnsValidationProblem_WhenContentMissing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var draft = CreateTerms("1.0.0", DateTimeOffset.UtcNow, content: " ");
        db.TermsVersions.Add(draft);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.PublishAsync(draft.Id, ct);

        var problem = AssertValidationProblem(result);
        Assert.Contains("content", problem.Errors.Keys);
    }

    [Fact]
    public async Task PublishAsync_ReturnsConflict_WhenAlreadyActive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var active = CreateTerms("1.0.0", DateTimeOffset.UtcNow.AddDays(-1), isActive: true);
        db.TermsVersions.Add(active);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.PublishAsync(active.Id, ct);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task PublishAsync_ReturnsNotFound_ForUnknownVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.PublishAsync(Guid.NewGuid(), ct);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_DeletesDraftWithoutAcceptances_AndAuditEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        var draft = CreateTerms("1.0.0", DateTimeOffset.UtcNow);
        db.TermsVersions.Add(draft);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db, actorId);
        var result = await controller.DeleteAsync(draft.Id, ct);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await db.TermsVersions.ToListAsync(ct));
        var audit = await db.AuditEvents.SingleAsync(ct);
        Assert.Equal(actorId, audit.ActorUserId);
        Assert.Equal("admin.terms_deleted", audit.EventType);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsConflict_WhenVersionIsActive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var active = CreateTerms("1.0.0", DateTimeOffset.UtcNow.AddDays(-1), isActive: true);
        db.TermsVersions.Add(active);
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.DeleteAsync(active.Id, ct);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsConflict_WhenVersionHasAcceptances()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var draft = CreateTerms("1.0.0", DateTimeOffset.UtcNow);
        db.TermsVersions.Add(draft);
        db.UserTermsAcceptances.Add(CreateAcceptance(Guid.NewGuid(), draft.Id, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(ct);

        var controller = CreateController(db);
        var result = await controller.DeleteAsync(draft.Id, ct);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(await db.TermsVersions.ToListAsync(ct));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_ForUnknownVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.DeleteAsync(Guid.NewGuid(), ct);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var attribute = Assert.Single(typeof(AdminTermsController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true));
        var authorize = Assert.IsType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>(attribute);
        Assert.Equal("Admin", authorize.Roles);
    }

    [Fact]
    public void AdminTermsDtos_ExposeAllProperties()
    {
        var id = Guid.NewGuid();
        var publishedAt = DateTimeOffset.UtcNow;
        var effectiveFrom = publishedAt.AddHours(1);
        var createdAt = publishedAt.AddHours(-2);
        var updatedAt = publishedAt.AddHours(2);

        var listItem = new AdminTermsVersionListItem(
            id,
            "1.2.3",
            1,
            2,
            3,
            "Terms",
            true,
            publishedAt,
            effectiveFrom,
            createdAt,
            4);
        var detail = new AdminTermsVersionDetail(
            id,
            "1.2.3",
            1,
            2,
            3,
            "Terms",
            "<p>Terms</p>",
            "https://example.test/terms",
            true,
            publishedAt,
            effectiveFrom,
            createdAt,
            updatedAt,
            4);
        var createRequest = new CreateTermsVersionRequest(
            "1.2.3",
            "Terms",
            "<p>Terms</p>",
            "https://example.test/terms",
            effectiveFrom);
        var updateRequest = new UpdateTermsVersionRequest(
            "1.2.4",
            "Updated",
            "<p>Updated</p>",
            "https://example.test/updated",
            updatedAt);

        Assert.Equal(id, listItem.Id);
        Assert.Equal("1.2.3", listItem.Version);
        Assert.Equal(1, listItem.MajorVersion);
        Assert.Equal(2, listItem.MinorVersion);
        Assert.Equal(3, listItem.PatchVersion);
        Assert.Equal("Terms", listItem.Title);
        Assert.True(listItem.IsActive);
        Assert.Equal(publishedAt, listItem.PublishedAt);
        Assert.Equal(effectiveFrom, listItem.EffectiveFrom);
        Assert.Equal(createdAt, listItem.CreatedAt);
        Assert.Equal(4, listItem.AcceptanceCount);

        Assert.Equal(id, detail.Id);
        Assert.Equal("1.2.3", detail.Version);
        Assert.Equal(1, detail.MajorVersion);
        Assert.Equal(2, detail.MinorVersion);
        Assert.Equal(3, detail.PatchVersion);
        Assert.Equal("Terms", detail.Title);
        Assert.Equal("<p>Terms</p>", detail.Content);
        Assert.Equal("https://example.test/terms", detail.ContentUrl);
        Assert.True(detail.IsActive);
        Assert.Equal(publishedAt, detail.PublishedAt);
        Assert.Equal(effectiveFrom, detail.EffectiveFrom);
        Assert.Equal(createdAt, detail.CreatedAt);
        Assert.Equal(updatedAt, detail.UpdatedAt);
        Assert.Equal(4, detail.AcceptanceCount);

        Assert.Equal("1.2.3", createRequest.Version);
        Assert.Equal("Terms", createRequest.Title);
        Assert.Equal("<p>Terms</p>", createRequest.Content);
        Assert.Equal("https://example.test/terms", createRequest.ContentUrl);
        Assert.Equal(effectiveFrom, createRequest.EffectiveFrom);
        Assert.Equal("1.2.4", updateRequest.Version);
        Assert.Equal("Updated", updateRequest.Title);
        Assert.Equal("<p>Updated</p>", updateRequest.Content);
        Assert.Equal("https://example.test/updated", updateRequest.ContentUrl);
        Assert.Equal(updatedAt, updateRequest.EffectiveFrom);
    }

    private static ValidationProblemDetails AssertValidationProblem(IActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return Assert.IsType<ValidationProblemDetails>(objectResult.Value);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static AdminTermsController CreateController(AppDbContext db, Guid? actorId = null)
    {
        var actor = actorId ?? Guid.NewGuid();
        var controller = new AdminTermsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, actor.ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    ], "TestAuth"))
                }
            }
        };

        controller.ProblemDetailsFactory = new DefaultProblemDetailsFactory(
            Options.Create(new ApiBehaviorOptions()),
            Options.Create(new ProblemDetailsOptions()));

        return controller;
    }

    private static TermsVersion CreateTerms(
        string version,
        DateTimeOffset effectiveFrom,
        bool isActive = false,
        DateTimeOffset? publishedAt = null,
        string? content = "<p>Terms</p>")
    {
        Assert.True(TermsVersionParser.TryParse(version, out var major, out var minor, out var patch));

        return new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = version,
            MajorVersion = major,
            MinorVersion = minor,
            PatchVersion = patch,
            Title = $"Terms {version}",
            Content = content,
            IsActive = isActive,
            PublishedAt = publishedAt,
            EffectiveFrom = effectiveFrom,
            CreatedAt = effectiveFrom,
            UpdatedAt = effectiveFrom
        };
    }

    private static UserTermsAcceptance CreateAcceptance(
        Guid userId,
        Guid termsVersionId,
        DateTimeOffset acceptedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TermsVersionId = termsVersionId,
            AcceptedAt = acceptedAt,
            IpAddress = "127.0.0.1"
        };
}
