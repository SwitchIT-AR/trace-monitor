using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Security;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(TraceMonitorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetAll(CancellationToken ct) =>
        await db.Users.OrderBy(u => u.Username)
            .Select(u => new UserSummaryDto(u.Id, u.Username, u.IsActive, u.IsAdmin, u.CreatedAtUtc))
            .ToListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username y Password son requeridos");
        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
            return BadRequest("Ya existe un usuario con ese nombre");

        var user = new User { Username = request.Username, PasswordHash = "", IsAdmin = request.IsAdmin };
        user.PasswordHash = UserPasswordHasher.Hash(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserSummaryDto(user.Id, user.Username, user.IsActive, user.IsAdmin, user.CreatedAtUtc);
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return NotFound();
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("NewPassword es requerido");

        user.PasswordHash = UserPasswordHasher.Hash(user, request.NewPassword);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return NotFound();

        user.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>The (target, agent) pairs a non-admin user is allowed to see — the RBAC matrix
    /// picker in Settings reads this to render the current checked state.</summary>
    [HttpGet("{id:int}/access")]
    public async Task<ActionResult<IReadOnlyList<AccessPairDto>>> GetAccess(int id, CancellationToken ct) =>
        await db.UserTargetAgentAccess
            .Where(a => a.UserId == id)
            .Select(a => new AccessPairDto(a.TargetId, a.AgentId))
            .ToListAsync(ct);

    /// <summary>Replaces the full set of (target, agent) pairs a user can see — simpler than
    /// incremental add/remove endpoints, since the matrix UI naturally produces "the whole desired
    /// state" on every checkbox toggle.</summary>
    [HttpPut("{id:int}/access")]
    public async Task<IActionResult> SetAccess(int id, IReadOnlyList<AccessPairDto> pairs, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null)
            return NotFound();

        var existing = await db.UserTargetAgentAccess.Where(a => a.UserId == id).ToListAsync(ct);
        db.UserTargetAgentAccess.RemoveRange(existing);
        db.UserTargetAgentAccess.AddRange(
            pairs.Select(p => new UserTargetAgentAccess { UserId = id, TargetId = p.TargetId, AgentId = p.AgentId }));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
