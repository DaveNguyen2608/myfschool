namespace MyFSchool.Api.Models
{
    public class ParentStudent
    {
        public long ParentId { get; set; }
        public long StudentId { get; set; }
        public string? RelationshipType { get; set; }
    }
}