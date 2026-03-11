using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClubsController : ControllerBase
    {
        private readonly MyFSchoolDbContext _context;

        public ClubsController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string username)
        {
            var mapping = await GetParentStudentMapping(username);
            if (mapping == null)
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });

            var result = await _context.Clubs
                .Select(c => new
                {
                    c.Id,
                    c.ClubCode,
                    ClubName = c.ClubName,
                    Description = c.Description ?? "",
                    SlotLimit = c.SlotLimit,
                    RegisteredCount = _context.ClubRegistrations.Count(r => r.ClubId == c.Id && r.Status == "REGISTERED"),
                    StartDate = c.StartDate.HasValue ? c.StartDate.Value.ToString("dd-MM-yyyy") : "",
                    EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToString("dd-MM-yyyy") : "",
                    Status = c.Status,
                    IsRegistered = _context.ClubRegistrations.Any(r =>
                        r.ClubId == c.Id &&
                        r.StudentId == mapping.StudentId &&
                        r.Status == "REGISTERED")
                })
                .OrderBy(x => x.ClubName)
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyClubs([FromQuery] string username)
        {
            var mapping = await GetParentStudentMapping(username);
            if (mapping == null)
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });

            var result = await _context.ClubRegistrations
                .Include(r => r.Club)
                .Where(r =>
                    r.StudentId == mapping.StudentId &&
                    r.ParentId == mapping.ParentId &&
                    r.Status == "REGISTERED")
                .Select(r => new
                {
                    r.Club!.Id,
                    r.Club.ClubCode,
                    ClubName = r.Club.ClubName,
                    Description = r.Club.Description ?? "",
                    SlotLimit = r.Club.SlotLimit,
                    RegisteredCount = _context.ClubRegistrations.Count(x => x.ClubId == r.ClubId && x.Status == "REGISTERED"),
                    StartDate = r.Club.StartDate.HasValue ? r.Club.StartDate.Value.ToString("dd-MM-yyyy") : "",
                    EndDate = r.Club.EndDate.HasValue ? r.Club.EndDate.Value.ToString("dd-MM-yyyy") : "",
                    Status = r.Club.Status,
                    IsRegistered = true
                })
                .OrderBy(x => x.ClubName)
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{clubId:int}")]
        public async Task<IActionResult> GetDetail(int clubId, [FromQuery] string username)
        {
            var mapping = await GetParentStudentMapping(username);
            if (mapping == null)
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });

            var club = await _context.Clubs
                .Where(c => c.Id == clubId)
                .Select(c => new
                {
                    c.Id,
                    c.ClubCode,
                    ClubName = c.ClubName,
                    Description = c.Description ?? "",
                    SlotLimit = c.SlotLimit,
                    RegisteredCount = _context.ClubRegistrations.Count(r => r.ClubId == c.Id && r.Status == "REGISTERED"),
                    StartDate = c.StartDate.HasValue ? c.StartDate.Value.ToString("dd-MM-yyyy") : "",
                    EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToString("dd-MM-yyyy") : "",
                    Status = c.Status,
                    IsRegistered = _context.ClubRegistrations.Any(r =>
                        r.ClubId == c.Id &&
                        r.StudentId == mapping.StudentId &&
                        r.Status == "REGISTERED")
                })
                .FirstOrDefaultAsync();

            if (club == null)
                return NotFound(new { message = "Không tìm thấy câu lạc bộ" });

            return Ok(club);
        }

        [HttpPost("{clubId:int}/register")]
        public async Task<IActionResult> Register(int clubId, [FromBody] ClubActionRequest request)
        {
            var mapping = await GetParentStudentMapping(request.Username);
            if (mapping == null)
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });

            var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
            if (club == null)
                return NotFound(new { message = "Không tìm thấy câu lạc bộ" });

            if (club.Status != "OPEN")
                return BadRequest(new { message = "Câu lạc bộ không mở đăng ký" });

            var activeRegistrationCount = await _context.ClubRegistrations
                .CountAsync(r => r.ClubId == clubId && r.Status == "REGISTERED");

            if (activeRegistrationCount >= club.SlotLimit)
                return BadRequest(new { message = "Câu lạc bộ đã hết slot" });

            var existing = await _context.ClubRegistrations
                .FirstOrDefaultAsync(r => r.ClubId == clubId && r.StudentId == mapping.StudentId);

            if (existing != null)
            {
                existing.Status = "REGISTERED";
                existing.CancelledAt = null;
                existing.RegisteredAt = DateTime.Now;
            }
            else
            {
                _context.ClubRegistrations.Add(new ClubRegistration
                {
                    ClubId = clubId,
                    StudentId = mapping.StudentId,
                    ParentId = mapping.ParentId,
                    RegisteredAt = DateTime.Now,
                    Status = "REGISTERED"
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đăng ký câu lạc bộ thành công" });
        }

        [HttpPost("{clubId:int}/cancel")]
        public async Task<IActionResult> Cancel(int clubId, [FromBody] ClubActionRequest request)
        {
            var mapping = await GetParentStudentMapping(request.Username);
            if (mapping == null)
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });

            var registration = await _context.ClubRegistrations
                .FirstOrDefaultAsync(r =>
                    r.ClubId == clubId &&
                    r.StudentId == mapping.StudentId &&
                    r.ParentId == mapping.ParentId &&
                    r.Status == "REGISTERED");

            if (registration == null)
                return NotFound(new { message = "Chưa có đăng ký để hủy" });

            registration.Status = "CANCELLED";
            registration.CancelledAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Hủy đăng ký câu lạc bộ thành công" });
        }

        private async Task<ParentStudentMapping?> GetParentStudentMapping(string username)
        {
            return await (
                from p in _context.Parents
                join u in _context.Users on p.UserId equals u.Id
                join ps in _context.ParentStudents on p.Id equals ps.ParentId
                where u.Username == username
                select new ParentStudentMapping
                {
                    ParentId = p.Id,
                    StudentId = ps.StudentId
                }
            ).FirstOrDefaultAsync();
        }
    }

    public class ClubActionRequest
    {
        public string Username { get; set; } = string.Empty;
    }

    public class ParentStudentMapping
    {
        public long ParentId { get; set; }
        public long StudentId { get; set; }
    }
}