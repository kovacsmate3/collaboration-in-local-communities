using System.Security.Claims;
using System.Text.Encodings.Web;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Conversations;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace backend.Tests;

// End-to-end SignalR delivery: two real HubConnection clients negotiate against an
// in-memory TestServer-hosted ChatHub, join the conversation group through the real
// hub method, and a group broadcast must reach both. Reliable in CI because there is
// no external transport, and join completion is awaited before broadcasting.
public sealed class ChatHubIntegrationTests
{
    [Fact]
    public async Task ConversationGroupBroadcast_DeliversToBothJoinedClients()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = await BuildHostAsync(ct);

        (UserProfile seeker, UserProfile helper, TaskConversation conversation) =
            await SeedConversationAsync(host, ct);

        var server = host.GetTestServer();
        await using var seekerClient = CreateConnection(server, seeker.UserId);
        await using var helperClient = CreateConnection(server, helper.UserId);

        var seekerReceived = new TaskCompletionSource<HubMessageEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var helperReceived = new TaskCompletionSource<HubMessageEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        seekerClient.On<HubMessageEvent>("ReceiveMessage", m => seekerReceived.TrySetResult(m));
        helperClient.On<HubMessageEvent>("ReceiveMessage", m => helperReceived.TrySetResult(m));

        await seekerClient.StartAsync(ct);
        await helperClient.StartAsync(ct);

        await seekerClient.InvokeAsync(nameof(ChatHub.JoinConversationAsync), conversation.Id.ToString(), ct);
        await helperClient.InvokeAsync(nameof(ChatHub.JoinConversationAsync), conversation.Id.ToString(), ct);

        var hubEvent = new HubMessageEvent(
            Guid.NewGuid(),
            "Hello across two clients",
            DateTimeOffset.UtcNow,
            seeker.Id,
            seeker.DisplayName);

        var hubContext = host.Services.GetRequiredService<IHubContext<ChatHub>>();
        await hubContext.Clients
            .Group($"conversation-{conversation.Id}")
            .SendAsync("ReceiveMessage", hubEvent, ct);

        var delivered = await Task.WhenAll(
            WaitAsync(seekerReceived.Task, ct),
            WaitAsync(helperReceived.Task, ct));

        Assert.All(delivered, received =>
        {
            Assert.Equal(hubEvent.Id, received.Id);
            Assert.Equal(hubEvent.Content, received.Content);
            Assert.Equal(hubEvent.SenderProfileId, received.SenderProfileId);
            Assert.Equal(hubEvent.SenderDisplayName, received.SenderDisplayName);
        });
    }

    [Fact]
    public async Task UserGroupBroadcast_DeliversNotificationOnlyToRecipientClient()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = await BuildHostAsync(ct);

        (UserProfile seeker, UserProfile helper, TaskConversation conversation) =
            await SeedConversationAsync(host, ct);

        var server = host.GetTestServer();
        await using var helperClient = CreateConnection(server, helper.UserId);

        var helperNotified = new TaskCompletionSource<NewMessageNotification>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        helperClient.On<NewMessageNotification>("NewMessageNotification", n => helperNotified.TrySetResult(n));

        await helperClient.StartAsync(ct);
        await helperClient.InvokeAsync(nameof(ChatHub.JoinUserGroupAsync), ct);

        var notification = new NewMessageNotification(conversation.Id, seeker.DisplayName, "preview text");

        var hubContext = host.Services.GetRequiredService<IHubContext<ChatHub>>();
        await hubContext.Clients
            .Group($"user-{helper.Id}")
            .SendAsync("NewMessageNotification", notification, ct);

        var received = await WaitAsync(helperNotified.Task, ct);

        Assert.Equal(notification.ConversationId, received.ConversationId);
        Assert.Equal(notification.SenderDisplayName, received.SenderDisplayName);
        Assert.Equal(notification.ContentPreview, received.ContentPreview);
    }

    // ── Infrastructure ───────────────────────────────────────────────────────

    private static async Task<IHost> BuildHostAsync(CancellationToken ct)
    {
        var databaseName = Guid.NewGuid().ToString();

        var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddDbContext<AppDbContext>(o =>
                        o.UseInMemoryDatabase(databaseName, b => b.EnableNullChecks(false)));
                    services.AddRouting();
                    services.AddSignalR();
                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                    services.AddAuthorization();
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapHub<ChatHub>("/hubs/chat"));
                });
            })
            .Build();

        await host.StartAsync(ct);
        return host;
    }

    private static HubConnection CreateConnection(TestServer server, Guid userId) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "hubs/chat"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(userId.ToString());
            })
            .Build();

    private static Task<T> WaitAsync<T>(Task<T> task, CancellationToken ct) =>
        task.WaitAsync(TimeSpan.FromSeconds(15), ct);

    private static async Task<(UserProfile Seeker, UserProfile Helper, TaskConversation Conversation)>
        SeedConversationAsync(IHost host, CancellationToken ct)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

    private static UserProfile CreateProfile(string displayName) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        DisplayName = displayName,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    // Authenticates the SignalR access token (a raw user id) into a NameIdentifier claim,
    // mirroring what the real JWT bearer scheme yields for ChatHub.
    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? token = null;

            var authorization = Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization["Bearer ".Length..].Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                token = Request.Query["access_token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, token)],
                SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
