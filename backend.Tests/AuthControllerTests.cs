using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Auth;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace backend.Tests;

public sealed class AuthControllerTests
{
    private const string DefaultPassword = "P@ssword123";
    private const string DefaultEmail = "user@example.test";
    private const string DefaultClientIp = "203.0.113.7";
    private const string RefreshTokenCookieName = "refreshToken";

    // ----------------------------------------------------------------------
    // RegisterAsync
    // ----------------------------------------------------------------------

    [Fact]
    public async Task RegisterAsync_TermsNotAccepted_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var controller = CreateController(scope);
        var request = BuildRegisterRequest(acceptTerms: false);

        var result = await controller.RegisterAsync(request, cancellationToken);

        AssertValidationProblem(result, expectedKey: nameof(RegisterRequest.AcceptTerms));
    }

    [Fact]
    public async Task RegisterAsync_LatitudeWithoutLongitude_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var controller = CreateController(scope);
        var request = BuildRegisterRequest(latitude: 47.5, longitude: null);

        var result = await controller.RegisterAsync(request, cancellationToken);

        AssertValidationProblem(result, expectedKey: nameof(RegisterRequest.Latitude));
    }

    [Theory]
    [InlineData(-91.0, 0.0)]
    [InlineData(91.0, 0.0)]
    [InlineData(double.NaN, 0.0)]
    public async Task RegisterAsync_OutOfRangeLatitude_ReturnsValidationProblem(
        double latitude,
        double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var controller = CreateController(scope);
        var request = BuildRegisterRequest(latitude: latitude, longitude: longitude);

        var result = await controller.RegisterAsync(request, cancellationToken);

        AssertValidationProblem(result, expectedKey: nameof(RegisterRequest.Latitude));
    }

    [Theory]
    [InlineData(0.0, -181.0)]
    [InlineData(0.0, 181.0)]
    public async Task RegisterAsync_OutOfRangeLongitude_ReturnsValidationProblem(
        double latitude,
        double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var controller = CreateController(scope);
        var request = BuildRegisterRequest(latitude: latitude, longitude: longitude);

        var result = await controller.RegisterAsync(request, cancellationToken);

        AssertValidationProblem(result, expectedKey: nameof(RegisterRequest.Longitude));
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);
        await SeedActiveTermsAsync(scope, cancellationToken);

        var controller = CreateController(scope);
        var request = BuildRegisterRequest(password: "short");

        var result = await controller.RegisterAsync(request, cancellationToken);

        Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task RegisterAsync_HappyPath_PersistsUserProfileAndSendsVerificationEmail()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);
        await SeedActiveTermsAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var emailSender = new RecordingEmailSender();
        var controller = CreateController(scope, clientIp: DefaultClientIp, emailSender: emailSender);
        var request = BuildRegisterRequest();

        var result = await controller.RegisterAsync(request, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RegisterResponse>(ok.Value);
        Assert.Contains("Registration successful", response.Message, StringComparison.Ordinal);

        var persistedUser = await userManager.FindByEmailAsync(request.Email);
        Assert.NotNull(persistedUser);
        Assert.True(await userManager.IsInRoleAsync(persistedUser, ApplicationRoleNames.User));

        // Reload from a clean change tracker so EF relationship fixup on the still-tracked
        // UserProfile instance can't mask a PrivacySettings row that was never persisted.
        db.ChangeTracker.Clear();
        var profile = await db.Profiles
            .Include(p => p.PrivacySettings)
            .FirstAsync(p => p.UserId == persistedUser.Id, cancellationToken);
        Assert.Equal(request.DisplayName, profile.DisplayName);
        Assert.True(profile.IsProfileCompleted);
        Assert.NotNull(profile.PrivacySettings);

        // #159: registration no longer issues tokens/cookies — it sends a verification email.
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == persistedUser.Id, cancellationToken));
        Assert.Equal([request.Email], emailSender.SentTo);

        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.registered" && e.ActorUserId == persistedUser.Id, cancellationToken));
    }

    [Theory]
    [InlineData("hu", "2gather – E-mail-cím megerősítése", "Erősítsd meg az e-mail-címed")]
    [InlineData("hu-HU", "2gather – E-mail-cím megerősítése", "Erősítsd meg az e-mail-címed")]
    [InlineData("en", "2gather - Verify your email address", "Verify your email address")]
    [InlineData(null, "2gather - Verify your email address", "Verify your email address")]
    public async Task ResendVerificationEmailAsync_SendsEmailInRequestedLocale(
        string? acceptLanguage,
        string expectedSubject,
        string expectedBodySnippet)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);
        var user = await SeedUserAsync(scope, cancellationToken, emailConfirmed: false);

        var emailSender = new RecordingEmailSender();
        var controller = CreateController(
            scope,
            clientIp: DefaultClientIp,
            emailSender: emailSender,
            acceptLanguage: acceptLanguage);

        var result = await controller.ResendVerificationEmailAsync(
            new ResendVerificationEmailRequest(user.Email!),
            cancellationToken);

        Assert.IsType<OkResult>(result);
        var email = emailSender.Single;
        Assert.Equal(user.Email, email.ToEmail);
        Assert.Equal(expectedSubject, email.Subject);
        Assert.Contains(expectedBodySnippet, email.HtmlContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("hu", "2gather – Új jelszó beállítása", "Állíts be új jelszót")]
    [InlineData("hu-HU", "2gather – Új jelszó beállítása", "Állíts be új jelszót")]
    [InlineData("en", "2gather - Set a new password", "Set a new password")]
    [InlineData(null, "2gather - Set a new password", "Set a new password")]
    public async Task ForgotPasswordAsync_SendsResetEmailInRequestedLocale(
        string? acceptLanguage,
        string expectedSubject,
        string expectedBodySnippet)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);
        var user = await SeedUserAsync(scope, cancellationToken);

        var emailSender = new RecordingEmailSender();
        var controller = CreateController(
            scope,
            clientIp: DefaultClientIp,
            emailSender: emailSender,
            acceptLanguage: acceptLanguage);

        var result = await controller.ForgotPasswordAsync(
            new ForgotPasswordRequest(user.Email!),
            cancellationToken);

        Assert.IsType<OkResult>(result);
        var email = emailSender.Single;
        Assert.Equal(user.Email, email.ToEmail);
        Assert.Equal(expectedSubject, email.Subject);
        Assert.Contains(expectedBodySnippet, email.HtmlContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_WhenActiveTermsExists_RecordsTermsAcceptance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activeTerms = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Title = "Terms",
            IsActive = true,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        db.TermsVersions.Add(activeTerms);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(scope, clientIp: DefaultClientIp);
        var request = BuildRegisterRequest();

        var result = await controller.RegisterAsync(request, cancellationToken);

        Assert.IsType<OkObjectResult>(result);

        var acceptance = await db.UserTermsAcceptances
            .FirstAsync(a => a.TermsVersionId == activeTerms.Id, cancellationToken);
        Assert.Equal(DefaultClientIp, acceptance.IpAddress);
    }

    [Fact]
    public async Task RegisterAsync_WhenSkillsRequested_AttachesOnlyApprovedActiveSkills()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);
        await SeedActiveTermsAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var approvedActive = SeedSkill(db, "approved", isActive: true, status: SkillStatus.Approved);
        var pending = SeedSkill(db, "pending", isActive: true, status: SkillStatus.Pending);
        var inactive = SeedSkill(db, "inactive", isActive: false, status: SkillStatus.Approved);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(scope);
        var request = BuildRegisterRequest(skillIds: [approvedActive.Id, pending.Id, inactive.Id]);

        var result = await controller.RegisterAsync(request, cancellationToken);

        Assert.IsType<OkObjectResult>(result);

        var profile = await db.Profiles
            .Include(p => p.ProfileSkills)
            .FirstAsync(cancellationToken);
        var attachedSkillIds = profile.ProfileSkills.Select(ps => ps.SkillId).ToHashSet();
        Assert.Equal([approvedActive.Id], attachedSkillIds);
    }

    // ----------------------------------------------------------------------
    // LoginAsync
    // ----------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsUnauthorized_AndAuditsFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var controller = CreateController(scope);

        var result = await controller.LoginAsync(
            new LoginRequest("nobody@example.test", DefaultPassword),
            cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.login_failed" && e.ActorUserId == null, cancellationToken));
    }

    [Fact]
    public async Task LoginAsync_LockedOutUser_ReturnsForbid_AndAuditsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(scope, cancellationToken);

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));

        var controller = CreateController(scope);
        var result = await controller.LoginAsync(
            new LoginRequest(user.Email!, DefaultPassword),
            cancellationToken);

        Assert.IsType<ForbidResult>(result);
        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.login_forbidden" && e.ActorUserId == user.Id, cancellationToken));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized_AndIncrementsFailedCount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(scope, cancellationToken);

        var controller = CreateController(scope);
        var result = await controller.LoginAsync(
            new LoginRequest(user.Email!, "wrong-password"),
            cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        var reloaded = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded.AccessFailedCount);
    }

    [Fact]
    public async Task LoginAsync_EmailNotConfirmed_ReturnsForbidden_AndAudits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedUserAsync(scope, cancellationToken, emailConfirmed: false);

        var controller = CreateController(scope);
        var result = await controller.LoginAsync(
            new LoginRequest(user.Email!, DefaultPassword),
            cancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.login_email_not_confirmed" && e.ActorUserId == user.Id, cancellationToken));
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsOk_AndResetsFailedCount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(scope, cancellationToken);

        // Pre-bump the failure counter to confirm the success path resets it.
        await userManager.AccessFailedAsync(user);

        var controller = CreateController(scope, clientIp: DefaultClientIp);
        var result = await controller.LoginAsync(
            new LoginRequest(user.Email!, DefaultPassword),
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal(user.Id, response.UserId);

        var reloaded = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(reloaded);
        Assert.Equal(0, reloaded.AccessFailedCount);

        Assert.True(await db.RefreshTokens.AnyAsync(t => t.UserId == user.Id, cancellationToken));
        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.login_succeeded" && e.ActorUserId == user.Id, cancellationToken));
        AssertRefreshCookieSet(controller);
    }

    // ----------------------------------------------------------------------
    // LogoutAsync
    // ----------------------------------------------------------------------

    [Fact]
    public async Task LogoutAsync_NoCurrentUser_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();

        var controller = CreateController(scope);
        var result = await controller.LogoutAsync(cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task LogoutAsync_WithMatchingRefreshCookie_RevokesTokenAndClearsCookie()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedUserAsync(scope, cancellationToken);
        var (rawToken, hash) = await IssueAndPersistRefreshTokenAsync(scope, user.Id, cancellationToken);

        var controller = CreateController(
            scope,
            currentUserId: user.Id,
            refreshTokenCookie: rawToken,
            clientIp: DefaultClientIp);

        var result = await controller.LogoutAsync(cancellationToken);

        Assert.IsType<OkResult>(result);

        var stored = await db.RefreshTokens.FirstAsync(t => t.TokenHash == hash, cancellationToken);
        Assert.NotNull(stored.RevokedAt);
        Assert.Equal(DefaultClientIp, stored.RevokedByIp);

        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.logout" && e.ActorUserId == user.Id, cancellationToken));
        AssertRefreshCookieCleared(controller);
    }

    [Fact]
    public async Task LogoutAsync_WithMalformedRefreshCookie_StillReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var user = await SeedUserAsync(scope, cancellationToken);

        var controller = CreateController(
            scope,
            currentUserId: user.Id,
            refreshTokenCookie: "not-base64");

        var result = await controller.LogoutAsync(cancellationToken);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task LogoutAsync_WithoutRefreshCookie_StillReturnsOk()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var user = await SeedUserAsync(scope, cancellationToken);

        var controller = CreateController(scope, currentUserId: user.Id);
        var result = await controller.LogoutAsync(cancellationToken);

        Assert.IsType<OkResult>(result);
    }

    // ----------------------------------------------------------------------
    // RefreshAsync
    // ----------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_NoCookie_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();

        var controller = CreateController(scope);
        var result = await controller.RefreshAsync(cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RefreshAsync_MalformedCookie_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();

        var controller = CreateController(scope, refreshTokenCookie: "not-base64");
        var result = await controller.RefreshAsync(cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RefreshAsync_TokenNotInDatabase_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var user = await SeedUserAsync(scope, cancellationToken);
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();
        var tokens = await tokenService.CreateTokenPairAsync(user, cancellationToken);

        var controller = CreateController(scope, refreshTokenCookie: tokens.RefreshToken);
        var result = await controller.RefreshAsync(cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedUserAsync(scope, cancellationToken);
        var (rawToken, hash) = await IssueAndPersistRefreshTokenAsync(scope, user.Id, cancellationToken);

        var stored = await db.RefreshTokens.FirstAsync(t => t.TokenHash == hash, cancellationToken);
        stored.RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(scope, refreshTokenCookie: rawToken);
        var result = await controller.RefreshAsync(cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var user = await SeedUserAsync(scope, cancellationToken);
        var (rawToken, _) = await IssueAndPersistRefreshTokenAsync(
            scope,
            user.Id,
            cancellationToken,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var controller = CreateController(scope, refreshTokenCookie: rawToken);
        var result = await controller.RefreshAsync(cancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RefreshAsync_LockedOutUser_ReturnsForbid_AndAuditsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(scope, cancellationToken);
        var (rawToken, _) = await IssueAndPersistRefreshTokenAsync(scope, user.Id, cancellationToken);

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));

        var controller = CreateController(scope, refreshTokenCookie: rawToken);
        var result = await controller.RefreshAsync(cancellationToken);

        Assert.IsType<ForbidResult>(result);
        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.refresh_forbidden" && e.ActorUserId == user.Id, cancellationToken));
    }

    // RefreshAsync's success path uses ExecuteUpdateAsync for atomic token revocation,
    // which the EF Core InMemory provider does not implement. The early-return paths
    // (no/malformed cookie, missing/revoked/expired token, locked-out user) are all
    // covered above; this happy-path scenario is exercised end-to-end by the
    // Playwright suite and via real Postgres in the deployed app.
    [Fact(Skip = "ExecuteUpdateAsync isn't supported by EF Core InMemory; covered by E2E and integration tests.")]
    public async Task RefreshAsync_HappyPath_RotatesTokenAndAuditsSuccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        await SeedRolesAsync(scope, cancellationToken);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedUserAsync(scope, cancellationToken);
        var (rawToken, originalHash) = await IssueAndPersistRefreshTokenAsync(scope, user.Id, cancellationToken);

        var controller = CreateController(
            scope,
            refreshTokenCookie: rawToken,
            clientIp: DefaultClientIp);
        var result = await controller.RefreshAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal(user.Id, response.UserId);

        var allTokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync(cancellationToken);
        Assert.Equal(2, allTokens.Count);

        var oldToken = allTokens.Single(t => t.TokenHash == originalHash);
        Assert.NotNull(oldToken.RevokedAt);
        Assert.Equal(DefaultClientIp, oldToken.RevokedByIp);
        Assert.NotNull(oldToken.ReplacedByTokenHash);

        var newToken = allTokens.Single(t => t.TokenHash != originalHash);
        Assert.Null(newToken.RevokedAt);
        Assert.Equal(oldToken.ReplacedByTokenHash, newToken.TokenHash);

        Assert.True(await db.AuditEvents
            .AnyAsync(e => e.EventType == "auth.refresh_succeeded" && e.ActorUserId == user.Id, cancellationToken));
        AssertRefreshCookieSet(controller);
    }

    [Fact]
    public async Task RegisterAsync_Returns400_WhenNoActiveTermsExist()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // No TermsVersions seeded — the "no active terms" path should trigger.
        var controller = CreateController(scope);

        var request = new RegisterRequest(
            Email: "user@test.com",
            Password: "P@ssword123!",
            DisplayName: "Test User",
            Workplace: null,
            Position: null,
            LocationText: null,
            Latitude: null,
            Longitude: null,
            Bio: null,
            AcceptTerms: true,
            SkillIds: null);

        var result = await controller.RegisterAsync(request, cancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Registration unavailable", problem.Title);

        // FakeUserStore never writes to db.Users, so the user was not persisted.
        Assert.Empty(db.Users);
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                // RegisterAsync wraps its work in a transaction; the InMemory provider
                // can't honour that and raises TransactionIgnoredWarning as an error
                // by default. Downgrade it so the test exercises the same control flow
                // as production without the warning short-circuiting the controller.
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddApplicationIdentity();
        services.AddSingleton(new SymmetricSecurityKey(
            "auth-controller-test-signing-key-32-byte"u8.ToArray()));
        services.AddSingleton(Options.Create(new AuthOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30
        }));
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        return services.BuildServiceProvider();
    }

    private static AuthController CreateController(
        IServiceScope scope,
        Guid? currentUserId = null,
        string? refreshTokenCookie = null,
        string? clientIp = null,
        IEmailSender? emailSender = null,
        string? acceptLanguage = null)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();

        var httpContext = new DefaultHttpContext();

        if (refreshTokenCookie is not null)
        {
            httpContext.Request.Headers.Cookie = $"{RefreshTokenCookieName}={refreshTokenCookie}";
        }

        if (acceptLanguage is not null)
        {
            httpContext.Request.Headers.AcceptLanguage = acceptLanguage;
        }

        if (currentUserId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, currentUserId.Value.ToString())],
                "TestAuth"));
        }

        var emailOptions = Options.Create(new EmailOptions
        {
            FromEmail = "noreply@test.local",
            FromName = "Test",
            FrontendBaseUrl = "http://localhost:3000"
        });

        return new AuthController(
            db,
            userManager,
            tokenService,
            new StubClientIpAccessor(clientIp),
            emailSender ?? new RecordingEmailSender(),
            emailOptions,
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static async Task SeedRolesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        Assert.True((await roleManager.CreateAsync(new ApplicationRole { Name = ApplicationRoleNames.User })).Succeeded);
        Assert.True((await roleManager.CreateAsync(new ApplicationRole { Name = ApplicationRoleNames.Admin })).Succeeded);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task<ApplicationUser> SeedUserAsync(
        IServiceScope scope,
        CancellationToken cancellationToken,
        string email = DefaultEmail,
        string password = DefaultPassword,
        string role = ApplicationRoleNames.User,
        bool emailConfirmed = true)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed
        };
        Assert.True((await userManager.CreateAsync(user, password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        cancellationToken.ThrowIfCancellationRequested();
        return user;
    }

    private static async Task<(string RawToken, string Hash)> IssueAndPersistRefreshTokenAsync(
        IServiceScope scope,
        Guid userId,
        CancellationToken cancellationToken,
        DateTimeOffset? expiresAt = null)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User must be seeded before issuing a refresh token.");

        var pair = await tokenService.CreateTokenPairAsync(user, cancellationToken);
        var hash = tokenService.HashRefreshToken(pair.RefreshToken);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expiresAt ?? pair.RefreshTokenExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return (pair.RefreshToken, hash);
    }

    private static Skill SeedSkill(AppDbContext db, string code, bool isActive, SkillStatus status)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            IsActive = isActive,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Skills.Add(skill);
        return skill;
    }

    private static RegisterRequest BuildRegisterRequest(
        string email = DefaultEmail,
        string password = DefaultPassword,
        bool acceptTerms = true,
        string displayName = "Example User",
        double? latitude = null,
        double? longitude = null,
        IReadOnlyList<Guid>? skillIds = null)
    {
        return new RegisterRequest(
            Email: email,
            Password: password,
            DisplayName: displayName,
            Workplace: null,
            Position: null,
            LocationText: null,
            Latitude: latitude,
            Longitude: longitude,
            Bio: null,
            AcceptTerms: acceptTerms,
            SkillIds: skillIds);
    }

    private static async Task SeedActiveTermsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        db.TermsVersions.Add(new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Title = "Terms",
            IsActive = true,
            EffectiveFrom = now.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void AssertValidationProblem(IActionResult result, string expectedKey)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        var details = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains(expectedKey, details.Errors.Keys);
    }

    private static void AssertRefreshCookieSet(AuthController controller)
    {
        var setCookie = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains(RefreshTokenCookieName, setCookie, StringComparison.Ordinal);
    }

    private static void AssertRefreshCookieCleared(AuthController controller)
    {
        var setCookie = controller.Response.Headers.SetCookie.ToString();
        // ASP.NET Core marks a cleared cookie with an expires header in the past.
        Assert.Contains(RefreshTokenCookieName, setCookie, StringComparison.Ordinal);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.Ordinal);
    }

    private sealed class StubClientIpAccessor(string? clientIp) : IClientIpAccessor
    {
        public string? GetClientIp()
        {
            return clientIp;
        }
    }

    private sealed record SentEmail(string ToEmail, string Subject, string HtmlContent);

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<SentEmail> Sent { get; } = [];

        public List<string> SentTo => Sent.ConvertAll(m => m.ToEmail);

        public SentEmail Single => Assert.Single(Sent);

        public Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlContent,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(new SentEmail(toEmail, subject, htmlContent));
            return Task.CompletedTask;
        }
    }
}
