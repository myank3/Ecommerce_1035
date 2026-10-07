// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;

namespace Ecommerce_1035.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IUnitofWork _unitofwork;
    private readonly ISmsSender _sms;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender,
        IUnitofWork work,
        RoleManager<IdentityRole> roleManager,
        ISmsSender sms)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
        _unitofwork = work;
        _roleManager = roleManager;
        _sms = sms;
    }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        public string? StreetAddress { get; set; }
        public string? City { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public int? CompanyId { get; set; }
        public string? Role { get; set; }

        public IEnumerable<SelectListItem>? RoleList { get; set; }
        public IEnumerable<SelectListItem>? CompanyList { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        Input = new InputModel();
        PopulateDropdowns(Input);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            var user = CreateUser();

            user.Name = Input.Name;
            user.StreetAddress = Input.StreetAddress;
            user.City = Input.City;
            user.PhoneNumber = Input.PhoneNumber;
            user.State = Input.State;
            user.PostalCode = Input.PostalCode;
            user.CompanyId = Input.CompanyId;
            user.Role = Input.Role;

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Ensure base roles exist
                if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                if (!await _roleManager.RoleExistsAsync(SD.Role_Employee))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));
                if (!await _roleManager.RoleExistsAsync(SD.Role_Company))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Company));
                if (!await _roleManager.RoleExistsAsync(SD.Role_Individual))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Individual));

                // Assign proper role
                const string AdminEmail = "vibeound@gmail.com";
                var isRootAdmin = string.Equals(Input.Email, AdminEmail, StringComparison.OrdinalIgnoreCase);

                if (isRootAdmin)
                {
                    await _userManager.AddToRoleAsync(user, SD.Role_Admin);
                }
                else if (Input.Role == SD.Role_Admin)
                {
                    _logger.LogWarning("Blocked admin self-registration attempt from {Email}", Input.Email);
                    await _userManager.AddToRoleAsync(user, SD.Role_Individual);
                }
                else if (Input.Role == null && Input.CompanyId == null)
                {
                    await _userManager.AddToRoleAsync(user, SD.Role_Individual);
                }
                else if (Input.CompanyId > 0)
                {
                    await _userManager.AddToRoleAsync(user, SD.Role_Company);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, Input.Role);
                }

                var userId = await _userManager.GetUserIdAsync(user);

                // 1. Generate & Send Email Confirmation
                var emailCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                emailCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailCode));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = emailCode, returnUrl = returnUrl },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                // 2. Generate & Send SMS (if a phone number was supplied)
                if (!string.IsNullOrWhiteSpace(Input.PhoneNumber))
                {
                    try
                    {
                        var smsToken = await _userManager.GenerateChangePhoneNumberTokenAsync(user, Input.PhoneNumber);
                        await _sms.SendSmsBeeAsync(
                            Input.PhoneNumber,
                            Input.Email,
                            $"Your Book Store registration code is: {smsToken}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to dispatch SMS to {Phone}", Input.PhoneNumber);
                    }
                }

                // Redirect to the holding page asking the user to check their email
                return RedirectToPage("RegisterConfirmation", new { area = "Identity", email = Input.Email, returnUrl = returnUrl });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // Repopulate select lists before redisplaying the form upon validation failure
        PopulateDropdowns(Input);
        return Page();
    }

    private void PopulateDropdowns(InputModel model)
    {
        model.CompanyList = _unitofwork.Company.GetAll().Select(cl => new SelectListItem
        {
            Text = cl.Name,
            Value = cl.Id.ToString()
        });

        model.RoleList = _roleManager.Roles
            .Where(r => r.Name != SD.Role_Individual)
            .Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Name
            });
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<ApplicationUser>)_userStore;
    }
}