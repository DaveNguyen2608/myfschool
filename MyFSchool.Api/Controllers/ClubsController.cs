using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;
using MyFSchool.Api.Security;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "PARENT")]
    [Route("api/[controller]")]
    public class ClubsController : ControllerBase
    {
        private const string ParentRoleCode = "PARENT";
        private readonly MyFSchoolDbContext _context;

        public ClubsController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? username)
        {
            var mappingResult = await GetParentStudentMapping(username);
            if (mappingResult.Error != null)
            {
                return mappingResult.Error;
            }

            if (mappingResult.Mapping == null)
            {
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });
            }

            var mapping = mappingResult.Mapping;

            var result = await _context.Clubs
                .Select(c => new
                {
                    c.Id,
                    c.ClubCode,
                    ClubName = c.ClubName,
                    Description = c.Description ?? string.Empty,
                    SlotLimit = c.SlotLimit,
                    RegisteredCount = _context.ClubRegistrations.Count(r => r.ClubId == c.Id && r.Status == "REGISTERED"),
                    StartDate = c.StartDate.HasValue ? c.StartDate.Value.ToString("dd-MM-yyyy") : string.Empty,
                    EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToString("dd-MM-yyyy") : string.Empty,
                    Status = c.Status,
                    IsRegistered = _context.ClubRegistrations.Any(r =>
                        r.ClubId == c.Id &&
                        r.StudentUserId == mapping.StudentUserId &&
                        r.Status == "REGISTERED")
                })
                .OrderBy(x => x.ClubName)
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyClubs([FromQuery] string? username)
        {
            var mappingResult = await GetParentStudentMapping(username);
            if (mappingResult.Error != null)
            {
                return mappingResult.Error;
            }

            if (mappingResult.Mapping == null)
            {
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });
            }

            var mapping = mappingResult.Mapping;

            var result = await _context.ClubRegistrations
                .Include(r => r.Club)
                .Where(r =>
                    r.StudentUserId == mapping.StudentUserId &&
                    r.ParentUserId == mapping.ParentUserId &&
                    r.Status == "REGISTERED")
                .Select(r => new
                {
                    r.Club!.Id,
                    r.Club.ClubCode,
                    ClubName = r.Club.ClubName,
                    Description = r.Club.Description ?? string.Empty,
                    SlotLimit = r.Club.SlotLimit,
                    RegisteredCount = _context.ClubRegistrations.Count(x => x.ClubId == r.ClubId && x.Status == "REGISTERED"),
                    StartDate = r.Club.StartDate.HasValue ? r.Club.StartDate.Value.ToString("dd-MM-yyyy") : string.Empty,
                    EndDate = r.Club.EndDate.HasValue ? r.Club.EndDate.Value.ToString("dd-MM-yyyy") : string.Empty,
                    Status = r.Club.Status,
                    IsRegistered = true
                })
                .OrderBy(x => x.ClubName)
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{clubId:int}")]
        public async Task<IActionResult> GetDetail(int clubId, [FromQuery] string? username)
        {
            var mappingResult = await GetParentStudentMapping(username);
            if (mappingResult.Error != null)
            {
                return mappingResult.Error;
            }

            if (mappingResult.Mapping == null)
            {
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });
            }

            var mapping = mappingResult.Mapping;

            var club = await _context.Clubs
                .Where(c => c.Id == clubId)
                .Select(c => new
                {
                    c.Id,
                    c.ClubCode,
                    ClubName = c.ClubName,
                    Description = c.Description ?? string.Empty,
                    SlotLimit = c.SlotLimit,
                    RegisteredCount = _context.ClubRegistrations.Count(r => r.ClubId == c.Id && r.Status == "REGISTERED"),
                    StartDate = c.StartDate.HasValue ? c.StartDate.Value.ToString("dd-MM-yyyy") : string.Empty,
                    EndDate = c.EndDate.HasValue ? c.EndDate.Value.ToString("dd-MM-yyyy") : string.Empty,
                    Status = c.Status,
                    IsRegistered = _context.ClubRegistrations.Any(r =>
                        r.ClubId == c.Id &&
                        r.StudentUserId == mapping.StudentUserId &&
                        r.Status == "REGISTERED")
                })
                .FirstOrDefaultAsync();

            if (club == null)
            {
                return NotFound(new { message = "Không tìm thấy câu lạc bộ" });
            }

            return Ok(club);
        }

        [HttpPost("{clubId:int}/register")]
        public async Task<IActionResult> Register(int clubId, [FromBody] ClubActionRequest request)
        {
            var mappingResult = await GetParentStudentMapping(request.Username);
            if (mappingResult.Error != null)
            {
                return mappingResult.Error;
            }

            if (mappingResult.Mapping == null)
            {
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });
            }

            var mapping = mappingResult.Mapping;

            var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
            if (club == null)
            {
                return NotFound(new { message = "Không tìm thấy câu lạc bộ" });
            }

            if (club.Status != "OPEN")
            {
                return BadRequest(new { message = "Câu lạc bộ không mở đăng ký" });
            }

            var activeRegistrationCount = await _context.ClubRegistrations
                .CountAsync(r => r.ClubId == clubId && r.Status == "REGISTERED");

            if (activeRegistrationCount >= club.SlotLimit)
            {
                return BadRequest(new { message = "Câu lạc bộ đã hết slot" });
            }

            var existing = await _context.ClubRegistrations
                .FirstOrDefaultAsync(r => r.ClubId == clubId && r.StudentUserId == mapping.StudentUserId);

            if (existing != null)
            {
                existing.Status = "REGISTERED";
                existing.CancelledAt = null;
                existing.RegisteredAt = DateTime.Now;
                existing.ParentUserId = mapping.ParentUserId;
            }
            else
            {
                _context.ClubRegistrations.Add(new ClubRegistration
                {
                    ClubId = clubId,
                    StudentUserId = mapping.StudentUserId,
                    ParentUserId = mapping.ParentUserId,
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
            var mappingResult = await GetParentStudentMapping(request.Username);
            if (mappingResult.Error != null)
            {
                return mappingResult.Error;
            }

            if (mappingResult.Mapping == null)
            {
                return BadRequest(new { message = "Không tìm thấy phụ huynh hoặc học sinh" });
            }

            var mapping = mappingResult.Mapping;

            var registration = await _context.ClubRegistrations
                .FirstOrDefaultAsync(r =>
                    r.ClubId == clubId &&
                    r.StudentUserId == mapping.StudentUserId &&
                    r.ParentUserId == mapping.ParentUserId &&
                    r.Status == "REGISTERED");

            if (registration == null)
            {
                return NotFound(new { message = "Chưa có đăng ký để hủy" });
            }

            registration.Status = "CANCELLED";
            registration.CancelledAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Hủy đăng ký câu lạc bộ thành công" });
        }

        private async Task<(ParentStudentMapping? Mapping, IActionResult? Error)> GetParentStudentMapping(string? requestedUsername)
        {
            var tokenUserId = User.GetUserId();
            var tokenUsername = User.GetUsername();

            if (!tokenUserId.HasValue || string.IsNullOrWhiteSpace(tokenUsername))
            {
                return (null, Unauthorized(new { message = "Không xác định được tài khoản từ token" }));
            }

            if (!string.IsNullOrWhiteSpace(requestedUsername) &&
                !string.Equals(requestedUsername.Trim(), tokenUsername, StringComparison.OrdinalIgnoreCase))
            {
                return (null, StatusCode(403, new { message = "Không thể thao tác thay cho tài khoản khác" }));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == tokenUserId.Value && x.Status == "ACTIVE");

            if (user == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy tài khoản" }));
            }

            var hasParentRole = await (
                from ur in _context.UserRoles
                join role in _context.Roles on ur.RoleId equals role.Id
                where ur.UserId == user.Id && role.Code == ParentRoleCode
                select ur
            ).AnyAsync();

            if (!hasParentRole)
            {
                return (null, StatusCode(403, new { message = "Tài khoản không có quyền phụ huynh" }));
            }

            var mapping = await (
                from rel in _context.ParentStudentRelationships
                where rel.ParentUserId == user.Id
                orderby rel.StudentUserId
                select new ParentStudentMapping
                {
                    ParentUserId = rel.ParentUserId,
                    StudentUserId = rel.StudentUserId
                }
            ).FirstOrDefaultAsync();

            return (mapping, null);
        }
    }

    public class ClubActionRequest
    {
        public string? Username { get; set; }
    }

    public class ParentStudentMapping
    {
        public long ParentUserId { get; set; }
        public long StudentUserId { get; set; }
    }
}
