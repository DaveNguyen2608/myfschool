namespace MyFSchool.Api.Models
{
    public class StudentRequest
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public long ParentId { get; set; }
        public long RequestTypeId { get; set; }
        public string Title { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? PeriodFrom { get; set; }
        public int? PeriodTo { get; set; }
        public int? TotalDays { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime SubmittedAt { get; set; }
    }
}