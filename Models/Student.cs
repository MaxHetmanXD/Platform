using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Enums;
using Platform.DTOs;

namespace Platform.Models
{
    public class Student : User
    {
        public string Group { get; private set; }

        public List<Course> EnrolledCourses { get; set; } = new List<Course>();

        public event EventHandler<StudentResponse>? OnTaskSubmitted;

        public Student(string login, string password, string nickname, string email, string group)
            : base(login, password, nickname, email, UserRole.Student)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException("Група є обов'язковою для студента.");
            }
            Group = group;
        }

        public bool RequestEnrollment(Course course, string? password = null)
        {
            bool success = course.ProcessEnrollmentRequest(this, password);
            return success;
        }

        public List<Lesson> GetAvailableContent(Course course)
        {
            if (!course.Students.Contains(this)) return new List<Lesson>();

            return course.Lessons.Where(l => l.AllowedStudents.Contains(this)).ToList();
        }

        public StudentResponse SubmitTask(Task task, List<FileModel> files)
        {
            if (!task.IsVisible)
            {
                throw new InvalidOperationException("Це завдання наразі недоступне.");
            }

            var response = new StudentResponse(this, task);

            response.AttachedFiles = files ?? new List<FileModel>();

            response.Submit();
            response.RefreshStatus();

            OnTaskSubmitted?.Invoke(this, response);

            return response;
        }

        public double GetLessonAverage(Lesson lesson)
        {
            var gradedResponses = lesson.Tasks
                .SelectMany(t => t.Responses)
                .Where(r => r.Author.Id == this.Id && r.FinalGrade != null)
                .ToList();

            if (!gradedResponses.Any()) return 0;

            return gradedResponses.Average(r => r.FinalGrade!.Value);
        }

        public double GetCourseAverage(Course course)
        {
            var lessonAverages = course.Lessons
                .Select(l => GetLessonAverage(l))
                .Where(avg => avg > 0)
                .ToList();

            if (!lessonAverages.Any()) return 0;

            return lessonAverages.Average();
        }

        public List<TaskStatusDTO> GetTaskAnalytics(Course course)
        {
            var analytics = new List<TaskStatusDTO>();

            var allTasks = course.Lessons.SelectMany(l => l.Tasks);

            foreach (var task in allTasks)
            {
                var response = task.Responses.FirstOrDefault(r => r.Author.Id == this.Id);

                string statusText = "Ще не здано";
                if (response != null)
                {
                    if (response.FinalGrade != null)
                        statusText = $"Оцінено ({response.FinalGrade.Value})";
                    else
                        statusText = "На перевірці";
                }

                analytics.Add(new TaskStatusDTO
                {
                    TaskTitle = task.Title,
                    Status = statusText
                });
            }

            return analytics;
        }

        public void Unenroll(Course course)
        {
            if (EnrolledCourses.Contains(course))
            {
                EnrolledCourses.Remove(course);
                course.ExcludeStudent(this);
            }
        }
    }
}