using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;
using MyFSchool.Api.Security;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly MyFSchoolDbContext _context;

        public AnnouncementsController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements([FromQuery] string? username)
        {
            var (user, error) = await ResolveActiveUser(username);
            if (error != null)
            {
                return error;
            }

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản" });
            }

            var roleCodes = await (
                from ur in _context.UserRoles
                join role in _context.Roles on ur.RoleId equals role.Id
                where ur.UserId == user.Id
                select role.Code
            ).ToListAsync();

            var normalizedRoleCodes = roleCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .ToHashSet();

            var classId = user.CurrentClassId;
            if (!classId.HasValue && normalizedRoleCodes.Contains("PARENT"))
            {
                var childUserId = await _context.ParentStudentRelationships
                    .Where(x => x.ParentUserId == user.Id)
                    .OrderBy(x => x.StudentUserId)
                    .Select(x => (long?)x.StudentUserId)
                    .FirstOrDefaultAsync();

                if (childUserId.HasValue)
                {
                    var activeAcademicYearId = await _context.AcademicYears
                        .Where(x => x.IsActive)
                        .Select(x => (long?)x.Id)
                        .FirstOrDefaultAsync();

                    classId = await _context.ClassStudents
                        .Where(x =>
                            x.StudentUserId == childUserId.Value &&
                            (!activeAcademicYearId.HasValue || x.AcademicYearId == activeAcademicYearId.Value) &&
                            !x.LeftAt.HasValue)
                        .OrderByDescending(x => x.AcademicYearId)
                        .ThenByDescending(x => x.JoinedAt)
                        .Select(x => (long?)x.ClassId)
                        .FirstOrDefaultAsync();

                    if (!classId.HasValue)
                    {
                        classId = await _context.Users
                            .Where(x => x.Id == childUserId.Value)
                            .Select(x => x.CurrentClassId)
                            .FirstOrDefaultAsync();
                    }
                }
            }

            var query = _context.Announcements
                .Where(x => x.IsActive)
                .Where(x =>
                    x.TargetType == "ALL" ||
                    (x.TargetType == "USER" && x.TargetUserId == user.Id) ||
                    (x.TargetType == "CLASS" && classId.HasValue && x.ClassId == classId.Value) ||
                    (x.TargetType == "PARENT" && normalizedRoleCodes.Contains("PARENT")) ||
                    (x.TargetType == "STUDENT" && normalizedRoleCodes.Contains("STUDENT")) ||
                    (x.TargetType == "TEACHER" && normalizedRoleCodes.Contains("TEACHER")) ||
                    (x.TargetType == "ADMIN" && normalizedRoleCodes.Contains("ADMIN")));

            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    content = x.Content,
                    description = x.Content,
                    announcementType = x.AnnouncementType,
                    targetType = x.TargetType,
                    classId = x.ClassId,
                    targetUserId = x.TargetUserId,
                    createdBy = x.CreatedBy,
                    createdAt = x.CreatedAt,
                    isActive = x.IsActive,
                    imageUrl = string.Empty,
                    startDate = (DateTime?)null,
                    endDate = (DateTime?)null
                })
                .ToListAsync();

            return Ok(data);
        }

        private async Task<(User? User, IActionResult? Error)> ResolveActiveUser(string? requestedUsername)
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

            return (user, null);
        }
    }
}
