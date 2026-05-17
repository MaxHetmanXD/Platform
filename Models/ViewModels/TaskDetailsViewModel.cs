using System;
using System.Collections.Generic;

namespace Platform.Models.ViewModels
{
    public class TaskDetailsViewModel
    {
        // Навігація
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public Guid LessonId { get; set; }
        public Guid TaskId { get; set; }

        // Блок Завдання (Ліворуч)
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskInfo { get; set; } = string.Empty;
        public int MaxPoints { get; set; }
        public DateTime? Deadline { get; set; }
        public bool IsPublic { get; set; }
        public List<FileItemViewModel> TaskFiles { get; set; } = new();

        public bool IsOwnerOrAdmin { get; set; }
        public bool IsStudent { get; set; }

        public Guid? ResponseStudentId { get; set; }
        public string ResponseStudentNickname { get; set; } = string.Empty;
        public string ResponseStatus { get; set; } = string.Empty;
        public DateTime? SubmissionDate { get; set; }
        public double? CurrentGrade { get; set; }
        public List<FileItemViewModel> ResponseFiles { get; set; } = new();

        // Модальне вікно редагування
        public List<ParticipantItemViewModel> AvailableStudentsForTask { get; set; } = new();
        public List<Guid> AllowedStudentIds { get; set; } = new();
    }
}