using Microsoft.EntityFrameworkCore;

namespace onlineclearance.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Student> Students { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AcademicPeriod> AcademicPeriods { get; set; }
    public DbSet<ClearanceSubject> ClearanceSubjects { get; set; }
    public DbSet<ClearanceOrganization> ClearanceOrganizations { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<StatusTable> StatusTables { get; set; }
    public DbSet<SubjectOffering> SubjectOfferings { get; set; }
    public DbSet<Signatory> Signatories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>().ToTable("students");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Course>().ToTable("courses");
        modelBuilder.Entity<Subject>().ToTable("subjects");
        modelBuilder.Entity<Announcement>().ToTable("announcements");
        modelBuilder.Entity<AcademicPeriod>().ToTable("academicperiods");
        modelBuilder.Entity<ClearanceSubject>().ToTable("clearance_subjects");
        modelBuilder.Entity<ClearanceOrganization>().ToTable("clearance_organizations");
        modelBuilder.Entity<Organization>().ToTable("organizations");
        modelBuilder.Entity<StatusTable>().ToTable("status_tables");
        modelBuilder.Entity<SubjectOffering>().ToTable("subject_offerings");
        modelBuilder.Entity<Signatory>().ToTable("signatories");
    }
}