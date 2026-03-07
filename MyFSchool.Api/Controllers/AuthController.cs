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

            if (user == null)
            {
                return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
            }

            if (user.PasswordHash != request.Password)
            {
                return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
            }

            var response = new LoginResponse
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Status = user.Status,
                Message = "Đăng nhập thành công"
            };

            return Ok(response);
        }
    }
}