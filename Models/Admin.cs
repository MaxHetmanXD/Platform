using System;
using System.Collections.Generic;
using Platform.Enums;

namespace Platform.Models
{
    public class Admin : User
    {
        public bool AccessLevel { get; set; } = true;

        public Admin(string login, string password, string nickname, string email)
            : base(login, password, nickname, email, UserRole.Admin)
        {
        }

        public User CreateUser(string login, string password, string nickname, string email, UserRole role)
        {
            if (role == UserRole.Student)
            {
                return new Student(login, password, email, nickname, "Нерозподілені");
            }
            if (role == UserRole.Teacher)
            {
                return new Teacher(login, password, email, nickname, "Нерозподілені");
            }

            return new User(login, password, nickname, email, role);
        }

        public void EditUserFields(User target, string newLogin, string newPassword, string newNickname, string newEmail, FileModel? newAvatar, string newInfo)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (target.Role == UserRole.Admin && target.Id != this.Id)
            {
                throw new UnauthorizedAccessException("Адміністратор не може редагувати дані іншого адміністратора.");
            }

            if (!string.IsNullOrWhiteSpace(newLogin))
            {
                target.Login = newLogin;
            }

            if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length >= 8)
            {
                target.Password = newPassword;
            }

            if (!string.IsNullOrWhiteSpace(newInfo))
            {
                target.Info = newInfo;
            }

            target.UpdateProfile(
                string.IsNullOrWhiteSpace(newNickname) ? target.Nickname : newNickname,
                string.IsNullOrWhiteSpace(newEmail) ? target.Email : newEmail,
                newAvatar ?? target.Avatar
            );
        }

        public bool BlockUser(User target)
        {
            if (target == null) return false;

            if (target.Role == UserRole.Admin && target.Id != this.Id)
            {
                throw new UnauthorizedAccessException("Адміністратор не може заблокувати іншого адміністратора.");
            }

            target.IsActive = false;
            return true;
        }

        public bool UnblockUser(User target)
        {
            if (target == null) return false;

            target.IsActive = true;
            return true;
        }

        public void EditCourse(Course course, string title, string desc, FileModel? banner, CourseCategory category, string? pass)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));

            string finalTitle = string.IsNullOrWhiteSpace(title) ? course.Title : title;
            string finalDesc = string.IsNullOrWhiteSpace(desc) ? course.Description : desc;

            course.UpdateCourseInfo(finalTitle, finalDesc, banner, category, pass);
        }

        public void AcceptStudent(Student student, Course course)
        {
            if (course == null || student == null) throw new ArgumentNullException("Студент або курс не існують.");
            course.ActivateStudent(student);
        }

        public void BanStudent(Student student, Course course)
        {
            if (course == null || student == null) throw new ArgumentNullException("Студент або курс не існують.");
            course.AddToBanList(student);
        }

        public void RemoveStudent(Student student, Course course)
        {
            if (course == null || student == null) throw new ArgumentNullException("Студент або курс не існують.");
            course.ExcludeStudent(student);
        }

        public Lesson CreateLesson(Course course, string title, string text)
        {
            var lesson = new Lesson { Title = title, TheoryContent = text, Course = course, IsPublic = true };
            course.Lessons.Add(lesson);
            return lesson;
        }

        public void EditLesson(Lesson lesson, string title, string text, bool isPublic, List<Student> allowedStudents)
        {
            lesson.Title = title;
            lesson.TheoryContent = text;
            lesson.IsPublic = isPublic;

            lesson.AllowedStudents.Clear();
            if (!isPublic && allowedStudents != null)
            {
                lesson.AllowedStudents.AddRange(allowedStudents);
            }
        }

        public void DeleteLesson(Course course, Lesson lesson)
        {
            course.Lessons.Remove(lesson);
        }

        public Task CreateTask(Lesson lesson, string title, string theory, int maxPoints, DateTime? deadline, List<FileModel> files)
        {
            if (lesson == null) throw new ArgumentNullException(nameof(lesson));
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
            if (lesson == null || task == null) return;
            lesson.RemoveTask(task);
        }

        public void SetAccessibility(object target, List<Student> allowedStudents)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (target is Lesson lesson)
            {
                lesson.SetAccessibility(allowedStudents);
            }
            else if (target is Task task)
            {
                task.SetAccessibility(allowedStudents);
            }
            else
            {
                throw new ArgumentException("Об'єкт доступу має бути Уроком або Завданням.");
            }
        }
        public void DeleteCourse(Course course)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));
        }

        public void UnbanStudent(Student student, Course course)
        {
            if (course == null || student == null) throw new ArgumentNullException();
            course.RemoveFromBanList(student);
        }

        public void RejectStudent(Student student, Course course)
        {
            if (course == null || student == null) throw new ArgumentNullException();
            course.RemoveFromPending(student);
        }
    }
}