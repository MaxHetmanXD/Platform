using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Enums;

namespace Platform.Models
{
    public class PlatformData
    {
        public List<User> AllUsers { get; set; } = new List<User>();
        public List<Course> AllCourses { get; set; } = new List<Course>();

        public List<Course> GetCourses(bool onlyPublic)
        {
            if (onlyPublic)
            {
                return AllCourses.Where(c => c.IsPublic).ToList();
            }

            return AllCourses.ToList();
        }

        public List<Course> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Course>();
            }

            return AllCourses.Where(c => c.MatchSearch(query)).ToList();
        }

        public List<User> GetUsers(UserRole? role = null)
        {
            if (!role.HasValue)
            {
                return AllUsers.ToList();
            }

            return AllUsers.Where(u => u.Role == role.Value).ToList();
        }

        public List<User> SearchUsers(string nicknameQuery)
        {
            if (string.IsNullOrWhiteSpace(nicknameQuery))
            {
                return new List<User>();
            }

            string lowerQuery = nicknameQuery.ToLower();

            return AllUsers.Where(u =>
                !string.IsNullOrEmpty(u.Nickname) &&
                u.Nickname.ToLower().Contains(lowerQuery)
            ).ToList();
        }

        public void SeedData()
        {

        }
    }
}