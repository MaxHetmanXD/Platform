using System;

namespace Platform.Models.ViewModels
{
    public class CourseCardViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string OwnerNickname { get; set; }
        public int StudentsCount { get; set; }
        public Guid? BannerId { get; set; }
    }
}