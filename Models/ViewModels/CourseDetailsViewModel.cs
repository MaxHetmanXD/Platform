using System;
using Platform.Enums;

namespace Platform.Models.ViewModels
{
    public class CourseDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? BannerId { get; set; }
        public CourseCategory Category { get; set; }
        public bool IsPublic { get; set; }
        public bool IsGuest { get; set; }
        public bool IsStudent { get; set; }
        public bool IsOwnerOrAdmin { get; set; }
        public bool IsUserBanned { get; set; }
        public bool IsUserPending { get; set; }
        public bool IsUserEnrolled { get; set; }
    }
}