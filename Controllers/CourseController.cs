using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models;
using Platform.Models.ViewModels;
using Platform.Enums;
using Platform.Services;
using System.Security.Claims;

namespace Platform.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class CourseController : Controller
    {
        private readonly PlatformDbContext _context;
        private readonly FileManager _fileManager;

        public CourseController(PlatformDbContext context, FileManager fileManager)
        {
            _context = context;
            _fileManager = fileManager;
        }

        [HttpPost]
        public async Task<IActionResult> AutoCreate()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var teacher = await _context.Users.OfType<Teacher>().FirstOrDefaultAsync(t => t.Id == userId);
            if (teacher == null) return Forbid();

            string randomId = Guid.NewGuid().ToString().Substring(0, 4);
            var newCourse = teacher.CreateCourse($"Новий курс {randomId}", "Додайте опис вашого курсу тут...", CourseCategory.Programming, null);

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            return RedirectToAction("Manage", new { id = newCourse.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Manage(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses.Include(c => c.Banner).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var vm = new CourseManagementViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Pass = course.Pass,
                IsPublic = course.IsPublic,
                Category = course.Category,
                BannerId = course.Banner?.Id
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Manage(CourseManagementViewModel model, IFormFile? bannerFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses.Include(c => c.Banner).FirstOrDefaultAsync(c => c.Id == model.Id);
            if (course == null) return NotFound();

            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            FileModel? newBanner = course.Banner;
            if (bannerFile != null && currentUser != null)
            {
                newBanner = await _fileManager.SaveFileAsync(bannerFile, currentUser);
                if (newBanner != null) _context.Files.Add(newBanner);
            }

            if (isAdmin)
            {
                var admin = (Admin)currentUser!;
                admin.EditCourse(course, model.Title, model.Description, newBanner);
            }
            else
            {
                var teacher = (Teacher)currentUser!;
                teacher.EditCourse(course, model.Title, model.Description, newBanner);
            }

            course.Category = model.Category;
            course.Pass = model.Pass;

            course.IsPublic = string.IsNullOrWhiteSpace(model.Pass);

            await _context.SaveChangesAsync();
            TempData["Message"] = "Налаштування курсу успішно збережено!";

            return RedirectToAction("Manage", new { id = model.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (isAdmin) ((Admin)currentUser!).DeleteCourse(course);
            else ((Teacher)currentUser!).DeleteCourse(course);

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return RedirectToAction(isAdmin ? "Courses" : "MyCourses", isAdmin ? "Admin" : "Home");
        }
    }
}