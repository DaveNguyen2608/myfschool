using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFSchool.Api.Models
{
    [Table("student_scores")]
    public class StudentScore
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("student_id")]
        public long StudentId { get; set; }

        [Column("subject_name")]
        public string SubjectName { get; set; } = string.Empty;

        [Column("academic_year_id")]
        public long AcademicYearId { get; set; }

        [Column("semester_no")]
        public int SemesterNo { get; set; }

        [Column("average_score")]
        public decimal? AverageScore { get; set; }

        [Column("result")]
        public string? Result { get; set; }

        [Column("conduct")]
        public string? Conduct { get; set; }

        [Column("academic_performance")]
        public string? AcademicPerformance { get; set; }

        [Column("note")]
        public string? Note { get; set; }
    }
}