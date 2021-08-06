﻿using System.Linq;
using System.Threading.Tasks;
using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Project.ApplicationCore.Interfaces;
using Project.Infrastructure.Data;
using Project.SharedKernel.Constants;

namespace Project.Infrastructure.Services
{
  public class PermissionsService: IPermissionsService
  {
    private readonly AppDbContext _appDbContext;

    public PermissionsService(AppDbContext appDbContext)
    {
      _appDbContext = appDbContext;
    }
    
    public async Task<Result<Permissions>> GetPermissionsByUsernameAsync(string username)
    {
      var user =  await _appDbContext.Set<ApplicationUser>()
        .AsNoTracking()
        .Where(_ => _.UserName == username)
        .FirstOrDefaultAsync();

      if (user is null)
      {
        return Result<Permissions>.NotFound();
      }

      var roles = _appDbContext.Set<ApplicationRole>()
        .AsNoTracking();

      var rolePermissions = await _appDbContext.UserRoles
        .AsNoTracking()
        .Where(_ => _.UserId == user.Id)
        .Join(roles, role => role.RoleId, applicationRole => applicationRole.Id,
          (role, applicationRole) => applicationRole.Permissions)
        .ToListAsync();

      var permissions = rolePermissions.Aggregate((current, rolePermission) => current | rolePermission);

      if ((permissions & Permissions.Administrator) == Permissions.Administrator)
      {
        return Permissions.All;
      }
      
      return permissions;
    }
  }
}