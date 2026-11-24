using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Platform.IdentityServer.Controllers;

public class AccountController(
    SignInManager<IdentityUser> signInManager,
    IIdentityServerInteractionService interaction) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (interaction.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl ?? "/");
            }
            return Redirect("~/");
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout(string? logoutId)
    {
        await signInManager.SignOutAsync();
        var context = await interaction.GetLogoutContextAsync(logoutId);
        if (!string.IsNullOrEmpty(context?.PostLogoutRedirectUri))
        {
            return Redirect(context.PostLogoutRedirectUri);
        }
        return Redirect("~/");
    }
}

public class LoginViewModel : LoginInputModel
{
}

public class LoginInputModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberLogin { get; set; }
    public string? ReturnUrl { get; set; }
}
