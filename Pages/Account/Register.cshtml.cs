using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Microsoft.AspNetCore.Identity;

namespace TaskPilot.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly string _connectionString;

        public RegisterModel(IConfiguration configuration)
        {
            // Reads "ConnectionStrings:Pg" from appsettings + user secrets.
            _connectionString = configuration.GetConnectionString("Pg");
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public bool Submitted { get; set; }

        public IActionResult OnPost()
        {
            var hasher = new PasswordHasher<string>();
            string hash = hasher.HashPassword(null, Password);

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var checkCmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE username = @username OR email = @email;",
                conn
            );

            checkCmd.Parameters.AddWithValue("@username", Name);
            checkCmd.Parameters.AddWithValue("@email", Email);

            long exists = (long)checkCmd.ExecuteScalar();

            if (exists > 0)
            {
                ModelState.AddModelError(string.Empty, "Username or email is already in use.");
                return Page();
            }

            using var insertCmd = new NpgsqlCommand(
                "INSERT INTO users (username, email, password_hash) VALUES (@username, @email, @password_hash);",
                conn
            );

            insertCmd.Parameters.AddWithValue("@username", Name);
            insertCmd.Parameters.AddWithValue("@email", Email);
            insertCmd.Parameters.AddWithValue("@password_hash", hash);

            insertCmd.ExecuteNonQuery();

            Submitted = true;

            return RedirectToPage("/Account/Login");
        }
    }
}
