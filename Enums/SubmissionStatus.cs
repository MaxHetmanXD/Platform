namespace Platform.Enums;

public enum SubmissionStatus
{
    Pending,   // Очікує перевірки
    Checked,   // Перевірено
    Rejected,  // Повернуто на доопрацювання
    Overdue    // Прострочено
}
