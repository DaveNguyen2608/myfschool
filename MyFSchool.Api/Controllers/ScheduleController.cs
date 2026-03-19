using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleController : ControllerBase
    {
        private const string ParentRoleCode = "PARENT";
        private const string TeacherRoleCode = "TEACHER";

        private static readonly DateTime DefaultAcademicStartDate = new(2025, 9, 5);
        private static readonly DateTime DefaultAcademicEndDate = new(2026, 5, 31);

        private readonly MyFSchoolDbContext _context;

        public ScheduleController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] string username)
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
                homeroomTeacherId: scope.HomeroomTeacherId,
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
            [FromQuery] string username,
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
                homeroomTeacherId: scope.HomeroomTeacherId,
                contactAliases: contactAliases);

            if (periods.Count == 0)
            {
                periods = await QueryScheduleItems(
                    classId: scope.ClassId,
                    academicYearId: null,
                    semesterId: null,
                    dayOfWeek: dayOfWeek,
                    homeroomTeacherId: scope.HomeroomTeacherId,
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

        private async Task<(UserScheduleScope? Scope, IActionResult? Error)> ResolveUserScope(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return (null, BadRequest(new { message = "Tên đăng nhập không được để trống" }));
            }

            var normalizedUsername = username.Trim();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == normalizedUsername && x.Status == "ACTIVE");

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
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(x => x.UserId == user.Id);

                if (teacher == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy hồ sơ giáo viên" }));
                }

                var schoolClass = await _context.SchoolClasses
                    .Where(x => x.HomeroomTeacherId == teacher.Id)
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
                    HomeroomTeacherId = schoolClass.HomeroomTeacherId,
                    IsTeacher = true
                }, null);
            }

            if (normalizedRoleCodes.Contains(ParentRoleCode))
            {
                var parent = await _context.Parents
                    .FirstOrDefaultAsync(x => x.UserId == user.Id);

                if (parent == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy hồ sơ phụ huynh" }));
                }

                var student = await (
                    from ps in _context.ParentStudents
                    join s in _context.Students on ps.StudentId equals s.Id
                    where ps.ParentId == parent.Id && s.Status == "ACTIVE"
                    orderby s.Id
                    select s
                ).FirstOrDefaultAsync();

                if (student == null)
                {
                    return (null, NotFound(new { message = "Phụ huynh chưa được liên kết với học sinh" }));
                }

                if (!student.CurrentClassId.HasValue)
                {
                    return (null, BadRequest(new { message = "Học sinh chưa có lớp hiện tại" }));
                }

                var schoolClass = await _context.SchoolClasses
                    .FirstOrDefaultAsync(x => x.Id == student.CurrentClassId.Value);

                if (schoolClass == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy lớp hiện tại của học sinh" }));
                }

                return (new UserScheduleScope
                {
                    UserId = user.Id,
                    ClassId = schoolClass.Id,
                    OwnerName = student.FullName,
                    HomeroomTeacherId = schoolClass.HomeroomTeacherId,
                    IsTeacher = false
                }, null);
            }

            return (null, StatusCode(403, new { message = "Tài khoản không có quyền xem lịch học" }));
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
                    TeacherId = x.TeacherId,
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
                join teacher in _context.Teachers on tt.TeacherId equals teacher.Id
                join teacherUser in _context.Users on teacher.UserId equals teacherUser.Id
                where tt.ClassId == classId
                select new RawScheduleItem
                {
                    AcademicYearId = tt.AcademicYearId,
                    SemesterId = tt.SemesterId,
                    TeacherId = tt.TeacherId,
                    SubjectId = tt.SubjectId,
                    DayOfWeek = tt.DayOfWeek,
                    PeriodNo = slot.PeriodNo,
                    StartTime = slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = slot.EndTime.ToString(@"hh\:mm"),
                    SubjectName = sub.SubjectName,
                    RoomName = tt.RoomName,
                    TimetableAlias = tt.Note,
                    TeacherName = teacherUser.FullName,
                    TeacherPhone = teacherUser.Phone,
                    TeacherEmail = !string.IsNullOrWhiteSpace(teacher.FptEmail)
                        ? teacher.FptEmail
                        : teacherUser.Email
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
        public long? HomeroomTeacherId { get; set; }
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
