using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Admin.Categories;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class AdminCategoriesControllerTests
{
    [Fact]
    public async Task DeleteAsync_ReturnsNoContent_WhenCategoryIsUnreferenced()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = "test-category",
            Name = "Test Category",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.DeleteAsync(category.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);

        var deletedCategory = await db.Categories.FindAsync([category.Id], cancellationToken);
        Assert.Null(deletedCategory);
    }

    [Fact]
    public async Task DeleteAsync_SuccessfullyDeletesUnreferencedCategory()
    {
        // Note: The FK conflict scenario (DELETE returning 409 when a category
        // is referenced by tasks) cannot be reliably tested with EF Core InMemory
        // because it doesn't replicate Postgres DeleteBehavior.Restrict behavior.
        // That scenario should be covered by integration tests against real Postgres.

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = "test-category-2",
            Name = "Test Category 2",
            Icon = Category.DefaultIcon,
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.DeleteAsync(category.Id, cancellationToken);
        Assert.IsType<NoContentResult>(result);

        var deleted = await db.Categories.FindAsync([category.Id], cancellationToken);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var controller = CreateController(db);

        var result = await controller.DeleteAsync(Guid.NewGuid(), cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeactivateAsync_DeactivatesActiveCategory_ReturnsNoContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = "test-category",
            Name = "Test Category",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.DeactivateAsync(category.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);

        var updated = await db.Categories.FindAsync([category.Id], cancellationToken);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_IsIdempotent_WhenCategoryAlreadyInactive()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = "test-category",
            Name = "Test Category",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.DeactivateAsync(category.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);

        var unchanged = await db.Categories.FindAsync([category.Id], cancellationToken);
        Assert.NotNull(unchanged);
        Assert.False(unchanged.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_ActivatesInactiveCategory_ReturnsNoContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = "test-category",
            Name = "Test Category",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.ActivateAsync(category.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);

        var updated = await db.Categories.FindAsync([category.Id], cancellationToken);
        Assert.NotNull(updated);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_IsIdempotent_WhenCategoryAlreadyActive()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Code = "test-category",
            Name = "Test Category",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);

        var result = await controller.ActivateAsync(category.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);

        var unchanged = await db.Categories.FindAsync([category.Id], cancellationToken);
        Assert.NotNull(unchanged);
        Assert.True(unchanged.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var controller = CreateController(db);

        var result = await controller.ActivateAsync(Guid.NewGuid(), cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var controller = CreateController(db);

        var result = await controller.DeactivateAsync(Guid.NewGuid(), cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AdminCategoriesController CreateController(AppDbContext db)
    {
        var outputCacheStore = new TestOutputCacheStore();

        var controller = new AdminCategoriesController(db, outputCacheStore)
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

    private sealed class TestOutputCacheStore : IOutputCacheStore
    {
        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<byte[]?>(null);
        }

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
