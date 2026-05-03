using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Tests
{
    [TestClass]
    public class GuestTests
    {
        [TestMethod]
        public void Constructor_SetsTempIdSuccessfully()
        {
            var guest = new Guest();

            Assert.IsFalse(string.IsNullOrWhiteSpace(guest.TempId), "Тимчасовий ID не має бути порожнім.");
            Assert.AreEqual(32, guest.TempId.Length, "Формат GUID 'N' має містити рівно 32 символи.");
        }

        [TestMethod]
        public void Constructor_CreatesUniqueTempIdForDifferentGuests()
        {
            var guest1 = new Guest();
            var guest2 = new Guest();

            Assert.AreNotEqual(guest1.TempId, guest2.TempId, "У різних гостей мають бути різні тимчасові ідентифікатори.");
        }

        [TestMethod]
        public void Login_ReturnsNull_AsNotImplemented()
        {
            var guest = new Guest();
            object dummyAuthService = new object();

            var result = guest.Login("login", "password", dummyAuthService);

            Assert.IsNull(result, "Заглушка Login має повертати null.");
        }

        [TestMethod]
        public void BrowseCourses_ReturnsOnlyPublicCourses()
        {
            var guest = new Guest();
            var allCourses = new List<Course>
            {
                new Course { Title = "Public Course 1", IsPublic = true },
                new Course { Title = "Private Course", IsPublic = false },
                new Course { Title = "Public Course 2", IsPublic = true }
            };

            var result = guest.BrowseCourses(allCourses);

            Assert.AreEqual(2, result.Count, "Має повернутися рівно 2 курси.");
            Assert.IsTrue(result.All(c => c.IsPublic), "Серед результатів є непублічний курс.");
        }

        [TestMethod]
        public void BrowseCourses_WithCategory_ReturnsFilteredPublicCourses()
        {
            var guest = new Guest();
            var allCourses = new List<Course>
            {
                new Course { Title = "Public Programming", IsPublic = true, Category = CourseCategory.Programming },
                new Course { Title = "Private Programming", IsPublic = false, Category = CourseCategory.Programming },
                new Course { Title = "Public Design", IsPublic = true, Category = CourseCategory.Design }
            };

            var result = guest.BrowseCourses(allCourses, CourseCategory.Programming);

            Assert.AreEqual(1, result.Count, "Має повернутися лише 1 курс.");
            Assert.AreEqual("Public Programming", result[0].Title);
            Assert.AreEqual(CourseCategory.Programming, result[0].Category);
            Assert.IsTrue(result[0].IsPublic, "Курс має бути публічним.");
        }

        [TestMethod]
        public void BrowseCourses_NoPublicCourses_ReturnsEmptyList()
        {
            var guest = new Guest();
            var allCourses = new List<Course>
            {
                new Course { Title = "Private 1", IsPublic = false },
                new Course { Title = "Private 2", IsPublic = false }
            };

            var result = guest.BrowseCourses(allCourses);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count, "Якщо немає публічних курсів, список має бути порожнім.");
        }

        [TestMethod]
        public void BrowseCourses_EmptyInputList_ReturnsEmptyList()
        {
            var guest = new Guest();
            var emptyCourses = new List<Course>();
            var result = guest.BrowseCourses(emptyCourses);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}