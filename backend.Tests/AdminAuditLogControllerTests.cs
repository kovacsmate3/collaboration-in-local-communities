using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Admin.AuditLog;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class AdminAuditLogControllerTests
{
    [Fact]
    public async Task ListAsync_ReturnsEmptyPage_WhenNoEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.ListAsync(cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuditLogPagedResponse>(ok.Value);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task ListAsync_ReturnsPaginatedResults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        for (var i = 0; i < 25; i++)
        {
            db.AuditEvents.Add(CreateAuditEvent("auth.login_succeeded"));
        }
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.ListAsync(page: 1, pageSize: 20, cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuditLogPagedResponse>(ok.Value);
        Assert.Equal(25, response.TotalCount);
        Assert.Equal(20, response.Items.Count);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(2, response.TotalPages);
    }

    [Fact]
    public async Task ListAsync_FiltersByEventType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        db.AuditEvents.Add(CreateAuditEvent("auth.login_succeeded"));
        db.AuditEvents.Add(CreateAuditEvent("auth.login_succeeded"));
        db.AuditEvents.Add(CreateAuditEvent("admin.skill_approved"));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.ListAsync(eventType: "auth.login_succeeded", cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuditLogPagedResponse>(ok.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.All(response.Items, item => Assert.Equal("auth.login_succeeded", item.EventType));
    }

    [Fact]
    public async Task ListAsync_FiltersByEntityType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        db.AuditEvents.Add(CreateAuditEvent("event.a", entityType: "ApplicationUser"));
        db.AuditEvents.Add(CreateAuditEvent("event.b", entityType: "ApplicationUser"));
        db.AuditEvents.Add(CreateAuditEvent("event.c", entityType: "CommunityTask"));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.ListAsync(entityType: "ApplicationUser", cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuditLogPagedResponse>(ok.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.All(response.Items, item => Assert.Equal("ApplicationUser", item.EntityType));
    }

    [Fact]
    public async Task ListAsync_FiltersByDateRange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var now = DateTimeOffset.UtcNow;

        db.AuditEvents.Add(CreateAuditEvent("event.old", createdAt: now.AddDays(-10)));
        db.AuditEvents.Add(CreateAuditEvent("event.recent", createdAt: now.AddDays(-2)));
        db.AuditEvents.Add(CreateAuditEvent("event.now", createdAt: now));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.ListAsync(
            from: now.AddDays(-3),
            to: now.AddDays(-1),
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuditLogPagedResponse>(ok.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal("event.recent", response.Items[0].EventType);
    }

    [Fact]
    public async Task ListAsync_ReturnsValidationError_WhenPageLessThan1()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.ListAsync(page: 0, cancellationToken: cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AdminAuditLogController CreateController(AppDbContext db)
    {
        return new AdminAuditLogController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    ], "TestAuth"))
                }
            },
            ProblemDetailsFactory = new FakeProblemDetailsFactory(),
        };
    }

    private sealed class FakeProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new() { Status = statusCode, Title = title, Detail = detail };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new(modelStateDictionary) { Status = statusCode ?? 400 };
    }

    private static AuditEvent CreateAuditEvent(
        string eventType,
        string? entityType = null,
        DateTimeOffset? createdAt = null)
    {
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityType is not null ? Guid.NewGuid() : null,
            ActorUserId = null,
            Payload = null,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
    }
}
