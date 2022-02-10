﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Sentaku.ApplicationCore.Interfaces;
using Sentaku.SharedKernel;

namespace Sentaku.Infrastructure.Services
{
  public class IdentityTokenClaimService : IIdentityTokenClaimService
  {

    public Task<string> GetTokenAsync(string username)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = new SigningIssuerCertificate().GetPrivateKey();
      var claims = new List<Claim> { new(ClaimTypes.Name, username) };
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Issuer = "https://localhost:44307",
        Audience = "https://localhost:44307",
        Subject = new ClaimsIdentity(claims.ToArray()),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = key
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);
      return Task.FromResult(tokenHandler.WriteToken(token));
    }
  }
}
