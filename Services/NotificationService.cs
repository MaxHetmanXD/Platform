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
        public List<NotificationMessage> Notifications { get; set; } = new List<NotificationMessage>();

        public void Send(User recipient, string title, string message)
        {
            if (recipient == null || string.IsNullOrWhiteSpace(message)) return;

            var notification = new NotificationMessage(recipient, title, message);
            Notifications.Add(notification);

            string formattedMessage = $"[{DateTime.Now:g}] Для {recipient.Login}: {message}";
            EventLog.Add(formattedMessage);
            Console.WriteLine(formattedMessage);
        }

        public void Send(User recipient, string message)
        {
            Send(recipient, "Нове сповіщення", message);
        }

        public void MarkAsReadAsync(Guid id)
        {
            var notif = Notifications.FirstOrDefault(n => n.Id == id);
            if (notif != null)
            {
                notif.MarkAsRead();
            }
        }

        public void HandleGradeEvent(Grade grade, Student student)
        {
            if (grade == null || student == null) return;
            string taskName = grade.Response?.TargetTask?.Title ?? "Невідоме завдання";
            string message = $"Ви отримали {grade.Value} за завдання '{taskName}'.";

            Send(student, "Оцінка за завдання", message);
        }

        public void HandleSecurityEvent(User user, string action)
        {
            string message = $"Критичне сповіщення: {action}. Якщо це робили не ви, негайно зверніться до підтримки.";
            Send(user, "Безпека", message);
        }

        public void NotifyTeacherOfSubmission(Student student, Task task, Course course, PlatformData platform)
        {
            var teacher = platform.AllUsers.FirstOrDefault(u => u.Id == course.OwnerId);
            if (teacher != null)
            {
                string message = $"Студент {student.Nickname} здав роботу до завдання '{task.Title}'.";
                Send(teacher, "Нова відповідь", message);
            }
        }

        public void NotifyNewContent(Course course, object content)
        {
            string contentTitle = (content as Lesson)?.Title ?? (content as Task)?.Title ?? string.Empty;
            if (string.IsNullOrEmpty(contentTitle)) return;

            string message = $"У курсі '{course.Title}' опубліковано новий матеріал: '{contentTitle}'.";
            foreach (var student in course.Students)
            {
                Send(student, "Новий матеріал", message);
            }
        }

        public void NotifyEnrollmentChange(Student student, Course course, string actionType)
        {
            string message = actionType.ToLower() switch
            {
                "accepted" => $"Вітаємо! Вас зараховано на курс '{course.Title}'.",
                "banned" => $"Вас заблоковано в курсі '{course.Title}'. Доступ обмежено.",
                "excluded" => $"Вас було виключено з учасників курсу '{course.Title}'.",
                _ => $"Ваш статус у курсі '{course.Title}' змінено на: {actionType}."
            };

            Send(student, "Оновлення курсу", message);
        }
    }
}