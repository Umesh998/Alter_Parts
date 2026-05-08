// Controllers/AccountController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleSeederService roleSeeder) : Controller
{
    // ── GET /Account/Login ────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // ── POST /Account/Login ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);

        // Don't reveal whether email exists (security best practice)
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            isPersistent: model.RememberMe, // ← "Remember Me" cookie
            lockoutOnFailure: true);         // ← triggers lockout policy

        if (result.Succeeded)
        {
            // Safe redirect — prevent open redirect attacks
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
            var remaining = lockoutEnd - DateTimeOffset.UtcNow;
            ModelState.AddModelError("",
                $"Account locked. Try again in {remaining?.Minutes + 1} minutes.");
            return View(model);
        }

        var failedAttempts = await userManager.GetAccessFailedCountAsync(user);
        var attemptsLeft = 5 - failedAttempts;
        ModelState.AddModelError("",
            $"Invalid credentials. {attemptsLeft} attempt(s) remaining.");

        return View(model);
    }

    // ── POST /Account/Logout ──────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    // ── GET /Account/Register (Admin only) ───────────────────────────────
    [HttpGet, Authorize(Policy = "AdminOnly")]
    public IActionResult Register() => View(new RegisterViewModel());

    // ── POST /Account/Register ────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Department = model.Department
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"User {model.FullName} created successfully.";
            return RedirectToAction("Register");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    // ── GET /Account/AccessDenied ─────────────────────────────────────────
    public IActionResult AccessDenied() => View();





    // Full controller restriction
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller { }

    // Full controller, multiple roles
    [Authorize(Roles = "Admin,Engineer")]
    public class BomController : Controller { }

    // Per-action restriction
    public class ReportsController : Controller
    {
        [Authorize(Roles = "Admin,Engineer,Viewer")]
        public IActionResult Index() => View();

        [Authorize(Policy = "CanEditBom")]      // uses named policy
        public IActionResult Edit(int id) => View();

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id) => View();

        [AllowAnonymous]                         // public, no auth needed
        public IActionResult PublicSummary() => View();
    }
}