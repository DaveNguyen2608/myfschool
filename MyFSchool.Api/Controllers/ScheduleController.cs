using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;
using MyFSchool.Api.Security;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "PARENT,TEACHER,STUDENT")]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private const string ParentRoleCode = "PARENT";
        private const string TeacherRoleCode = "TEACHER";
        private const string StudentRoleCode = "STUDENT";

        private static readonly DateTime DefaultAcademicStartDate = new(2025, 9, 5);
        private static readonly DateTime DefaultAcademicEndDate = new(2026, 5, 31);

        private readonly MyFSchoolDbContext _context;

        public ScheduleController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] string? username)
        {
            var (scope, error) = await ResolveUserScope(username);
            if (error != null)
            {
                return error;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp để xem lịch học" });
            }

            var (academic, academicError) = await ResolveAcademicContext();
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "Không xác định được năm học hiện tại" });
            }

            var contactAliases = await GetContactAliases(scope.ClassId);
            var items = await QueryScheduleItems(
                classId: scope.ClassId,
                academicYearId: academic.AcademicYearId,
                semesterId: academic.SemesterId,
                dayOfWeek: null,
                homeroomTeacherId: scope.HomeroomTeacherUserId,
                contactAliases: contactAliases);

            return Ok(new
            {
                message = "Lấy lịch học thành công",
                studentName = scope.OwnerName,
                classId = scope.ClassId,
                academicYear = academic.AcademicYearName,
                semester = academic.SemesterName,
                data = items
            });
        }

        [HttpGet("by-date")]
        public async Task<IActionResult> GetScheduleByDate(
            [FromQuery] string? username,
            [FromQuery] DateTime date,
            [FromQuery] long? classId)
        {
            if (date == default)
            {
                return BadRequest(new { message = "Ngày không hợp lệ" });
            }

            var (scope, error) = await ResolveUserScope(username);
            if (error != null)
            {
                return error;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp để xem lịch học" });
            }

            if (classId.HasValue && classId.Value != scope.ClassId)
            {
                return StatusCode(403, new { message = "Bạn chỉ được xem lịch học của lớp thuộc quyền của mình" });
            }

            var (academic, academicError) = await ResolveAcademicContext();
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "Không xác định được năm học hiện tại" });
            }

            var selectedDate = date.Date;
            var dayOfWeek = ToScheduleDayOfWeek(selectedDate);
            var academicRange = ResolveAcademicRange(academic);
            var isInAcademicYear =
                selectedDate >= academicRange.StartDate &&
                selectedDate <= academicRange.EndDate;

            if (!isInAcademicYear)
            {
                return Ok(new
                {
                    selectedDate = selectedDate.ToString("yyyy-MM-dd"),
                    dayOfWeek,
                    isInAcademicYear = false,
                    isHoliday = false,
                    holidayTitle = string.Empty,
                    holidayDescription = string.Empty,
                    classId = scope.ClassId,
                    academicYear = academic.AcademicYearName,
                    academicYearStartDate = academicRange.StartDate.ToString("yyyy-MM-dd"),
                    academicYearEndDate = academicRange.EndDate.ToString("yyyy-MM-dd"),
                    semester = academic.SemesterName,
                    periods = new List<ScheduleItemResponse>()
                });
            }

            var holiday = await _context.SchoolCalendarExceptions
                .Where(x =>
                    x.IsActive &&
                    x.Date >= selectedDate &&
                    x.Date < selectedDate.AddDays(1))
                .OrderByDescending(x => x.AcademicYearId == academic.AcademicYearId)
                .FirstOrDefaultAsync();

            if (holiday != null)
            {
                return Ok(new
                {
                    selectedDate = selectedDate.ToString("yyyy-MM-dd"),
                    dayOfWeek,
                    isInAcademicYear = true,
                    isHoliday = true,
                    holidayTitle = holiday.Title,
                    holidayDescription = holiday.Description ?? string.Empty,
                    classId = scope.ClassId,
                    academicYear = academic.AcademicYearName,
                    academicYearStartDate = academicRange.StartDate.ToString("yyyy-MM-dd"),
                    academicYearEndDate = academicRange.EndDate.ToString("yyyy-MM-dd"),
                    semester = academic.SemesterName,
                    periods = new List<ScheduleItemResponse>()
                });
            }

            var contactAliases = await GetContactAliases(scope.ClassId);
            var periods = await QueryScheduleItems(
                classId: scope.ClassId,
                academicYearId: academic.AcademicYearId,
                semesterId: academic.SemesterId,
                dayOfWeek: dayOfWeek,
                homeroomTeacherId: scope.HomeroomTeacherUserId,
                contactAliases: contactAliases);

            if (periods.Count == 0)
            {
                periods = await QueryScheduleItems(
                    classId: scope.ClassId,
                    academicYearId: null,
                    semesterId: null,
                    dayOfWeek: dayOfWeek,
                    homeroomTeacherId: scope.HomeroomTeacherUserId,
                    contactAliases: contactAliases);
            }

            return Ok(new
            {
                selectedDate = selectedDate.ToString("yyyy-MM-dd"),
                dayOfWeek,
                isInAcademicYear = true,
                isHoliday = false,
                holidayTitle = string.Empty,
                holidayDescription = string.Empty,
                classId = scope.ClassId,
                academicYear = academic.AcademicYearName,
                academicYearStartDate = academicRange.StartDate.ToString("yyyy-MM-dd"),
                academicYearEndDate = academicRange.EndDate.ToString("yyyy-MM-dd"),
                semester = academic.SemesterName,
                periods
            });
        }

        private async Task<(UserScheduleScope? Scope, IActionResult? Error)> ResolveUserScope(string? requestedUsername)
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

            if (normalizedRoleCodes.Contains(TeacherRoleCode))
            {
                var schoolClass = await _context.SchoolClasses
                    .Where(x => x.HomeroomTeacherUserId == user.Id)
                    .OrderBy(x => x.ClassName)
                    .FirstOrDefaultAsync();

                if (schoolClass == null)
                {
                    return (null, NotFound(new { message = "Bạn chưa được phân công chủ nhiệm lớp nào" }));
                }

                return (new UserScheduleScope
                {
                    UserId = user.Id,
                    ClassId = schoolClass.Id,
                    OwnerName = user.FullName,
                    HomeroomTeacherUserId = schoolClass.HomeroomTeacherUserId,
                    IsTeacher = true
                }, null);
            }

            if (normalizedRoleCodes.Contains(ParentRoleCode))
            {
                var student = await (
                    from rel in _context.ParentStudentRelationships
                    join s in _context.Users on rel.StudentUserId equals s.Id
                    where rel.ParentUserId == user.Id && s.Status == "ACTIVE"
                    orderby s.Id
                    select s
                ).FirstOrDefaultAsync();

                if (student == null)
                {
                    return (null, NotFound(new { message = "Phụ huynh chưa được liên kết với học sinh" }));
                }

                var classId = await ResolveStudentClassId(student.Id);
                if (!classId.HasValue)
                {
                    return (null, BadRequest(new { message = "Học sinh chưa có lớp hiện tại" }));
                }

                var schoolClass = await _context.SchoolClasses
                    .FirstOrDefaultAsync(x => x.Id == classId.Value);

                if (schoolClass == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy lớp hiện tại của học sinh" }));
                }

                return (new UserScheduleScope
                {
                    UserId = user.Id,
                    ClassId = schoolClass.Id,
                    OwnerName = student.FullName,
                    HomeroomTeacherUserId = schoolClass.HomeroomTeacherUserId,
                    IsTeacher = false
                }, null);
            }

            if (normalizedRoleCodes.Contains(StudentRoleCode))
            {
                var classId = await ResolveStudentClassId(user.Id);
                if (!classId.HasValue)
                {
                    return (null, BadRequest(new { message = "Học sinh chưa có lớp hiện tại" }));
                }

                var schoolClass = await _context.SchoolClasses
                    .FirstOrDefaultAsync(x => x.Id == classId.Value);

                if (schoolClass == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy lớp hiện tại của học sinh" }));
                }

                return (new UserScheduleScope
                {
                    UserId = user.Id,
                    ClassId = schoolClass.Id,
                    OwnerName = user.FullName,
                    HomeroomTeacherUserId = schoolClass.HomeroomTeacherUserId,
                    IsTeacher = false
                }, null);
            }

            return (null, StatusCode(403, new { message = "Tài khoản không có quyền xem lịch học" }));
        }

        private async Task<long?> ResolveStudentClassId(long studentUserId)
        {
            var activeAcademicYearId = await _context.AcademicYears
                .Where(x => x.IsActive)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();

            var classId = await _context.ClassStudents
                .Where(x =>
                    x.StudentUserId == studentUserId &&
                    (!activeAcademicYearId.HasValue || x.AcademicYearId == activeAcademicYearId.Value) &&
                    !x.LeftAt.HasValue)
                .OrderByDescending(x => x.AcademicYearId)
                .ThenByDescending(x => x.JoinedAt)
                .Select(x => (long?)x.ClassId)
                .FirstOrDefaultAsync();

            if (classId.HasValue)
            {
                return classId;
            }

            return await _context.Users
                .Where(x => x.Id == studentUserId)
                .Select(x => x.CurrentClassId)
                .FirstOrDefaultAsync();
        }

        private async Task<(AcademicContext? Context, IActionResult? Error)> ResolveAcademicContext()
        {
            var academicYear = await _context.AcademicYears
                .FirstOrDefaultAsync(x => x.IsActive);

            if (academicYear == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy năm học đang hoạt động" }));
            }

            var semester = await _context.Semesters
                .Where(x => x.AcademicYearId == academicYear.Id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (semester == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy học kỳ" }));
            }

            return (new AcademicContext
            {
                AcademicYearId = academicYear.Id,
                AcademicYearName = academicYear.YearName,
                AcademicYearStartDate = academicYear.StartDate,
                AcademicYearEndDate = academicYear.EndDate,
                SemesterId = semester.Id,
                SemesterName = semester.SemesterName
            }, null);
        }

        private static AcademicDateRange ResolveAcademicRange(AcademicContext academic)
        {
            if (academic.AcademicYearStartDate != default && academic.AcademicYearEndDate != default)
            {
                return new AcademicDateRange
                {
                    StartDate = academic.AcademicYearStartDate.Date,
                    EndDate = academic.AcademicYearEndDate.Date
                };
            }

            return new AcademicDateRange
            {
                StartDate = DefaultAcademicStartDate,
                EndDate = DefaultAcademicEndDate
            };
        }

        private async Task<List<ContactAliasRow>> GetContactAliases(long classId)
        {
            return await _context.TeacherContacts
                .Where(x => !x.ClassId.HasValue || x.ClassId == classId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Select(x => new ContactAliasRow
                {
                    TeacherId = x.TeacherUserId,
                    SubjectId = x.SubjectId,
                    Alias = x.Note
                })
                .ToListAsync();
        }

        private async Task<List<ScheduleItemResponse>> QueryScheduleItems(
            long classId,
            long? academicYearId,
            long? semesterId,
            int? dayOfWeek,
            long? homeroomTeacherId,
            List<ContactAliasRow> contactAliases)
        {
            var query =
                from tt in _context.Timetables
                join slot in _context.ScheduleSlots on tt.SlotId equals slot.Id
                join sub in _context.Subjects on tt.SubjectId equals sub.Id
                join teacherUser in _context.Users on tt.TeacherUserId equals (long?)teacherUser.Id into teacherJoin
                from teacherUser in teacherJoin.DefaultIfEmpty()
                where tt.ClassId == classId
                select new RawScheduleItem
                {
                    AcademicYearId = tt.AcademicYearId,
                    SemesterId = tt.SemesterId,
                    TeacherId = tt.TeacherUserId ?? 0,
                    SubjectId = tt.SubjectId,
                    DayOfWeek = tt.DayOfWeek,
                    PeriodNo = slot.PeriodNo,
                    StartTime = slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = slot.EndTime.ToString(@"hh\:mm"),
                    SubjectName = sub.SubjectName,
                    RoomName = tt.RoomName,
                    TimetableAlias = tt.Note,
                    TeacherName = teacherUser != null ? teacherUser.FullName : string.Empty,
                    TeacherPhone = teacherUser != null ? teacherUser.Phone : string.Empty,
                    TeacherEmail = teacherUser != null
                        ? (!string.IsNullOrWhiteSpace(teacherUser.FptEmail)
                            ? teacherUser.FptEmail
                            : teacherUser.Email)
                        : string.Empty
                };

            if (academicYearId.HasValue)
            {
                query = query.Where(x => x.AcademicYearId == academicYearId.Value);
            }

            if (semesterId.HasValue)
            {
                query = query.Where(x => x.SemesterId == semesterId.Value);
            }

            if (dayOfWeek.HasValue)
            {
                query = query.Where(x => x.DayOfWeek == dayOfWeek.Value);
            }

            var rawItems = await query
                .OrderBy(x => x.DayOfWeek)
                .ThenBy(x => x.PeriodNo)
                .ToListAsync();

            return rawItems
                .Select(x =>
                {
                    var alias = ResolveAlias(
                        timetableAlias: x.TimetableAlias,
                        teacherId: x.TeacherId,
                        subjectId: x.SubjectId,
                        contactAliases: contactAliases,
                        fallbackName: x.TeacherName ?? string.Empty);

                    var role = homeroomTeacherId.HasValue && homeroomTeacherId.Value == x.TeacherId
                        ? "Giáo viên chủ nhiệm"
                        : "Giáo viên bộ môn";

                    return new ScheduleItemResponse
                    {
                        TeacherId = x.TeacherId,
                        TeacherAlias = alias,
                        DayOfWeek = x.DayOfWeek,
                        PeriodNo = x.PeriodNo,
                        StartTime = x.StartTime,
                        EndTime = x.EndTime,
                        SubjectName = x.SubjectName,
                        RoomName = x.RoomName ?? string.Empty,
                        TeacherName = x.TeacherName ?? string.Empty,
                        TeacherPhone = x.TeacherPhone ?? string.Empty,
                        TeacherEmail = x.TeacherEmail ?? string.Empty,
                        TeacherRole = role,
                        Note = x.TimetableAlias ?? string.Empty
                    };
                })
                .ToList();
        }

        private static string ResolveAlias(
            string? timetableAlias,
            long teacherId,
            long subjectId,
            IEnumerable<ContactAliasRow> contactAliases,
            string fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(timetableAlias))
            {
                return timetableAlias.Trim();
            }

            var aliasByTeacherAndSubject = contactAliases
                .Where(x =>
                    x.TeacherId == teacherId &&
                    x.SubjectId.HasValue &&
                    x.SubjectId.Value == subjectId &&
                    !string.IsNullOrWhiteSpace(x.Alias))
                .Select(x => x.Alias!.Trim())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(aliasByTeacherAndSubject))
            {
                return aliasByTeacherAndSubject;
            }

            var aliasByTeacher = contactAliases
                .Where(x => x.TeacherId == teacherId && !string.IsNullOrWhiteSpace(x.Alias))
                .Select(x => x.Alias!.Trim())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(aliasByTeacher))
            {
                return aliasByTeacher;
            }

            return fallbackName;
        }

        private static int ToScheduleDayOfWeek(DateTime date)
        {
            var dow = (int)date.DayOfWeek;
            return dow == 0 ? 7 : dow;
        }
    }

    internal sealed class UserScheduleScope
    {
        public long UserId { get; set; }
        public long ClassId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public long? HomeroomTeacherUserId { get; set; }
        public bool IsTeacher { get; set; }
    }

    internal sealed class AcademicContext
    {
        public long AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;
        public DateTime AcademicYearStartDate { get; set; }
        public DateTime AcademicYearEndDate { get; set; }
        public long SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
    }

    internal sealed class AcademicDateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    internal sealed class ContactAliasRow
    {
        public long TeacherId { get; set; }
        public long? SubjectId { get; set; }
        public string? Alias { get; set; }
    }

    internal sealed class RawScheduleItem
    {
        public long AcademicYearId { get; set; }
        public long SemesterId { get; set; }
        public long TeacherId { get; set; }
        public long SubjectId { get; set; }
        public int DayOfWeek { get; set; }
        public int PeriodNo { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string? RoomName { get; set; }
        public string? TimetableAlias { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPhone { get; set; }
        public string? TeacherEmail { get; set; }
    }
}
