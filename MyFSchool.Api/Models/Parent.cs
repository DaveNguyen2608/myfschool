namespace MyFSchool.Api.Models
{
    public class Parent
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string? Occupation { get; set; }
        public string? Address { get; set; }
    }
}