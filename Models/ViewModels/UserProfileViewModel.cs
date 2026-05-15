using System;
using Platform.Enums;

namespace Platform.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public Guid? AvatarId { get; set; }

        public UserRole Role { get; set; }

        public string? SpecialFieldLabel { get; set; }
        public string? SpecialFieldValue { get; set; }

        public bool IsOwnProfile { get; set; }
        public bool IsAdmin { get; set; }

        public string? CurrentPassword { get; set; }
    }
}