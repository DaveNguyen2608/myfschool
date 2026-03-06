using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;

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
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Username và password không được để trống" });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Username == request.Username && x.Status == "ACTIVE");

                if (user == null)
                {
                    return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
                }

                // Tạm thời so sánh password thường để test
                if (user.PasswordHash != request.Password)
                {
                    return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
                }

                return Ok(new LoginResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Status = user.Status,
                    Message = "Đăng nhập thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi server",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}