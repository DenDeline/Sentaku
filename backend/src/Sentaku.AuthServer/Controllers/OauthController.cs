﻿using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sentaku.ApplicationCore.Interfaces;
using Sentaku.AuthServer.AuthServer.Models;
using Sentaku.AuthServer.Models.Oauth;
using Sentaku.AuthServer.ViewModels;
using Sentaku.Infrastructure.Data;

namespace Sentaku.AuthServer.Controllers;

public class OauthController : Controller
  {
    private readonly UserManager<AppUser> _userManager;

    public OauthController(UserManager<AppUser> userManager)
    {
      _userManager = userManager;
    }

    [HttpGet("/oauth2/authorize")]
    public IActionResult Authorize([FromQuery] GetAuthorizationRequest request)
    {
      var client = AuthServerConfig.InMemoryClients.FirstOrDefault(_ => _.ClientId == request.ClientId);

      // TODO: Separate class for validation required params 
      if (client is null)
        return ResponseHelper.InvalidClient(request.RedirectUri, state: request.State);

      if (!client.RedirectUris.Contains(request.RedirectUri))
        return ResponseHelper.InvalidRequest(request.RedirectUri, state: request.State);

      if (AuthServerConfig.SupportedResponseTypes.All(_ => _ != request.ResponseType))
        return ResponseHelper.UnsupportedGrantType(request.RedirectUri, state: request.State);

      var vm = new AuthorizeViewModel
      {
        ClientId = request.ClientId,
        RedirectUri = request.RedirectUri,
        State = request.State,
        CodeChallenge = request.CodeChallenge,
        CodeChallengeMethod = request.CodeChallengeMethod
      };

      return View(vm);
    }

    [HttpPost("/oauth2/signin-code")]
    public async Task<IActionResult> SignInCode(
      [FromForm] AuthorizeViewModel vm,
      [FromServices] IDataProtectionProvider provider)
    {
      var user = (await _userManager.FindByEmailAsync(vm.Login)) ?? (await _userManager.FindByNameAsync(vm.Login));

      if (user is null)
      {
        // TODO: Add validation error and append query params
        ModelState.AddModelError(vm.Login, "");
        return View("Authorize", vm);
      }

      var isPasswordValid = await _userManager.CheckPasswordAsync(user, vm.Password);

      if (!isPasswordValid)
      {
        // TODO: Add validation error and append query params
        ModelState.AddModelError(vm.Password, "");
        return View("Authorize", vm);
      }

      // TODO: Move out code token generation
      var client = AuthServerConfig.InMemoryClients.FirstOrDefault(_ => _.ClientId == vm.ClientId);

      if (client is null)
      {
        // TODO: Add validation error and append query params
        ModelState.AddModelError("", "");
        return View("Authorize", vm);
      }

      var codeToken = new CodeToken
      {
        ClientId = vm.ClientId,
        RedirectUri = vm.RedirectUri,
        UserId = user.Id,
        CodeChallenge = vm.CodeChallenge,
        CodeChallengeMethod = vm.CodeChallengeMethod
      };

      var jsonCodeToken = JsonConvert.SerializeObject(codeToken);

      var protector =  provider
        .CreateProtector("AuthServer.Oauth2.SecureCodeToken")
        .ToTimeLimitedDataProtector();
      
      var encodedCodeToken = protector.Protect(jsonCodeToken, TimeSpan.FromMinutes(5));

      // TODO: Create separate class for building redirectUrl
      var queryBuilder = new QueryBuilder
      {
        {"code", encodedCodeToken},
        {"state", vm.State}
      };

      var redirectUrl = $"{client.RedirectUris.FirstOrDefault()}{queryBuilder}";
      return Redirect(redirectUrl);
    }


    [HttpPost("/oauth2/token")]
    public async Task<IActionResult> GetAccessTokenAsync(
      [FromForm] GetAccessTokenRequest request,
      [FromServices] IIdentityTokenClaimService tokenService,
      [FromServices] IDataProtectionProvider provider,
      CancellationToken cancellationToken)
    {
      if (AuthServerConfig.SupportedGrantTypes.All(_ => _ != request.GrantType))
        return ResponseHelper.UnsupportedGrantType(request.RedirectUri);

      var client = AuthServerConfig.InMemoryClients.FirstOrDefault(_ => _.ClientId == request.ClientId);
      
      if (client is null)
        return ResponseHelper.InvalidClient(request.RedirectUri);

      var protector =  provider
        .CreateProtector("AuthServer.Oauth2.SecureCodeToken")
        .ToTimeLimitedDataProtector();

      string decodedCodeToken;
      
      try
      { 
        decodedCodeToken = protector.Unprotect(request.Code);
      }
      catch (CryptographicException e)
      {
        return ResponseHelper.InvalidGrant(request.RedirectUri, e.Message);
      }
      
      var codeToken = JsonConvert.DeserializeObject<CodeToken?>(decodedCodeToken);

      if (codeToken is null)
        return ResponseHelper.InvalidGrant(request.RedirectUri);

      var codeTokenValidationResult = codeToken.Validate(request.ClientId, request.RedirectUri, request.CodeVerifier);

      if (!codeTokenValidationResult.IsValid)
        return ResponseHelper.ErrorResponse(request.RedirectUri, codeTokenValidationResult.Error, codeTokenValidationResult.ErrorDescription);

      var user = await _userManager.FindByIdAsync(codeToken.UserId);

      if (user is null)
        return ResponseHelper.InvalidRequest(request.RedirectUri);

      var token = await tokenService.GetTokenAsync(
        user.UserName, 
        "https://localhost:7045", 
        "https://localhost:5001");

      return Ok(new
      {
        access_token = token,
        token_type = "Bearer",
        expires_in = 3600
      });
    }
  }