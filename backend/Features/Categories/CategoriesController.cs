using Backend.Application.Categories;
using Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Features.Categories;

[ApiController]
[Route("api/categories")]
[EnableRateLimiting(RateLimitingExtensions.TasksReadPolicy)]
public sealed class CategoriesController(IListCategoriesQuery listCategoriesQuery) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 300, Tags = [CategoriesCache.Tag])]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var categories = await listCategoriesQuery.ExecuteAsync(cancellationToken);
        var response = categories
            .Select(category => new CategoryResponse(
                category.Id,
                category.Code,
                category.Name,
                category.Icon,
                category.Description));

        return Ok(response);
    }
}
