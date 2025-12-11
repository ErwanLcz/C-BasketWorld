using BasketWorld.Data;
using BasketWorld.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BasketWorld.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
{
    opts.Password.RequireNonAlphanumeric = false;
    opts.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<BalldontlieClient>();
builder.Services.AddScoped<NbaSyncService>();
builder.Services.AddHttpClient<TheSportsDbClient>();
builder.Services.AddScoped<EuroleagueSyncService>();
builder.Services.AddHttpClient<EuroleagueOfficialClient>(client =>
{
    client.BaseAddress = new Uri("https://api-live.euroleague.net/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BasketWorld/1.0");
});
builder.Services.AddScoped<EuroleagueOfficialSyncService>();




var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Identity UI (Register/Login/Logout/Manage)
app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await BasketWorld.Data.DbSeeder.SeedAsync(app.Services);
app.Run();
