using Platform.Enums;
using Platform.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Models
{
    public class Task : IAttachable
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid LessonId { get; set; }

        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TheoryContent { get; set; } = string.Empty;

        public int MaxPoints { get; set; }
        public DateTime? Deadline { get; set; }

        public bool IsVisible { get; set; } = true;

        public List<FileModel> Attachments { get; set; } = new List<FileModel>();
        public bool HasAttachments => Attachments.Any();

        public List<Student> AllowedStudents { get; set; } = new List<Student>();
        public List<StudentResponse> Responses { get; set; } = new List<StudentResponse>();

        public void UpdateTaskInfo(string title, string theory, int maxPoints, DateTime? deadline, List<FileModel> newMaterials)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Заголовок (Title) не може бути порожнім.");
            }

            if (maxPoints < 0 || maxPoints > 100)
            {
                throw new ArgumentException("MaxPoints має бути в межах 0..100.");
            }

            if (deadline.HasValue && deadline.Value < DateTime.Now)
            {
                throw new ArgumentException("Дедлайн не може бути встановлений у минулому часі.");
            }

            Title = title;
            TheoryContent = theory ?? string.Empty;
            MaxPoints = maxPoints;
            Deadline = deadline;
            Attachments = newMaterials ?? new List<FileModel>();
        }

        public void SetAccessibility(List<Student> students)
        {
            AllowedStudents.Clear();
            if (students != null)
            {
                AllowedStudents.AddRange(students);
            }
        }

        public bool CheckSubmissionEligibility(Student student)
        {
            return AllowedStudents.Contains(student);
        }

        public string GetTaskStatus(Student student)
        {
            var response = Responses.FirstOrDefault(r => r.Author.Id == student.Id);

            if (response != null)
            {
                if (response.Status == SubmissionStatus.Checked) return "Оцінено";
                if (response.Status == SubmissionStatus.Pending) return "На перевірці";
            }

            if (IsOverdue())
            {
                return "Прострочено";
            }

            return "Не здано";
        }

        public Grade? GetStudentGrade(Student student)
        {
            var response = Responses.FirstOrDefault(r => r.Author.Id == student.Id);
            return response?.FinalGrade;
        }

        public bool IsOverdue()
        {
            if (!Deadline.HasValue) return false;
            return DateTime.Now > Deadline.Value;
        }

        public void AttachFile(FileModel file)
        {
            if (IsAttachmentAllowed() && file != null)
            {
                Attachments.Add(file);
            }
        }

        public bool RemoveFile(FileModel file)
        {
            return Attachments.Remove(file);
        }

        public List<FileModel> GetAttachments()
        {
            return Attachments;
        }

        public bool IsAttachmentAllowed()
        {
            return true;
        }
    }
}