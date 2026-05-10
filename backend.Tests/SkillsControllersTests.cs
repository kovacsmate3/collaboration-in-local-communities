using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Admin.Skills;
using Backend.Features.Skills;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class SkillsControllersTests
{
    [Fact]
    public async Task SearchAsync_WithoutPrefix_ReturnsOnlyApprovedAndActiveSkills()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        db.Skills.AddRange(
            CreateSkill("alpha", "Alpha", SkillStatus.Approved, isActive: true),
            CreateSkill("alphabet", "Alphabet", SkillStatus.Approved, isActive: true),
            CreateSkill("alpine", "Alpine", SkillStatus.Pending, isActive: true),
            CreateSkill("albatross", "Albatross", SkillStatus.Approved, isActive: false),
            CreateSkill("beta", "Beta", SkillStatus.Approved, isActive: true));

        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateSkillsController(db, Guid.NewGuid());

        var result = await controller.SearchAsync(null, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<SkillResponse>>(ok.Value).ToList();

        Assert.Equal(["Alpha", "Alphabet", "Beta"], items.Select(item => item.Name));
        Assert.All(items, item => Assert.Equal(nameof(SkillStatus.Approved), item.Status));
    }

    [Fact]
    public async Task GetSkillAsync_ReturnsPendingSkillOnlyWhenLinkedToCurrentProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var userId = Guid.NewGuid();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Test User"
        };

        var pendingSkill = CreateSkill("pending_skill", "Pending Skill", SkillStatus.Pending, isActive: true);

        db.Profiles.Add(profile);
        db.Skills.Add(pendingSkill);
        db.ProfileSkills.Add(new ProfileSkill
        {
            ProfileId = profile.Id,
            SkillId = pendingSkill.Id
        });

        await db.SaveChangesAsync(cancellationToken);

        var linkedController = CreateSkillsController(db, userId);
        var linkedResult = await linkedController.GetSkillAsync(pendingSkill.Id, cancellationToken);

        var linkedOk = Assert.IsType<OkObjectResult>(linkedResult);
        var linkedResponse = Assert.IsType<SkillResponse>(linkedOk.Value);
        Assert.Equal(pendingSkill.Id, linkedResponse.Id);

        var unlinkedController = CreateSkillsController(db, Guid.NewGuid());
        var unlinkedResult = await unlinkedController.GetSkillAsync(pendingSkill.Id, cancellationToken);

        Assert.IsType<NotFoundResult>(unlinkedResult);
    }

    [Fact]
    public async Task CreateAsync_CreatesPendingSkillAndLinksItToCurrentProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var userId = Guid.NewGuid();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Creator"
        };

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateSkillsController(db, userId);

        var result = await controller.CreateAsync(
            new CreateSkillRequest
            {
                Name = "Bike Repair",
                Description = "Fixing bikes"
            },
            cancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var createdSkill = Assert.IsType<SkillResponse>(created.Value);

        var storedSkill = await db.Skills.SingleAsync(skill => skill.Id == createdSkill.Id, cancellationToken);
        Assert.Equal(SkillStatus.Pending, storedSkill.Status);
        Assert.Null(storedSkill.ApprovedAt);
        Assert.Equal("bike_repair", storedSkill.Code);

        Assert.True(await db.ProfileSkills.AnyAsync(
            profileSkill => profileSkill.ProfileId == profile.Id && profileSkill.SkillId == storedSkill.Id,
            cancellationToken));
    }

    [Fact]
    public async Task ListAsync_AppliesStatusFilterAndPaginationMetadata()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;

        db.Skills.AddRange(
            CreateSkill("pending_old", "Pending Old", SkillStatus.Pending, createdAt: now.AddMinutes(-10)),
            CreateSkill("pending_new", "Pending New", SkillStatus.Pending, createdAt: now.AddMinutes(-1)),
            CreateSkill("approved", "Approved", SkillStatus.Approved, createdAt: now));

        await db.SaveChangesAsync(cancellationToken);

        var controller = new AdminSkillsController(db);

        var result = await controller.ListAsync(page: 1, pageSize: 1, status: "Pending", cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminSkillPagedResponse>(ok.Value);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        var item = Assert.Single(response.Items);
        Assert.Equal("Pending New", item.Name);
        Assert.Equal(nameof(SkillStatus.Pending), item.Status);
    }

    [Fact]
    public async Task PatchAsync_ApproveActionIsIdempotent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var skill = CreateSkill("to_approve", "To Approve", SkillStatus.Pending);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new AdminSkillsController(db);
        var request = new PatchSkillRequest { Action = "Approve" };

        var firstResult = await controller.PatchAsync(skill.Id, request, cancellationToken);
        var firstOk = Assert.IsType<OkObjectResult>(firstResult);
        var firstResponse = Assert.IsType<AdminSkillResponse>(firstOk.Value);

        var approvedAt = firstResponse.ApprovedAt;
        Assert.NotNull(approvedAt);

        var secondResult = await controller.PatchAsync(skill.Id, request, cancellationToken);
        var secondOk = Assert.IsType<OkObjectResult>(secondResult);
        var secondResponse = Assert.IsType<AdminSkillResponse>(secondOk.Value);

        Assert.Equal(nameof(SkillStatus.Approved), secondResponse.Status);
        Assert.Equal(approvedAt, secondResponse.ApprovedAt);
    }

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

    private static SkillsController CreateSkillsController(AppDbContext db, Guid userId)
    {
        var controller = new SkillsController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "TestAuth"))
                }
            }
        };

        return controller;
    }
}
