using System;
using Platform.Enums;

namespace Platform.Models.ViewModels
{
    public class UserListItemViewModel
    {
        public Guid Id { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string SpecialField { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
    }
}