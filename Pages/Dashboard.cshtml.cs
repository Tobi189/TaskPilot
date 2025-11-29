using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Security.Claims;

namespace TaskPilot.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly string _connectionString;

        public string Username { get; private set; } = string.Empty;

        public List<TaskItem> Tasks { get; private set; } = new();

        // For create / edit modal
        [BindProperty]
        public int? TaskId { get; set; }

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string? Content { get; set; }

        [BindProperty]
        public string Status { get; set; } = "pending";

        [BindProperty]
        public int Priority { get; set; } = 0;

        public DashboardModel(ILogger<DashboardModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("Pg");
        }

        public void OnGet()
        {
            Username = User.Identity?.Name ?? "User";
            LoadTasks();
        }

        // Handles both create and edit via asp-page-handler="SaveTask"
        public async Task<IActionResult> OnPostSaveTaskAsync()
        {
            Username = User.Identity?.Name ?? "User";

            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError(nameof(Title), "Title is required.");
                LoadTasks();
                return Page();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogError("Could not resolve user id from claims.");
                return Forbid();
            }

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            if (TaskId == null || TaskId == 0)
            {
                // CREATE
                const string insertSql = @"
                    INSERT INTO tasks (user_id, title, content, status, priority)
                    VALUES (@user_id, @title, @content, @status, @priority);
                ";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.Parameters.AddWithValue("@title", Title);
                cmd.Parameters.AddWithValue("@content", (object?)Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", Status ?? "pending");
                cmd.Parameters.AddWithValue("@priority", Priority);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                // UPDATE
                const string updateSql = @"
                    UPDATE tasks
                    SET title = @title,
                        content = @content,
                        status = @status,
                        priority = @priority,
                        updated_at = NOW()
                    WHERE id = @id AND user_id = @user_id;
                ";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@id", TaskId.Value);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.Parameters.AddWithValue("@title", Title);
                cmd.Parameters.AddWithValue("@content", (object?)Content ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", Status ?? "pending");
                cmd.Parameters.AddWithValue("@priority", Priority);
                await cmd.ExecuteNonQueryAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteTaskAsync(int id)
        {
            Username = User.Identity?.Name ?? "User";

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogError("Could not resolve user id from claims.");
                return Forbid();
            }

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string deleteSql = @"DELETE FROM tasks WHERE id = @id AND user_id = @user_id;";

            await using var cmd = new NpgsqlCommand(deleteSql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@user_id", userId);
            await cmd.ExecuteNonQueryAsync();

            return RedirectToPage();
        }

        private void LoadTasks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Authenticated user has no valid NameIdentifier claim.");
                return;
            }

            Tasks.Clear();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            const string sql = @"
                SELECT id, title, content, status, priority, created_at, updated_at
                FROM tasks
                WHERE user_id = @user_id
                ORDER BY created_at DESC;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var task = new TaskItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Status = reader.IsDBNull(3) ? "pending" : reader.GetString(3),
                    Priority = reader.IsDBNull(4) ? 0 : reader.GetInt16(4),
                    CreatedAt = reader.GetDateTime(5),
                    UpdatedAt = reader.GetDateTime(6)
                };

                Tasks.Add(task);
            }
        }

        public class TaskItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string Status { get; set; } = "pending";
            public int Priority { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
