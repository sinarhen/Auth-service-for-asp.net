﻿using Data;
using Data.Tables;
using Lib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Web.Config;
using Web.Models;

namespace Web.Controllers;

public class AccountController : Controller
{
    private readonly Db db;
    private readonly UserManager<User> userManager;
    private readonly Jwt jwt;

    public AccountController(Db db, UserManager<User> userManager, Jwt jwt)
    {
        this.db = db;
        this.userManager = userManager;
        this.jwt = jwt;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var uid = User.Uid();

        var user = await db.Users.QueryOne(x => x.Id == uid);
        if (user == null) return RedirectToAction("NotFoundPage", "Home");

        return View(new AccountViewModel(user.Id, user.Email));
    }

    [HttpGet]
    public IActionResult Delete()
    {
        return View(new DeleteViewModel(new List<string>()));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ConfirmDelete()
    {
        var uid = User.Uid();

        var user = await db.Users.QueryOne(x => x.Id == uid);
        if (user == null) return Redirect("/");

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded) return Redirect("/");

        return View("Delete", new DeleteViewModel(result.Errors.Select(x => x.Description)));
    }

    [HttpGet]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("token");
        return Redirect("/");
    }

    [HttpGet]
    [Authorize]
    public IActionResult Email()
    {
        return View();
    }

    [NonAction]
    public void AddAuthCookie(string token)
    {
        Response.Cookies.Append("token", token, new()
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTimeOffset.Now.AddDays(30)
        });
    }

    public record EmailBody(string Email);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangeEmail(EmailBody body)
    {
        var uid = User.Uid();

        var user = await db.Users.QueryOne(x => x.Id == uid);
        if (user == null) return Redirect("/");

        user.Email = body.Email;
        user.NormalizedEmail = body.Email.ToUpper();
        user.Version += 1;

        var saved = await db.Save();
        if (!saved) return Redirect("/Account");

        var token = jwt.Token(user.Id, user.Version);
        AddAuthCookie(token);

        return Redirect("/Account");
    }
}