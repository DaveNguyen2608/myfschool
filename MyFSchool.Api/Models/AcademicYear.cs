namespace MyFSchool.Api.Models
{
    public class AcademicYear
    {
        public long Id { get; set; }
        public string YearName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}