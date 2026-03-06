namespace MyFSchool.Api.Models
{
    public class Student
    {
        public long Id { get; set; }
        public string StudentCode { get; set; } = "";
        public long? UserId { get; set; }
        public string FullName { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public long? CurrentClassId { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime CreatedAt { get; set; }
    }
}