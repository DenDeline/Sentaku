﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project.ApplicationCore.Interfaces;
using Project.Infrastructure.Data;
using Project.SharedKernel.Constants;
using Project.WebMVC.Authorization.PermissionsAuthorization;
using Project.WebMVC.Models.Api.User;

namespace Project.WebMVC.Controllers.Api.User
{
  [ApiController]
  public class UserRolesController: ControllerBase
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleService _roleService;

    public UserRolesController(
      UserManager<ApplicationUser> userManager,
      IRoleService roleService)
    {
      _userManager = userManager;
      _roleService = roleService;
    }
    
    [Authorize]
    [HttpGet("/api/user/roles")]
    public async Task<ActionResult<GetUserRolesResponse>> GetCurrentUserRoles()
    {
      if (User.Identity.Name is null)
      {
        return Forbid();
      }
      
      var user = await _userManager.FindByNameAsync(User.Identity.Name);
      var roles = await _userManager.GetRolesAsync(user);
      
      return Ok(new GetUserRolesResponse{ Roles = roles});
    }

    [HttpGet("/api/users/{username}/roles")]
    public async Task<ActionResult<GetUserRolesResponse>> GetUserRolesByName([FromRoute] string username)
    {
      var user = await _userManager.FindByNameAsync(username);
      if (user is null)
      {
        return NotFound();
      }
      var roles = (await _userManager.GetRolesAsync(user)).ToList();
      return Ok(new GetUserRolesResponse{ Roles = roles});
    }

    [RequirePermissions(Permissions.ManageUserRoles)]
    [HttpPost("/api/users/{username}/roles")]
    public async Task<ActionResult<UpdateUserRolesResponse>> UpdateUserRolesByName(
      [FromRoute] string username, 
      [FromBody] UpdateUserRolesRequest request,
      CancellationToken cancellationToken)
    {
      if (User.Identity.Name is null)
      {
        return Forbid();
      }
      
      var result = await _roleService.UpdateRolesByUsernameAsync(
        User.Identity.Name, 
        username, request.Roles, 
        cancellationToken);
      
      return result.Status switch
      {
        ResultStatus.Forbidden => Forbid(),
        ResultStatus.Error => BadRequest(result.Errors),
        ResultStatus.NotFound => NotFound(),
        ResultStatus.Invalid => BadRequest(result.ValidationErrors),
        ResultStatus.Ok => Ok(new UpdateUserRolesResponse{Roles = result.Value}),
        _ => BadRequest()
      };
    }
    
    [RequirePermissions(Permissions.ManageUserRoles)]
    [HttpDelete("/api/users/{username}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteUserRolesByName(
      [FromRoute] string username, 
      CancellationToken cancellationToken)
    {
      if (User.Identity.Name is null)
      {
        return Forbid();
      }
      
      var result = await _roleService.DeleteUserRolesByUsernameAsync(User.Identity.Name, username, cancellationToken);
      
      return result.Status switch
      {
        ResultStatus.Forbidden => Forbid(),
        ResultStatus.Error => BadRequest(result.Errors),
        ResultStatus.NotFound => NotFound(),
        ResultStatus.Invalid => BadRequest(result.ValidationErrors),
        ResultStatus.Ok => NoContent(),
        _ => BadRequest()
      };
    }
  }
}
