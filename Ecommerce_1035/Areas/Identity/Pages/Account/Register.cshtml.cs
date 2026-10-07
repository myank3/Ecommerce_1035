// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Ecommerce_1035.Utilities.Service;
using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

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
        IEmailSender emailSender, IUnitofWork work,
        RoleManager<IdentityRole> roleManager, ISmsSender sms)
        
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

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = default!;

    /// <summary>
    /// 

    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = default!;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
       
        public string Name { get; set; }
        public string StreetAddress { get; set; }
     
        public string EmailAddress { get; set; }

        public string City { get; set; }
        public string PhoneNumber { get; set; }
        public string State { get; set; }

        public string PostalCode { get; set; }
        public int? CompanyId { get; set; }

        public string Role { get; set; }
        public IEnumerable<SelectListItem> RoleList { get; set; }
        public IEnumerable<SelectListItem> CompanyList { get; set; }
    }


    public async Task OnGetAsync(string? returnUrl = null)
    {
        Input = new InputModel()
        {
            CompanyList = _unitofwork.Company.GetAll().Select(cl => new SelectListItem
            {
                Text = cl.Name,
                Value = cl.Id.ToString()
            }),
            RoleList = _roleManager.Roles.Where(r => r.Name != SD.Role_Individual).Select(r => r.Name).Select(rl => new SelectListItem
            {
                Text = rl,
                Value =rl

            })

        };
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser()
            {
                Name = Input.Name,
                Email = Input.Email,
                UserName = Input.Email,
                StreetAddress = Input.StreetAddress,
                City = Input.City,
                PhoneNumber = Input.PhoneNumber,
                State = Input.State,
                PostalCode = Input.PostalCode,
                CompanyId = Input.CompanyId,
                Role = Input.Role
            };

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                if (!await _roleManager.RoleExistsAsync(SD.Role_Employee))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));
                if (!await _roleManager.RoleExistsAsync(SD.Role_Company))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Company));
                if (!await _roleManager.RoleExistsAsync(SD.Role_Individual))
                    await _roleManager.CreateAsync(new IdentityRole(SD.Role_Individual));


                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme)!;

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                const string AdminEmail = "vibeound@gmail.com";
                var isRootAdmin = string.Equals(
                    Input.Email,
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);

                if (isRootAdmin)
                {
                    await _userManager.AddToRoleAsync(user, SD.Role_Admin);
                }
                else if (Input.Role == SD.Role_Admin)
                {
                    _logger.LogWarning(
                        "Blocked admin self-registration attempt from {Email}",
                        Input.Email);

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

                // Redirect straight to ConfirmEmail page
                return RedirectToPage("ConfirmEmail", new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return Page();
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
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
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
