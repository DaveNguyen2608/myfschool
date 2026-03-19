using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;
using System.Text.RegularExpressions;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string ParentRoleCode = "PARENT";
        private const string TeacherRoleCode = "TEACHER";

        private readonly MyFSchoolDbContext _context;

        public AuthController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Số điện thoại và mật khẩu không được để trống" });
            }

            var phone = request.PhoneNumber.Trim();

            var vietnamPhoneRegex = @"^0[35789][0-9]{8}$";
            if (!Regex.IsMatch(phone, vietnamPhoneRegex))
            {
                return BadRequest(new { message = "Số điện thoại không đúng định dạng Việt Nam (10 số)" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Phone == phone && x.Status == "ACTIVE");

            if (user == null || user.PasswordHash != request.Password)
            {
                return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
            }

            var roleCodes = await (
                from ur in _context.UserRoles
                join role in _context.Roles on ur.RoleId equals role.Id
                where ur.UserId == user.Id
                select role.Code
            ).ToListAsync();

            var roleCode = ResolvePrimaryRole(roleCodes);

            string? studentName = null;
            string? studentCode = null;
            string? className = null;
            string? campusName = null;

            if (roleCode == ParentRoleCode)
            {
                var parent = await _context.Parents
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (parent != null)
                {
                    var parentStudent = await _context.ParentStudents
                        .Where(ps => ps.ParentId == parent.Id)
                        .OrderBy(ps => ps.StudentId)
                        .FirstOrDefaultAsync();

                    if (parentStudent != null)
                    {
                        var student = await _context.Students
                            .FirstOrDefaultAsync(s => s.Id == parentStudent.StudentId);

                        if (student != null)
                        {
                            studentName = student.FullName;
                            studentCode = student.StudentCode;
                            campusName = "Hola";

                            if (student.CurrentClassId.HasValue)
                            {
                                className = await _context.SchoolClasses
                                    .Where(c => c.Id == student.CurrentClassId.Value)
                                    .Select(c => c.ClassName)
                                    .FirstOrDefaultAsync();
                            }
                        }
                    }
                }
            }
            else if (roleCode == TeacherRoleCode)
            {
                campusName = "Hola";

                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                if (teacher != null)
                {
                    className = await _context.SchoolClasses
                        .Where(c => c.HomeroomTeacherId == teacher.Id)
                        .OrderBy(c => c.ClassName)
                        .Select(c => c.ClassName)
                        .FirstOrDefaultAsync();
                }
            }

            var response = new LoginResponse
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Status = user.Status,
                Message = "Đăng nhập thành công",
                RoleCode = roleCode,
                StudentName = studentName,
                StudentCode = studentCode,
                ClassName = className,
                CampusName = campusName
            };

            return Ok(response);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin" });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Mật khẩu mới cần ít nhất 6 ký tự" });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { message = "Xác nhận mật khẩu mới chưa khớp" });
            }

            if (request.NewPassword == request.CurrentPassword)
            {
                return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại" });
            }

            var username = request.Username.Trim();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == username && x.Status == "ACTIVE");

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản" });
            }

            if (user.PasswordHash != request.CurrentPassword)
            {
                return Unauthorized(new { message = "Mật khẩu hiện tại không đúng" });
            }

            user.PasswordHash = request.NewPassword;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        private static string ResolvePrimaryRole(IEnumerable<string> roleCodes)
        {
            var roles = roleCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .ToList();

            if (roles.Contains(TeacherRoleCode))
            {
                return TeacherRoleCode;
            }

            if (roles.Contains(ParentRoleCode))
            {
                return ParentRoleCode;
            }

            return roles.FirstOrDefault() ?? string.Empty;
        }
    }
}
