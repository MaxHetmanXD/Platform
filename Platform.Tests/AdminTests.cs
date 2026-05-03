using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;
using System.Collections.Generic;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class AdminTests
    {
        private Admin CreateTestAdmin(string login = "admin1")
        {
            return new Admin(login, "AdminPass123", "SuperAdmin", $"{login}@test.com");
        }

        private User CreateTestUser(string login = "user1")
        {
            return new User(login, "UserPass123", "User", $"{login}@test.com", UserRole.Student);
        }

        private Course CreateTestCourse()
        {
            return new Course { Title = "Test Course", IsPublic = true };
        }

        [TestMethod]
        public void Constructor_SetsRoleAndAccessLevel()
        {
            var admin = CreateTestAdmin();

            Assert.AreEqual(UserRole.Admin, admin.Role);
            Assert.IsTrue(admin.AccessLevel, "Адмін повинен мати AccessLevel = true.");
        }

        [TestMethod]
        public void CreateUser_CreatesCorrectTypeBasedOnRole()
        {
            var admin = CreateTestAdmin();

            var newAdmin = admin.CreateUser("a2", "pass1234", "Nick", "a2@t.com", UserRole.Admin);
            var newStudent = admin.CreateUser("s1", "pass1234", "Nick", "s1@t.com", UserRole.Student);
            var newTeacher = admin.CreateUser("t1", "pass1234", "Nick", "t1@t.com", UserRole.Teacher);
            var guestFallback = admin.CreateUser("g1", "pass1234", "Nick", "g1@t.com", UserRole.Guest);

            Assert.IsInstanceOfType(newAdmin, typeof(Admin));
            Assert.AreEqual(UserRole.Student, newStudent.Role);
            Assert.AreEqual(UserRole.Teacher, newTeacher.Role);
            Assert.AreEqual(UserRole.Guest, guestFallback.Role);
            Assert.IsInstanceOfType(guestFallback, typeof(User));
        }

        [TestMethod]
        public void EditUserFields_ValidData_UpdatesUser()
        {
            var admin = CreateTestAdmin();
            var targetUser = CreateTestUser();

            admin.EditUserFields(targetUser, "new_login", "NewPass888", "NewNick", "new@test.com", null, "Some info");

            Assert.AreEqual("new_login", targetUser.Login);
            Assert.AreEqual("NewPass888", targetUser.Password);
            Assert.AreEqual("NewNick", targetUser.Nickname);
            Assert.AreEqual("new@test.com", targetUser.Email);
            Assert.AreEqual("Some info", targetUser.Info);
        }

        [TestMethod]
        public void EditUserFields_AnotherAdmin_ThrowsUnauthorizedAccessException()
        {
            var admin1 = CreateTestAdmin("admin1");
            var admin2 = CreateTestAdmin("admin2");

            var exception = Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                admin1.EditUserFields(admin2, "hack", "hack1234", "hack", "h@h.com", null, ""));

            StringAssert.Contains(exception.Message, "не може редагувати дані іншого адміністратора");
        }

        [TestMethod]
        public void EditUserFields_Self_UpdatesSuccessfully()
        {
            var admin = CreateTestAdmin();

            admin.EditUserFields(admin, "new_admin_login", "", "", "", null, "");

            Assert.AreEqual("new_admin_login", admin.Login, "Адмін має право змінювати свій власний логін.");
        }

        [TestMethod]
        public void ChangeRole_ValidUser_UpdatesRole()
        {
            var admin = CreateTestAdmin();
            var targetUser = CreateTestUser();

            admin.ChangeRole(targetUser, UserRole.Teacher);

            Assert.AreEqual(UserRole.Teacher, targetUser.Role);
        }

        [TestMethod]
        public void ChangeRole_AnotherAdmin_ThrowsUnauthorizedAccessException()
        {
            var admin1 = CreateTestAdmin("admin1");
            var admin2 = CreateTestAdmin("admin2");

            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                admin1.ChangeRole(admin2, UserRole.Student));
        }

        [TestMethod]
        public void DeleteUser_ValidUser_SetsIsActiveToFalse()
        {
            var admin = CreateTestAdmin();
            var targetUser = CreateTestUser();

            bool result = admin.DeleteUser(targetUser);

            Assert.IsTrue(result);
            Assert.IsFalse(targetUser.IsActive, "Користувач має бути позначений як неактивний.");
        }

        [TestMethod]
        public void DeleteUser_AnotherAdmin_ThrowsUnauthorizedAccessException()
        {
            var admin1 = CreateTestAdmin("admin1");
            var admin2 = CreateTestAdmin("admin2");

            Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                admin1.DeleteUser(admin2));
        }

        [TestMethod]
        public void EditCourse_UpdatesCourseProperties()
        {
            var admin = CreateTestAdmin();
            var course = CreateTestCourse();

            admin.EditCourse(course, "Updated Course", "New Description", null);

            Assert.AreEqual("Updated Course", course.Title);
            Assert.AreEqual("New Description", course.Description);
        }

        [TestMethod]
        public void StudentManagement_AcceptBanRemove_CallsCourseMethods()
        {
            var admin = CreateTestAdmin();
            var course = CreateTestCourse();
            var student = new Student("stud", "pass", "s@test.com", "Stud", "КН-21");

            course.AddToPending(student);
            admin.AcceptStudent(student, course);
            Assert.IsTrue(course.Students.Contains(student), "Студент не був прийнятий.");

            admin.BanStudent(student, course);
            Assert.IsTrue(course.BannedStudents.Contains(student), "Студент не був забанений.");

            course.BannedStudents.Clear();
            course.Students.Add(student);
            admin.RemoveStudent(student, course);
            Assert.IsFalse(course.Students.Contains(student), "Студент не був видалений.");
        }

        [TestMethod]
        public void CreateEditDeleteLesson_WorksCorrectly()
        {
            var admin = CreateTestAdmin();
            var course = CreateTestCourse();

            var lesson = admin.CreateLesson(course, "Lesson 1", "Theory", null!);
            Assert.IsTrue(course.Lessons.Contains(lesson));
            Assert.AreEqual("Lesson 1", lesson.Title);

            admin.EditLesson(lesson, "Updated Lesson", "New Theory", null!);
            Assert.AreEqual("Updated Lesson", lesson.Title);

            admin.DeleteLesson(course, lesson);
            Assert.IsFalse(course.Lessons.Contains(lesson));
        }

        [TestMethod]
        public void CreateEditDeleteTask_WorksCorrectly()
        {
            var admin = CreateTestAdmin();
            var lesson = new Lesson();
            var deadline = DateTime.Now.AddDays(1);

            var task = admin.CreateTask(lesson, "Task 1", "Theory", 100, deadline, null!);
            Assert.IsTrue(lesson.Tasks.Contains(task));
            Assert.AreEqual("Task 1", task.Title);
            Assert.AreEqual(100, task.MaxPoints);

            admin.EditTask(task, "Updated Task", "Theory", 50, null, null!);
            Assert.AreEqual("Updated Task", task.Title);
            Assert.AreEqual(50, task.MaxPoints);

            admin.DeleteTask(lesson, task);
            Assert.IsFalse(lesson.Tasks.Contains(task));
        }

        [TestMethod]
        public void SetAccessibility_LessonOrTask_SetsAllowedStudents()
        {
            var admin = CreateTestAdmin();
            var lesson = new Lesson();
            var task = new Task();
            var student = new Student("s", "p", "e", "n", "КН - 21");
            var list = new List<Student> { student };

            admin.SetAccessibility(lesson, list);
            admin.SetAccessibility(task, list);

            Assert.IsTrue(lesson.AllowedStudents.Contains(student));
            Assert.IsTrue(task.AllowedStudents.Contains(student));
        }

        [TestMethod]
        public void SetAccessibility_InvalidObject_ThrowsArgumentException()
        {
            var admin = CreateTestAdmin();
            var invalidObject = new Course();

            var exception = Assert.ThrowsExactly<ArgumentException>(() =>
                admin.SetAccessibility(invalidObject, new List<Student>()));

            StringAssert.Contains(exception.Message, "Об'єкт доступу має бути Уроком або Завданням");
        }
    }
}