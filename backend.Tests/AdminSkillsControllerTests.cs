using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Admin.Skills;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class AdminSkillsControllerTests
{
    // ── PatchAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PatchAsync_Approve_SetsStatusApproved()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("alpha", "Alpha", SkillStatus.Pending);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.PatchAsync(skill.Id, new PatchSkillRequest { Action = "Approve" }, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminSkillResponse>(ok.Value);
        Assert.Equal(nameof(SkillStatus.Approved), response.Status);
        Assert.NotNull(response.ApprovedAt);
    }

    [Fact]
    public async Task PatchAsync_Approve_IsIdempotent_WhenAlreadyApproved()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var approvedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Code = "already_approved",
            Name = "Already Approved",
            IsActive = true,
            Status = SkillStatus.Approved,
            ApprovedAt = approvedAt,
            CreatedAt = approvedAt,
            UpdatedAt = approvedAt
        };
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.PatchAsync(skill.Id, new PatchSkillRequest { Action = "Approve" }, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminSkillResponse>(ok.Value);
        Assert.Equal(nameof(SkillStatus.Approved), response.Status);
        // ApprovedAt should not have changed
        Assert.Equal(approvedAt, response.ApprovedAt);
    }

    [Fact]
    public async Task PatchAsync_Deactivate_SetsIsActiveFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("active_skill", "Active Skill", SkillStatus.Approved, isActive: true);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.PatchAsync(skill.Id, new PatchSkillRequest { Action = "Deactivate" }, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminSkillResponse>(ok.Value);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task PatchAsync_Activate_SetsIsActiveTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("inactive_skill", "Inactive Skill", SkillStatus.Approved, isActive: false);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.PatchAsync(skill.Id, new PatchSkillRequest { Action = "Activate" }, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminSkillResponse>(ok.Value);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task PatchAsync_InvalidAction_Returns422()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("test_skill", "Test Skill", SkillStatus.Pending);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.PatchAsync(skill.Id, new PatchSkillRequest { Action = "Destroy" }, cancellationToken);

        var obj = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.IsType<ValidationProblemDetails>(obj.Value);
    }

    [Fact]
    public async Task PatchAsync_ReturnsNotFound_WhenSkillDoesNotExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var controller = CreateController(db);

        var result = await controller.PatchAsync(Guid.NewGuid(), new PatchSkillRequest { Action = "Approve" }, cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsNoContent_WhenNotLinked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("test_skill", "Test Skill", SkillStatus.Pending);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.DeleteAsync(skill.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);

        var deleted = await db.Skills.FindAsync([skill.Id], cancellationToken);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenSkillDoesNotExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var controller = CreateController(db);

        var result = await controller.DeleteAsync(Guid.NewGuid(), cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsConflict_WhenLinkedToProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("linked_skill", "Linked Skill", SkillStatus.Approved);
        db.Skills.Add(skill);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DisplayName = "Test User"
        };
        db.Profiles.Add(profile);

        db.ProfileSkills.Add(new ProfileSkill
        {
            SkillId = skill.Id,
            ProfileId = profile.Id,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.DeleteAsync(skill.Id, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);

        // Skill must still exist
        var stillExists = await db.Skills.FindAsync([skill.Id], cancellationToken);
        Assert.NotNull(stillExists);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Skill CreateSkill(
        string code,
        string name,
        SkillStatus status,
        bool isActive = true,
        DateTimeOffset? createdAt = null)
    {
        var timestamp = createdAt ?? DateTimeOffset.UtcNow;

        return new Skill
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IsActive = isActive,
            Status = status,
            ApprovedAt = status == SkillStatus.Approved ? timestamp : null,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AdminSkillsController CreateController(AppDbContext db)
    {
        var controller = new AdminSkillsController(db)
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
            }
        };

        return controller;
    }
}
