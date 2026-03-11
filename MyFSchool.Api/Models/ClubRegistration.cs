using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFSchool.Api.Models
{
    [Table("club_registrations")]
    public class ClubRegistration
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("club_id")]
        public long ClubId { get; set; }

        [Column("student_id")]
        public long StudentId { get; set; }

        [Column("parent_id")]
        public long ParentId { get; set; }

        [Column("registered_at")]
        public DateTime RegisteredAt { get; set; }

        [Column("status")]
        public string Status { get; set; } = "REGISTERED";

        [Column("cancelled_at")]
        public DateTime? CancelledAt { get; set; }

        [ForeignKey(nameof(ClubId))]
        public Club? Club { get; set; }
    }
}