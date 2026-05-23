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
        public string Position { get; set; }

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

        public void EditCourse(Course course, string title, string desc, FileModel? banner, CourseCategory category, string? pass)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));

            if (course.OwnerId != this.Id)
            {
                throw new UnauthorizedAccessException("Ви не є власником цього курсу.");
            }

            course.UpdateCourseInfo(title, desc, banner, category, pass);
        }

        public void AcceptStudent(Student student, Course course)
        {
            course.ActivateStudent(student);
            if (!student.EnrolledCourses.Contains(course)) student.EnrolledCourses.Add(course);
        }

        public void BanStudent(Student student, Course course)
        {
            course.AddToBanList(student);
            student.Unenroll(course);
        }

        public void RemoveStudent(Student student, Course course)
        {
            course.ExcludeStudent(student);
            student.Unenroll(course);
        }

        public Lesson CreateLesson(Course course, string title, string text)
        {
            if (course.OwnerId != this.Id) throw new UnauthorizedAccessException("Ви не власник цього курсу.");
            var lesson = new Lesson { Title = title, TheoryContent = text, Course = course, IsPublic = true };

            course.AddLesson(lesson);
            return lesson;
        }

        public void EditLesson(Lesson lesson, string title, string text, bool isPublic, List<Student> allowedStudents)
        {
            lesson.UpdateContent(title, text, lesson.Attachments);
            lesson.IsPublic = isPublic;
            lesson.SetAccessibility(allowedStudents);
        }

        public void DeleteLesson(Course course, Lesson lesson)
        {
            if (course.OwnerId != this.Id) throw new UnauthorizedAccessException();

            course.RemoveLesson(lesson);
        }
        public Task CreateTask(Lesson lesson, string title, string theory, int maxPoints, DateTime deadline, List<FileModel> files)
        {
            var task = new Task();
            task.UpdateTaskInfo(title, theory, maxPoints, deadline, files);

            lesson.AddTask(task);
            return task;
        }

        public void EditTask(Task task, string title, string theory, int maxPoints, DateTime? deadline, List<FileModel> files)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            task.UpdateTaskInfo(title, theory, maxPoints, deadline, files);
        }

        public void DeleteTask(Lesson lesson, Task task)
        {
            lesson.RemoveTask(task);
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

        public void DeleteCourse(Course course)
        {
            if (course.OwnerId != this.Id)
            {
                throw new UnauthorizedAccessException("Ви не є власником цього курсу.");
            }
            OwnCourses.Remove(course);
        }
        public void UnbanStudent(Student student, Course course)
        {
            if (course.OwnerId != this.Id)
            {
                throw new UnauthorizedAccessException("Ви не є власником цього курсу.");
            }
            course.RemoveFromBanList(student);
        }

        public void RejectStudent(Student student, Course course)
        {
            if (course.OwnerId != this.Id)
            {
                throw new UnauthorizedAccessException("Ви не є власником цього курсу.");
            }
            course.RemoveFromPending(student);
        }
    }
}