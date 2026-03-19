namespace MyFSchool.Api.Models
{
    public class ScheduleItemResponse
    {
        public long TeacherId { get; set; }
        public string TeacherAlias { get; set; } = "";
        public int DayOfWeek { get; set; }
        public int PeriodNo { get; set; }
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string SubjectName { get; set; } = "";
        public string RoomName { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string TeacherPhone { get; set; } = "";
        public string TeacherEmail { get; set; } = "";
        public string TeacherRole { get; set; } = "";
        public string Note { get; set; } = "";
    }
}
