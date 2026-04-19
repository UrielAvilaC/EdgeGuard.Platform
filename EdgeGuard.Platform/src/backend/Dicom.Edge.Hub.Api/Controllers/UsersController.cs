using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.Identity;
using Dicom.Edge.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.ViewUsers)]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] UserFilter filter, CancellationToken ct)
    {
        var pagination = new PaginationRequest { Page = filter.Page, PageSize = filter.PageSize };
        var criteria = new UserFilterCriteria
        {
            Search = filter.Search,
            IsActive = filter.IsActive,
            Role = filter.Role,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };
        var result = await _userService.GetFilteredPagedAsync(pagination, criteria, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var currentUserId = User.FindFirst("userId")?.Value;
        var roles = request.Roles?.Select(r => Enum.Parse<Role>(r, ignoreCase: true)).ToList();

        var command = new CreateUserCommand(
            request.Username,
            request.Password,
            request.FullName,
            roles,
            currentUserId);

        var user = await _userService.CreateUserAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}/profile")]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        await _userService.UpdateProfileAsync(id, request.FullName, ct);
        return NoContent();
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        await _userService.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> Activate(string id, CancellationToken ct)
    {
        await _userService.ActivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/roles")]
    [Authorize(Policy = Policies.ManageRoles)]
    public async Task<IActionResult> AssignRole(string id, [FromBody] RoleRequest request, CancellationToken ct)
    {
        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        var currentUserId = User.FindFirst("userId")?.Value;
        await _userService.AssignRoleAsync(id, role, currentUserId, ct);
        return NoContent();
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Policy = Policies.ManageRoles)]
    public async Task<IActionResult> RemoveRole(string id, string role, CancellationToken ct)
    {
        var parsedRole = Enum.Parse<Role>(role, ignoreCase: true);
        await _userService.RemoveRoleAsync(id, parsedRole, ct);
        return NoContent();
    }

    [HttpPost("{id}/permissions")]
    [Authorize(Policy = Policies.GrantPermissions)]
    public async Task<IActionResult> GrantPermission(string id, [FromBody] PermissionRequest request, CancellationToken ct)
    {
        var permission = Enum.Parse<Permission>(request.Permission, ignoreCase: true);
        var currentUserId = User.FindFirst("userId")?.Value;
        await _userService.GrantPermissionAsync(id, permission, currentUserId, ct);
        return NoContent();
    }

    [HttpDelete("{id}/permissions/{permission}")]
    [Authorize(Policy = Policies.GrantPermissions)]
    public async Task<IActionResult> RevokePermission(string id, string permission, CancellationToken ct)
    {
        var parsedPermission = Enum.Parse<Permission>(permission, ignoreCase: true);
        await _userService.RevokePermissionAsync(id, parsedPermission, ct);
        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _userService.ResetPasswordAsync(id, request.NewPassword, ct);
        return NoContent();
    }

    [HttpPost("{id}/unlock")]
    [Authorize(Policy = Policies.ManageUsers)]
    public async Task<IActionResult> Unlock(string id, CancellationToken ct)
    {
        await _userService.UnlockAsync(id, ct);
        return NoContent();
    }
}

public record CreateUserRequest(string Username, string Password, string FullName, List<string>? Roles = null);
public record UpdateProfileRequest(string FullName);
public record RoleRequest(string Role);
public record PermissionRequest(string Permission);
public record ResetPasswordRequest(string NewPassword);
