﻿using Data.Tables;
using Lib;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Config;
using Web.Models;

namespace Web.Controllers;

public class RegisterController : Controller
{
    private readonly UserManager<User> userManager;
    private readonly Jwt jwt;

    public RegisterController(UserManager<User> userManager, Jwt jwt)
    {
        this.userManager = userManager;
        this.jwt = jwt;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", new RegisterViewModel());

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            return BadRequest("Passwords do not match");
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Email already registered");
            return BadRequest("Email already registered");
        }

        var newUser = new User(model.Email);

        var result = await userManager.CreateAsync(newUser, model.Password);
        if (!result.Succeeded)
        {
            // Handle identity errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return StatusCode(500, ModelState);
        }

        // User created successfully, generate authentication token
        var token = jwt.Token(newUser.Id, newUser.Version);

        Response.AddAuthCookie(token);

        return Redirect("/Account");
    }

    [HttpPost]
    public async Task<IActionResult> LoginPost()
    {
        // Implement your login logic here
        // This method should handle user login
        return Ok();
    }
}