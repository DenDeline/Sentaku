﻿using System.Collections.Generic;

namespace Project.WebMVC.Models.Api.User
{
  public class GetUserRolesResponse
  {
    public IEnumerable<string> Roles { get; set; }
  }
}