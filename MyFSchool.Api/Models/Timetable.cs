namespace MyFSchool.Api.Models
{
    public class Timetable
    {
        public long Id { get; set; }
        public long ClassId { get; set; }
        public long AcademicYearId { get; set; }
        public long SemesterId { get; set; }
        public int DayOfWeek { get; set; }
        public long SlotId { get; set; }
        public long SubjectId { get; set; }
        public long TeacherId { get; set; }
        public string? RoomName { get; set; }
        public string? Note { get; set; }
    }
}