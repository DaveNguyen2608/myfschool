namespace MyFSchool.Api.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? StudentCode { get; set; }
        public long? CurrentClassId { get; set; }
        public string? TeacherCode { get; set; }
        public string? Department { get; set; }
        public string? SubjectSpecialty { get; set; }
        public string? PositionTitle { get; set; }
        public string? FptEmail { get; set; }
        public string? ContactInfo { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
