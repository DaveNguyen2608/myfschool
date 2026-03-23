using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;
using MyFSchool.Api.Models;
using MyFSchool.Api.Security;
using System.Globalization;
using System.Text;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ScoresController : ControllerBase
    {
        private const string TeacherRoleCode = "TEACHER";
        private const string AdminRoleCode = "ADMIN";
        private const string StudentRoleCode = "STUDENT";

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
                        : $"NÄƒm há»c {x.Id}"
                })
                .ToListAsync();

            return Ok(years);
        }

        [HttpGet("summary")]
        [Authorize(Roles = "PARENT,STUDENT")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] string? username,
            [FromQuery] long academicYearId,
            [FromQuery] int semesterNo)
        {
            var (studentUserId, studentError) = await ResolveStudentUserIdForScoreView(username);
            if (studentError != null)
            {
                return studentError;
            }

            if (!studentUserId.HasValue)
            {
                return BadRequest(new { message = "Không tìm thấy học sinh" });
            }

            var semester = await ResolveSemesterByLegacyNo(academicYearId, semesterNo);
            if (semester == null)
            {
                return Ok(new
                {
                    academicYear = $"NÄƒm há»c {academicYearId}",
                    semester = GetLegacySemesterLabel(semesterNo),
                    averageScore = 0,
                    academicPerformance = string.Empty,
                    conduct = string.Empty,
                    note = string.Empty
                });
            }

            var summary = await _context.StudentGradeSummaries
                .FirstOrDefaultAsync(x =>
                    x.StudentUserId == studentUserId.Value &&
                    x.AcademicYearId == academicYearId &&
                    x.SemesterId == semester.Id);

            decimal? averageScore = summary?.AverageScore;

            if (!averageScore.HasValue)
            {
                var scores = await _context.StudentGrades
                    .Where(x =>
                        x.StudentUserId == studentUserId.Value &&
                        x.AcademicYearId == academicYearId &&
                        x.SemesterId == semester.Id &&
                        x.Score.HasValue)
                    .Select(x => x.Score!.Value)
                    .ToListAsync();

                if (scores.Count > 0)
                {
                    averageScore = scores.Average();
                }
            }

            return Ok(new
            {
                academicYear = $"NÄƒm há»c {academicYearId}",
                semester = semester.SemesterName,
                averageScore = averageScore.HasValue ? Math.Round(averageScore.Value, 1) : 0,
                academicPerformance = summary?.AcademicPerformance ?? string.Empty,
                conduct = summary?.Conduct ?? string.Empty,
                note = summary?.Note ?? string.Empty
            });
        }

        [HttpGet]
        [Authorize(Roles = "PARENT,STUDENT")]
        public async Task<IActionResult> GetScores(
            [FromQuery] string? username,
            [FromQuery] long academicYearId,
            [FromQuery] int semesterNo)
        {
            var (studentUserId, studentError) = await ResolveStudentUserIdForScoreView(username);
            if (studentError != null)
            {
                return studentError;
            }

            if (!studentUserId.HasValue)
            {
                return BadRequest(new { message = "Không tìm thấy học sinh" });
            }

            var semester = await ResolveSemesterByLegacyNo(academicYearId, semesterNo);
            if (semester == null)
            {
                return Ok(new List<object>());
            }

            var gradeRows = await (
                from grade in _context.StudentGrades
                join subject in _context.Subjects on grade.SubjectId equals subject.Id
                join category in _context.GradeCategories on grade.CategoryId equals category.Id into categoryJoin
                from category in categoryJoin.DefaultIfEmpty()
                where grade.StudentUserId == studentUserId.Value
                      && grade.AcademicYearId == academicYearId
                      && grade.SemesterId == semester.Id
                select new
                {
                    subject.SubjectName,
                    CategoryCode = category != null ? category.Code : string.Empty,
                    grade.Score
                }
            ).ToListAsync();

            var result = gradeRows
                .GroupBy(x => x.SubjectName)
                .Select(g =>
                {
                    var avgCategory = g.FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.CategoryCode) &&
                        x.CategoryCode.Trim().ToUpperInvariant() == "AVG" &&
                        x.Score.HasValue);

                    var avgScore = avgCategory?.Score;
                    if (!avgScore.HasValue)
                    {
                        var values = g.Where(x => x.Score.HasValue).Select(x => x.Score!.Value).ToList();
                        avgScore = values.Count > 0 ? values.Average() : null;
                    }

                    var resultText = string.Empty;
                    if (avgScore.HasValue)
                    {
                        resultText = avgScore.Value >= 5 ? "Äáº¡t" : "ChÆ°a Ä‘áº¡t";
                    }

                    return new
                    {
                        SubjectName = g.Key,
                        AverageScore = avgScore,
                        Result = resultText
                    };
                })
                .OrderBy(x => x.SubjectName)
                .ToList();

            return Ok(result);
        }

        [HttpGet("teacher/meta")]
        [Authorize(Roles = TeacherRoleCode + "," + AdminRoleCode)]
        public async Task<IActionResult> GetTeacherMeta(
            [FromQuery] string? username,
            [FromQuery] long? academicYearId)
        {
            var (scope, scopeError) = await ResolveTeacherScope(username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "KhÃ´ng xÃ¡c Ä‘á»‹nh Ä‘Æ°á»£c lá»›p giÃ¡o viÃªn" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
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
                categories,
                manualEditAllowed = false,
                scoreUpdateMode = "IMPORT_EXCEL"
            });
        }

        [HttpGet("teacher/gradebook")]
        [Authorize(Roles = TeacherRoleCode + "," + AdminRoleCode)]
        public async Task<IActionResult> GetTeacherGradebook(
            [FromQuery] string? username,
            [FromQuery] long? academicYearId,
            [FromQuery] long? semesterId,
            [FromQuery] long? subjectId,
            [FromQuery] long? categoryId)
        {
            if (!semesterId.HasValue || !subjectId.HasValue || !categoryId.HasValue)
            {
                return BadRequest(new { message = "Thiáº¿u bá»™ lá»c ká»³, mÃ´n há»c hoáº·c cá»™t Ä‘iá»ƒm" });
            }

            var (scope, scopeError) = await ResolveTeacherScope(username);
            if (scopeError != null)
            {
                return scopeError;
            }

            if (scope == null)
            {
                return NotFound(new { message = "KhÃ´ng xÃ¡c Ä‘á»‹nh Ä‘Æ°á»£c lá»›p giÃ¡o viÃªn" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
            }

            var semester = await _context.Semesters
                .FirstOrDefaultAsync(x => x.Id == semesterId.Value && x.AcademicYearId == academic.Id);
            if (semester == null)
            {
                return BadRequest(new { message = "Ká»³ há»c khÃ´ng há»£p lá»‡" });
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(x => x.Id == subjectId.Value);
            if (subject == null)
            {
                return BadRequest(new { message = "MÃ´n há»c khÃ´ng há»£p lá»‡" });
            }

            var category = await _context.GradeCategories
                .FirstOrDefaultAsync(x => x.Id == categoryId.Value);
            if (category == null)
            {
                return BadRequest(new { message = "Cá»™t Ä‘iá»ƒm khÃ´ng há»£p lá»‡" });
            }

            var studentIds = await ResolveStudentIdsInClass(scope.ClassId, academic.Id);
            if (!studentIds.Any())
            {
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
                    rows = new List<object>(),
                    manualEditAllowed = false,
                    scoreUpdateMode = "IMPORT_EXCEL"
                });
            }

            var students = await _context.Users
                .Where(x => studentIds.Contains(x.Id) && x.Status == "ACTIVE")
                .OrderBy(x => x.StudentCode)
                .ThenBy(x => x.FullName)
                .Select(x => new
                {
                    x.Id,
                    StudentCode = x.StudentCode ?? string.Empty,
                    x.FullName
                })
                .ToListAsync();

            var grades = await _context.StudentGrades
                .Where(x => studentIds.Contains(x.StudentUserId)
                         && x.AcademicYearId == academic.Id
                         && x.SemesterId == semesterId.Value
                         && x.SubjectId == subjectId.Value
                         && x.CategoryId == categoryId.Value)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var gradeByStudent = grades
                .GroupBy(x => x.StudentUserId)
                .ToDictionary(g => g.Key, g => g.First());

            var rows = students.Select(s =>
            {
                var hasGrade = gradeByStudent.TryGetValue(s.Id, out var grade);
                var clientGradeType = hasGrade
                    ? ToClientGradeType(grade!.GradeType, category.Code)
                    : ToClientGradeType(string.Empty, category.Code);

                return new
                {
                    studentId = s.Id,
                    studentCode = s.StudentCode,
                    studentName = s.FullName,
                    gradeId = hasGrade ? grade!.Id : (long?)null,
                    gradeType = clientGradeType,
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
                rows,
                manualEditAllowed = false,
                scoreUpdateMode = "IMPORT_EXCEL"
            });
        }

        [HttpGet("teacher/summary")]
        [Authorize(Roles = TeacherRoleCode + "," + AdminRoleCode)]
        public async Task<IActionResult> GetTeacherSummary(
            [FromQuery] string? username,
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
                return NotFound(new { message = "KhÃ´ng xÃ¡c Ä‘á»‹nh Ä‘Æ°á»£c lá»›p giÃ¡o viÃªn" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
            }

            var studentIds = await ResolveStudentIdsInClass(scope.ClassId, academic.Id);

            var query =
                from sgs in _context.StudentGradeSummaries
                join s in _context.Users on sgs.StudentUserId equals s.Id
                join sem in _context.Semesters on sgs.SemesterId equals sem.Id
                where studentIds.Contains(s.Id)
                      && sgs.AcademicYearId == academic.Id
                select new
                {
                    StudentCode = s.StudentCode ?? string.Empty,
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
                rows,
                manualEditAllowed = false,
                scoreUpdateMode = "IMPORT_EXCEL"
            });
        }

        [HttpGet("teacher/export")]
        [Authorize(Roles = TeacherRoleCode + "," + AdminRoleCode)]
        public async Task<IActionResult> ExportTeacherScores(
            [FromQuery] string? username,
            [FromQuery] long classId,
            [FromQuery] long? academicYearId,
            [FromQuery] long? semesterId,
            [FromQuery] string? semesterType)
        {
            if (classId <= 0)
            {
                return BadRequest(new { message = "ClassId khÃ´ng há»£p lá»‡" });
            }

            var (access, accessError) = await AuthorizeTeacherClassOperation(username, classId);
            if (accessError != null)
            {
                return accessError;
            }

            if (access == null)
            {
                return StatusCode(403, new { message = "KhÃ´ng Ä‘á»§ quyá»n export Ä‘iá»ƒm" });
            }

            var classInfo = await _context.SchoolClasses.FirstOrDefaultAsync(x => x.Id == classId);
            if (classInfo == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y lá»›p" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
            }

            var semester = await ResolveSemesterByInput(academic.Id, semesterId, semesterType);
            if (semester == null)
            {
                return BadRequest(new { message = "KhÃ´ng tÃ¬m tháº¥y há»c ká»³ phÃ¹ há»£p" });
            }

            var fileBytes = await BuildScoreWorkbook(
                classInfo: classInfo,
                academic: academic,
                semester: semester,
                includeScores: true);

            var fileName = $"Score_{SanitizeFileToken(classInfo.ClassCode)}_{SanitizeFileToken(semester.SemesterName)}.xlsx";
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet("teacher/template")]
        [Authorize(Roles = TeacherRoleCode + "," + AdminRoleCode)]
        public async Task<IActionResult> DownloadTeacherImportTemplate(
            [FromQuery] string? username,
            [FromQuery] long classId,
            [FromQuery] long? academicYearId,
            [FromQuery] long? semesterId,
            [FromQuery] string? semesterType)
        {
            if (classId <= 0)
            {
                return BadRequest(new { message = "ClassId khÃ´ng há»£p lá»‡" });
            }

            var (access, accessError) = await AuthorizeTeacherClassOperation(username, classId);
            if (accessError != null)
            {
                return accessError;
            }

            if (access == null)
            {
                return StatusCode(403, new { message = "KhÃ´ng Ä‘á»§ quyá»n táº£i template" });
            }

            var classInfo = await _context.SchoolClasses.FirstOrDefaultAsync(x => x.Id == classId);
            if (classInfo == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y lá»›p" });
            }

            var (academic, academicError) = await ResolveAcademicContext(academicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
            }

            var semester = await ResolveSemesterByInput(academic.Id, semesterId, semesterType);
            if (semester == null)
            {
                return BadRequest(new { message = "KhÃ´ng tÃ¬m tháº¥y há»c ká»³ phÃ¹ há»£p" });
            }

            var fileBytes = await BuildScoreWorkbook(
                classInfo: classInfo,
                academic: academic,
                semester: semester,
                includeScores: false);

            var fileName = $"ScoreTemplate_{SanitizeFileToken(classInfo.ClassCode)}_{SanitizeFileToken(semester.SemesterName)}.xlsx";
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpPost("teacher/import")]
        [Authorize(Roles = TeacherRoleCode + "," + AdminRoleCode)]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> ImportTeacherScores([FromForm] TeacherScoreImportRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Thiáº¿u thÃ´ng tin tÃ i khoáº£n" });
            }

            if (request.ClassId <= 0)
            {
                return BadRequest(new { message = "ClassId khÃ´ng há»£p lá»‡" });
            }

            if (request.File == null || request.File.Length <= 0)
            {
                return BadRequest(new { message = "Vui lÃ²ng chá»n file Excel Ä‘á»ƒ import" });
            }

            var extension = Path.GetExtension(request.File.FileName)?.Trim().ToLowerInvariant();
            if (extension != ".xlsx")
            {
                return BadRequest(new { message = "Chá»‰ há»— trá»£ file .xlsx" });
            }

            var (access, accessError) = await AuthorizeTeacherClassOperation(request.Username, request.ClassId);
            if (accessError != null)
            {
                return accessError;
            }

            if (access == null)
            {
                return StatusCode(403, new { message = "KhÃ´ng Ä‘á»§ quyá»n import Ä‘iá»ƒm" });
            }

            var classInfo = await _context.SchoolClasses.FirstOrDefaultAsync(x => x.Id == request.ClassId);
            if (classInfo == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y lá»›p" });
            }

            var (academic, academicError) = await ResolveAcademicContext(request.AcademicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
            }

            var semester = await ResolveSemesterByInput(academic.Id, request.SemesterId, request.SemesterType);
            if (semester == null)
            {
                return BadRequest(new { message = "KhÃ´ng tÃ¬m tháº¥y há»c ká»³ phÃ¹ há»£p" });
            }

            var category = await ResolveImportGradeCategory();
            if (category == null)
            {
                return BadRequest(new { message = "KhÃ´ng tÃ¬m tháº¥y loáº¡i Ä‘iá»ƒm Ä‘á»ƒ import" });
            }

            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                return BadRequest(new { message = "File Excel khÃ´ng cÃ³ worksheet" });
            }

            var headerMap = ReadHeaderMap(worksheet);
            var studentCodeCol = FindColumn(headerMap, "student_code", "studentcode", "ma_hoc_sinh", "mahocsinh");
            var classCodeCol = FindColumn(headerMap, "class_code", "classcode", "ma_lop", "malop");
            var semesterNameCol = FindColumn(headerMap, "semester_name", "semestername", "hoc_ky", "hocky");
            var academicYearCol = FindColumn(headerMap, "academic_year", "academicyear", "nam_hoc", "namhoc");
            var conductCol = FindColumn(headerMap, "conduct", "hanh_kiem", "hanhkiem");
            var academicPerformanceCol = FindColumn(headerMap, "academic_performance", "academicperformance", "hoc_luc", "hocluc");
            var averageScoreCol = FindColumn(headerMap, "average_score", "averagescore", "diem_tb", "diemtb");
            var gradeCategoryCol = FindColumn(headerMap, "grade_category", "gradecategory", "grade_category_code", "category_code", "category");

            if (!studentCodeCol.HasValue)
            {
                return BadRequest(new { message = "Template khÃ´ng há»£p lá»‡: thiáº¿u cá»™t student_code" });
            }

            var scoreColumns = ExtractScoreColumns(headerMap);
            if (scoreColumns.Count == 0)
            {
                return BadRequest(new { message = "Template khÃ´ng há»£p lá»‡: khÃ´ng tÃ¬m tháº¥y cá»™t score_<subject_code>" });
            }

            var subjectCodes = scoreColumns.Select(x => x.SubjectCode.ToUpperInvariant()).Distinct().ToList();
            var subjects = await _context.Subjects
                .Where(x => subjectCodes.Contains(x.SubjectCode.ToUpper()))
                .Select(x => new { x.Id, x.SubjectCode })
                .ToListAsync();

            var subjectByCode = subjects
                .GroupBy(x => x.SubjectCode.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var missingSubjectCodes = subjectCodes.Where(x => !subjectByCode.ContainsKey(x)).ToList();
            if (missingSubjectCodes.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Template cÃ³ mÃ´n há»c khÃ´ng há»£p lá»‡",
                    subjectCodes = missingSubjectCodes
                });
            }

            var studentIdsInClass = await ResolveStudentIdsInClass(classInfo.Id, academic.Id);
            var students = await _context.Users
                .Where(x => studentIdsInClass.Contains(x.Id) && x.Status == "ACTIVE" && x.StudentCode != null)
                .Select(x => new
                {
                    x.Id,
                    StudentCode = x.StudentCode!,
                    x.FullName
                })
                .ToListAsync();

            var studentByCode = students
                .GroupBy(x => x.StudentCode.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var subjectIds = subjectByCode.Values.Select(x => x.Id).Distinct().ToList();
            var existingGrades = await _context.StudentGrades
                .Where(x =>
                    studentIdsInClass.Contains(x.StudentUserId) &&
                    x.AcademicYearId == academic.Id &&
                    x.SemesterId == semester.Id &&
                    x.CategoryId == category.Id &&
                    subjectIds.Contains(x.SubjectId))
                .ToListAsync();

            var gradeByKey = existingGrades
                .GroupBy(x => $"{x.StudentUserId}|{x.SubjectId}")
                .ToDictionary(g => g.Key, g => g.First());

            var summaryByStudent = await _context.StudentGradeSummaries
                .Where(x =>
                    studentIdsInClass.Contains(x.StudentUserId) &&
                    x.AcademicYearId == academic.Id &&
                    x.SemesterId == semester.Id)
                .ToDictionaryAsync(x => x.StudentUserId, x => x);

            var errors = new List<ScoreImportErrorItem>();
            int totalRows = 0;
            int successRows = 0;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var rowHasData = RowHasData(worksheet, row, headerMap.Values);
                var studentCodeRaw = GetCellText(worksheet, row, studentCodeCol.Value);
                if (!rowHasData && string.IsNullOrWhiteSpace(studentCodeRaw))
                {
                    continue;
                }

                totalRows++;
                var rowErrors = new List<string>();
                var studentCode = studentCodeRaw.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(studentCode))
                {
                    rowErrors.Add("Thiáº¿u student_code");
                }

                if (!studentByCode.TryGetValue(studentCode, out var student))
                {
                    rowErrors.Add("student_code khÃ´ng tá»“n táº¡i hoáº·c khÃ´ng thuá»™c lá»›p Ä‘Æ°á»£c phÃ©p");
                }

                if (classCodeCol.HasValue)
                {
                    var classCodeInFile = GetCellText(worksheet, row, classCodeCol.Value);
                    if (!string.IsNullOrWhiteSpace(classCodeInFile) &&
                        !string.Equals(classCodeInFile.Trim(), classInfo.ClassCode, StringComparison.OrdinalIgnoreCase))
                    {
                        rowErrors.Add("class_code khÃ´ng khá»›p vá»›i lá»›p import");
                    }
                }

                if (semesterNameCol.HasValue)
                {
                    var semesterNameInFile = GetCellText(worksheet, row, semesterNameCol.Value);
                    if (!string.IsNullOrWhiteSpace(semesterNameInFile) &&
                        !string.Equals(semesterNameInFile.Trim(), semester.SemesterName, StringComparison.OrdinalIgnoreCase))
                    {
                        rowErrors.Add("semester_name khÃ´ng khá»›p há»c ká»³ import");
                    }
                }

                if (academicYearCol.HasValue)
                {
                    var yearNameInFile = GetCellText(worksheet, row, academicYearCol.Value);
                    if (!string.IsNullOrWhiteSpace(yearNameInFile) &&
                        !string.Equals(yearNameInFile.Trim(), academic.YearName, StringComparison.OrdinalIgnoreCase))
                    {
                        rowErrors.Add("academic_year khÃ´ng khá»›p nÄƒm há»c import");
                    }
                }

                if (gradeCategoryCol.HasValue)
                {
                    var categoryCodeInFile = GetCellText(worksheet, row, gradeCategoryCol.Value);
                    if (!string.IsNullOrWhiteSpace(categoryCodeInFile) &&
                        !string.Equals(categoryCodeInFile.Trim(), category.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        rowErrors.Add($"grade_category khÃ´ng khá»›p ({category.Code})");
                    }
                }

                var scoreBySubject = new Dictionary<long, decimal?>();
                foreach (var scoreCol in scoreColumns)
                {
                    var subject = subjectByCode[scoreCol.SubjectCode.ToUpperInvariant()];
                    var parseResult = TryParseNullableScore(worksheet.Cell(row, scoreCol.ColumnIndex));
                    if (!parseResult.IsValid)
                    {
                        rowErrors.Add($"Cot {scoreCol.HeaderText}: {parseResult.Error}");
                        continue;
                    }

                    if (parseResult.Value.HasValue && (parseResult.Value.Value < 0 || parseResult.Value.Value > 10))
                    {
                        rowErrors.Add($"Cá»™t {scoreCol.HeaderText}: Ä‘iá»ƒm pháº£i trong khoáº£ng 0-10");
                        continue;
                    }

                    scoreBySubject[subject.Id] = parseResult.Value;
                }

                var conduct = conductCol.HasValue ? GetCellText(worksheet, row, conductCol.Value) : string.Empty;
                var academicPerformance = academicPerformanceCol.HasValue ? GetCellText(worksheet, row, academicPerformanceCol.Value) : string.Empty;

                decimal? averageScore = null;
                if (averageScoreCol.HasValue)
                {
                    var avgResult = TryParseNullableScore(worksheet.Cell(row, averageScoreCol.Value));
                    if (!avgResult.IsValid)
                    {
                        rowErrors.Add($"Cot average_score: {avgResult.Error}");
                    }
                    else if (avgResult.Value.HasValue && (avgResult.Value.Value < 0 || avgResult.Value.Value > 10))
                    {
                        rowErrors.Add("Cá»™t average_score: Ä‘iá»ƒm pháº£i trong khoáº£ng 0-10");
                    }
                    else
                    {
                        averageScore = avgResult.Value;
                    }
                }

                if (rowErrors.Count > 0)
                {
                    errors.Add(new ScoreImportErrorItem
                    {
                        RowNumber = row,
                        StudentCode = studentCodeRaw,
                        Message = string.Join("; ", rowErrors)
                    });
                    continue;
                }

                if (student == null)
                {
                    errors.Add(new ScoreImportErrorItem
                    {
                        RowNumber = row,
                        StudentCode = studentCodeRaw,
                        Message = "KhÃ´ng tÃ¬m tháº¥y há»c sinh"
                    });
                    continue;
                }

                foreach (var scoreItem in scoreBySubject)
                {
                    var key = $"{student.Id}|{scoreItem.Key}";
                    if (scoreItem.Value.HasValue && gradeByKey.TryGetValue(key, out var grade))
                    {
                        grade.Score = scoreItem.Value;
                        grade.Note = "Imported from Excel";
                    }
                    else if (scoreItem.Value.HasValue)
                    {
                        var entity = new StudentGrade
                        {
                            StudentUserId = student.Id,
                            SubjectId = scoreItem.Key,
                            SemesterId = semester.Id,
                            AcademicYearId = academic.Id,
                            CategoryId = category.Id,
                            GradeType = "MAIN",
                            Score = scoreItem.Value,
                            Note = "Imported from Excel",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.StudentGrades.Add(entity);
                        gradeByKey[key] = entity;
                    }
                }

                if (!averageScore.HasValue)
                {
                    var values = scoreBySubject.Values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                    if (values.Count > 0)
                    {
                        averageScore = values.Average();
                    }
                }

                var conductValue = string.IsNullOrWhiteSpace(conduct) ? null : conduct.Trim();
                var academicPerformanceValue = string.IsNullOrWhiteSpace(academicPerformance) ? null : academicPerformance.Trim();

                if (summaryByStudent.TryGetValue(student.Id, out var summary))
                {
                    summary.AverageScore = averageScore;
                    summary.Conduct = conductValue;
                    summary.AcademicPerformance = academicPerformanceValue;
                    summary.Note = "Imported from Excel";
                }
                else if (averageScore.HasValue || !string.IsNullOrWhiteSpace(conductValue) || !string.IsNullOrWhiteSpace(academicPerformanceValue))
                {
                    var summaryEntity = new StudentGradeSummary
                    {
                        StudentUserId = student.Id,
                        SemesterId = semester.Id,
                        AcademicYearId = academic.Id,
                        AverageScore = averageScore,
                        Conduct = conductValue,
                        AcademicPerformance = academicPerformanceValue,
                        Note = "Imported from Excel"
                    };
                    _context.StudentGradeSummaries.Add(summaryEntity);
                    summaryByStudent[student.Id] = summaryEntity;
                }

                successRows++;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Import Ä‘iá»ƒm hoÃ n táº¥t",
                classId = classInfo.Id,
                classCode = classInfo.ClassCode,
                semesterId = semester.Id,
                semesterName = semester.SemesterName,
                totalRows,
                successRows,
                failedRows = errors.Count,
                errors
            });
        }

        [HttpPost("teacher/grades")]
        [Authorize(Roles = AdminRoleCode)]
        public async Task<IActionResult> CreateTeacherGrade([FromBody] TeacherCreateGradeRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Thiáº¿u thÃ´ng tin tÃ i khoáº£n" });
            }

            if (request.StudentId <= 0 || request.SemesterId <= 0 || request.SubjectId <= 0 || request.CategoryId <= 0)
            {
                return BadRequest(new { message = "Thiáº¿u thÃ´ng tin há»c sinh hoáº·c bá»™ lá»c Ä‘iá»ƒm" });
            }

            if (request.Score < 0 || request.Score > 10)
            {
                return BadRequest(new { message = "Äiá»ƒm pháº£i trong khoáº£ng 0 Ä‘áº¿n 10" });
            }

            var (userContext, userError) = await ResolveUserContext(request.Username);
            if (userError != null)
            {
                return userError;
            }

            if (userContext == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n" });
            }

            if (!userContext.IsAdmin)
            {
                return StatusCode(403, new
                {
                    message = "GiÃ¡o viÃªn chá»‰ Ä‘Æ°á»£c cáº­p nháº­t Ä‘iá»ƒm qua import Excel. Endpoint nÃ y chá»‰ dÃ nh cho admin."
                });
            }

            var normalizedGradeType = NormalizeGradeTypeForDb(request.GradeType);
            if (string.IsNullOrWhiteSpace(normalizedGradeType))
            {
                return BadRequest(new { message = "Loáº¡i Ä‘iá»ƒm khÃ´ng há»£p lá»‡" });
            }

            var (academic, academicError) = await ResolveAcademicContext(request.AcademicYearId);
            if (academicError != null)
            {
                return academicError;
            }

            if (academic == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" });
            }

            var studentExists = await _context.Users
                .AnyAsync(x => x.Id == request.StudentId && x.Status == "ACTIVE");
            if (!studentExists)
            {
                return BadRequest(new { message = "Há»c sinh khÃ´ng há»£p lá»‡" });
            }

            var semesterExists = await _context.Semesters
                .AnyAsync(x => x.Id == request.SemesterId && x.AcademicYearId == academic.Id);
            if (!semesterExists)
            {
                return BadRequest(new { message = "Ká»³ há»c khÃ´ng há»£p lá»‡" });
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(x => x.Id == request.SubjectId);
            if (!subjectExists)
            {
                return BadRequest(new { message = "MÃ´n há»c khÃ´ng há»£p lá»‡" });
            }

            var categoryExists = await _context.GradeCategories
                .AnyAsync(x => x.Id == request.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new { message = "Cá»™t Ä‘iá»ƒm khÃ´ng há»£p lá»‡" });
            }

            var existed = await _context.StudentGrades
                .FirstOrDefaultAsync(x =>
                    x.StudentUserId == request.StudentId &&
                    x.AcademicYearId == academic.Id &&
                    x.SemesterId == request.SemesterId &&
                    x.SubjectId == request.SubjectId &&
                    x.CategoryId == request.CategoryId &&
                    x.GradeType == normalizedGradeType);

            if (existed != null)
            {
                return Conflict(new
                {
                    message = "Äiá»ƒm Ä‘Ã£ tá»“n táº¡i, vui lÃ²ng dÃ¹ng chá»©c nÄƒng sá»­a Ä‘iá»ƒm",
                    gradeId = existed.Id
                });
            }

            var grade = new StudentGrade
            {
                StudentUserId = request.StudentId,
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
                message = "ThÃªm Ä‘iá»ƒm thÃ nh cÃ´ng",
                gradeId = grade.Id
            });
        }

        [HttpPut("teacher/grades/{gradeId:long}")]
        [Authorize(Roles = AdminRoleCode)]
        public async Task<IActionResult> UpdateTeacherGrade(
            [FromRoute] long gradeId,
            [FromBody] TeacherUpdateGradeRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Thiáº¿u thÃ´ng tin tÃ i khoáº£n" });
            }

            if (request.Score < 0 || request.Score > 10)
            {
                return BadRequest(new { message = "Äiá»ƒm pháº£i trong khoáº£ng 0 Ä‘áº¿n 10" });
            }

            var (userContext, userError) = await ResolveUserContext(request.Username);
            if (userError != null)
            {
                return userError;
            }

            if (userContext == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n" });
            }

            if (!userContext.IsAdmin)
            {
                return StatusCode(403, new
                {
                    message = "GiÃ¡o viÃªn chá»‰ Ä‘Æ°á»£c cáº­p nháº­t Ä‘iá»ƒm qua import Excel. Endpoint nÃ y chá»‰ dÃ nh cho admin."
                });
            }

            var grade = await _context.StudentGrades.FirstOrDefaultAsync(x => x.Id == gradeId);
            if (grade == null)
            {
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y báº£n ghi Ä‘iá»ƒm" });
            }

            grade.Score = request.Score;
            grade.Note = request.Note;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cáº­p nháº­t Ä‘iá»ƒm thÃ nh cÃ´ng" });
        }

        private async Task<(UserScoreContext? Context, IActionResult? Error)> ResolveUserContext(string? requestedUsername)
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

            var normalizedRoles = User.GetRoleCodes();
            if (!normalizedRoles.Any())
            {
                var roleCodes = await (
                    from ur in _context.UserRoles
                    join role in _context.Roles on ur.RoleId equals role.Id
                    where ur.UserId == user.Id
                    select role.Code
                ).ToListAsync();

                normalizedRoles = roleCodes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToUpperInvariant())
                    .ToHashSet();
            }

            return (new UserScoreContext
            {
                UserId = user.Id,
                Username = user.Username,
                IsTeacher = normalizedRoles.Contains(TeacherRoleCode),
                IsAdmin = normalizedRoles.Contains(AdminRoleCode),
                IsStudent = normalizedRoles.Contains(StudentRoleCode),
                IsParent = normalizedRoles.Contains("PARENT")
            }, null);
        }

        private async Task<(ScoreClassAccessContext? Access, IActionResult? Error)> AuthorizeTeacherClassOperation(
            string? username,
            long classId)
        {
            var (userContext, userError) = await ResolveUserContext(username);
            if (userError != null)
            {
                return (null, userError);
            }

            if (userContext == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy tài khoản" }));
            }

            if (!userContext.IsTeacher && !userContext.IsAdmin)
            {
                return (null, StatusCode(403, new { message = "Tài khoản không có quyền thao tác điểm" }));
            }

            if (userContext.IsAdmin)
            {
                return (new ScoreClassAccessContext
                {
                    UserId = userContext.UserId,
                    ClassId = classId,
                    IsAdmin = true,
                    IsTeacher = userContext.IsTeacher
                }, null);
            }

            var isHomeroomClass = await _context.SchoolClasses.AnyAsync(x =>
                x.Id == classId && x.HomeroomTeacherUserId == userContext.UserId);

            var isTimetableClass = await _context.Timetables.AnyAsync(x =>
                x.ClassId == classId && x.TeacherUserId == userContext.UserId);

            var isTeacherContactClass = await _context.TeacherContacts.AnyAsync(x =>
                x.ClassId == classId && x.TeacherUserId == userContext.UserId);

            if (!isHomeroomClass && !isTimetableClass && !isTeacherContactClass)
            {
                return (null, StatusCode(403, new { message = "Giáo viên không được thao tác lớp này" }));
            }

            return (new ScoreClassAccessContext
            {
                UserId = userContext.UserId,
                ClassId = classId,
                IsTeacher = true,
                IsAdmin = false
            }, null);
        }

        private async Task<Semester?> ResolveSemesterByInput(
            long academicYearId,
            long? semesterId,
            string? semesterType)
        {
            var semesters = await _context.Semesters
                .Where(x => x.AcademicYearId == academicYearId)
                .OrderBy(x => x.StartDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (semesters.Count == 0)
            {
                return null;
            }

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                return semesters.FirstOrDefault(x => x.Id == semesterId.Value);
            }

            if (string.IsNullOrWhiteSpace(semesterType))
            {
                return semesters.FirstOrDefault(x => NormalizeSearchToken(x.SemesterName).Contains("KY1"))
                    ?? semesters.FirstOrDefault();
            }

            var token = NormalizeSearchToken(semesterType);
            if (token.Contains("KY1") || token == "1" || token.Contains("SEMESTER1"))
            {
                return semesters.FirstOrDefault(x => NormalizeSearchToken(x.SemesterName).Contains("1"))
                    ?? semesters.FirstOrDefault();
            }

            if (token.Contains("KY2") || token == "2" || token.Contains("SEMESTER2"))
            {
                return semesters.FirstOrDefault(x => NormalizeSearchToken(x.SemesterName).Contains("2"))
                    ?? semesters.LastOrDefault();
            }

            if (token.Contains("CANAM") || token.Contains("FULLYEAR") || token.Contains("YEAR"))
            {
                return semesters.FirstOrDefault(x =>
                        NormalizeSearchToken(x.SemesterName).Contains("CANAM") ||
                        NormalizeSearchToken(x.SemesterName).Contains("YEAR"))
                    ?? semesters.LastOrDefault();
            }

            return semesters.FirstOrDefault();
        }

        private async Task<GradeCategory?> ResolveImportGradeCategory()
        {
            var categories = await _context.GradeCategories
                .Select(x => new GradeCategory
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name
                })
                .ToListAsync();

            if (categories.Count == 0)
            {
                return null;
            }

            foreach (var code in new[] { "AVG", "FINAL", "MIDTERM", "REGULAR" })
            {
                var matched = categories.FirstOrDefault(x =>
                    string.Equals(x.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));

                if (matched != null)
                {
                    return matched;
                }
            }

            return categories.FirstOrDefault();
        }

        private async Task<byte[]> BuildScoreWorkbook(
            SchoolClass classInfo,
            TeacherAcademicContext academic,
            Semester semester,
            bool includeScores)
        {
            var studentIds = await ResolveStudentIdsInClass(classInfo.Id, academic.Id);

            var students = await _context.Users
                .Where(x => studentIds.Contains(x.Id) && x.Status == "ACTIVE")
                .OrderBy(x => x.StudentCode)
                .ThenBy(x => x.FullName)
                .Select(x => new
                {
                    x.Id,
                    StudentCode = x.StudentCode ?? string.Empty,
                    x.FullName
                })
                .ToListAsync();

            var subjectIds = await _context.Timetables
                .Where(x =>
                    x.ClassId == classInfo.Id &&
                    x.AcademicYearId == academic.Id &&
                    x.SemesterId == semester.Id)
                .Select(x => x.SubjectId)
                .Distinct()
                .ToListAsync();

            if (!subjectIds.Any())
            {
                subjectIds = await _context.StudentGrades
                    .Where(x =>
                        studentIds.Contains(x.StudentUserId) &&
                        x.AcademicYearId == academic.Id &&
                        x.SemesterId == semester.Id)
                    .Select(x => x.SubjectId)
                    .Distinct()
                    .ToListAsync();
            }

            if (!subjectIds.Any())
            {
                subjectIds = await _context.Subjects
                    .OrderBy(x => x.SubjectCode)
                    .Select(x => x.Id)
                    .ToListAsync();
            }

            var subjects = await _context.Subjects
                .Where(x => subjectIds.Contains(x.Id))
                .OrderBy(x => x.SubjectCode)
                .Select(x => new
                {
                    x.Id,
                    x.SubjectCode,
                    x.SubjectName
                })
                .ToListAsync();

            var importCategory = await ResolveImportGradeCategory();

            var gradeByKey = new Dictionary<string, decimal?>();
            var summaryByStudent = new Dictionary<long, StudentGradeSummary>();

            if (includeScores && students.Count > 0 && subjects.Count > 0 && importCategory != null)
            {
                var subjectIdSet = subjects.Select(x => x.Id).ToList();

                var grades = await _context.StudentGrades
                    .Where(x =>
                        studentIds.Contains(x.StudentUserId) &&
                        x.AcademicYearId == academic.Id &&
                        x.SemesterId == semester.Id &&
                        x.CategoryId == importCategory.Id &&
                        subjectIdSet.Contains(x.SubjectId))
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.StudentUserId,
                        x.SubjectId,
                        x.Score
                    })
                    .ToListAsync();

                gradeByKey = grades
                    .GroupBy(x => $"{x.StudentUserId}|{x.SubjectId}")
                    .ToDictionary(g => g.Key, g => g.First().Score);

                summaryByStudent = await _context.StudentGradeSummaries
                    .Where(x =>
                        studentIds.Contains(x.StudentUserId) &&
                        x.AcademicYearId == academic.Id &&
                        x.SemesterId == semester.Id)
                    .ToDictionaryAsync(x => x.StudentUserId, x => x);
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Scores");

            var headers = new List<string>
            {
                "student_code",
                "full_name",
                "class_code",
                "class_name",
                "academic_year",
                "semester_name"
            };

            headers.AddRange(subjects.Select(x => $"score_{x.SubjectCode}"));
            headers.Add("average_score");
            headers.Add("conduct");
            headers.Add("academic_performance");
            headers.Add("grade_category");
            headers.Add("note");

            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.SheetView.FreezeRows(1);

            int dataRow = 2;
            foreach (var student in students)
            {
                int col = 1;
                worksheet.Cell(dataRow, col++).Value = student.StudentCode;
                worksheet.Cell(dataRow, col++).Value = student.FullName;
                worksheet.Cell(dataRow, col++).Value = classInfo.ClassCode;
                worksheet.Cell(dataRow, col++).Value = classInfo.ClassName;
                worksheet.Cell(dataRow, col++).Value = academic.YearName;
                worksheet.Cell(dataRow, col++).Value = semester.SemesterName;

                var currentScores = new List<decimal>();
                foreach (var subject in subjects)
                {
                    decimal? value = null;
                    if (includeScores && importCategory != null &&
                        gradeByKey.TryGetValue($"{student.Id}|{subject.Id}", out var scoreValue))
                    {
                        value = scoreValue;
                    }

                    if (value.HasValue)
                    {
                        worksheet.Cell(dataRow, col).Value = value.Value;
                        worksheet.Cell(dataRow, col).Style.NumberFormat.Format = "0.00";
                        currentScores.Add(value.Value);
                    }

                    col++;
                }

                StudentGradeSummary? summary = null;
                if (summaryByStudent.TryGetValue(student.Id, out var foundSummary))
                {
                    summary = foundSummary;
                }

                var average = summary?.AverageScore;
                if (!average.HasValue && currentScores.Count > 0)
                {
                    average = currentScores.Average();
                }

                if (average.HasValue)
                {
                    worksheet.Cell(dataRow, col).Value = Math.Round(average.Value, 2);
                    worksheet.Cell(dataRow, col).Style.NumberFormat.Format = "0.00";
                }
                col++;

                worksheet.Cell(dataRow, col++).Value = summary?.Conduct ?? string.Empty;
                worksheet.Cell(dataRow, col++).Value = summary?.AcademicPerformance ?? string.Empty;
                worksheet.Cell(dataRow, col++).Value = importCategory?.Code ?? string.Empty;
                worksheet.Cell(dataRow, col++).Value = summary?.Note ?? string.Empty;

                dataRow++;
            }

            worksheet.Columns().AdjustToContents();

            var subjectSheet = workbook.Worksheets.Add("Subjects");
            subjectSheet.Cell(1, 1).Value = "subject_code";
            subjectSheet.Cell(1, 2).Value = "subject_name";
            subjectSheet.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < subjects.Count; i++)
            {
                subjectSheet.Cell(i + 2, 1).Value = subjects[i].SubjectCode;
                subjectSheet.Cell(i + 2, 2).Value = subjects[i].SubjectName;
            }
            subjectSheet.Columns().AdjustToContents();

            var guideSheet = workbook.Worksheets.Add("HÆ°á»›ng dáº«n");
            guideSheet.Cell(1, 1).Value = "HÆ°á»›ng dáº«n import Ä‘iá»ƒm";
            guideSheet.Cell(1, 1).Style.Font.Bold = true;
            guideSheet.Cell(3, 1).Value = "1. KhÃ´ng Ä‘á»•i tÃªn cá»™t á»Ÿ sheet Scores.";
            guideSheet.Cell(4, 1).Value = "2. Cá»™t score_<subject_code> nháº­p sá»‘ tá»« 0 Ä‘áº¿n 10, Ä‘á»ƒ trá»‘ng náº¿u khÃ´ng cáº­p nháº­t.";
            guideSheet.Cell(5, 1).Value = "3. student_code pháº£i thuá»™c lá»›p Ä‘Æ°á»£c phÃ¢n cÃ´ng.";
            guideSheet.Cell(6, 1).Value = "4. grade_category hiá»‡n táº¡i: " + (importCategory?.Code ?? "N/A");
            guideSheet.Cell(7, 1).Value = "5. Import lÃ  kÃªnh cáº­p nháº­t Ä‘iá»ƒm chÃ­nh thá»©c cho giÃ¡o viÃªn.";
            guideSheet.Columns().AdjustToContents();

            using var memory = new MemoryStream();
            workbook.SaveAs(memory);
            return memory.ToArray();
        }

        private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet worksheet)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int col = 1; col <= lastColumn; col++)
            {
                var rawHeader = worksheet.Cell(1, col).GetValue<string>();
                var normalized = NormalizeHeader(rawHeader);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (!map.ContainsKey(normalized))
                {
                    map[normalized] = col;
                }
            }

            return map;
        }

        private static int? FindColumn(IReadOnlyDictionary<string, int> headerMap, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                var key = NormalizeHeader(alias);
                if (headerMap.TryGetValue(key, out var col))
                {
                    return col;
                }
            }

            return null;
        }

        private static List<ScoreColumnInfo> ExtractScoreColumns(IReadOnlyDictionary<string, int> headerMap)
        {
            var list = new List<ScoreColumnInfo>();

            foreach (var item in headerMap)
            {
                if (!item.Key.StartsWith("score_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var subjectCode = item.Key.Substring("score_".Length).Trim('_').ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(subjectCode))
                {
                    continue;
                }

                list.Add(new ScoreColumnInfo
                {
                    ColumnIndex = item.Value,
                    HeaderText = item.Key,
                    SubjectCode = subjectCode
                });
            }

            return list.OrderBy(x => x.ColumnIndex).ToList();
        }

        private static bool RowHasData(IXLWorksheet worksheet, int row, IEnumerable<int> columns)
        {
            foreach (var col in columns.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(GetCellText(worksheet, row, col)))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetCellText(IXLWorksheet worksheet, int row, int column)
        {
            return worksheet.Cell(row, column).GetValue<string>().Trim();
        }

        private static ScoreParseResult TryParseNullableScore(IXLCell cell)
        {
            if (cell.TryGetValue<decimal>(out var numericValue))
            {
                return ScoreParseResult.Valid(numericValue);
            }

            var raw = cell.GetValue<string>().Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ScoreParseResult.Valid(null);
            }

            var compact = raw.Replace(" ", string.Empty);

            if (decimal.TryParse(compact, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var valueInvariant))
            {
                return ScoreParseResult.Valid(valueInvariant);
            }

            var vi = CultureInfo.GetCultureInfo("vi-VN");
            if (decimal.TryParse(compact, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign,
                    vi, out var valueVi))
            {
                return ScoreParseResult.Valid(valueVi);
            }

            if (decimal.TryParse(compact.Replace(",", "."), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out var normalized))
            {
                return ScoreParseResult.Valid(normalized);
            }

            return ScoreParseResult.Invalid("giÃ¡ trá»‹ khÃ´ng pháº£i sá»‘ há»£p lá»‡");
        }

        private static string SanitizeFileToken(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "Unknown";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(raw.Trim().Length);

            foreach (var ch in raw.Trim())
            {
                if (invalidChars.Contains(ch))
                {
                    sb.Append('_');
                    continue;
                }

                sb.Append(char.IsWhiteSpace(ch) ? '_' : ch);
            }

            var output = sb.ToString();
            while (output.Contains("__", StringComparison.Ordinal))
            {
                output = output.Replace("__", "_", StringComparison.Ordinal);
            }

            output = output.Trim('_');
            return string.IsNullOrWhiteSpace(output) ? "Unknown" : output;
        }

        private static string NormalizeHeader(string value)
        {
            var token = NormalizeSearchToken(value).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            if (token.StartsWith("score", StringComparison.Ordinal) && token.Length > "score".Length)
            {
                return $"score_{token.Substring("score".Length)}";
            }

            return token;
        }

        private static string NormalizeSearchToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var noAccent = RemoveDiacritics(value).ToUpperInvariant();
            var sb = new StringBuilder(noAccent.Length);
            foreach (var ch in noAccent)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private async Task<List<long>> ResolveStudentIdsInClass(long classId, long academicYearId)
        {
            var classStudentIds = await _context.ClassStudents
                .Where(x =>
                    x.ClassId == classId &&
                    x.AcademicYearId == academicYearId &&
                    !x.LeftAt.HasValue)
                .Select(x => x.StudentUserId)
                .Distinct()
                .ToListAsync();

            if (classStudentIds.Any())
            {
                return classStudentIds;
            }

            return await (
                from user in _context.Users
                join ur in _context.UserRoles on user.Id equals ur.UserId
                join role in _context.Roles on ur.RoleId equals role.Id
                where user.CurrentClassId == classId
                      && user.Status == "ACTIVE"
                      && role.Code == "STUDENT"
                select user.Id
            ).Distinct().ToListAsync();
        }

        private async Task<bool> IsStudentInClass(long studentUserId, long classId, long academicYearId)
        {
            var byClassStudent = await _context.ClassStudents.AnyAsync(x =>
                x.StudentUserId == studentUserId &&
                x.ClassId == classId &&
                x.AcademicYearId == academicYearId &&
                !x.LeftAt.HasValue);

            if (byClassStudent)
            {
                return true;
            }

            return await _context.Users.AnyAsync(x =>
                x.Id == studentUserId &&
                x.CurrentClassId == classId &&
                x.Status == "ACTIVE");
        }

        private async Task<Semester?> ResolveSemesterByLegacyNo(long academicYearId, int semesterNo)
        {
            var semesters = await _context.Semesters
                .Where(x => x.AcademicYearId == academicYearId)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (semesters.Count == 0)
            {
                return null;
            }

            Semester? semester = semesterNo switch
            {
                1 => semesters.FirstOrDefault(x => x.SemesterName.Contains("1")),
                2 => semesters.FirstOrDefault(x => x.SemesterName.Contains("2")),
                _ => semesters.FirstOrDefault(x =>
                        x.SemesterName.Contains("Cáº£ nÄƒm", StringComparison.OrdinalIgnoreCase) ||
                        x.SemesterName.Contains("Year", StringComparison.OrdinalIgnoreCase))
            };

            if (semester == null && semesterNo > 2)
            {
                semester = semesters.LastOrDefault();
            }

            return semester;
        }

        private static string GetLegacySemesterLabel(int semesterNo)
        {
            return semesterNo switch
            {
                1 => "Ká»³ 1",
                2 => "Ká»³ 2",
                _ => "Cáº£ nÄƒm"
            };
        }

        private async Task<(long? StudentUserId, IActionResult? Error)> ResolveStudentUserIdForScoreView(string? requestedUsername)
        {
            var (userContext, userError) = await ResolveUserContext(requestedUsername);
            if (userError != null)
            {
                return (null, userError);
            }

            if (userContext == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy tài khoản" }));
            }

            if (userContext.IsStudent)
            {
                return (userContext.UserId, null);
            }

            if (userContext.IsParent)
            {
                var studentUserId = await (
                    from rel in _context.ParentStudentRelationships
                    where rel.ParentUserId == userContext.UserId
                    orderby rel.StudentUserId
                    select (long?)rel.StudentUserId
                ).FirstOrDefaultAsync();

                if (!studentUserId.HasValue)
                {
                    return (null, BadRequest(new { message = "Không tìm thấy học sinh thuộc quyền phụ huynh" }));
                }

                return (studentUserId.Value, null);
            }

            return (null, StatusCode(403, new { message = "Tài khoản không có quyền xem điểm" }));
        }

        private async Task<(TeacherScope? Scope, IActionResult? Error)> ResolveTeacherScope(string? username)
        {
            var (userContext, userError) = await ResolveUserContext(username);
            if (userError != null)
            {
                return (null, userError);
            }

            if (userContext == null)
            {
                return (null, NotFound(new { message = "Không tìm thấy tài khoản" }));
            }

            if (!userContext.IsTeacher && !userContext.IsAdmin)
            {
                return (null, StatusCode(403, new { message = "Tài khoản không có quyền xem bảng điểm giáo viên" }));
            }

            var schoolClass = await _context.SchoolClasses
                .Where(x => x.HomeroomTeacherUserId == userContext.UserId)
                .OrderBy(x => x.ClassCode)
                .FirstOrDefaultAsync();

            if (schoolClass == null)
            {
                schoolClass = await (
                    from tt in _context.Timetables
                    join c in _context.SchoolClasses on tt.ClassId equals c.Id
                    where tt.TeacherUserId == userContext.UserId
                    orderby c.ClassCode
                    select c
                ).FirstOrDefaultAsync();
            }

            if (schoolClass == null)
            {
                schoolClass = await (
                    from tc in _context.TeacherContacts
                    join c in _context.SchoolClasses on tc.ClassId equals c.Id
                    where tc.TeacherUserId == userContext.UserId
                    orderby c.ClassCode
                    select c
                ).FirstOrDefaultAsync();
            }

            if (schoolClass == null)
            {
                return (null, NotFound(new { message = "Giáo viên chưa được phân công lớp" }));
            }

            return (new TeacherScope
            {
                TeacherUserId = userContext.UserId,
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
                    return (null, NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c theo bá»™ lá»c" }));
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
                    return (null, NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y nÄƒm há»c" }));
                }
            }

            return (new TeacherAcademicContext
            {
                Id = academicYear.Id,
                YearName = string.IsNullOrWhiteSpace(academicYear.YearName)
                    ? $"NÄƒm há»c {academicYear.Id}"
                    : academicYear.YearName
            }, null);
        }

        private static string NormalizeGradeTypeForDb(string? gradeType)
        {
            var input = (gradeType ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(input))
            {
                return "MAIN";
            }

            return input switch
            {
                "FPT" => "FPT",
                "MAIN" => "MAIN",
                "REGULAR" => "MAIN",
                "MIDTERM" => "MAIN",
                "FINAL" => "MAIN",
                _ => string.Empty
            };
        }

        private static string ToClientGradeType(string? dbGradeType, string? categoryCode)
        {
            var db = (dbGradeType ?? string.Empty).Trim().ToUpperInvariant();
            if (db == "FPT")
            {
                return "FINAL";
            }

            var category = (categoryCode ?? string.Empty).Trim().ToUpperInvariant();
            if (category.Contains("FINAL") || category.Contains("CK"))
            {
                return "FINAL";
            }

            if (category.Contains("MID") || category.Contains("GK"))
            {
                return "MIDTERM";
            }

            return "REGULAR";
        }
    }

    internal sealed class TeacherScope
    {
        public long TeacherUserId { get; set; }
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
        public string? Username { get; set; }
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
        public string? Username { get; set; }
        public decimal Score { get; set; }
        public string? Note { get; set; }
    }

    public sealed class TeacherScoreImportRequest
    {
        public string? Username { get; set; }
        public long ClassId { get; set; }
        public long? AcademicYearId { get; set; }
        public long? SemesterId { get; set; }
        public string? SemesterType { get; set; }
        public IFormFile? File { get; set; }
    }

    public sealed class ScoreImportErrorItem
    {
        public int RowNumber { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class UserScoreContext
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsTeacher { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsStudent { get; set; }
        public bool IsParent { get; set; }
    }

    internal sealed class ScoreClassAccessContext
    {
        public long UserId { get; set; }
        public long ClassId { get; set; }
        public bool IsTeacher { get; set; }
        public bool IsAdmin { get; set; }
    }

    internal sealed class ScoreColumnInfo
    {
        public int ColumnIndex { get; set; }
        public string HeaderText { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
    }

    internal sealed class ScoreParseResult
    {
        public bool IsValid { get; private set; }
        public decimal? Value { get; private set; }
        public string Error { get; private set; } = string.Empty;

        public static ScoreParseResult Valid(decimal? value)
        {
            return new ScoreParseResult
            {
                IsValid = true,
                Value = value,
                Error = string.Empty
            };
        }

        public static ScoreParseResult Invalid(string error)
        {
            return new ScoreParseResult
            {
                IsValid = false,
                Value = null,
                Error = string.IsNullOrWhiteSpace(error) ? "dá»¯ liá»‡u khÃ´ng há»£p lá»‡" : error
            };
        }
    }
}


