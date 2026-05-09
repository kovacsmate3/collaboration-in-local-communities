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

    public async Task<IReadOnlyList<MessageDocument>> GetByConversationAsync(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var container = client.GetContainer(DatabaseId, ContainerId);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.conversationId = @conversationId ORDER BY c.sentAt ASC")
            .WithParameter("@conversationId", conversationId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(conversationId),
        };

        var results = new List<MessageDocument>();
        using var iterator = container.GetItemQueryIterator<MessageDocument>(
            query,
            requestOptions: options);

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        return results;
    }
}
