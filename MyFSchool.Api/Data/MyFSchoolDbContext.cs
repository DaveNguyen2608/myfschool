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
        public DbSet<ParentStudentRelationship> ParentStudentRelationships { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
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
        public DbSet<GradeCategory> GradeCategories { get; set; }
        public DbSet<StudentGrade> StudentGrades { get; set; }
        public DbSet<StudentGradeSummary> StudentGradeSummaries { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<AppNotification> Notifications { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<NewsArticle> NewsArticles { get; set; }

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
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.Gender).HasColumnName("gender");
                entity.Property(e => e.Address).HasColumnName("address");
                entity.Property(e => e.Occupation).HasColumnName("occupation");
                entity.Property(e => e.StudentCode).HasColumnName("student_code");
                entity.Property(e => e.CurrentClassId).HasColumnName("current_class_id");
                entity.Property(e => e.TeacherCode).HasColumnName("teacher_code");
                entity.Property(e => e.Department).HasColumnName("department");
                entity.Property(e => e.SubjectSpecialty).HasColumnName("subject_specialty");
                entity.Property(e => e.PositionTitle).HasColumnName("position_title");
                entity.Property(e => e.FptEmail).HasColumnName("fpt_email");
                entity.Property(e => e.ContactInfo).HasColumnName("contact_info");
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

            modelBuilder.Entity<ParentStudentRelationship>(entity =>
            {
                entity.ToTable("parent_student_relationships");
                entity.HasKey(e => new { e.ParentUserId, e.StudentUserId });

                entity.Property(e => e.ParentUserId).HasColumnName("parent_user_id");
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
                entity.Property(e => e.RelationshipType).HasColumnName("relationship_type");
            });

            modelBuilder.Entity<ClassStudent>(entity =>
            {
                entity.ToTable("class_students");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClassId).HasColumnName("class_id");
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
                entity.Property(e => e.LeftAt).HasColumnName("left_at");
            });

            modelBuilder.Entity<SchoolClass>(entity =>
            {
                entity.ToTable("classes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClassCode).HasColumnName("class_code");
                entity.Property(e => e.ClassName).HasColumnName("class_name");
                entity.Property(e => e.GradeLevel).HasColumnName("grade_level");
                entity.Property(e => e.HomeroomTeacherUserId).HasColumnName("homeroom_teacher_user_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            });

            modelBuilder.Entity<StudentRequest>(entity =>
            {
                entity.ToTable("student_requests");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
                entity.Property(e => e.ParentUserId).HasColumnName("parent_user_id");
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
                entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");
                entity.Property(e => e.RoomName).HasColumnName("room_name");
                entity.Property(e => e.Note).HasColumnName("note");
            });

            modelBuilder.Entity<TeacherContact>(entity =>
            {
                entity.ToTable("teacher_contacts");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");
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

            modelBuilder.Entity<Club>(entity =>
            {
                entity.ToTable("clubs");
            });

            modelBuilder.Entity<ClubRegistration>(entity =>
            {
                entity.ToTable("club_registrations");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClubId).HasColumnName("club_id");
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
                entity.Property(e => e.ParentUserId).HasColumnName("parent_user_id");
                entity.Property(e => e.RegisteredAt).HasColumnName("registered_at");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
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
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
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
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
                entity.Property(e => e.SemesterId).HasColumnName("semester_id");
                entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
                entity.Property(e => e.AverageScore).HasColumnName("average_score");
                entity.Property(e => e.AcademicPerformance).HasColumnName("academic_performance");
                entity.Property(e => e.Conduct).HasColumnName("conduct");
                entity.Property(e => e.Note).HasColumnName("note");
            });

            modelBuilder.Entity<ChatConversation>(entity =>
            {
                entity.ToTable("conversations");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ConversationType).HasColumnName("conversation_type");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.LastMessageAt).HasColumnName("last_message_at");
                entity.Property(e => e.ParentUserId).HasColumnName("parent_user_id");
                entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id");
                entity.Property(e => e.StudentUserId).HasColumnName("student_user_id");
                entity.Property(e => e.ClassId).HasColumnName("class_id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<ConversationParticipant>(entity =>
            {
                entity.ToTable("conversation_participants");
                entity.HasKey(e => new { e.ConversationId, e.UserId });

                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("messages");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.SenderUserId).HasColumnName("sender_user_id");
                entity.Property(e => e.MessageType).HasColumnName("message_type");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.SentAt).HasColumnName("sent_at");
                entity.Property(e => e.IsRead).HasColumnName("is_read");
                entity.Property(e => e.ReadAt).HasColumnName("read_at");
                entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            });

            modelBuilder.Entity<AppNotification>(entity =>
            {
                entity.ToTable("notifications");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.NotificationType).HasColumnName("notification_type");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.ReferenceType).HasColumnName("reference_type");
                entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
                entity.Property(e => e.IsRead).HasColumnName("is_read");
                entity.Property(e => e.ReadAt).HasColumnName("read_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<PasswordResetOtp>(entity =>
            {
                entity.ToTable("password_reset_otps");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Purpose).HasColumnName("purpose");
                entity.Property(e => e.OtpCodeHash).HasColumnName("otp_code_hash");
                entity.Property(e => e.OtpSalt).HasColumnName("otp_salt");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UsedAt).HasColumnName("used_at");
                entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
                entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
                entity.Property(e => e.LastAttemptAt).HasColumnName("last_attempt_at");
                entity.Property(e => e.SentToEmail).HasColumnName("sent_to_email");
            });

            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.ToTable("announcements");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.AnnouncementType).HasColumnName("announcement_type");
                entity.Property(e => e.TargetType).HasColumnName("target_type");
                entity.Property(e => e.ClassId).HasColumnName("class_id");
                entity.Property(e => e.TargetUserId).HasColumnName("target_user_id");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<NewsArticle>(entity =>
            {
                entity.ToTable("news_posts");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Summary).HasColumnName("summary");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.CoverImageUrl).HasColumnName("image_url");
                entity.Property(e => e.ArticleType).HasColumnName("post_type");
                entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
                entity.Property(e => e.IsPublished).HasColumnName("is_published");
                entity.Property(e => e.PublishedAt).HasColumnName("published_at");
                entity.Property(e => e.CreatedBy).HasColumnName("author_user_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });
        }
    }
}
