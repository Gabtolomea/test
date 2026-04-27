// ============================================================
// Controllers/StudentController.cs
// Only students can access these pages.
// [Authorize(Roles = "Student")] enforces this automatically.
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnlineClearance.Models;
using System.Security.Claims;

namespace OnlineClearance.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly IConfiguration _config;

        public StudentController(IConfiguration config)
        {
            _config = config;
        }

        // GET /Student/Index  — Student Dashboard
        public IActionResult Index()
        {
            // Get the logged-in user's ID from their cookie claims
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            // 1. Get student info (joined with users table for full name)
            var student = GetStudent(conn, userId);
            if (student == null) return RedirectToAction("Login", "Home");

            // 2. Get the active academic period
            var period = GetActivePeriod(conn);

            // 3. Get subject clearances for this student
            var subjectClearances = GetSubjectClearances(conn, student.StudentNumber, period?.Id);

            // 4. Get org clearances for this student
            var orgClearances = GetOrgClearances(conn, student.StudentNumber, period?.Id);

            // 5. Get latest announcements
            var announcements = GetAnnouncements(conn);

            // Bundle everything into one ViewModel and pass to the view
            var vm = new StudentDashboardViewModel
            {
                Student = student,
                ActivePeriod = period,
                SubjectClearances = subjectClearances,
                OrgClearances = orgClearances,
                Announcements = announcements
            };

            return View(vm);
        }

        // ---- Private helper methods ----

        private Student? GetStudent(MySqlConnection conn, int userId)
        {
            var cmd = new MySqlCommand(@"
                SELECT s.id, s.user_id, s.student_number, s.status,
                       CONCAT(u.first_name, ' ', u.last_name) AS full_name,
                       u.username
                FROM students s
                JOIN users u ON u.id = s.user_id
                WHERE s.user_id = @userId
                LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new Student
            {
                Id = r.GetInt32("id"),
                UserId = r.GetInt32("user_id"),
                StudentNumber = r.GetString("student_number"),
                Status = r.GetString("status"),
                FullName = r.GetString("full_name"),
                Username = r.GetString("username")
            };
        }

        private AcademicPeriod? GetActivePeriod(MySqlConnection conn)
        {
            var cmd = new MySqlCommand(
                "SELECT id, academic_year, semester FROM academic_periods WHERE is_active = 1 LIMIT 1", conn);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new AcademicPeriod
            {
                Id = r.GetInt32("id"),
                AcademicYear = r.GetString("academic_year"),
                Semester = r.GetString("semester"),
                IsActive = true
            };
        }

        private List<ClearanceSubject> GetSubjectClearances(MySqlConnection conn, string studentNumber, int? periodId)
        {
            var list = new List<ClearanceSubject>();

            // Join clearance_subjects → subject_offerings → subjects → signatories → users
            // to get subject title and instructor name in one query
            var cmd = new MySqlCommand(@"
                SELECT cs.id, cs.student_number, cs.mis_code, cs.status,
                       cs.remarks, cs.period_id, cs.signed_at,
                       sub.title AS subject_title, sub.subject_code,
                       CONCAT(u.first_name, ' ', u.last_name) AS instructor_name,
                       st.label AS status_label
                FROM clearance_subjects cs
                JOIN subject_offerings so ON so.mis_code = cs.mis_code
                JOIN subjects sub ON sub.subject_code = so.subject_code
                JOIN signatories sig ON sig.employee_id = so.instructor_id
                JOIN users u ON u.id = sig.user_id
                LEFT JOIN status_table st ON st.id = cs.status
                WHERE cs.student_number = @sn
                  AND (@pid IS NULL OR cs.period_id = @pid)", conn);

            cmd.Parameters.AddWithValue("@sn", studentNumber);
            cmd.Parameters.AddWithValue("@pid", (object?)periodId ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ClearanceSubject
                {
                    Id = r.GetInt32("id"),
                    StudentNumber = r.GetString("student_number"),
                    MisCode = r.GetString("mis_code"),
                    Status = r.IsDBNull(r.GetOrdinal("status")) ? null : r.GetInt32("status"),
                    Remarks = r.IsDBNull(r.GetOrdinal("remarks")) ? null : r.GetString("remarks"),
                    SubjectTitle = r.GetString("subject_title"),
                    SubjectCode = r.GetString("subject_code"),
                    InstructorName = r.GetString("instructor_name"),
                    StatusLabel = r.IsDBNull(r.GetOrdinal("status_label")) ? "Pending" : r.GetString("status_label")
                });
            }
            return list;
        }

        private List<ClearanceOrg> GetOrgClearances(MySqlConnection conn, string studentNumber, int? periodId)
        {
            var list = new List<ClearanceOrg>();
            var cmd = new MySqlCommand(@"
                SELECT co.id, co.student_number, co.org_name, co.org_signatory,
                       co.status, st.label AS status_label
                FROM clearance_organization co
                LEFT JOIN status_table st ON st.id = co.status
                WHERE co.student_number = @sn
                  AND (@pid IS NULL OR co.period_id = @pid)", conn);

            cmd.Parameters.AddWithValue("@sn", studentNumber);
            cmd.Parameters.AddWithValue("@pid", (object?)periodId ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ClearanceOrg
                {
                    Id = r.GetInt32("id"),
                    StudentNumber = r.GetString("student_number"),
                    OrgName = r.IsDBNull(r.GetOrdinal("org_name")) ? "" : r.GetString("org_name"),
                    OrgSignatory = r.IsDBNull(r.GetOrdinal("org_signatory")) ? "" : r.GetString("org_signatory"),
                    Status = r.IsDBNull(r.GetOrdinal("status")) ? null : r.GetInt32("status"),
                    StatusLabel = r.IsDBNull(r.GetOrdinal("status_label")) ? "Pending" : r.GetString("status_label")
                });
            }
            return list;
        }

        private List<Announcement> GetAnnouncements(MySqlConnection conn)
        {
            var list = new List<Announcement>();
            var cmd = new MySqlCommand(
                "SELECT id, title, content, type, created_at FROM announcements ORDER BY created_at DESC LIMIT 5", conn);

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
            return list;
        }
    }
}
