using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;
using MyFSchool.Api.Security;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string AdminRoleCode = "ADMIN";
        private const string ParentRoleCode = "PARENT";
        private const string TeacherRoleCode = "TEACHER";
        private const string PasswordResetPurpose = "FORGOT_PASSWORD";
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

        private readonly MyFSchoolDbContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            MyFSchoolDbContext context,
            JwtTokenService jwtTokenService,
            IEmailSender emailSender,
            IWebHostEnvironment environment)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _emailSender = emailSender;
            _environment = environment;
        }

        [AllowAnonymous]
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

            var needsUpgrade = false;
            var isPasswordValid = user != null &&
                                  PasswordHasher.Verify(request.Password, user.PasswordHash, out needsUpgrade);
            if (!isPasswordValid || user == null)
            {
                return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
            }

            if (needsUpgrade)
            {
                user.PasswordHash = PasswordHasher.Hash(request.Password);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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
                .Distinct()
                .ToList();

            var roleCode = ResolvePrimaryRole(normalizedRoleCodes);

            string? studentName = null;
            string? studentCode = null;
            string? className = null;
            string? campusName = null;

            if (roleCode == ParentRoleCode)
            {
                var child = await (
                    from rel in _context.ParentStudentRelationships
                    join student in _context.Users on rel.StudentUserId equals student.Id
                    join schoolClass in _context.SchoolClasses on student.CurrentClassId equals (long?)schoolClass.Id into classJoin
                    from schoolClass in classJoin.DefaultIfEmpty()
                    where rel.ParentUserId == user.Id
                    orderby student.Id
                    select new
                    {
                        student.FullName,
                        student.StudentCode,
                        ClassName = schoolClass != null ? schoolClass.ClassName : null
                    }
                ).FirstOrDefaultAsync();

                if (child != null)
                {
                    studentName = child.FullName;
                    studentCode = child.StudentCode;
                    className = child.ClassName;
                    campusName = "Hola";
                }
            }
            else if (roleCode == TeacherRoleCode)
            {
                campusName = "Hola";

                className = await _context.SchoolClasses
                    .Where(c => c.HomeroomTeacherUserId == user.Id)
                    .OrderBy(c => c.ClassName)
                    .Select(c => c.ClassName)
                    .FirstOrDefaultAsync();
            }

            var tokenResult = _jwtTokenService.GenerateToken(user, normalizedRoleCodes);

            var response = new LoginResponse
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Status = user.Status,
                Message = "Đăng nhập thành công",
                RoleCode = roleCode,
                RoleCodes = normalizedRoleCodes,
                AccessToken = tokenResult.AccessToken,
                TokenType = "Bearer",
                ExpiresAtUtc = tokenResult.ExpiresAtUtc,
                StudentName = studentName,
                StudentCode = studentCode,
                ClassName = className,
                CampusName = campusName
            };

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> SendForgotPasswordOtp([FromBody] ForgotPasswordSendOtpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest(new { message = "Vui lòng nhập số điện thoại" });
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
                return NotFound(new { message = "Không tìm thấy tài khoản theo số điện thoại" });
            }

            var toEmail = ResolveRegisteredEmail(user);
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return BadRequest(new { message = "Tài khoản chưa đăng ký email để nhận OTP" });
            }

            var now = DateTime.UtcNow;
            var expiresAt = now.Add(OtpLifetime);

            var activeOtps = await _context.PasswordResetOtps
                .Where(x =>
                    x.UserId == user.Id &&
                    x.Purpose == PasswordResetPurpose &&
                    !x.UsedAt.HasValue &&
                    x.ExpiresAt > now)
                .ToListAsync();

            if (activeOtps.Count > 0)
            {
                foreach (var oldOtp in activeOtps)
                {
                    oldOtp.UsedAt = now;
                }
            }

            var otpCode = OtpHasher.GenerateNumericOtpCode();
            var salt = OtpHasher.GenerateSalt();
            var codeHash = OtpHasher.Hash(otpCode, salt);

            var otpEntity = new PasswordResetOtp
            {
                UserId = user.Id,
                Purpose = PasswordResetPurpose,
                OtpCodeHash = codeHash,
                OtpSalt = salt,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                UsedAt = null,
                AttemptCount = 0,
                MaxAttempts = 5,
                LastAttemptAt = null,
                SentToEmail = toEmail
            };

            _context.PasswordResetOtps.Add(otpEntity);

            await _context.SaveChangesAsync();

            var subject = "MyFSchool - Mã OTP đặt lại mật khẩu";
            var textBody =
                $"Xin chào {user.FullName},\n\n" +
                $"Mã OTP đặt lại mật khẩu của bạn là: {otpCode}\n" +
                $"Mã có hiệu lực trong 5 phút và chỉ dùng 1 lần.\n\n" +
                "Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.";

            try
            {
                await _emailSender.SendAsync(toEmail, subject, textBody);
            }
            catch (SmtpException ex)
            {
                // Không gửi được email => vô hiệu OTP vừa tạo để tránh OTP "mồ côi".
                otpEntity.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var detail = _environment.IsDevelopment() ? ex.Message : null;
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Không thể gửi OTP qua email. Vui lòng kiểm tra cấu hình SMTP (username/password, SSL/TLS, app password).",
                    detail
                });
            }

            return Ok(new
            {
                message = "Đã gửi OTP qua email đã đăng ký",
                maskedEmail = MaskEmail(toEmail),
                expiresInSeconds = (int)OtpLifetime.TotalSeconds
            });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password/reset-by-otp")]
        public async Task<IActionResult> ResetPasswordByOtp([FromBody] ForgotPasswordResetRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                string.IsNullOrWhiteSpace(request.OtpCode) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin" });
            }

            var phone = request.PhoneNumber.Trim();
            var vietnamPhoneRegex = @"^0[35789][0-9]{8}$";
            if (!Regex.IsMatch(phone, vietnamPhoneRegex))
            {
                return BadRequest(new { message = "Số điện thoại không đúng định dạng Việt Nam (10 số)" });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Mật khẩu mới cần ít nhất 6 ký tự" });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { message = "Xác nhận mật khẩu mới chưa khớp" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Phone == phone && x.Status == "ACTIVE");

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản theo số điện thoại" });
            }

            var now = DateTime.UtcNow;
            var otpRecord = await _context.PasswordResetOtps
                .Where(x =>
                    x.UserId == user.Id &&
                    x.Purpose == PasswordResetPurpose &&
                    !x.UsedAt.HasValue)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return BadRequest(new { message = "OTP không hợp lệ hoặc đã hết hạn" });
            }

            if (otpRecord.ExpiresAt <= now)
            {
                otpRecord.UsedAt = now;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "OTP đã hết hạn, vui lòng yêu cầu mã mới" });
            }

            if (otpRecord.AttemptCount >= otpRecord.MaxAttempts)
            {
                otpRecord.UsedAt = now;
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "OTP đã bị khóa do nhập sai quá nhiều lần" });
            }

            var isOtpValid = OtpHasher.Verify(request.OtpCode, otpRecord.OtpSalt, otpRecord.OtpCodeHash);
            if (!isOtpValid)
            {
                otpRecord.AttemptCount += 1;
                otpRecord.LastAttemptAt = now;
                if (otpRecord.AttemptCount >= otpRecord.MaxAttempts)
                {
                    otpRecord.UsedAt = now;
                }

                await _context.SaveChangesAsync();
                return BadRequest(new { message = "OTP không chính xác" });
            }

            otpRecord.UsedAt = now;
            otpRecord.LastAttemptAt = now;

            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            user.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
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

            var (activeUserId, _, identityError) = ResolveAuthenticatedIdentity(request.Username);
            if (identityError != null)
            {
                return identityError;
            }

            if (!activeUserId.HasValue)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == activeUserId.Value && x.Status == "ACTIVE");

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản" });
            }

            if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Unauthorized(new { message = "Mật khẩu hiện tại không đúng" });
            }

            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        [Authorize(Roles = AdminRoleCode)]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { message = "Thiếu thông tin tạo tài khoản" });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { message = "Mật khẩu cần ít nhất 6 ký tự" });
            }

            var username = request.Username.Trim();
            var email = request.Email?.Trim();
            var phone = request.Phone?.Trim();

            if (await _context.Users.AnyAsync(x => x.Username == username))
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });
            }

            if (!string.IsNullOrWhiteSpace(phone) &&
                await _context.Users.AnyAsync(x => x.Phone == phone))
            {
                return BadRequest(new { message = "Số điện thoại đã tồn tại" });
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.Hash(request.Password),
                FullName = request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "ACTIVE" : request.Status.Trim().ToUpperInvariant(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var normalizedRoleCodes = request.RoleCodes?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct()
                .ToList() ?? new List<string>();

            if (!normalizedRoleCodes.Any())
            {
                normalizedRoleCodes.Add(ParentRoleCode);
            }

            var validRoles = await _context.Roles
                .Where(x => normalizedRoleCodes.Contains(x.Code))
                .Select(x => new { x.Id, x.Code })
                .ToListAsync();

            foreach (var role in validRoles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo tài khoản thành công",
                userId = user.Id,
                roleCodes = validRoles.Select(x => x.Code).ToList()
            });
        }

        [Authorize(Roles = AdminRoleCode)]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return BadRequest(new { message = "Thiếu thông tin đặt lại mật khẩu" });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Mật khẩu mới cần ít nhất 6 ký tự" });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { message = "Xác nhận mật khẩu mới chưa khớp" });
            }

            var username = request.Username.Trim();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == username);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản" });
            }

            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công" });
        }

        private (long? UserId, string Username, IActionResult? Error) ResolveAuthenticatedIdentity(string? requestedUsername)
        {
            var tokenUserId = User.GetUserId();
            var tokenUsername = User.GetUsername();

            if (!tokenUserId.HasValue || string.IsNullOrWhiteSpace(tokenUsername))
            {
                return (null, string.Empty, Unauthorized(new { message = "Không xác định được tài khoản từ token" }));
            }

            if (!string.IsNullOrWhiteSpace(requestedUsername) &&
                !string.Equals(requestedUsername.Trim(), tokenUsername, StringComparison.OrdinalIgnoreCase))
            {
                return (null, string.Empty, StatusCode(403, new { message = "Không thể thao tác thay cho tài khoản khác" }));
            }

            return (tokenUserId.Value, tokenUsername, null);
        }

        private static string? ResolveRegisteredEmail(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(user.FptEmail))
            {
                return user.FptEmail.Trim();
            }

            return null;
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return string.Empty;
            }

            var at = email.IndexOf('@');
            if (at <= 1)
            {
                return email;
            }

            var name = email[..at];
            var domain = email[(at + 1)..];
            var visible = name.Length <= 2 ? 1 : 2;
            var maskLength = Math.Max(name.Length - visible, 1);

            return $"{name[..visible]}{new string('*', maskLength)}@{domain}";
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
