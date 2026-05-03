using Platform.DTOs;
using Platform.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Platform.Models
{
    public class Teacher : User
    {
        public string Position { get; private set; }

        public List<Course> OwnCourses { get; set; } = new List<Course>();
        protected Teacher() { }

        public Teacher(string login, string password, string nickname, string email, string position)
            : base(login, password, nickname, email, UserRole.Teacher)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                throw new ArgumentException("Посада є обов'язковою для вчителя.");
            }
            Position = position;
        }

        public Course CreateCourse(string title, string desc, CourseCategory cat, string? pass)
        {
            var course = new Course
            {
                Title = title,
                Description = desc,
                Category = cat,
                Pass = pass,
                IsPublic = string.IsNullOrWhiteSpace(pass),
                OwnerId = this.Id
            };

            OwnCourses.Add(course);
            return course;
        }

        public void EditCourse(Course course, string title, string desc, FileModel? banner)
        {
            if (course.OwnerId != this.Id)
            {
                throw new UnauthorizedAccessException("Ви не є власником цього курсу.");
            }

            course.Title = title;
            course.Description = desc;
            if (banner != null)
            {
                course.Banner = banner;
            }
        }

        public void AcceptStudent(Student student, Course course)
        {
            if (course.PendingStudents.Contains(student))
            {
                course.PendingStudents.Remove(student);
                course.Students.Add(student);
                student.EnrolledCourses.Add(course);
            }
        }

        public void BanStudent(Student student, Course course)
        {
            course.Students.Remove(student);
            course.PendingStudents.Remove(student);

            if (!course.BannedStudents.Contains(student))
            {
                course.BannedStudents.Add(student);
            }

            student.EnrolledCourses.Remove(course);
        }

        public void RemoveStudent(Student student, Course course)
        {
            course.Students.Remove(student);
            student.EnrolledCourses.Remove(course);
        }

        public Lesson CreateLesson(Course course, string title, string theory, List<FileModel> files)
        {
            var lesson = new Lesson
            {
                Title = title,
                TheoryContent = theory,
                Attachments = files ?? new List<FileModel>()
            };
            course.Lessons.Add(lesson);
            return lesson;
        }

        public void EditLesson(Lesson lesson, string theory, List<FileModel> files)
        {
            lesson.TheoryContent = theory;
            lesson.Attachments = files ?? new List<FileModel>();
        }

        public void DeleteLesson(Course course, Lesson lesson)
        {
            course.Lessons.Remove(lesson);
        }

        public Task CreateTask(Lesson lesson, string title, string theory, int maxPoints, DateTime deadline, List<FileModel> files)
        {
            var task = new Task
            {
                Title = title,
                TheoryContent = theory,
                MaxPoints = maxPoints,
                Deadline = deadline,
                Attachments = files ?? new List<FileModel>()
            };
            lesson.Tasks.Add(task);
            return task;
        }

        public void EditTask(Task task, string title, string theory, int maxPoints, DateTime deadline, List<FileModel> files)
        {
            task.Title = title;
            task.TheoryContent = theory;
            task.MaxPoints = maxPoints;
            task.Deadline = deadline;
            task.Attachments = files ?? new List<FileModel>();
        }

        public void DeleteTask(Lesson lesson, Task task)
        {
            lesson.Tasks.Remove(task);
        }

        public Grade GradeSubmission(StudentResponse resp, int score, string feedback)
        {
            var grade = new Grade(score, feedback, resp);
            resp.ApplyGrade(grade);

            return grade;
        }

        public List<TaskStatusDTO> GetStudentDetails(Student student, Course course)
        {
            return student.GetTaskAnalytics(course);
        }
    }
}