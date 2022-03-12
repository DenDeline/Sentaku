﻿using System.Linq;
using System.Threading.Tasks;
using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Sentaku.ApplicationCore.Interfaces;
using Sentaku.Infrastructure.Data;
using Sentaku.SharedKernel.Constants;

namespace Sentaku.Infrastructure.Services
{
  public class PermissionsService : IPermissionsService
  {
    private readonly AppDbContext _appDbContext;

    public PermissionsService(AppDbContext appDbContext)
    {
      _appDbContext = appDbContext;
    }

    public async Task<Result<Permissions>> GetPermissionsByUsernameAsync(string username)
    {
      var user = await _appDbContext.Users
        .Where(_ => _.UserName == username)
        .Select(_ => new { _.Id })
        .FirstOrDefaultAsync();

      if (user is null)
        return Result<Permissions>.NotFound();

      var rolePermissions = await _appDbContext.UserRoles
        .Where(_ => _.UserId == user.Id)
        .AsSplitQuery()
        .Join(_appDbContext.Roles, role => role.RoleId, applicationRole => applicationRole.Id,
          (role, applicationRole) => applicationRole.Permissions)
        .ToListAsync();

      var permissions = rolePermissions.Aggregate((current, rolePermission) => current | rolePermission);

      return (permissions & Permissions.Administrator) == Permissions.Administrator ? Permissions.All : permissions;
    }
  }
}