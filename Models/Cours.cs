using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Enums;
using Platform.Interfaces;
using Platform.DTOs;

namespace Platform.Models
{
    public class Course : ISearchable
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid OwnerId { get; set; }
        public virtual Teacher Owner { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public FileModel? Banner { get; set; }

        public bool IsPublic { get; set; }
        public string? Pass { get; set; }
        public CourseCategory Category { get; set; }
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Student> PendingStudents { get; set; } = new List<Student>();
        public List<Student> BannedStudents { get; set; } = new List<Student>();

        public List<Lesson> Lessons { get; set; } = new List<Lesson>();

        public string SearchKey
        {
            get => Title;
            set => Title = value;
        }

        public List<string> Tags { get; set; } = new List<string>();

        public Course()
        {
            Title = string.Empty;
            Description = string.Empty;
        }

        public void UpdateCourseInfo(string title, string description, FileModel? banner, CourseCategory category, string? pass)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Назва курсу не може бути порожньою.");
            }

            Title = title;
            Description = description ?? string.Empty;

            Category = category;
            Pass = pass;
            IsPublic = string.IsNullOrWhiteSpace(pass);

            if (banner != null)
            {
                Banner = banner;
            }
        }

        public bool VerifyCoursePassword(string inputPassword)
        {
            if (IsPublic) return true;
            return Pass == inputPassword;
        }

        public bool ProcessEnrollmentRequest(Student student, string? password)
        {
            if (BannedStudents.Contains(student)) return false;

            if (BannedStudents.Any(s => s.Id == student.Id))
            {
                throw new InvalidOperationException("Ви заблоковані на цьому курсі і не можете приєднатися.");
            }

            if (!IsPublic && !VerifyCoursePassword(password ?? ""))
            {
                return false;
            }

            AddToPending(student);
            return true;
        }

        public void AddToPending(Student student)
        {
            if (!BannedStudents.Contains(student) && !PendingStudents.Contains(student) && !Students.Contains(student))
            {
                PendingStudents.Add(student);
            }
        }

        public void ActivateStudent(Student student)
        {
            if (PendingStudents.Contains(student))
            {
                PendingStudents.Remove(student);
                Students.Add(student);
            }
        }

        public void ExcludeStudent(Student student)
        {
            if (Students.Contains(student)) Students.Remove(student);
        }

        public void AddToBanList(Student student)
        {
            Students.Remove(student);
            PendingStudents.Remove(student);

            if (!BannedStudents.Contains(student))
            {
                BannedStudents.Add(student);
            }
        }

        public void AddLesson(Lesson lesson)
        {
            if (lesson != null && !Lessons.Contains(lesson))
            {
                Lessons.Add(lesson);
            }
        }

        public void RemoveLesson(Lesson lesson)
        {
            if (Lessons.Contains(lesson)) Lessons.Remove(lesson);
        }

        public List<Lesson> GetFilteredContent(Student student)
        {
            if (!Students.Contains(student))
            {
                return new List<Lesson>();
            }

            return Lessons.Where(l => l.AllowedStudents.Contains(student)).ToList();
        }

        public double GetGlobalAverage()
        {
            if (!Students.Any()) return 0;

            var averages = Students
                .Select(s => s.GetCourseAverage(this))
                .Where(avg => avg > 0)
                .ToList();

            if (!averages.Any()) return 0;

            return averages.Average();
        }

        public bool MatchSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            query = query.ToLower();

            return SearchKey.ToLower().Contains(query) ||
                   Category.ToString().ToLower().Contains(query) ||
                   Tags.Any(t => t.ToLower().Contains(query));
        }

        public SearchItem GetSearchResultData()
        {
            return new SearchItem
            {
                Id = this.Id,
                Title = this.Title,
                ShortDescription = this.Description,
                Type = "Course"
            };
        }

        public int GetSearchWeight(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return 0;
            query = query.ToLower();

            if (SearchKey.ToLower() == query) return 100; // Точний збіг назви
            if (SearchKey.ToLower().Contains(query)) return 75; // Частковий збіг назви
            if (Tags.Any(t => t.ToLower() == query)) return 50; // Збіг за тегом
            if (Category.ToString().ToLower().Contains(query)) return 25; // Збіг за категорією

            return 0;
        }
        public void RemoveFromBanList(Student student)
        {
            if (BannedStudents.Contains(student))  BannedStudents.Remove(student);
        }

        public void RemoveFromPending(Student student)
        {
            if (PendingStudents.Contains(student)) PendingStudents.Remove(student);
        }
    }
}