using Backend.Common;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Skills;

[ApiController]
[Route("api/skills")]
[Authorize]
public sealed partial class SkillsController(AppDbContext db) : ControllerBase
{
    private const int MaxSearchResults = 20;

    [HttpGet]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? prefix,
        CancellationToken cancellationToken)
    {
        var query = db.Skills
            .AsNoTracking()
            .Where(skill => skill.Status == SkillStatus.Approved && skill.IsActive);

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var escaped = prefix.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
            query = query.Where(skill => EF.Functions.ILike(skill.Name, $"{escaped}%", @"\"));
        }

        var skills = await query
            .OrderBy(skill => skill.Name)
            .Take(MaxSearchResults)
            .ToListAsync(cancellationToken);

        return Ok(skills.Select(SkillResponse.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSkillAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var skill = await db.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (skill is null)
        {
            return NotFound();
        }

        if (skill.Status != SkillStatus.Pending)
        {
            return Ok(SkillResponse.FromEntity(skill));
        }

        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        var isLinked = await db.ProfileSkills
            .AnyAsync(ps => ps.SkillId == id && ps.ProfileId == profile.Id, cancellationToken);

        if (!isLinked)
        {
            return NotFound();
        }

        return Ok(SkillResponse.FromEntity(skill));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateSkillRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        if (!FieldValidator.ValidateTrimmedString(ModelState, nameof(request.Name), request.Name, 2, 120, out var name))
        {
            return ValidationProblem(ModelState);
        }

        var description = StringUtilities.Normalize(request.Description);
        var code = GenerateCode(name);

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            Status = SkillStatus.Pending
        };

        var profileSkill = new ProfileSkill
        {
            ProfileId = profile.Id,
            SkillId = skill.Id
        };

        db.Skills.Add(skill);
        db.ProfileSkills.Add(profileSkill);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsUniqueConstraintViolation(ex, "ux_skills_code"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Skill already exists",
                Detail = $"A skill named '{name}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return CreatedAtAction("GetSkill", new { id = skill.Id }, SkillResponse.FromEntity(skill));
    }

    private static string GenerateCode(string name)
    {
        var builder = new StringBuilder();
        var previousUnderscore = false;

        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousUnderscore = false;
            }
            else if (!previousUnderscore && builder.Length > 0)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }

        return builder.ToString().TrimEnd('_');
    }

    private async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            return null;
        }

        return await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
