using System;
using System.Collections.Generic;

namespace Platform.Models.ViewModels
{
    public class LessonDetailsViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;

        public Guid LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string LessonInfo { get; set; } = string.Empty;
        public double? AverageGrade { get; set; }

        public bool IsOwnerOrAdmin { get; set; }
        public bool IsPublic { get; set; }

        public List<FileItemViewModel> Files { get; set; } = new();
        public List<TaskItemViewModel> Tasks { get; set; } = new();

        public List<ParticipantItemViewModel> CourseStudents { get; set; } = new();
        public List<Guid> AllowedStudentIds { get; set; } = new();
    }
}