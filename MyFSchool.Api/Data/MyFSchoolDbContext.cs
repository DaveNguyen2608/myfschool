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
        public DbSet<StudentRequest> StudentRequests { get; set; }
        public DbSet<Subject> Subjects { get; set; }



        public DbSet<ParentStudent> ParentStudents { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; }
        public DbSet<Timetable> Timetables { get; set; }


        public DbSet<Club> Clubs { get; set; }
        public DbSet<ClubRegistration> ClubRegistrations { get; set; }

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
            });

            modelBuilder.Entity<Parent>(entity =>
            {
                entity.ToTable("parents");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("teachers");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<StudentRequest>(entity =>
            {
                entity.ToTable("student_requests");
                entity.HasKey(e => e.Id);
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

            modelBuilder.Entity<ParentStudent>(entity =>
            {
                entity.ToTable("parent_student");
                entity.HasKey(e => new { e.ParentId, e.StudentId });

                entity.Property(e => e.ParentId).HasColumnName("parent_id");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.RelationshipType).HasColumnName("relationship_type");
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

            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("subjects");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SubjectCode).HasColumnName("subject_code");
                entity.Property(e => e.SubjectName).HasColumnName("subject_name");
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
        }
    }
}