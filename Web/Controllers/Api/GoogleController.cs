﻿using Data.Tables;
using Lib;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Web.Services;

namespace Web.Controllers.Api;

[Route("api/auth/google")]
[ApiController]
public class GoogleController : ControllerBase
{
    private readonly SignInManager<User> signInManager;
    private readonly UserManager<User> userManager;
    private readonly Jwt jwt;
    private readonly GoogleService googleService;

    public GoogleController(SignInManager<User> signInManager, UserManager<User> userManager, Jwt jwt, GoogleService googleService)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.jwt = jwt;
        this.googleService = googleService;
    }

    public record UserInfo(string Token, string Email);

    [HttpGet]
    public IActionResult Login()
    {
        var redirect = "/api/auth/google/callback";
        var properties = signInManager.ConfigureExternalAuthenticationProperties("google", redirect);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string? remoteError = null)
    {
        if (remoteError != null) return BadRequest($"Google error: {remoteError}");

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null) return BadRequest("Error loading external login information.");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email == null) return BadRequest("No email is found");

        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var newToken = await googleService.SignInUserWithExternal(user, info);
            if (newToken == null) return Problem();

            return Ok(new UserInfo(newToken, email));
        }

        var token = await googleService.CreateUserWithExternal(email, info);
        if (token == null) return Problem("Error creating a new user.");

        return Ok(new UserInfo(token, email));
    }
}