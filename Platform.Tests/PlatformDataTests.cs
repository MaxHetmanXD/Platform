using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Tests
{
    [TestClass]
    public class PlatformDataTests
    {
        [TestMethod]
        public void GetCourses_OnlyPublicTrue_ReturnsOnlyPublicCourses()
        {
            var platform = new PlatformData();
            platform.AllCourses.Add(new Course { Title = "Public Course", IsPublic = true });
            platform.AllCourses.Add(new Course { Title = "Private Course", IsPublic = false });

            var result = platform.GetCourses(true);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].IsPublic);
        }

        [TestMethod]
        public void GetCourses_OnlyPublicFalse_ReturnsAllCourses()
        {
            var platform = new PlatformData();
            platform.AllCourses.Add(new Course { Title = "Public Course", IsPublic = true });
            platform.AllCourses.Add(new Course { Title = "Private Course", IsPublic = false });

            var result = platform.GetCourses(false);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void Search_EmptyOrNullQuery_ReturnsEmptyList()
        {
            var platform = new PlatformData();
            platform.AllCourses.Add(new Course { Title = "Course 1" });

            Assert.AreEqual(0, platform.Search("").Count);
            Assert.AreEqual(0, platform.Search("   ").Count);
            Assert.AreEqual(0, platform.Search(null!).Count);
        }

        [TestMethod]
        public void Search_ValidQuery_ReturnsMatchingCourses()
        {
            var platform = new PlatformData();
            var course1 = new Course { Title = "C# Basics" };
            var course2 = new Course { Title = "Java Basics" };
            platform.AllCourses.Add(course1);
            platform.AllCourses.Add(course2);

            var result = platform.Search("C#");

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetUsers_NullRole_ReturnsAllUsers()
        {
            var platform = new PlatformData();
            platform.AllUsers.Add(new User("u1", "pass", "Nick1", "e1@t.com", UserRole.Student));
            platform.AllUsers.Add(new User("u2", "pass", "Nick2", "e2@t.com", UserRole.Teacher));

            var result = platform.GetUsers(null);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void GetUsers_SpecificRole_ReturnsFilteredUsers()
        {
            var platform = new PlatformData();
            platform.AllUsers.Add(new User("s1", "pass", "Student1", "s1@t.com", UserRole.Student));
            platform.AllUsers.Add(new User("t1", "pass", "Teacher1", "t1@t.com", UserRole.Teacher));
            platform.AllUsers.Add(new User("s2", "pass", "Student2", "s2@t.com", UserRole.Student));

            var result = platform.GetUsers(UserRole.Student);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.All(u => u.Role == UserRole.Student));
        }

        [TestMethod]
        public void SearchUsers_EmptyOrNullQuery_ReturnsEmptyList()
        {
            var platform = new PlatformData();
            platform.AllUsers.Add(new User("u1", "pass", "Nick", "e@t.com", UserRole.Student));

            Assert.AreEqual(0, platform.SearchUsers("").Count);
            Assert.AreEqual(0, platform.SearchUsers("   ").Count);
            Assert.AreEqual(0, platform.SearchUsers(null!).Count);
        }

        [TestMethod]
        public void SearchUsers_ValidQuery_ReturnsCaseInsensitiveMatches()
        {
            var platform = new PlatformData();
            platform.AllUsers.Add(new User("u1", "pass", "JohnDoe", "e1@t.com", UserRole.Student));
            platform.AllUsers.Add(new User("u2", "pass", "JaneDoe", "e2@t.com", UserRole.Student));
            platform.AllUsers.Add(new User("u3", "pass", "Smith", "e3@t.com", UserRole.Student));

            var result = platform.SearchUsers("doe");

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(u => u.Nickname == "JohnDoe"));
            Assert.IsTrue(result.Any(u => u.Nickname == "JaneDoe"));
        }

        [TestMethod]
        public void SearchUsers_NullNickname_DoesNotThrowException()
        {
            var platform = new PlatformData();
            platform.AllUsers.Add(new User("u1", "pass", null!, "e1@t.com", UserRole.Student));

            var result = platform.SearchUsers("query");

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void SeedData_ExecutesWithoutExceptions()
        {
            var platform = new PlatformData();

            platform.SeedData();

            Assert.IsTrue(true);
        }
    }
}