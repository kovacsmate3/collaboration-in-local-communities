using Backend.Application.Categories;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Queries;

internal sealed class EfListCategoriesQuery(AppDbContext db) : IListCategoriesQuery
{
    public async Task<IReadOnlyList<CategoryListItem>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await db.Categories
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .Select(category => new CategoryListItem(
                category.Id,
                category.Code,
                category.Name,
                category.Description))
            .ToListAsync(cancellationToken);
    }
}
