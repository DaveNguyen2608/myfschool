namespace MyFSchool.Api.Models
{
    public class ScheduleSlot
    {
        public long Id { get; set; }
        public int PeriodNo { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}