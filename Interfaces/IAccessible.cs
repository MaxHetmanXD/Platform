using System.Collections.Generic;
using Platform.Models;

namespace Platform.Interfaces
{
    public interface IAccessible
    {
        void SetAccessibility(List<Student> allowedStudents);
    }
}