using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using FurShield.Data;

namespace FurShield.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = default!;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = default!;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 1. Find User by Email
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null)
                {
                    // 2. Perform Password Check
                    var result = await _signInManager.CheckPasswordSignInAsync(
                        user,
                        Input.Password,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // 3. Sign in the user
                        await _signInManager.SignInAsync(user, Input.RememberMe);

                        // 4. Update Name Claim (for FullName display)
                        var existingClaims = await _userManager.GetClaimsAsync(user);
                        var nameClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                        string displayName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.UserName!;

                        if (nameClaim != null)
                        {
                            await _userManager.ReplaceClaimAsync(user, nameClaim, new Claim(ClaimTypes.Name, displayName));
                        }
                        else
                        {
                            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, displayName));
                        }

                        await _signInManager.RefreshSignInAsync(user);

                        _logger.LogInformation("User logged in successfully.");

                        // 5. Detect Role Automatically & Redirect
                        var userRoles = await _userManager.GetRolesAsync(user);

                        if (userRoles.Contains("Admin") || userRoles.Contains("SuperAdmin"))
                        {
                            return RedirectToAction("Index", "SuperAdmin", new { area = "" });
                        }
                        if (userRoles.Contains("Vet") || userRoles.Contains("Veterinarian"))
                        {
                            return RedirectToAction("Index", "Veterinarian", new { area = "" });
                        }
                        if (userRoles.Contains("Shelter"))
                        {
                            return RedirectToAction("Index", "Shelter", new { area = "" });
                        }
                        if (userRoles.Contains("PetOwner"))
                        {
                            return RedirectToAction("Index", "PetOwner", new { area = "" });
                        }

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && !returnUrl.Contains("/Identity/Account/Login"))
                        {
                            return LocalRedirect(returnUrl);
                        }

                        return RedirectToAction("Index", "Home", new { area = "" });
                    }

                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            return Page();
        }
    }
}