using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Interfaces;
using Platform.Enums;

namespace Platform.Models
{
    public class Lesson : IAttachable
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;
        public string TheoryContent { get; set; } = string.Empty;

        public List<FileModel> Attachments { get; set; } = new List<FileModel>();

        public bool HasAttachments => Attachments.Any();

        public List<Task> Tasks { get; set; } = new List<Task>();

        public List<Student> AllowedStudents { get; set; } = new List<Student>();

        public void UpdateContent(string newTitle, string newTheory, List<FileModel> newMaterials)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                throw new ArgumentException("Заголовок (Title) обов'язковий.");
            }

            Title = newTitle;
            TheoryContent = newTheory;

            Attachments = newMaterials ?? new List<FileModel>();
        }

        public void SetAccessibility(List<Student> students)
        {
            AllowedStudents = students ?? new List<Student>();
        }

        public void AddTask(Task task)
        {
            if (task != null && !Tasks.Contains(task))
            {
                Tasks.Add(task);
            }
        }

        public void RemoveTask(Task task)
        {
            Tasks.Remove(task);
        }

        public List<Task> GetAccessibleTasks(Student student)
        {
            if (!AllowedStudents.Contains(student))
            {
                return new List<Task>();
            }

            return Tasks.Where(t => t.AllowedStudents.Contains(student)).ToList();
        }

        public double CalculateAverage(Student student)
        {
            var gradedResponses = Tasks
                .SelectMany(t => t.Responses)
                .Where(r => r.Author.Id == student.Id && r.FinalGrade != null)
                .ToList();

            if (!gradedResponses.Any()) return 0;

            return gradedResponses.Average(r => r.FinalGrade!.Value);
        }

        public Dictionary<string, int> GetSubmissionStats()
        {
            var stats = new Dictionary<string, int>
            {
                { "Checked", 0 },
                { "Pending", 0 },
                { "Overdue", 0 }
            };

            var allResponses = Tasks.SelectMany(t => t.Responses);

            foreach (var response in allResponses)
            {
                if (response.Status == SubmissionStatus.Checked) stats["Checked"]++;
                else if (response.Status == SubmissionStatus.Pending) stats["Pending"]++;
                else if (response.Status == SubmissionStatus.Overdue) stats["Overdue"]++;
            }

            return stats;
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