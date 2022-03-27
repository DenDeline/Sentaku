﻿using System;

namespace Sentaku.WebApi.Models.User;

public class GetUserResponse
{
  public string Id { get; set; }
  public string Username { get; set; }
  public string Name { get; set; }
  public string Surname { get; set; }
  public bool Verified { get; set; }
}
