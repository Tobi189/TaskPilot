using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Npgsql;

namespace TaskPilot.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly string _connectionString;

        public LoginModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Pg");
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Dashboard");

            return Page();
        }

        [BindProperty]
        public string UsernameOrEmail { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT id, username, email, password_hash
                FROM users
                WHERE username = @u OR email = @u
                LIMIT 1;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", UsernameOrEmail);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return Page();
            }

            var userId = reader.GetInt32(0);
            var username = reader.GetString(1);
            var email = reader.GetString(2);
            var storedHash = reader.GetString(3);

            var hasher = new PasswordHasher<string>();
            var verification = hasher.VerifyHashedPassword(null, storedHash, Password);

            if (verification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToPage("/Dashboard");
        }
    }
}
