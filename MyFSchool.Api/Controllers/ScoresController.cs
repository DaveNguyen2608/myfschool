using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScoresController : ControllerBase
    {
        private const string TeacherRoleCode = "TEACHER";

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
                    Name = !string.IsNullOrWhiteSpace(x.YearName)
                        ? x.YearName
                        : $"Năm học {x.Id}"
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
                    academicYear = $"Năm học {academicYearId}",
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

            return Ok(new
            {
                academicYear = $"Năm học {academicYearId}",
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

        [HttpGet("teacher/meta")]
        public async Task<IActionResult> GetTeacherMeta(
            [FromQuery] string username,
            [FromQuery] long? academicYearId)
        {
            var (scope, scopeError) = await ResolveTeacherScope(username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp giáo viên" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "Không tìm thấy năm học" });
            }

            var semesters = await _context.Semesters
                .Where(x => x.AcademicYearId == academic.Id)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    Name = x.SemesterName
                })
                .ToListAsync();

            var subjects = await (
                from tt in _context.Timetables
                join sub in _context.Subjects on tt.SubjectId equals sub.Id
                where tt.ClassId == scope.ClassId && tt.AcademicYearId == academic.Id
                select new
                {
                    sub.Id,
                    Name = sub.SubjectName
                }
            ).Distinct().OrderBy(x => x.Name).ToListAsync();

            if (subjects.Count == 0)
            {
                subjects = await (
                    from tt in _context.Timetables
                    join sub in _context.Subjects on tt.SubjectId equals sub.Id
                    where tt.ClassId == scope.ClassId
                    select new
                    {
                        sub.Id,
                        Name = sub.SubjectName
                    }
                ).Distinct().OrderBy(x => x.Name).ToListAsync();
            }

            var categories = await _context.GradeCategories
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name
                })
                .ToListAsync();

            return Ok(new
            {
                classId = scope.ClassId,
                classCode = scope.ClassCode,
                className = scope.ClassName,
                academicYearId = academic.Id,
                academicYearName = academic.YearName,
                semesters,
                subjects,
                categories
            });
        }

        [HttpGet("teacher/gradebook")]
        public async Task<IActionResult> GetTeacherGradebook(
            [FromQuery] string username,
            [FromQuery] long? academicYearId,
            [FromQuery] long? semesterId,
            [FromQuery] long? subjectId,
            [FromQuery] long? categoryId)
        {
            if (!semesterId.HasValue || !subjectId.HasValue || !categoryId.HasValue)
            {
                return BadRequest(new { message = "Thiếu bộ lọc kỳ, môn học hoặc cột điểm" });
            }

            var (scope, scopeError) = await ResolveTeacherScope(username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp giáo viên" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "Không tìm thấy năm học" });
            }

            var semester = await _context.Semesters
                .FirstOrDefaultAsync(x => x.Id == semesterId.Value && x.AcademicYearId == academic.Id);
            if (semester == null)
            {
                return BadRequest(new { message = "Kỳ học không hợp lệ" });
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(x => x.Id == subjectId.Value);
            if (subject == null)
            {
                return BadRequest(new { message = "Môn học không hợp lệ" });
            }

            var category = await _context.GradeCategories
                .FirstOrDefaultAsync(x => x.Id == categoryId.Value);
            if (category == null)
            {
                return BadRequest(new { message = "Cột điểm không hợp lệ" });
            }

            var students = await _context.Students
                .Where(x => x.CurrentClassId == scope.ClassId && x.Status == "ACTIVE")
                .OrderBy(x => x.StudentCode)
                .ThenBy(x => x.FullName)
                .Select(x => new
                {
                    x.Id,
                    x.StudentCode,
                    x.FullName
                })
                .ToListAsync();

            var studentIds = students.Select(x => x.Id).ToList();

            var grades = await _context.StudentGrades
                .Where(x => studentIds.Contains(x.StudentId)
                         && x.AcademicYearId == academic.Id
                         && x.SemesterId == semesterId.Value
                         && x.SubjectId == subjectId.Value
                         && x.CategoryId == categoryId.Value)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var gradeByStudent = grades
                .GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => g.First());

            var rows = students.Select(s =>
            {
                var hasGrade = gradeByStudent.TryGetValue(s.Id, out var grade);
                return new
                {
                    studentId = s.Id,
                    studentCode = s.StudentCode,
                    studentName = s.FullName,
                    gradeId = hasGrade ? grade!.Id : (long?)null,
                    gradeType = hasGrade ? grade!.GradeType : string.Empty,
                    score = hasGrade ? grade!.Score : (decimal?)null,
                    note = hasGrade ? grade!.Note : string.Empty
                };
            }).ToList();

            return Ok(new
            {
                classId = scope.ClassId,
                classCode = scope.ClassCode,
                className = scope.ClassName,
                academicYearId = academic.Id,
                academicYearName = academic.YearName,
                semesterId = semester.Id,
                semesterName = semester.SemesterName,
                subjectId = subject.Id,
                subjectName = subject.SubjectName,
                categoryId = category.Id,
                categoryCode = category.Code,
                categoryName = category.Name,
                rows
            });
        }

        [HttpGet("teacher/summary")]
        public async Task<IActionResult> GetTeacherSummary(
            [FromQuery] string username,
            [FromQuery] long? academicYearId,
            [FromQuery] long? semesterId)
        {
            var (scope, scopeError) = await ResolveTeacherScope(username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp giáo viên" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "Không tìm thấy năm học" });
            }

            var query =
                from sgs in _context.StudentGradeSummaries
                join s in _context.Students on sgs.StudentId equals s.Id
                join sem in _context.Semesters on sgs.SemesterId equals sem.Id
                where s.CurrentClassId == scope.ClassId
                      && sgs.AcademicYearId == academic.Id
                select new
                {
                    s.StudentCode,
                    StudentName = s.FullName,
                    sgs.SemesterId,
                    SemesterName = sem.SemesterName,
                    sgs.AverageScore,
                    sgs.AcademicPerformance,
                    sgs.Conduct,
                    sgs.Note
                };

            if (semesterId.HasValue)
            {
                query = query.Where(x => x.SemesterId == semesterId.Value);
            }

            var rows = await query
                .OrderBy(x => x.StudentCode)
                .ThenBy(x => x.SemesterId)
                .ToListAsync();

            return Ok(new
            {
                classId = scope.ClassId,
                classCode = scope.ClassCode,
                className = scope.ClassName,
                academicYearId = academic.Id,
                academicYearName = academic.YearName,
                rows
            });
        }

        [HttpPost("teacher/grades")]
        public async Task<IActionResult> CreateTeacherGrade([FromBody] TeacherCreateGradeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { message = "Thiếu thông tin tài khoản" });
            }

            if (request.StudentId <= 0 || request.SemesterId <= 0 || request.SubjectId <= 0 || request.CategoryId <= 0)
            {
                return BadRequest(new { message = "Thiếu thông tin học sinh hoặc bộ lọc điểm" });
            }

            if (request.Score < 0 || request.Score > 10)
            {
                return BadRequest(new { message = "Điểm phải trong khoảng 0 đến 10" });
            }

            var normalizedGradeType = (request.GradeType ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedGradeType))
            {
                normalizedGradeType = "REGULAR";
            }

            var validGradeTypes = new[] { "REGULAR", "MIDTERM", "FINAL" };
            if (!validGradeTypes.Contains(normalizedGradeType))
            {
                return BadRequest(new { message = "Loại điểm không hợp lệ" });
            }

            var (scope, scopeError) = await ResolveTeacherScope(request.Username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp giáo viên" });
            }

            var (academic, academicError) = await ResolveAcademicContext(request.AcademicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "Không tìm thấy năm học" });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(x => x.Id == request.StudentId && x.CurrentClassId == scope.ClassId);
            if (student == null)
            {
                return BadRequest(new { message = "Học sinh không thuộc lớp chủ nhiệm của giáo viên" });
            }

            var semesterExists = await _context.Semesters
                .AnyAsync(x => x.Id == request.SemesterId && x.AcademicYearId == academic.Id);
            if (!semesterExists)
            {
                return BadRequest(new { message = "Kỳ học không hợp lệ" });
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(x => x.Id == request.SubjectId);
            if (!subjectExists)
            {
                return BadRequest(new { message = "Môn học không hợp lệ" });
            }

            var categoryExists = await _context.GradeCategories
                .AnyAsync(x => x.Id == request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Cột điểm không hợp lệ" });
            }

            var existed = await _context.StudentGrades
                .FirstOrDefaultAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.AcademicYearId == academic.Id &&
                    x.SemesterId == request.SemesterId &&
                    x.SubjectId == request.SubjectId &&
                    x.CategoryId == request.CategoryId &&
                    x.GradeType == normalizedGradeType);

            if (existed != null)
            {
                return Conflict(new
                {
                    message = "Điểm đã tồn tại, vui lòng dùng chức năng sửa điểm",
                    gradeId = existed.Id
                });
            }

            var grade = new StudentGrade
            {
                StudentId = request.StudentId,
                AcademicYearId = academic.Id,
                SemesterId = request.SemesterId,
                SubjectId = request.SubjectId,
                CategoryId = request.CategoryId,
                GradeType = normalizedGradeType,
                Score = request.Score,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentGrades.Add(grade);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm điểm thành công",
                gradeId = grade.Id
            });
        }

        [HttpPut("teacher/grades/{gradeId:long}")]
        public async Task<IActionResult> UpdateTeacherGrade(
            [FromRoute] long gradeId,
            [FromBody] TeacherUpdateGradeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { message = "Thiếu thông tin tài khoản" });
            }

            if (request.Score < 0 || request.Score > 10)
            {
                return BadRequest(new { message = "Điểm phải trong khoảng 0 đến 10" });
            }

            var (scope, scopeError) = await ResolveTeacherScope(request.Username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "Không xác định được lớp giáo viên" });
            }

            var target = await (
                from g in _context.StudentGrades
                join s in _context.Students on g.StudentId equals s.Id
                where g.Id == gradeId
                select new
                {
                    Grade = g,
                    s.CurrentClassId
                }
            ).FirstOrDefaultAsync();

            if (target == null)
            {
                return NotFound(new { message = "Không tìm thấy bản ghi điểm" });
            }

            if (!target.CurrentClassId.HasValue || target.CurrentClassId.Value != scope.ClassId)
            {
                return StatusCode(403, new { message = "Bạn chỉ được sửa điểm học sinh trong lớp chủ nhiệm" });
            }

            target.Grade.Score = request.Score;
            target.Grade.Note = request.Note;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật điểm thành công" });
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

        private async Task<(TeacherScope? Scope, IActionResult? Error)> ResolveTeacherScope(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return (null, BadRequest(new { message = "Tên đăng nhập không được để trống" }));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == username.Trim() && x.Status == "ACTIVE");

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

            var hasTeacherRole = roleCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Contains(TeacherRoleCode);

            if (!hasTeacherRole)
            {
                return (null, StatusCode(403, new { message = "Tài khoản không có quyền xem/sửa bảng điểm giáo viên" }));
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (teacher == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy hồ sơ giáo viên" }));
            }

            var schoolClass = await _context.SchoolClasses
                .Where(x => x.HomeroomTeacherId == teacher.Id)
                .OrderBy(x => x.ClassCode)
                .FirstOrDefaultAsync();

            if (schoolClass == null)
            {
                return (null, NotFound(new { message = "Giáo viên chưa được phân công lớp chủ nhiệm" }));
            }

            return (new TeacherScope
            {
                TeacherId = teacher.Id,
                ClassId = schoolClass.Id,
                ClassCode = schoolClass.ClassCode,
                ClassName = schoolClass.ClassName
            }, null);
        }

        private async Task<(TeacherAcademicContext? Context, IActionResult? Error)> ResolveAcademicContext(long? requestedAcademicYearId)
        {
            AcademicYear? academicYear;

            if (requestedAcademicYearId.HasValue && requestedAcademicYearId.Value > 0)
            {
                academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(x => x.Id == requestedAcademicYearId.Value);

                if (academicYear == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy năm học theo bộ lọc" }));
                }
            }
            else
            {
                academicYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(x => x.YearName == "2025-2026");

                if (academicYear == null)
                {
                    academicYear = await _context.AcademicYears
                        .FirstOrDefaultAsync(x => x.IsActive);
                }

                if (academicYear == null)
                {
                    return (null, NotFound(new { message = "Không tìm thấy năm học" }));
                }
            }

            return (new TeacherAcademicContext
            {
                Id = academicYear.Id,
                YearName = string.IsNullOrWhiteSpace(academicYear.YearName)
                    ? $"Năm học {academicYear.Id}"
                    : academicYear.YearName
            }, null);
        }
    }

    internal sealed class TeacherScope
    {
        public long TeacherId { get; set; }
        public long ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
    }

    internal sealed class TeacherAcademicContext
    {
        public long Id { get; set; }
        public string YearName { get; set; } = string.Empty;
    }

    public sealed class TeacherCreateGradeRequest
    {
        public string Username { get; set; } = string.Empty;
        public long? AcademicYearId { get; set; }
        public long StudentId { get; set; }
        public long SemesterId { get; set; }
        public long SubjectId { get; set; }
        public long CategoryId { get; set; }
        public string? GradeType { get; set; }
        public decimal Score { get; set; }
        public string? Note { get; set; }
    }

    public sealed class TeacherUpdateGradeRequest
    {
        public string Username { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string? Note { get; set; }
    }
}
