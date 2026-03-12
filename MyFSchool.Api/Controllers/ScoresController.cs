using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScoresController : ControllerBase
    {
        private readonly MyFSchoolDbContext _context;

        public ScoresController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("academic-years")]
        public async Task<IActionResult> GetAcademicYears()
        {
            var years = await _context.AcademicYears
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    Name = "Năm học " + x.Id
                })
                .ToListAsync();

            return Ok(years);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string username,
            [FromQuery] long academicYearId,
            [FromQuery] int semesterNo)
        {
            var studentId = await GetStudentIdByParentUsername(username);
            if (studentId == null)
                return BadRequest(new { message = "Không tìm thấy học sinh" });

            var data = await _context.StudentScores
                .Where(x => x.StudentId == studentId
                         && x.AcademicYearId == academicYearId
                         && x.SemesterNo == semesterNo)
                .ToListAsync();

            if (!data.Any())
            {
                return Ok(new
                {
                    academicYear = "Năm học " + academicYearId,
                    semester = semesterNo == 1 ? "Kỳ 1" : semesterNo == 2 ? "Kỳ 2" : "Cả năm",
                    averageScore = 0,
                    academicPerformance = "",
                    conduct = "",
                    note = ""
                });
            }

            var first = data.First();

            var numericScores = data
                .Where(x => x.AverageScore.HasValue)
                .Select(x => (double)x.AverageScore!.Value)
                .ToList();

            var avg = numericScores.Any() ? numericScores.Average() : 0;

            var yearName = "Năm học " + academicYearId;

            return Ok(new
            {
                academicYear = yearName,
                semester = semesterNo == 1 ? "Kỳ 1" : semesterNo == 2 ? "Kỳ 2" : "Cả năm",
                averageScore = Math.Round(avg, 1),
                academicPerformance = first.AcademicPerformance ?? "",
                conduct = first.Conduct ?? "",
                note = first.Note ?? ""
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetScores(
            [FromQuery] string username,
            [FromQuery] long academicYearId,
            [FromQuery] int semesterNo)
        {
            var studentId = await GetStudentIdByParentUsername(username);
            if (studentId == null)
                return BadRequest(new { message = "Không tìm thấy học sinh" });

            var result = await _context.StudentScores
                .Where(x => x.StudentId == studentId
                         && x.AcademicYearId == academicYearId
                         && x.SemesterNo == semesterNo)
                .OrderBy(x => x.SubjectName)
                .Select(x => new
                {
                    x.SubjectName,
                    AverageScore = x.AverageScore,
                    Result = x.Result ?? ""
                })
                .ToListAsync();

            return Ok(result);
        }

        private async Task<long?> GetStudentIdByParentUsername(string username)
        {
            return await (
                from p in _context.Parents
                join u in _context.Users on p.UserId equals u.Id
                join ps in _context.ParentStudents on p.Id equals ps.ParentId
                where u.Username == username
                select (long?)ps.StudentId
            ).FirstOrDefaultAsync();
        }
    }
}