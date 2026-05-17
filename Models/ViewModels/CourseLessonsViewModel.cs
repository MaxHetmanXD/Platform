using System;
using System.Collections.Generic;

namespace Platform.Models.ViewModels
{
    public class TaskItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public string GradeString { get; set; } = string.Empty;
        public int MaxPoints { get; set; }
    }

    public class FileItemViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ReadableSize { get; set; } = string.Empty;
    }

    public class LessonItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public double? AverageGrade { get; set; }
        public List<FileItemViewModel> Files { get; set; } = new();
        public List<TaskItemViewModel> Tasks { get; set; } = new();
    }

    public class CourseLessonsViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public bool IsOwnerOrAdmin { get; set; }
        public List<LessonItemViewModel> Lessons { get; set; } = new();
    }
}