using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyFSchool.Api.Models
{
    [Table("clubs")]
    public class Club
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("club_code")]
        public string ClubCode { get; set; } = string.Empty;

        [Column("club_name")]
        public string ClubName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("slot_limit")]
        public int SlotLimit { get; set; }

        [Column("start_date")]
        public DateOnly? StartDate { get; set; }

        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "OPEN";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public ICollection<ClubRegistration> ClubRegistrations { get; set; } = new List<ClubRegistration>();
    }
}