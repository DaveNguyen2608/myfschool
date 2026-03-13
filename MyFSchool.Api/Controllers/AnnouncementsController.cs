using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Data;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly MyFSchoolDbContext _context;

        public AnnouncementsController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            var today = DateTime.Today;

            var data = await _context.Announcementss
                .Where(x => x.IsActive &&
                       (x.StartDate == null || x.StartDate <= today) &&
                       (x.EndDate == null || x.EndDate >= today))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    description = x.Description,
                    imageUrl = x.ImageUrl,
                    startDate = x.StartDate,
                    endDate = x.EndDate
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}