using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using PosItemVerificationWeb.Data;
using PosItemVerificationWeb.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using PosItemVerificationWeb.Models;

var builder = WebApplication.CreateBuilder(args);



// ========================================
// WINDOWS AUTHENTICATION - Environment Specific
// ========================================
if (builder.Environment.IsDevelopment())
{
    // Development: Use Negotiate handler for Kestrel
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
else
{
    // Production: Use IIS Authentication
    builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
}

builder.Services.AddScoped<RestaurantEventService>();


builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP
    options.ListenLocalhost(5237);

    // HTTPS
    options.ListenLocalhost(7145, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});



// ========================================
// MVC & RAZOR PAGES
// ========================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ========================================
// CUSTOM SERVICES
// ========================================
builder.Services.AddScoped<IPOSVerificationService, POSVerificationService>();
builder.Services.AddLogging();

builder.Services.Configure<AllowedUsersConfig>(
    builder.Configuration.GetSection("AllowedUsers"));

builder.Services.PostConfigure<AllowedUsersConfig>(cfg => cfg.Normalize());




// ========================================
// DATABASE CONTEXTS
// ========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var restaurantConnectionString = builder.Configuration.GetConnectionString("RestaurantOpeningConnection")
    ?? throw new InvalidOperationException("Connection string 'RestaurantOpeningConnection' not found.");

builder.Services.AddDbContext<RestaurantOpeningContext>(options =>
    options.UseSqlServer(restaurantConnectionString));

// ========================================
// DEVELOPMENT TOOLS
// ========================================
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// ========================================
// HTTP REQUEST PIPELINE
// ========================================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// CRITICAL: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// ========================================
// ROUTING
// ========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ========================================
// DATABASE INITIALIZATION
// ========================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RestaurantOpeningContext>();
    try
    {
        context.Database.EnsureCreated();
        // Add seed data if needed
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();