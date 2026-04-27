// ============================================================
// Models/AppModels.cs
// These C# classes mirror the database tables.
// Each property = one column in the table.
// ============================================================

namespace OnlineClearance.Models
{
    // ---- users table ----
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Role { get; set; }        // "Admin", "Instructor", or null (student)
        public bool IsActive { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    // ---- students table ----
    public class Student
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StudentNumber { get; set; } = "";
        public string Status { get; set; } = "Regular";  // Regular / Irregular

        // Extra info joined from users table
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
    }

    // ---- clearance_subjects table ----
    //  Tracks whether a student is cleared by each subject instructor
    public class ClearanceSubject
    {
        public int Id { get; set; }
        public string StudentNumber { get; set; } = "";
        public string MisCode { get; set; } = "";       // links to subject_offerings
        public int? Status { get; set; }                // 1=Pending, 2=Cleared, 3=Declined
        public string? Remarks { get; set; }
        public int? PeriodId { get; set; }
        public DateTime? SignedAt { get; set; }

        // Extra info joined from subjects & users tables
        public string SubjectTitle { get; set; } = "";
        public string SubjectCode { get; set; } = "";
        public string InstructorName { get; set; } = "";
        public string StatusLabel { get; set; } = "Pending";
    }

    // ---- clearance_organization table ----
    //  Tracks whether a student is cleared by each organization
    public class ClearanceOrg
    {
        public int Id { get; set; }
        public string StudentNumber { get; set; } = "";
        public string OrgName { get; set; } = "";
        public string OrgSignatory { get; set; } = "";
        public int? Status { get; set; }
        public string StatusLabel { get; set; } = "Pending";
    }

    // ---- academic_periods table ----
    public class AcademicPeriod
    {
        public int Id { get; set; }
        public string AcademicYear { get; set; } = "";
        public string Semester { get; set; } = "";
        public bool IsActive { get; set; }
    }

    // ---- announcements table ----
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Type { get; set; } = "General";
        public DateTime CreatedAt { get; set; }
    }

    // ---- ViewModel for the Student Dashboard ----
    //  A ViewModel bundles everything one page needs into one object.
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = new();
        public AcademicPeriod? ActivePeriod { get; set; }
        public List<ClearanceSubject> SubjectClearances { get; set; } = new();
        public List<ClearanceOrg> OrgClearances { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();

        // Helper: count how many are fully cleared
        public int ClearedCount => SubjectClearances.Count(x => x.Status == 2)
                                 + OrgClearances.Count(x => x.Status == 2);
        public int TotalCount => SubjectClearances.Count + OrgClearances.Count;
        public bool IsFullyCleared => TotalCount > 0 && ClearedCount == TotalCount;
    }

    // ---- ViewModel for the Instructor Clearance page ----
    public class InstructorClearanceViewModel
    {
        public string MisCode { get; set; } = "";
        public string SubjectTitle { get; set; } = "";
        public List<ClearanceSubject> Students { get; set; } = new();
    }

    // ---- Login form inputs ----
    public class LoginViewModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? ErrorMessage { get; set; }
    }
}
