using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Ecommerce_1035.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWithPhone2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISmsSender _sms;

    public LoginWithPhone2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ISmsSender sms)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _sms = sms;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = default!;

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToPage("./Login");

        await SendCodeAsync(user);

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        returnUrl ??= Url.Content("~/");

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return RedirectToPage("./Login");

        var code = Input.Code.Replace(" ", "").Replace("-", "");

        var result = await _signInManager.TwoFactorSignInAsync(
            TokenOptions.DefaultPhoneProvider,
            code,
            rememberMe,
            Input.RememberMachine);

        if (result.Succeeded)
            return LocalRedirect(returnUrl);

        if (result.IsLockedOut)
            return RedirectToPage("./Lockout");

        ModelState.AddModelError(string.Empty, "Invalid verification code.");
        return Page();
    }

    private async Task SendCodeAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
        await _sms.SendSmsAsync(user.PhoneNumber, $"Your Book Store login code is: {token}");
    }
}