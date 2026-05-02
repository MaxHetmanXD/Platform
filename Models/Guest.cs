using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Enums;

namespace Platform.Models
{
    public class Guest
    {
        public string TempId { get; private set; }

        public Guest()
        {
            TempId = Guid.NewGuid().ToString("N");
        }

        public User? Login(string login, string password, object authService)
        {
            Console.WriteLine("Спроба входу. Очікуємо реалізацію AuthService.");
            return null;
        }

        public List<Course> BrowseCourses(List<Course> allPlatformCourses, CourseCategory? category = null)
        {
            var query = allPlatformCourses.Where(c => c.IsPublic);

            if (category.HasValue)
            {
                query = query.Where(c => c.Category == category.Value);
            }

            return query.ToList();
        }
    }
}