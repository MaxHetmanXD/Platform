using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models;
using Platform.Models.ViewModels;
using System.Linq;
using System.Security.Claims;
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
            var courses = await _context.Courses
                .Include(c => c.Owner)
                .Include(c => c.Banner)
                .Include(c => c.Students)
                .ToListAsync();

            if (!User.Identity.IsAuthenticated)
            {
                var guest = new Guest();
                courses = guest.BrowseCourses(courses, null);
            }
            else
            {
                courses = courses.Where(c => c.IsPublic == true).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                courses = courses
                    .Where(c => c.MatchSearch(searchString))
                    .OrderByDescending(c => c.GetSearchWeight(searchString))
                    .ToList();
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var viewModels = courses.Select(c => {
                var vm = new CourseCardViewModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    CategoryName = c.Category.ToString(),
                    OwnerNickname = c.Owner?.Nickname ?? "Невідомий",
                    StudentsCount = c.Students.Count,
                    BannerId = c.Banner?.Id,
                    IsTeacherView = (userRole == "Teacher")
                };

                if (userRole == "Student")
                {
                    var student = c.Students.FirstOrDefault(s => s.Id == userId);
                    vm.AverageGrade = student != null ? ((Student)student).GetCourseAverage(c) : 0;
                }
                else if (userRole == "Teacher")
                {
                    vm.AverageGrade = c.GetGlobalAverage();
                }

                return vm;
            }).ToList();

            if (string.IsNullOrWhiteSpace(searchString))
            {
                viewModels = sortOrder switch
                {
                    "popularity" => viewModels.OrderByDescending(c => c.StudentsCount).ToList(),
                    "category" => viewModels.OrderBy(c => c.CategoryName).ToList(),
                    "grade" => viewModels.OrderByDescending(c => c.AverageGrade).ToList(),
                    _ => viewModels.OrderBy(c => c.Title).ToList()
                };
            }

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            return View(viewModels);
        }

        [Authorize(Roles = "Student,Teacher")]
        [HttpGet]
        public async Task<IActionResult> MyCourses(string searchString, string sortOrder)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId)) return RedirectToAction("Login", "Account");

            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Courses
                .Include(c => c.Banner)
                .Include(c => c.Owner)
                .Include(c => c.Students)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Responses)
                            .ThenInclude(r => r.Author)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Tasks)
                        .ThenInclude(t => t.Responses)
                            .ThenInclude(r => r.FinalGrade)
                .AsQueryable();

            if (userRole == "Student")
            {
                query = query.Where(c => c.Students.Any(s => s.Id == userId));
            }
            else if (userRole == "Teacher")
            {
                query = query.Where(c => c.OwnerId == userId);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(c => c.Title.Contains(searchString));
            }

            var courses = await query.ToListAsync();

            var viewModels = courses.Select(c => {
                var vm = new CourseCardViewModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    CategoryName = c.Category.ToString(),
                    OwnerNickname = c.Owner?.Nickname ?? "Невідомий",
                    StudentsCount = c.Students.Count,
                    BannerId = c.Banner?.Id,
                    IsTeacherView = (userRole == "Teacher")
                };

                if (userRole == "Student")
                {
                    var student = c.Students.FirstOrDefault(s => s.Id == userId);
                    vm.AverageGrade = student != null ? student.GetCourseAverage(c) : 0;
                }
                else if (userRole == "Teacher")
                {
                    vm.AverageGrade = c.GetGlobalAverage();
                }

                return vm;
            }).ToList();

            viewModels = sortOrder switch
            {
                "popularity" => viewModels.OrderByDescending(c => c.StudentsCount).ToList(),
                "category" => viewModels.OrderBy(c => c.CategoryName).ToList(),
                "grade" => viewModels.OrderByDescending(c => c.AverageGrade).ToList(),
                _ => viewModels.OrderBy(c => c.Title).ToList()
            };

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            return View(viewModels);
        }
    }
}