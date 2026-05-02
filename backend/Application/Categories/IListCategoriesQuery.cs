namespace Backend.Application.Categories;

/// <summary>
/// Lists active categories for API read models.
/// </summary>
public interface IListCategoriesQuery
{
    /// <summary>
    /// Executes the active category list query.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>The active category list ordered for display.</returns>
    Task<IReadOnlyList<CategoryListItem>> ExecuteAsync(CancellationToken cancellationToken);
}
