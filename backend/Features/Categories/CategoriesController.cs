using Backend.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Categories;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 300)]
    public async Task<IActionResult> ListAsync()
    {
        var categories = await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryResponse(c.Id, c.Code, c.Name, c.Description))
            .ToListAsync();

        return Ok(categories);
    }
}
