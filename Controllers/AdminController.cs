using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Enums;
using Platform.Models;
using Platform.Models.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Platform.Controllers
{
    // ТІЛЬКИ ДЛЯ АДМІНІВ
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly PlatformDbContext _context;

        public AdminController(PlatformDbContext context)
        {
            _context = context;
        }

        // Перенаправлення на вкладку курсів за замовчуванням
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Courses");
        }

        // ================= 1. ВКЛАДКА КУРСІВ =================
        [HttpGet]
        public async Task<IActionResult> Courses(string searchString, string sortOrder)
        {
            // Беремо ВСІ курси (навіть приватні)
            var query = from c in _context.Courses
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

            return View(await query.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Users(string searchString, string sortOrder)
        {
            var usersQuery = _context.Users.Where(u => u.Role == UserRole.Student || u.Role == UserRole.Teacher);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Nickname.Contains(searchString) ||
                    u.Login.Contains(searchString) ||
                    (u is Student && ((Student)u).Group.Contains(searchString)) ||
                    (u is Teacher && ((Teacher)u).Position.Contains(searchString))
                );
            }

            var users = await usersQuery.ToListAsync();

            var userViewModels = users.Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Nickname = u.Nickname,
                Login = u.Login,
                Role = u.Role,
                LastLogin = u.LastLogin,
                SpecialField = u is Student s ? s.Group : (u is Teacher t ? t.Position : "")
            }).ToList();

            userViewModels = sortOrder switch
            {
                "lastlogin" => userViewModels.OrderByDescending(u => u.LastLogin).ToList(),
                _ => userViewModels.OrderBy(u => u.Nickname).ToList()
            };

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            return View(userViewModels);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string roleType)
        {
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out Guid adminId);
            var currentAdmin = await _context.Users.OfType<Admin>().FirstOrDefaultAsync(a => a.Id == adminId);

            if (currentAdmin == null) return Forbid();

            string randomString = Guid.NewGuid().ToString().Substring(0, 6);
            string tempLogin = $"user_{randomString}";
            string tempEmail = $"{tempLogin}@platform.com";
            string tempPassword = "password123";

            if (!Enum.TryParse(roleType, out UserRole parsedRole))
            {
                return BadRequest("Невідома роль");
            }

            User newUser = currentAdmin.CreateUser(tempLogin, tempPassword, "Новий Користувач", tempEmail, parsedRole);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile", "Account", new { id = newUser.Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out Guid adminId);
            var currentAdmin = await _context.Users.OfType<Admin>().FirstOrDefaultAsync(a => a.Id == adminId);

            var targetUser = await _context.Users.FindAsync(userId);

            if (currentAdmin != null && targetUser != null)
            {
                currentAdmin.DeleteUser(targetUser);

                _context.Users.Remove(targetUser);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(Guid userId, UserRole newRole)
        {
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out Guid adminId);
            var currentAdmin = await _context.Users.OfType<Admin>().FirstOrDefaultAsync(a => a.Id == adminId);

            var targetUser = await _context.Users.FindAsync(userId);

            if (currentAdmin != null && targetUser != null)
            {
                currentAdmin.ChangeRole(targetUser, newRole);

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Users");
        }
    }
}