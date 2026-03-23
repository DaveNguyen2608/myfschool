namespace MyFSchool.Api.Models
{
    public class Announcement
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string AnnouncementType { get; set; } = "";
        public string TargetType { get; set; } = "";
        public long? ClassId { get; set; }
        public long? TargetUserId { get; set; }
        public long CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
