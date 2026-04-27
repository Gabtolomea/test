using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnlineClearance.Models;
using System.Security.Claims;

namespace OnlineClearance.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        // GET /Home/Login
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var existingRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Student";
                return RedirectToDashboard(existingRole);
            }
            return View(new LoginViewModel());
        }

        // POST /Home/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT id, username, password, first_name, last_name, role, is_active FROM users WHERE username = @username LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("@username", model.Username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                model.ErrorMessage = "Invalid username or password.";
                return View(model);
            }

            var user = new User
            {
                Id        = reader.GetInt32("id"),
                Username  = reader.GetString("username"),
                Password  = reader.GetString("password"),
                FirstName = reader.GetString("first_name"),
                LastName  = reader.GetString("last_name"),
                Role      = reader.IsDBNull(reader.GetOrdinal("role")) ? null : reader.GetString("role"),
                IsActive  = reader.GetBoolean("is_active")
            };
            reader.Close();

            if (!user.IsActive)
            {
                model.ErrorMessage = "Your account is deactivated.";
                return View(model);
            }

            // Password check (BCrypt hash OR plain text for demo accounts)
            bool valid = user.Password.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(model.Password, user.Password)
                : user.Password == model.Password;

            if (!valid)
            {
                model.ErrorMessage = "Invalid username or password.";
                return View(model);
            }

            // Build claims and sign in
            var role = user.Role ?? "Student";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim("FullName",                user.FullName),
                new Claim(ClaimTypes.Role,           role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Pass role directly — User.Claims is NOT updated until the NEXT request
            return RedirectToDashboard(role);
        }

        // GET /Home/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET /Home/AccessDenied
        // Shown when a logged-in user tries to access a page they don't have permission for
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET / — root goes to Login
        public IActionResult Index() => RedirectToAction("Login");

        // Redirects to the correct dashboard based on role
        private IActionResult RedirectToDashboard(string role)
        {
            return role switch
            {
                "Admin"      => RedirectToAction("Index", "Admin"),
                "Instructor" => RedirectToAction("Index", "Instructor"),
                _            => RedirectToAction("Index", "Student")
            };
        }
    }
}
