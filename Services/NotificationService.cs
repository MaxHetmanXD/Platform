using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Models;
using Task = Platform.Models.Task;

namespace Platform.Services
{
    public class NotificationService
    {
        public string SmtpConfig { get; set; } = string.Empty;
        public List<string> EventLog { get; set; } = new List<string>();

        public void Send(User recipient, string message)
        {
            if (recipient == null || string.IsNullOrWhiteSpace(message)) return;

            string formattedMessage = $"[{DateTime.Now:g}] Для {recipient.Login}: {message}";

            EventLog.Add(formattedMessage);

            Console.WriteLine(formattedMessage);
        }

        public void HandleGradeEvent(Grade grade, Student student)
        {
            if (grade == null || student == null) return;

            string taskName = grade.Response?.TargetTask?.Title ?? "Невідоме завдання";

            string message = $"Ви отримали {grade.Value} за завдання '{taskName}'.";
            Send(student, message);
        }

        public void HandleSecurityEvent(User user, string action)
        {
            string message = $"Критичне сповіщення: {action}. Якщо це робили не ви, негайно зверніться до підтримки.";
            Send(user, message);
        }

        public void NotifyTeacherOfSubmission(Student student, Task task, Course course, PlatformData platform)
        {
            var teacher = platform.AllUsers.FirstOrDefault(u => u.Id == course.OwnerId);

            if (teacher != null)
            {
                string message = $"Студент {student.Nickname} здав роботу до завдання '{task.Title}'.";
                Send(teacher, message);
            }
        }

        public void NotifyNewContent(Course course, object content)
        {
            string contentTitle = string.Empty;

            if (content is Lesson lesson)
            {
                contentTitle = lesson.Title;
            }
            else if (content is Task task)
            {
                contentTitle = task.Title;
            }
            else
            {
                return;
            }

            string message = $"У курсі '{course.Title}' опубліковано новий матеріал: '{contentTitle}'.";

            foreach (var student in course.Students)
            {
                Send(student, message);
            }
        }

        public void NotifyEnrollmentChange(Student student, Course course, string actionType)
        {
            string message = string.Empty;

            switch (actionType.ToLower())
            {
                case "accepted":
                    message = $"Вітаємо! Вас зараховано на курс '{course.Title}'.";
                    break;
                case "banned":
                    message = $"Вас заблоковано в курсі '{course.Title}'. Доступ обмежено.";
                    break;
                case "excluded":
                    message = $"Вас було виключено з учасників курсу '{course.Title}'.";
                    break;
                default:
                    message = $"Ваш статус у курсі '{course.Title}' змінено на: {actionType}.";
                    break;
            }

            Send(student, message);
        }
    }
}