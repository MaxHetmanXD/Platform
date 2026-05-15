using System;
using Platform.Enums;

namespace Platform.Models.ViewModels
{
    public class CourseManagementViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Pass { get; set; }
        public bool IsPublic { get; set; }
        public CourseCategory Category { get; set; }
        public Guid? BannerId { get; set; }
    }
}