using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;
using System.Linq;

namespace Platform.Tests
{
    [TestClass]
    public class CourseTests
    {
        private Course CreatePublicCourse()
        {
            return new Course
            {
                Title = "Public Test Course",
                IsPublic = true
            };
        }

        private Course CreatePrivateCourse(string password)
        {
            return new Course
            {
                Title = "Private Test Course",
                IsPublic = false,
                Pass = password
            };
        }

        private Student CreateDummyStudent(string login)
        {
            return new Student(login, "pass", $"{login}@test.com", "Tester", "avatar.png");
        }

        [TestMethod]
        public void VerifyCoursePassword_PublicCourse_AlwaysReturnsTrue()
        {
            var course = CreatePublicCourse();

            bool resultWithEmpty = course.VerifyCoursePassword("");
            bool resultWithWrongPass = course.VerifyCoursePassword("wrong123");

            Assert.IsTrue(resultWithEmpty, "Публічний курс не пропустив порожній пароль.");
            Assert.IsTrue(resultWithWrongPass, "Публічний курс не пропустив неправильний пароль.");
        }

        [TestMethod]
        public void VerifyCoursePassword_PrivateCourse_ValidatesCorrectly()
        {
            var course = CreatePrivateCourse("secret777");

            Assert.IsTrue(course.VerifyCoursePassword("secret777"), "Правильний пароль не підійшов.");
            Assert.IsFalse(course.VerifyCoursePassword("wrong_pass"), "Курс пропустив неправильний пароль.");
            Assert.IsFalse(course.VerifyCoursePassword(""), "Курс пропустив порожній пароль.");
        }

        [TestMethod]
        public void ProcessEnrollmentRequest_PublicCourse_AddsToPending()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("user1");

            bool result = course.ProcessEnrollmentRequest(student, null);

            Assert.IsTrue(result, "Заявка на публічний курс була відхилена.");
            Assert.IsTrue(course.PendingStudents.Contains(student), "Студент не потрапив у PendingStudents.");
            Assert.IsFalse(course.Students.Contains(student), "Студент одразу потрапив у Students (хоча має бути в Pending).");
        }

        [TestMethod]
        public void ProcessEnrollmentRequest_BannedStudent_IsRejected()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("bad_user");

            course.AddToBanList(student);

            bool result = course.ProcessEnrollmentRequest(student, null);

            Assert.IsFalse(result, "Забаненого студента було пропущено.");
            Assert.IsFalse(course.PendingStudents.Contains(student), "Забанений студент потрапив у Pending.");
        }

        [TestMethod]
        public void ProcessEnrollmentRequest_PrivateCourseWrongPassword_IsRejected()
        {

            var course = CreatePrivateCourse("pass123");
            var student = CreateDummyStudent("user2");

            bool result = course.ProcessEnrollmentRequest(student, "wrong");

            Assert.IsFalse(result);
            Assert.IsFalse(course.PendingStudents.Contains(student));
        }

        [TestMethod]
        public void ActivateStudent_PendingStudent_MovesToStudentsList()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("user3");
            course.AddToPending(student);

            course.ActivateStudent(student);

            Assert.IsFalse(course.PendingStudents.Contains(student), "Студент залишився в Pending.");
            Assert.IsTrue(course.Students.Contains(student), "Студент не потрапив до списку Students.");
        }

        [TestMethod]
        public void AddToBanList_ActiveStudent_RemovesFromActiveAndAddsToBan()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("user4");

            course.AddToPending(student);
            course.ActivateStudent(student);

            course.AddToBanList(student);

            Assert.IsFalse(course.Students.Contains(student), "Студента не видалено з активних.");
            Assert.IsFalse(course.PendingStudents.Contains(student), "Студента не видалено з Pending.");
            Assert.IsTrue(course.BannedStudents.Contains(student), "Студент не потрапив до списку забанених.");
        }

        [TestMethod]
        public void GetFilteredContent_NotEnrolledStudent_ReturnsEmptyList()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("stranger");
            var lesson = new Lesson { Title = "Lesson 1" };

            course.AddLesson(lesson);
            lesson.AllowedStudents.Add(student);

            var filteredLessons = course.GetFilteredContent(student);

            Assert.AreEqual(0, filteredLessons.Count);
        }

        [TestMethod]
        public void GetGlobalAverage_NoStudents_ReturnsZero()
        {
            var course = CreatePublicCourse();

            double average = course.GetGlobalAverage();

            Assert.AreEqual(0, average, "Курс без студентів має повертати середній бал 0.");
        }

        [TestMethod]
        public void MatchSearch_TitleMatch_ReturnsTrue()
        {
            var course = new Course
            {
                Title = "C# Programming Advanced",
                Category = CourseCategory.Programming
            };

            Assert.IsTrue(course.MatchSearch("c#"));
            Assert.IsTrue(course.MatchSearch("ADVANCED"));
            Assert.IsTrue(course.MatchSearch("programming"));
        }

        [TestMethod]
        public void MatchSearch_CategoryMatch_ReturnsTrue()
        {
            var course = new Course
            {
                Title = "How to draw a cat",
                Category = CourseCategory.Design
            };

            Assert.IsTrue(course.MatchSearch("design"), "Пошук за категорією не спрацював.");
        }

        [TestMethod]
        public void MatchSearch_TagMatch_ReturnsTrue()
        {
            var course = new Course { Title = "Basic HTML" };
            course.Tags.Add("web");
            course.Tags.Add("frontend");

            Assert.IsTrue(course.MatchSearch("WEB"));
            Assert.IsTrue(course.MatchSearch("Frontend"));
            Assert.IsFalse(course.MatchSearch("backend"), "Знайдено тег, якого не існує.");
        }

        [TestMethod]
        public void MatchSearch_EmptyQuery_ReturnsFalse()
        {
            var course = new Course { Title = "Test" };

            Assert.IsFalse(course.MatchSearch(""));
            Assert.IsFalse(course.MatchSearch("   "));
            Assert.IsFalse(course.MatchSearch(null!));
        }

        [TestMethod]
        public void GetSearchWeight_ExactTitle_ReturnsHighestWeight()
        {
            var course = new Course { Title = "WPF Basics" };

            int weight = course.GetSearchWeight("wpf basics");

            Assert.AreEqual(100, weight, "Точний збіг назви має давати вагу 100.");
        }

        [TestMethod]
        public void GetSearchWeight_PartialTitle_ReturnsHighWeight()
        {
            var course = new Course { Title = "WPF Basics for Beginners" };

            int weight = course.GetSearchWeight("wpf");

            Assert.AreEqual(75, weight, "Частковий збіг назви має давати вагу 75.");
        }

        [TestMethod]
        public void GetSearchWeight_ExactTagMatch_ReturnsMediumWeight()
        {
            var course = new Course { Title = "UI Design" };
            course.Tags.Add("xaml");

            int weight = course.GetSearchWeight("xaml");

            Assert.AreEqual(50, weight, "Точний збіг тегу має давати вагу 50.");
        }

        [TestMethod]
        public void GetSearchResultData_FillsFieldsCorrectly()
        {
            var courseId = Guid.NewGuid();
            var course = new Course
            {
                Title = "Docker 101",
                Description = "Containerization tutorial"
            };

            var searchData = course.GetSearchResultData();

            Assert.AreEqual(course.Id, searchData.Id);
            Assert.AreEqual("Docker 101", searchData.Title);
            Assert.AreEqual("Containerization tutorial", searchData.ShortDescription);
            Assert.AreEqual("Course", searchData.Type);
        }

        [TestMethod]
        public void AddLesson_ValidLesson_AddsToListAndIgnoresDuplicates()
        {
            var course = CreatePublicCourse();
            var lesson = new Lesson { Title = "Introduction" };

            course.AddLesson(lesson);
            course.AddLesson(lesson);
            course.AddLesson(null!);

            Assert.AreEqual(1, course.Lessons.Count, "Курс додав дублікат або null-урок.");
            Assert.IsTrue(course.Lessons.Contains(lesson));
        }

        [TestMethod]
        public void RemoveLesson_ExistingLesson_RemovesFromList()
        {
            var course = CreatePublicCourse();
            var lesson = new Lesson { Title = "To Be Removed" };
            course.AddLesson(lesson);

            course.RemoveLesson(lesson);

            Assert.IsFalse(course.Lessons.Contains(lesson));
            Assert.AreEqual(0, course.Lessons.Count);
        }

        [TestMethod]
        public void ExcludeStudent_ActiveStudent_RemovesFromStudentsList()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("user_exclude");

            course.Students.Add(student);

            course.ExcludeStudent(student);

            Assert.IsFalse(course.Students.Contains(student), "Студента не було виключено з курсу.");
        }

        [TestMethod]
        public void AddToPending_AlreadyEnrolledOrBanned_DoesNotAdd()
        {
            var course = CreatePublicCourse();
            var activeStudent = CreateDummyStudent("active");
            var bannedStudent = CreateDummyStudent("banned");

            course.Students.Add(activeStudent);
            course.BannedStudents.Add(bannedStudent);

            course.AddToPending(activeStudent);
            course.AddToPending(bannedStudent);

            Assert.IsFalse(course.PendingStudents.Contains(activeStudent), "Активного студента додано в Pending.");
            Assert.IsFalse(course.PendingStudents.Contains(bannedStudent), "Забаненого студента додано в Pending.");
        }

        [TestMethod]
        public void GetFilteredContent_EnrolledStudent_ReturnsOnlyAllowedLessons()
        {
            var course = CreatePublicCourse();
            var student = CreateDummyStudent("active_user");
            course.Students.Add(student);

            var lesson1 = new Lesson { Title = "Allowed Lesson" };
            lesson1.AllowedStudents.Add(student);

            var lesson2 = new Lesson { Title = "Forbidden Lesson" };

            course.AddLesson(lesson1);
            course.AddLesson(lesson2);

            var filtered = course.GetFilteredContent(student);

            Assert.AreEqual(1, filtered.Count, "Повернуто неправильну кількість уроків.");
            Assert.IsTrue(filtered.Contains(lesson1), "Дозволений урок не повернуто.");
            Assert.IsFalse(filtered.Contains(lesson2), "Повернуто недозволений урок.");
        }
    }
}