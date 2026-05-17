using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models.ViewModels;
using Platform.Services;
using System;
using System.Linq;
using System.Security.Claims;

namespace Platform.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly PlatformDbContext _context;
        private readonly NotificationService _notificationService;

        public NotificationController(PlatformDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public IActionResult Index(string? searchString)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var notificationsList = _notificationService.Notifications
                .Where(n => n.Recipient.Id == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string searchLower = searchString.ToLower();
                notificationsList = notificationsList
                    .Where(n => n.Title.ToLower().Contains(searchLower) || n.Message.ToLower().Contains(searchLower))
                    .ToList();
            }

            var groupedData = notificationsList
                .GroupBy(n => n.CreatedAt.Date)
                .Select(g => new NotificationGroupViewModel
                {
                    Date = g.Key,
                    Notifications = g.Select(n => new NotificationItemViewModel
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    }).ToList()
                })
                .OrderByDescending(g => g.Date)
                .ToList();

            var viewModel = new NotificationIndexViewModel
            {
                GroupedNotifications = groupedData,
                SearchString = searchString
            };

            foreach (var item in notificationsList.Where(n => !n.IsRead))
            {
                _notificationService.MarkAsReadAsync(item.Id);
            }

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult ClearAll()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            _notificationService.Notifications.RemoveAll(n => n.Recipient.Id == userId);

            return RedirectToAction("Index");
        }
    }
}