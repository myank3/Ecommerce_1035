using Ecommerce_1035.DataAccess.Data;
using Ecommerce_1035.DataAccess.Repository;
using Ecommerce_1035.DataAccess.Repository.IRepository;
using Ecommerce_1035.Models.Models;
using Ecommerce_1035.Utilities;
using Ecommerce_1035.Utilities.Service;
using Ecommerce_1035.Utilities.Service.IService;
using Ecommerce_1035.Utility;
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
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId not configured.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientSecret not configured.");
    })
    .AddLinkedIn(options =>
    {
        options.ClientId = builder.Configuration["Authentication:LinkedIn:ClientId"]
            ?? throw new InvalidOperationException("Authentication:LinkedIn:ClientId not configured.");
        options.ClientSecret = builder.Configuration["Authentication:LinkedIn:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:LinkedIn:ClientSecret not configured.");
    });

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    var clientId = config["PayPal:ClientId"]
        ?? throw new InvalidOperationException("PayPal:ClientId not configured.");
    var clientSecret = config["PayPal:ClientSecret"]
        ?? throw new InvalidOperationException("PayPal:ClientSecret not configured.");

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
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

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