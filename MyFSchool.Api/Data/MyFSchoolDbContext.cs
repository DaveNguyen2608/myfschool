using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Models;

namespace MyFSchool.Api.Data
{
    public class MyFSchoolDbContext : DbContext
    {
        public MyFSchoolDbContext(DbContextOptions<MyFSchoolDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        public DbSet<Student> Students { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<ParentStudent> ParentStudents { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }

        public DbSet<StudentRequest> StudentRequests { get; set; }
        public DbSet<RequestType> RequestTypes { get; set; }
        public DbSet<RequestApproval> RequestApprovals { get; set; }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<SchoolCalendarException> SchoolCalendarExceptions { get; set; }
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
        public DbSet<TeacherContact> TeacherContacts { get; set; }
        public DbSet<SchoolContact> SchoolContacts { get; set; }

        public DbSet<Club> Clubs { get; set; }
        public DbSet<ClubRegistration> ClubRegistrations { get; set; }

        public DbSet<StudentScore> StudentScores { get; set; }
        public DbSet<GradeCategory> GradeCategories { get; set; }
        public DbSet<StudentGrade> StudentGrades { get; set; }
        public DbSet<StudentGradeSummary> StudentGradeSummaries { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.FullName).HasColumnName("full_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("students");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StudentCode).HasColumnName("student_code");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.FullName).HasColumnName("full_name");
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.Gender).HasColumnName("gender");
                entity.Property(e => e.CurrentClassId).HasColumnName("current_class_id");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Parent>(entity =>
            {
                entity.ToTable("parents");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Occupation).HasColumnName("occupation");
                entity.Property(e => e.Address).HasColumnName("address");
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("teachers");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TeacherCode).HasColumnName("teacher_code");
                entity.Property(e => e.Department).HasColumnName("department");
                entity.Property(e => e.SubjectSpecialty).HasColumnName("subject_specialty");
                entity.Property(e => e.PositionTitle).HasColumnName("position_title");
                entity.Property(e => e.FptEmail).HasColumnName("fpt_email");
                entity.Property(e => e.ContactInfo).HasColumnName("contact_info");
            });

            modelBuilder.Entity<ParentStudent>(entity =>
            {
                entity.ToTable("parent_student");
                entity.HasKey(e => new { e.ParentId, e.StudentId });

                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.RelationshipType).HasColumnName("relationship_type");
            });

            modelBuilder.Entity<SchoolClass>(entity =>
            {
                entity.ToTable("classes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClassCode).HasColumnName("class_code");
                entity.Property(e => e.ClassName).HasColumnName("class_name");
                entity.Property(e => e.GradeLevel).HasColumnName("grade_level");
                entity.Property(e => e.HomeroomTeacherId).HasColumnName("homeroom_teacher_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            });

            modelBuilder.Entity<StudentRequest>(entity =>
            {
                entity.ToTable("student_requests");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.RequestTypeId).HasColumnName("request_type_id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Reason).HasColumnName("reason");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.PeriodFrom).HasColumnName("period_from");
                entity.Property(e => e.PeriodTo).HasColumnName("period_to");
                entity.Property(e => e.TotalDays).HasColumnName("total_days");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
                entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
                entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
                entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            });

            modelBuilder.Entity<RequestType>(entity =>
            {
                entity.ToTable("request_types");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
            });

            modelBuilder.Entity<RequestApproval>(entity =>
            {
                entity.ToTable("request_approvals");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.ApproverUserId).HasColumnName("approver_user_id");
                entity.Property(e => e.Action).HasColumnName("action");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.ActionAt).HasColumnName("action_at");
            });

            modelBuilder.Entity<AcademicYear>(entity =>
            {
                entity.ToTable("academic_years");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.YearName).HasColumnName("year_name");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<Semester>(entity =>
            {
                entity.ToTable("semesters");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.SemesterName).HasColumnName("semester_name");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
            });

            modelBuilder.Entity<SchoolCalendarException>(entity =>
            {
                entity.ToTable("school_calendar_exceptions");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.Date).HasColumnName("exception_date");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("subjects");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SubjectCode).HasColumnName("subject_code");
                entity.Property(e => e.SubjectName).HasColumnName("subject_name");
            });

            modelBuilder.Entity<ScheduleSlot>(entity =>
            {
                entity.ToTable("schedule_slots");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PeriodNo).HasColumnName("period_no");
                entity.Property(e => e.StartTime).HasColumnName("start_time");
                entity.Property(e => e.EndTime).HasColumnName("end_time");
            });

            modelBuilder.Entity<Timetable>(entity =>
            {
                entity.ToTable("timetables");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClassId).HasColumnName("class_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.SemesterId).HasColumnName("semester_id");
                entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
                entity.Property(e => e.SlotId).HasColumnName("slot_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
                entity.Property(e => e.RoomName).HasColumnName("room_name");
                entity.Property(e => e.Note).HasColumnName("note");
            });

            modelBuilder.Entity<TeacherContact>(entity =>
            {
                entity.ToTable("teacher_contacts");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.ClassId).HasColumnName("class_id");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
                entity.Property(e => e.Note).HasColumnName("note");
            });

            modelBuilder.Entity<SchoolContact>(entity =>
            {
                entity.ToTable("school_contacts");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DepartmentName).HasColumnName("department_name");
                entity.Property(e => e.ContactName).HasColumnName("contact_name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Address).HasColumnName("address");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<GradeCategory>(entity =>
            {
                entity.ToTable("grade_categories");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
            });

            modelBuilder.Entity<StudentGrade>(entity =>
            {
                entity.ToTable("student_grades");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.SemesterId).HasColumnName("semester_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.GradeType).HasColumnName("grade_type");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.Property(e => e.Score).HasColumnName("score");
                entity.Property(e => e.Note).HasColumnName("note");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<StudentGradeSummary>(entity =>
            {
                entity.ToTable("student_grade_summary");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.SemesterId).HasColumnName("semester_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.AverageScore).HasColumnName("average_score");
                entity.Property(e => e.AcademicPerformance).HasColumnName("academic_performance");
                entity.Property(e => e.Conduct).HasColumnName("conduct");
                entity.Property(e => e.Note).HasColumnName("note");
            });

            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.ToTable("announcements");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.ImageUrl).HasColumnName("image_url");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}

