using System;
using System.Collections.Generic;

namespace Platform.Models.ViewModels
{
    public class TaskGradeViewModel
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public string GradeString { get; set; } = string.Empty;
        public double? GradeValue { get; set; }
    }

    public class StudentGradesViewModel
    {
        public Guid StudentId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public Guid? AvatarId { get; set; }
        public double AverageGrade { get; set; }
        public List<TaskGradeViewModel> Tasks { get; set; } = new();
    }

    public class CourseGradesViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public bool IsOwnerOrAdmin { get; set; }

        public List<ParticipantItemViewModel> AllCourseStudents { get; set; } = new();
        public Guid? SelectedStudentId { get; set; }

        public List<StudentGradesViewModel> Students { get; set; } = new();
    }
}