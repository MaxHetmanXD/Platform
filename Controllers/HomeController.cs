using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Controllers
{
    public class HomeController : Controller
    {
        private readonly PlatformDbContext _context;

        public HomeController(PlatformDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            var query = from c in _context.Courses
                        where c.IsPublic == true
                        join u in _context.Users on c.OwnerId equals u.Id
                        select new CourseCardViewModel
                        {
                            Id = c.Id,
                            Title = c.Title,
                            CategoryName = c.Category.ToString(),
                            StudentsCount = c.Students.Count,
                            OwnerNickname = u.Nickname,
                            BannerId = c.Banner != null ? c.Banner.Id : null
                        };

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(c => c.Title.Contains(searchString));
            }

            query = sortOrder switch
            {
                "popularity" => query.OrderByDescending(c => c.StudentsCount),
                "category" => query.OrderBy(c => c.CategoryName),
                _ => query.OrderBy(c => c.Title)
            };

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            var courses = await query.ToListAsync();
            return View(courses);
        }
    }
}