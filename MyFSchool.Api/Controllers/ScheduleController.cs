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
        private readonly MyFSchoolDbContext _context;

        public ScheduleController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return BadRequest(new { message = "Username không được để trống" });
                }

                var parentUser = await _context.Users
                    .FirstOrDefaultAsync(x => x.Username == username && x.Status == "ACTIVE");

                if (parentUser == null)
                {
                    return NotFound(new { message = "Không tìm thấy phụ huynh" });
                }

                var parent = await _context.Parents
                    .FirstOrDefaultAsync(x => x.UserId == parentUser.Id);

                if (parent == null)
                {
                    return NotFound(new { message = "Tài khoản chưa có hồ sơ phụ huynh" });
                }

                var parentStudent = await _context.ParentStudents
                    .FirstOrDefaultAsync(x => x.ParentId == parent.Id);

                if (parentStudent == null)
                {
                    return NotFound(new { message = "Phụ huynh chưa được liên kết với học sinh" });
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(x => x.Id == parentStudent.StudentId && x.Status == "ACTIVE");

                if (student == null)
                {
                    return NotFound(new { message = "Không tìm thấy học sinh" });
                }

                if (student.CurrentClassId == null)
                {
                    return NotFound(new { message = "Học sinh chưa có lớp hiện tại" });
                }

                var academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(x => x.IsActive);

                if (academicYear == null)
                {
                    return NotFound(new { message = "Không tìm thấy năm học đang hoạt động" });
                }

                var semester = await _context.Semesters
                    .Where(x => x.AcademicYearId == academicYear.Id)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (semester == null)
                {
                    return NotFound(new { message = "Không tìm thấy học kỳ" });
                }

                var items = await (
                    from tt in _context.Timetables
                    join slot in _context.ScheduleSlots on tt.SlotId equals slot.Id
                    join sub in _context.Subjects on tt.SubjectId equals sub.Id
                    join teacher in _context.Teachers on tt.TeacherId equals teacher.Id
                    join teacherUser in _context.Users on teacher.UserId equals teacherUser.Id
                    where tt.ClassId == student.CurrentClassId
                          && tt.AcademicYearId == academicYear.Id
                          && tt.SemesterId == semester.Id
                    orderby tt.DayOfWeek, slot.PeriodNo
                    select new ScheduleItemResponse
                    {
                        DayOfWeek = tt.DayOfWeek,
                        PeriodNo = slot.PeriodNo,
                        StartTime = slot.StartTime.ToString(@"hh\:mm"),
                        EndTime = slot.EndTime.ToString(@"hh\:mm"),
                        SubjectName = sub.SubjectName,
                        RoomName = tt.RoomName ?? "",
                        TeacherName = !string.IsNullOrWhiteSpace(tt.Note)
                            ? tt.Note
                            : teacherUser.FullName,
                        Note = tt.Note ?? ""
                    }
                ).ToListAsync();

                return Ok(new
                {
                    message = "Lấy lịch học thành công",
                    studentName = student.FullName,
                    classId = student.CurrentClassId,
                    academicYear = academicYear.YearName,
                    semester = semester.SemesterName,
                    data = items
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