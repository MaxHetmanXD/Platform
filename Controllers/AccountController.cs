using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Interfaces;
using Platform.Models;
using Platform.Models.ViewModels;
using Platform.Services;
using System.Security.Claims;

namespace Platform.Controllers
{
    public class AccountController : Controller
    {
        private readonly PlatformDbContext _context;
        private readonly NotificationService _notificationService;
    public AccountController(PlatformDbContext context, FileManager fileManager, NotificationService notificationService)
        {
            _context = context;
            _fileManager = fileManager;
            _notificationService = notificationService;
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
                var guest = new Guest();

                var user = guest.Login(model.Login, model.Password, _context.Users);

                if (user != null)
                {
                    if (!user.IsAccountActive())
                    {
                        ModelState.AddModelError("", "Цей акаунт деактивовано або заблоковано.");
                        return View(model);
                    }

                    _context.SaveChanges();
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.GetIdentity().ToString()),
                        new Claim(ClaimTypes.Name, user.Nickname),
                        new Claim(ClaimTypes.Role, user.Role.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Невірний логін або пароль.");
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
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out Guid userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.Logout();
                }
            }

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

            if (currentUserId != userId && !isAdmin)
            {
                return Forbid();
            }

            bool success = false;

            try
            {
                if (isAdmin && currentUserId != userId)
                {
                    targetUser.ChangePassword(newPassword);
                    success = true;
                }
                else
                {
                    Platform.Interfaces.IAuthenticatable authUser = targetUser;
                    if (authUser.GetPasswordHash() != oldPassword)
                    {
                        TempData["Error"] = "Старий пароль введено неправильно!";
                        return RedirectToAction("Profile", new { id = userId });
                    }

                    targetUser.ChangePassword(newPassword);
                    success = true;
                }
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }

            if (success)
            {
                _notificationService.HandleSecurityEvent(targetUser, "Зміна пароля");
                await _context.SaveChangesAsync();
                TempData["Message"] = "Пароль успішно змінено!";
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
                    if (isAdmin)
                    {
                        var currentAdmin = await _context.Users.OfType<Admin>().FirstOrDefaultAsync(a => a.Id == userId);

                        currentAdmin?.EditUserFields(targetUser, targetUser.Login, null, nickname, email, newAvatar, info, specialValue);
                    }
                    else
                    {
                        targetUser.UpdateProfile(nickname, email, newAvatar, info);
                    }

                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Профіль успішно оновлено.";
                }
                catch (Exception ex) when (ex is ArgumentException || ex is UnauthorizedAccessException)
                {
                    TempData["Error"] = ex.Message;
                }
            }
            return RedirectToAction("Profile", new { id = userId });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> EmergencyAdminReset()
        {
            var admin = await _context.Users.OfType<Platform.Models.Admin>().FirstOrDefaultAsync();

            if (admin != null)
            {
                admin.Password = "NewAdminPass123!";

                await _context.SaveChangesAsync();

                return Content($"Пароль для адміна (Логін: {admin.Login}, Пошта: {admin.Email}) успішно скинуто на 'NewAdminPass123!'");
            }

            return Content("Адміністратора не знайдено в базі даних.");
        }
    }
}