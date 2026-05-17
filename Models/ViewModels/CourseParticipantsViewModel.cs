using System;
using System.Collections.Generic;

namespace Platform.Models.ViewModels
{
    public class ParticipantItemViewModel
    {
        public Guid Id { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public Guid? AvatarId { get; set; }
        public string SpecialField { get; set; } = string.Empty;
    }

    public class CourseParticipantsViewModel
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;

        public bool IsOwnerOrAdmin { get; set; }

        public ParticipantItemViewModel Owner { get; set; } = new();
        public List<ParticipantItemViewModel> EnrolledStudents { get; set; } = new();
        public List<ParticipantItemViewModel> PendingStudents { get; set; } = new();
        public List<ParticipantItemViewModel> BannedStudents { get; set; } = new();
    }
}