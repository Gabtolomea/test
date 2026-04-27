// ============================================================
// Controllers/InstructorController.cs
// Instructors can see their students and approve/decline clearance.
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnlineClearance.Models;
using System.Security.Claims;

namespace OnlineClearance.Controllers
{
    [Authorize(Roles = "Instructor")]
    public class InstructorController : Controller
    {
        private readonly IConfiguration _config;

        public InstructorController(IConfiguration config)
        {
            _config = config;
        }

        // GET /Instructor/Index  — shows all subject offerings this instructor handles
        public IActionResult Index()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            // Find the instructor's employee_id via signatories table
            var empIdCmd = new MySqlCommand(
                "SELECT employee_id FROM signatories WHERE user_id = @uid LIMIT 1", conn);
            empIdCmd.Parameters.AddWithValue("@uid", userId);
            var employeeId = empIdCmd.ExecuteScalar()?.ToString();

            if (string.IsNullOrEmpty(employeeId))
            {
                ViewBag.Message = "No instructor record found for your account.";
                return View(new List<InstructorClearanceViewModel>());
            }

            // Get all subject offerings taught by this instructor
            var cmd = new MySqlCommand(@"
                SELECT so.mis_code, sub.title,
                       COUNT(cs.id) AS student_count,
                       SUM(CASE WHEN cs.status = 2 THEN 1 ELSE 0 END) AS cleared_count
                FROM subject_offerings so
                JOIN subjects sub ON sub.subject_code = so.subject_code
                LEFT JOIN clearance_subjects cs ON cs.mis_code = so.mis_code
                WHERE so.instructor_id = @empId
                GROUP BY so.mis_code, sub.title", conn);
            cmd.Parameters.AddWithValue("@empId", employeeId);

            var list = new List<dynamic>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    MisCode = r.GetString("mis_code"),
                    SubjectTitle = r.GetString("title"),
                    StudentCount = r.GetInt32("student_count"),
                    ClearedCount = r.IsDBNull(r.GetOrdinal("cleared_count")) ? 0 : r.GetInt32("cleared_count")
                });
            }

            ViewBag.Subjects = list;
            ViewBag.EmployeeId = employeeId;
            return View();
        }

        // GET /Instructor/Students?misCode=MIS-789
        //   Shows all students enrolled in a subject, with their clearance status
        public IActionResult Students(string misCode)
        {
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            // Get subject info
            var infoCmd = new MySqlCommand(@"
                SELECT sub.title, so.mis_code
                FROM subject_offerings so
                JOIN subjects sub ON sub.subject_code = so.subject_code
                WHERE so.mis_code = @mc", conn);
            infoCmd.Parameters.AddWithValue("@mc", misCode);
            using var infoR = infoCmd.ExecuteReader();
            string subjectTitle = misCode;
            if (infoR.Read()) subjectTitle = infoR.GetString("title");
            infoR.Close();

            // Get all clearance records for this subject
            var cmd = new MySqlCommand(@"
                SELECT cs.id, cs.student_number, cs.status, cs.remarks, cs.signed_at,
                       CONCAT(u.first_name, ' ', u.last_name) AS student_name,
                       st.label AS status_label
                FROM clearance_subjects cs
                JOIN students s ON s.student_number = cs.student_number
                JOIN users u ON u.id = s.user_id
                LEFT JOIN status_table st ON st.id = cs.status
                WHERE cs.mis_code = @mc
                ORDER BY student_name", conn);
            cmd.Parameters.AddWithValue("@mc", misCode);

            var students = new List<ClearanceSubject>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                students.Add(new ClearanceSubject
                {
                    Id = r.GetInt32("id"),
                    StudentNumber = r.GetString("student_number"),
                    Status = r.IsDBNull(r.GetOrdinal("status")) ? null : r.GetInt32("status"),
                    Remarks = r.IsDBNull(r.GetOrdinal("remarks")) ? null : r.GetString("remarks"),
                    InstructorName = r.GetString("student_name"),  // reusing field for student name
                    StatusLabel = r.IsDBNull(r.GetOrdinal("status_label")) ? "Pending" : r.GetString("status_label"),
                    SignedAt = r.IsDBNull(r.GetOrdinal("signed_at")) ? null : r.GetDateTime("signed_at")
                });
            }

            var vm = new InstructorClearanceViewModel
            {
                MisCode = misCode,
                SubjectTitle = subjectTitle,
                Students = students
            };

            return View(vm);
        }

        // POST /Instructor/UpdateStatus  — Approve or Decline a student's clearance
        [HttpPost]
        public IActionResult UpdateStatus(int clearanceId, int status, string? remarks)
        {
            // status: 2 = Cleared, 3 = Declined
            using var conn = DbHelper.GetConnection(_config);
            conn.Open();

            // First, get the mis_code so we can redirect back to the right page
            var getCmd = new MySqlCommand(
                "SELECT mis_code FROM clearance_subjects WHERE id = @id", conn);
            getCmd.Parameters.AddWithValue("@id", clearanceId);
            string misCode = getCmd.ExecuteScalar()?.ToString() ?? "";

            // Update the clearance record
            var cmd = new MySqlCommand(@"
                UPDATE clearance_subjects
                SET status = @status,
                    remarks = @remarks,
                    signed_at = CASE WHEN @status = 2 THEN NOW() ELSE NULL END
                WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@remarks", (object?)remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", clearanceId);
            cmd.ExecuteNonQuery();

            TempData["Success"] = status == 2 ? "Student cleared successfully." : "Clearance declined.";
            return RedirectToAction("Students", new { misCode });
        }
    }
}
