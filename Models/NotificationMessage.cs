using System;

namespace Platform.Models
{
    public class NotificationMessage
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public User Recipient { get; private set; }
        public string Title { get; private set; }
        public string Message { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsRead { get; private set; }

        public NotificationMessage(User recipient, string title, string message)
        {
            Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
            Title = title;
            Message = message;
            CreatedAt = DateTime.Now;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}