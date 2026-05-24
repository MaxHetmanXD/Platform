using Platform.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Platform.Models
{
    public class StudentResponse
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Student Author { get; private set; }

        public Task TargetTask { get; private set; }

        private DateTime _submissionDate;
        public DateTime SubmissionDate
        {
            get => _submissionDate;
            private set
            {
                if (value < DateTime.Now.AddMinutes(-1))
                {
                    throw new ArgumentException("Дата здачі не може бути в минулому.");
                }
                _submissionDate = value;
            }
        }

        public List<FileModel> AttachedFiles { get; set; } = new List<FileModel>();

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        public Grade? FinalGrade { get; set; }
        public event EventHandler<StudentResponse>? OnSubmitted;
        public event EventHandler<StudentResponse>? OnGradeApplied;

        protected StudentResponse()
        {
        }

        public StudentResponse(Student author, Task targetTask)
        {
            Author = author ?? throw new ArgumentNullException(nameof(author));
            TargetTask = targetTask ?? throw new ArgumentNullException(nameof(targetTask));
            _submissionDate = DateTime.Now;
        }

        public void ModifyFiles(FileModel file, bool isAdding)
        {
            if (file == null) return;

            if (isAdding)
            {
                AttachedFiles.Add(file);
            }
            else
            {
                AttachedFiles.Remove(file);
            }
            SubmissionDate = DateTime.Now;
        }

        public void Submit()
        {
            Status = SubmissionStatus.Pending;
            OnSubmitted?.Invoke(this, this);
        }

        public void Reject()
        {
            if (Status != SubmissionStatus.Pending)
            {
                throw new ArgumentException("Статус не може бути змінений (завдання не було відправлено або вже перевірено)!");
            }

            Status = SubmissionStatus.Rejected;
            FinalGrade = null;
        }
        public void RefreshStatus()
        {
            if (TargetTask.Deadline.HasValue)
            {
                if (SubmissionDate > TargetTask.Deadline.Value)
                {
                    Status = SubmissionStatus.Overdue;
                }
                else if (Status == SubmissionStatus.Overdue)
                {
                    Status = SubmissionStatus.Pending;
                }
            }
        }

        public void ApplyGrade(Grade grade)
        {
            if (grade == null) throw new ArgumentNullException(nameof(grade));

            FinalGrade = grade;
            Status = SubmissionStatus.Checked;
            OnGradeApplied?.Invoke(this, this);
        }
    }
}