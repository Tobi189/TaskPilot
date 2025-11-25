using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Register Razor Pages.
builder.Services.AddRazorPages();

// Configure cookie-based authentication.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Paths used for automatic redirects during the authentication flow.
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

// Register authorization services (required for [Authorize] attributes).
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline for production environments.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable authentication and authorization middleware.
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages endpoints.
app.MapRazorPages();

app.Run();
