namespace MyFSchool.Api.Models
{
    public class Semester
    {
        public long Id { get; set; }
        public long AcademicYearId { get; set; }
        public string SemesterName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}