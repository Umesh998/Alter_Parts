using Alter_Parts.Models;
using Alter_Parts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Alter_Parts.Controllers;

[Route("[controller]/[action]")]
public sealed class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOGIN
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("More_Fruits", "Component");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            userName: model.Email,
            password: model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            _logger.LogInformation("User {Email} logged in at {Time}.", model.Email, DateTimeOffset.UtcNow);

            // Role-based redirect after login
            return LocalRedirect(returnUrl ?? Url.Content("~/"));

            
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} is locked out.", model.Email);
            return RedirectToAction(nameof(Lockout));
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Login is not allowed. Please confirm your account.");
            return View(model);
        }

        // Generic message — never distinguish bad email vs bad password
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOGOUT
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var email = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Email} logged out at {Time}.", email, DateTimeOffset.UtcNow);
        return RedirectToAction("Login", "Account");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REGISTER
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("More_Fruits", "Component");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Assign default role on registration
            await _userManager.AddToRoleAsync(user, "User");

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("New user {Email} registered at {Time}.", model.Email, DateTimeOffset.UtcNow);
            return RedirectToAction("More_Fruits", "Component");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MANAGE PROFILE  (any authenticated user)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var model = new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = await _userManager.GetRolesAsync(user)
        };

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CHANGE PASSWORD  (any authenticated user)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user); // refresh cookie after password change
            _logger.LogInformation("User {Email} changed their password.", user.Email);
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // USER MANAGEMENT  (Admin only)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult ManageUsers()
    {
        var users = _userManager.Users
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                LockoutEnd = u.LockoutEnd
            })
            .ToList();

        return View(users);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (!await _roleManager.RoleExistsAsync(role))
        {
            TempData["Error"] = $"Role '{role}' does not exist.";
            return RedirectToAction(nameof(ManageUsers));
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
            _logger.LogInformation("Admin assigned role {Role} to user {Email}.", role, user.Email);
        }

        TempData["Success"] = $"Role '{role}' assigned to {user.Email}.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.RemoveFromRoleAsync(user, role);
            _logger.LogInformation("Admin revoked role {Role} from user {Email}.", role, user.Email);
        }

        TempData["Success"] = $"Role '{role}' revoked from {user.Email}.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        _logger.LogWarning("Admin locked account for user {Email}.", user.Email);

        TempData["Success"] = $"{user.Email} has been locked.";
        return RedirectToAction(nameof(ManageUsers));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        _logger.LogInformation("Admin unlocked account for user {Email}.", user.Email);

        TempData["Success"] = $"{user.Email} has been unlocked.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILITY PAGES
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();



    // ─────────────────────────────────────────────────────────────────────────────
    // CREATE USER  (Admin only)
    // ─────────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateUser() => View();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "A user with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.Role))
                await _userManager.AddToRoleAsync(user, model.Role);
            else
                await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("Admin created new user {Email} with role {Role}.", model.Email, model.Role);
            TempData["Success"] = $"User {model.Email} created successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DELETE USER  (Admin only)
    // ─────────────────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        // Prevent admin from deleting themselves
        var currentUserId = _userManager.GetUserId(User);
        if (userId == currentUserId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(ManageUsers));
        }

        await _userManager.DeleteAsync(user);
        _logger.LogWarning("Admin deleted user {Email}.", user.Email);
        TempData["Success"] = $"User {user.Email} deleted.";
        return RedirectToAction(nameof(ManageUsers));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // EDIT USER  (Admin only)
    // ─────────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            CurrentRoles = roles.ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        _logger.LogInformation("Admin updated user {Email}.", user.Email);
        TempData["Success"] = $"User {user.Email} updated successfully.";
        return RedirectToAction(nameof(ManageUsers));
    }
}