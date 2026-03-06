using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MyFSchool.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly MyFSchoolDbContext _context;

        public TestController(MyFSchoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("db-check")]
        public async Task<IActionResult> CheckDatabase()
        {
            var canConnect = await _context.Database.CanConnectAsync();

            return Ok(new
            {
                success = canConnect,
                message = canConnect ? "Kết nối database thành công" : "Không kết nối được database"
            });
        }
    }
}