namespace MyFSchool.Api.Models
{
    public class LoginResponse
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";

        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public string? ClassName { get; set; }
        public string? CampusName { get; set; }
    }
}