using Ecommerce_1035.DataAccess.Data;
using Ecommerce_1035.DataAccess.Repository;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Ecommerce_1035.Utilities.Service;
using Ecommerce_1035.Utilities.Service.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity + external providers chained on the same builder
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientSecret = "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";
        options.ClientId = "331840492239-vc31bdovnf2c9t2aqqt7ls90q29mno08.apps.googleusercontent.com";
    })
    .AddLinkedIn(options =>
    {
        options.ClientId = "77pwu9vr7yf3zt";
        options.ClientSecret = "WPL_AP1.3G9A6exLXPIEninN.eHrIcA==";
    });
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    var clientId = config["PayPal:ClientId"];
    var clientSecret = config["PayPal:ClientSecret"];

    return new PaypalServerSdkClient.Builder()
        .ClientCredentialsAuth(
            new ClientCredentialsAuthModel.Builder(clientId, clientSecret)
                .Build())
        .Environment(PaypalServerSdk.Standard.Environment.Sandbox)
        .Build();
});

// Custom services
builder.Services.AddScoped<ITwilioSender, TwilioSender>();
builder.Services.AddScoped<IUnitofWork, UnitOfWork>();
builder.Services.AddScoped<ISmsSender, SmsSender>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
// MVC + Razor Pages
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromDays(31);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;

});


// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
});
builder.Services.AddHttpClient();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();