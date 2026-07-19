using PollBuilder.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Hook up your Clean Architecture Infrastructure (Database & Identity)
builder.Services.AddInfrastructureServices(builder.Configuration);

// --- NEW: Configure 30-Minute Session Timeout ---
builder.Services.ConfigureApplicationCookie(options =>
{
    // Set the exact timeout duration to 30 minutes
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

    // Resets the 30-minute timer if they are actively navigating the site
    options.SlidingExpiration = true;

    // Ensure redirects point to your newly styled Auth pages
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
// ------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 2. Add Authentication (Must be before Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages (Identity area)
app.MapRazorPages();

app.Run();