using Backend.Application.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Backend.Features.Categories;

[ApiController]
[Route("api/categories")]
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
                category.Description));

        return Ok(response);
    }
}
