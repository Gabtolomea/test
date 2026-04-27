// ============================================================
// Controllers/AdminController.cs
// Admin can see all students' clearance summary and manage users.
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnlineClearance.Models;

namespace OnlineClearance.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IConfiguration _config;

        public AdminController(IConfiguration config)
        {
            _config = config;
        }

        // GET /Admin/Index — list all students and their clearance progress
        public IActionResult Index()
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand(@"
                SELECT s.student_number,
                       CONCAT(u.first_name, ' ', u.last_name) AS full_name,
                       s.status AS student_status,
                       COUNT(cs.id) AS total_subjects,
                       SUM(CASE WHEN cs.status = 2 THEN 1 ELSE 0 END) AS cleared_subjects
                FROM students s
                JOIN users u ON u.id = s.user_id
                LEFT JOIN clearance_subjects cs ON cs.student_number = s.student_number
                GROUP BY s.student_number, full_name, student_status
                ORDER BY full_name", conn);

            var students = new List<dynamic>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int total = r.GetInt32("total_subjects");
                int cleared = r.IsDBNull(r.GetOrdinal("cleared_subjects")) ? 0 : r.GetInt32("cleared_subjects");
                students.Add(new
                {
                    StudentNumber = r.GetString("student_number"),
                    FullName = r.GetString("full_name"),
                    StudentStatus = r.GetString("student_status"),
                    Total = total,
                    Cleared = cleared,
                    IsFullyCleared = total > 0 && cleared == total
                });
            }

            ViewBag.Students = students;
            return View();
        }

        // GET /Admin/Users — list all users
        public IActionResult Users()
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand(@"
                SELECT id, username, first_name, last_name, role, is_active, created_at
                FROM users ORDER BY created_at DESC", conn);

            var users = new List<User>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                users.Add(new User
                {
                    Id = r.GetInt32("id"),
                    Username = r.GetString("username"),
                    FirstName = r.GetString("first_name"),
                    LastName = r.GetString("last_name"),
                    Role = r.IsDBNull(r.GetOrdinal("role")) ? "Student" : r.GetString("role"),
                    IsActive = r.GetBoolean("is_active")
                });
            }

            return View(users);
        }

        // POST /Admin/ToggleUser  — Activate or deactivate a user account
        [HttpPost]
        public IActionResult ToggleUser(int userId)
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand(
                "UPDATE users SET is_active = NOT is_active WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();

            TempData["Success"] = "User status updated.";
            return RedirectToAction("Users");
        }

        // GET /Admin/Announcements — manage announcements
        public IActionResult Announcements()
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT id, title, content, type, created_at FROM announcements ORDER BY created_at DESC", conn);
            var list = new List<Announcement>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Announcement
                {
                    Id = r.GetInt32("id"),
                    Title = r.GetString("title"),
                    Content = r.GetString("content"),
                    Type = r.GetString("type"),
                    CreatedAt = r.GetDateTime("created_at")
                });
            }

            return View(list);
        }

        // POST /Admin/AddAnnouncement
        [HttpPost]
        public IActionResult AddAnnouncement(string title, string content, string type)
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand(
                "INSERT INTO announcements (title, content, type) VALUES (@t, @c, @ty)", conn);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@c", content);
            cmd.Parameters.AddWithValue("@ty", type);
            cmd.ExecuteNonQuery();

            TempData["Success"] = "Announcement posted.";
            return RedirectToAction("Announcements");
        }

        // POST /Admin/DeleteAnnouncement
        [HttpPost]
        public IActionResult DeleteAnnouncement(int id)
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand("DELETE FROM announcements WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            TempData["Success"] = "Announcement deleted.";
            return RedirectToAction("Announcements");
        }
    }
}
