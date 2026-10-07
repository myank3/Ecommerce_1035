using Ecommerce_1035.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Ecommerce_1035.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmPhoneModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ConfirmPhoneModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = default!;
    }

    public IActionResult OnGet(string userId, string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(string userId, string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var phone = user.PhoneNumber?.Trim();
        if (string.IsNullOrEmpty(phone))
        {
            ModelState.AddModelError(string.Empty, "No phone number on file for this user.");
            return Page();
        }

        var code = Input.Code.Replace(" ", "").Replace("-", "").Trim();

        //  detect if this is a login-time OTP vs registration (change phone)
        var isLoginFlow = TempData["LoginUserId"] != null;

        if (isLoginFlow)
        {
            // Login-time flow: verify with the TWO-FACTOR token provider
            var valid2fa = await _userManager.VerifyTwoFactorTokenAsync(
                user, TokenOptions.DefaultPhoneProvider, code);

            Console.WriteLine($">>> ConfirmPhone (login flow): VerifyTwoFactorToken={valid2fa}");

            if (!valid2fa)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code.");
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var ret = (string?)TempData["LoginReturnUrl"];
            return LocalRedirect(ret ?? Url.Content("~/"));
        }

        // Registration flow: verify with the CHANGE PHONE token provider
        var result = await _userManager.ChangePhoneNumberAsync(user, phone, code);
        Console.WriteLine($">>> ConfirmPhone (registration): ChangePhoneNumberAsync={result.Succeeded}");

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                Console.WriteLine($">>> ERROR: {err.Code} - {err.Description}");

            ModelState.AddModelError(string.Empty, "Invalid verification code.");
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        TempData["StatusMessage"] = "Phone confirmed. Please log in.";
        return RedirectToPage("./Login");
    }
    /// <summary>
    /// Generate alternate phone formats in case the stored value is missing or has a different country code prefix.
    /// </summary>
    private static System.Collections.Generic.IEnumerable<string> TryAlternateFormats(string phone)
    {
        yield return phone.TrimStart('+');
        yield return "+" + phone.TrimStart('+');

        if (!phone.StartsWith("+"))
        {
            yield return "+91" + phone.TrimStart('0');   // adjust country code as needed
        }

        if (phone.StartsWith("+91"))
        {
            yield return phone.Substring(3);
        }
    }
}