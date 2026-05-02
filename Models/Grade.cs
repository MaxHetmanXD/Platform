using System;

namespace Platform.Models
{
    public class Grade
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public int Value { get; private set; }
        public string Comment { get; private set; } = string.Empty;
        public DateTime DateSet { get; private set; }

        public StudentResponse Response { get; private set; }

        public Guid ResponseId { get; set; }

        public Grade(int value, string comment, StudentResponse response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            if (value < 0) throw new ArgumentException("Оцінка не може бути від'ємною.");
            if (value > response.TargetTask.MaxPoints)
            {
                throw new ArgumentException($"Оцінка ({value}) не може перевищувати максимум завдання ({response.TargetTask.MaxPoints}).");
            }

            Value = value;
            Comment = comment ?? string.Empty;
            Response = response;
            ResponseId = response.Id;
            DateSet = DateTime.Now;
        }

        protected Grade()
        {
            Response = null!;
        }

        public void UpdateGrade(int newValue, string newComment)
        {
            if (newValue < 0)
            {
                throw new ArgumentException("Оцінка не може бути від'ємною.");
            }

            if (!ValidateValue(newValue, Response.TargetTask.MaxPoints))
            {
                throw new ArgumentException($"Нова оцінка ({newValue}) перевищує максимально допустимий бал ({Response.TargetTask.MaxPoints}).");
            }

            Value = newValue;
            Comment = newComment ?? string.Empty;
            DateSet = DateTime.Now;
        }

        public bool ValidateValue(int valueToCheck, int maxAllowedPoints)
        {
            return valueToCheck >= 0 && valueToCheck <= maxAllowedPoints;
        }

        public string FormatFeedback()
        {
            string maxPoints = Response?.TargetTask?.MaxPoints.ToString() ?? "?";

            string result = $"Оцінка: {Value}/{maxPoints}.";

            if (!string.IsNullOrWhiteSpace(Comment))
            {
                result += $" Коментар: {Comment}";
            }

            return result;
        }
    }
}