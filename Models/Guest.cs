using Platform.Enums;
using Platform.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Models
{
        public class Guest
        {
            public string TempId { get; private set; }

            public Guest()
            {
                TempId = Guid.NewGuid().ToString("N");
            }

            public User? Login(string login, string password, IQueryable<User> usersDb)
            {
                if (usersDb == null) throw new ArgumentNullException(nameof(usersDb));

                var user = usersDb.FirstOrDefault(u => u.Login == login);

                if (user != null && user.Authenticate(login, password))
                {
                    return user;
                }

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