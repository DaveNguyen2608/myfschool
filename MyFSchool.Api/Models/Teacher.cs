namespace MyFSchool.Api.Models
{
    public class Teacher
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string TeacherCode { get; set; } = "";
        public string? Department { get; set; }
        public string? SubjectSpecialty { get; set; }
        public string? PositionTitle { get; set; }
        public string? FptEmail { get; set; }
        public string? ContactInfo { get; set; }
    }
}