using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models;
using Platform.Models.ViewModels;
using Platform.Services;
using System.Security.Claims;

namespace Platform.Controllers
{
    public class AccountController : Controller
    {
        private readonly PlatformDbContext _context;

        public AccountController(PlatformDbContext context, FileManager fileManager)
        {
            _context = context;
            _fileManager = fileManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == model.Login);

                if (user != null && user.Authenticate(model.Login, model.Password))
                {
                    _context.SaveChanges();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Nickname),
                        new Claim(ClaimTypes.Role, user.Role.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Такого користувача не існує або пароль невірний.");
            }

            return View(model);
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Profile(Guid? id)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(currentUserIdStr, out Guid currentUserId)) return RedirectToAction("Login");

            var isAdmin = User.IsInRole("Admin");

            var targetId = id ?? currentUserId;

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetId);
            if (targetUser == null) return NotFound("Користувача не знайдено.");

            var isOwnProfile = (currentUserId == targetId);

            var vm = new UserProfileViewModel
            {
                Id = targetUser.Id,
                Login = targetUser.Login,
                Nickname = targetUser.Nickname,
                Email = targetUser.Email,
                Info = targetUser.Info,
                LastLogin = targetUser.LastLogin,
                AvatarId = targetUser.AvatarId,
                Role = targetUser.Role,
                IsOwnProfile = isOwnProfile,
                IsAdmin = isAdmin
            };

            if (targetUser is Teacher t)
            {
                vm.SpecialFieldLabel = "Посада";
                vm.SpecialFieldValue = t.Position;
            }
            else if (targetUser is Student s)
            {
                vm.SpecialFieldLabel = "Група";
                vm.SpecialFieldValue = s.Group;
            }

            if (isAdmin) vm.CurrentPassword = targetUser.Password;

            return View(vm);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(Guid userId, string oldPassword, string newPassword)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(currentUserIdStr, out Guid currentUserId);
            var isAdmin = User.IsInRole("Admin");

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (targetUser == null) return NotFound();

            bool success = false;

            if (isAdmin && currentUserId != userId)
            {
                targetUser.Password = newPassword;
                success = true;
            }

            else
            {
                success = targetUser.ChangePassword(oldPassword, newPassword);
            }

            if (success)
            {
                await _context.SaveChangesAsync();
                TempData["Message"] = "Пароль успішно змінено!";
            }
            else
            {
                TempData["Error"] = "Не вдалося змінити пароль. Перевірте старий пароль або довжину нового.";
            }

            return RedirectToAction("Profile", new { id = userId });
        }

        private readonly FileManager _fileManager;
        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> EditProfile(Guid userId, string nickname, string email, string info, IFormFile? avatarFile, string? login, string? specialValue)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(currentUserIdStr, out Guid currentUserId);
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != userId && !isAdmin) return Forbid();

            var targetUser = await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == userId);
            if (targetUser != null)
            {
                FileModel? newAvatar = targetUser.Avatar;

                if (avatarFile != null)
                {
                    newAvatar = await _fileManager.SaveFileAsync(avatarFile, targetUser);

                    if (newAvatar != null)
                    {
                        _context.Files.Add(newAvatar);
                    }
                }

                try
                {
                    targetUser.UpdateProfile(nickname, email, newAvatar);
                    targetUser.Info = info ?? string.Empty;

                    if (isAdmin)
                    {
                        if (!string.IsNullOrWhiteSpace(login)) targetUser.Login = login;

                        if (targetUser is Teacher t && !string.IsNullOrWhiteSpace(specialValue)) t.Position = specialValue;
                        if (targetUser is Student s && !string.IsNullOrWhiteSpace(specialValue)) s.Group = specialValue;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Профіль успішно оновлено!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Помилка збереження: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            return RedirectToAction("Profile", new { id = userId });
        }
    }
}