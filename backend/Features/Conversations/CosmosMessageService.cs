using Microsoft.Azure.Cosmos;

namespace Backend.Features.Conversations;

public sealed class CosmosMessageService(CosmosClient client)
{
    private const string DatabaseId = "collab";
    private const string ContainerId = "messages";
    private const string PartitionKeyPath = "/conversationId";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dbResponse = await client.CreateDatabaseIfNotExistsAsync(
            DatabaseId,
            cancellationToken: cancellationToken);

        await dbResponse.Database.CreateContainerIfNotExistsAsync(
            ContainerId,
            PartitionKeyPath,
            cancellationToken: cancellationToken);
    }

    public async Task<MessageDocument> AddAsync(
        MessageDocument message,
        CancellationToken cancellationToken)
    {
        var container = client.GetContainer(DatabaseId, ContainerId);
        var response = await container.CreateItemAsync(
            message,
            new PartitionKey(message.ConversationId),
            cancellationToken: cancellationToken);

        return response.Resource;
    }

    public async Task<(IReadOnlyList<MessageDocument> Messages, bool HasMore)> GetByConversationAsync(
        string conversationId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken)
    {
        var container = client.GetContainer(DatabaseId, ContainerId);

        var queryText = before.HasValue
            ? "SELECT * FROM c WHERE c.conversationId = @conversationId AND c.sentAt < @before ORDER BY c.sentAt DESC"
            : "SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.sentAt DESC";

        var query = new QueryDefinition(queryText)
            .WithParameter("@conversationId", conversationId);

        if (before.HasValue)
        {
            query = query.WithParameter("@before", before.Value);
        }

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(conversationId),
            MaxItemCount = limit + 1,
        };

        var results = new List<MessageDocument>();
        using var iterator = container.GetItemQueryIterator<MessageDocument>(query, requestOptions: options);

        while (iterator.HasMoreResults && results.Count < limit + 1)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        var hasMore = results.Count > limit;
        var messages = results.Take(limit).Reverse().ToList();
        return (messages, hasMore);
    }
}
