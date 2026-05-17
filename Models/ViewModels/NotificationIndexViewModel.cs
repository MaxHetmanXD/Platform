using System;
using System.Collections.Generic;

namespace Platform.Models.ViewModels
{
    public class NotificationItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class NotificationGroupViewModel
    {
        public DateTime Date { get; set; }
        public List<NotificationItemViewModel> Notifications { get; set; } = new();
    }

    public class NotificationIndexViewModel
    {
        public List<NotificationGroupViewModel> GroupedNotifications { get; set; } = new();
        public string? SearchString { get; set; }
    }
}