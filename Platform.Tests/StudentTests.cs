using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.DTOs;
using Platform.Enums;
using Platform.Models;
using System;
using System.Collections.Generic;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class StudentTests
    {
        private Student CreateDummyStudent(string login = "student1")
        {
            return new Student(login, "Pass1234", "Nick", $"{login}@test.com", "КН-21");
        }

        private Course CreateDummyCourse()
        {
            return new Course { Title = "Test Course", IsPublic = true };
        }

        [TestMethod]
        public void Constructor_ValidData_SetsPropertiesAndRole()
        {
            var student = new Student("s1", "pass", "Nick", "s@test.com", "КН-21");

            Assert.AreEqual(UserRole.Student, student.Role);
            Assert.AreEqual("КН-21", student.Group);
            Assert.IsNotNull(student.EnrolledCourses);
            Assert.AreEqual(0, student.EnrolledCourses.Count);
        }

        [TestMethod]
        public void Constructor_EmptyGroup_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Student("s1", "pass", "Nick", "s@test.com", ""));

            Assert.ThrowsExactly<ArgumentException>(() =>
                new Student("s1", "pass", "Nick", "s@test.com", "   "));

            Assert.ThrowsExactly<ArgumentException>(() =>
                new Student("s1", "pass", "Nick", "s@test.com", null!));
        }

        [TestMethod]
        public void RequestEnrollment_CallsCourseMethod_ReturnsBoolean()
        {
            var student = CreateDummyStudent();
            var course = CreateDummyCourse();

            bool result = student.RequestEnrollment(course, null);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Unenroll_WhenEnrolled_RemovesFromCourseAndStudentList()
        {
            var student = CreateDummyStudent();
            var course = CreateDummyCourse();

            student.EnrolledCourses.Add(course);
            course.Students.Add(student);

            student.Unenroll(course);

            Assert.IsFalse(student.EnrolledCourses.Contains(course));
            Assert.IsFalse(course.Students.Contains(student));
        }

        [TestMethod]
        public void Unenroll_WhenNotEnrolled_DoesNothing()
        {
            var student = CreateDummyStudent();
            var course = CreateDummyCourse();

            student.Unenroll(course);

            Assert.AreEqual(0, student.EnrolledCourses.Count);
        }

        [TestMethod]
        public void GetAvailableContent_NotEnrolled_ReturnsEmptyList()
        {
            var student = CreateDummyStudent();
            var course = CreateDummyCourse();
            var lesson = new Lesson();
            lesson.AllowedStudents.Add(student);
            course.Lessons.Add(lesson);

            var content = student.GetAvailableContent(course);

            Assert.AreEqual(0, content.Count);
        }

        [TestMethod]
        public void GetAvailableContent_Enrolled_ReturnsOnlyAllowedLessons()
        {
            var student = CreateDummyStudent();
            var course = CreateDummyCourse();

            course.Students.Add(student);

            var lesson1 = new Lesson { Title = "Allowed" };
            lesson1.AllowedStudents.Add(student);

            var lesson2 = new Lesson { Title = "Forbidden" };

            course.Lessons.Add(lesson1);
            course.Lessons.Add(lesson2);

            var content = student.GetAvailableContent(course);

            Assert.AreEqual(1, content.Count);
            Assert.AreEqual("Allowed", content[0].Title);
        }

        [TestMethod]
        public void SubmitTask_InvisibleTask_ThrowsInvalidOperationException()
        {
            var student = CreateDummyStudent();
            var task = new Task { IsVisible = false };

            var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
                student.SubmitTask(task, new List<FileModel>()));

            StringAssert.Contains(exception.Message, "недоступне");
        }

        [TestMethod]
        public void SubmitTask_VisibleTask_CreatesResponseFiresEventAndReturns()
        {
            var student = CreateDummyStudent();
            var task = new Task { IsVisible = true };

            bool eventFired = false;
            student.OnTaskSubmitted += (sender, response) =>
            {
                eventFired = true;
                Assert.AreEqual(student, sender);
                Assert.AreEqual(task, response.TargetTask);
            };

            var result = student.SubmitTask(task, new List<FileModel>());

            Assert.IsNotNull(result);
            Assert.AreEqual(student, result.Author);
            Assert.AreEqual(task, result.TargetTask);
            Assert.IsTrue(eventFired, "Подія OnTaskSubmitted не спрацювала.");
        }

        [TestMethod]
        public void SubmitTask_NullFiles_CreatesEmptyAttachmentList()
        {
            var student = CreateDummyStudent();
            var task = new Task { IsVisible = true };

            var result = student.SubmitTask(task, null!);

            Assert.IsNotNull(result.AttachedFiles);
            Assert.AreEqual(0, result.AttachedFiles.Count);
        }

        [TestMethod]
        public void GetLessonAverage_ReturnsCorrectAverageOfGradedTasks()
        {
            var student = CreateDummyStudent();
            var lesson = new Lesson();

            var task1 = new Task { MaxPoints = 100 };
            var resp1 = new StudentResponse(student, task1);
            resp1.ApplyGrade(new Grade(80, "Good", resp1));
            task1.Responses.Add(resp1);

            var task2 = new Task { MaxPoints = 100 };
            var resp2 = new StudentResponse(student, task2);
            resp2.ApplyGrade(new Grade(100, "Perfect", resp2));
            task2.Responses.Add(resp2);

            var task3 = new Task { MaxPoints = 100 };
            var resp3 = new StudentResponse(student, task3);
            task3.Responses.Add(resp3);

            lesson.Tasks.Add(task1);
            lesson.Tasks.Add(task2);
            lesson.Tasks.Add(task3);

            double average = student.GetLessonAverage(lesson);

            Assert.AreEqual(90.0, average, 0.001);
        }

        [TestMethod]
        public void GetLessonAverage_NoGrades_ReturnsZero()
        {
            var student = CreateDummyStudent();
            var lesson = new Lesson();

            Assert.AreEqual(0, student.GetLessonAverage(lesson));
        }

        [TestMethod]
        public void GetCourseAverage_ReturnsAverageOfLessonAverages()
        {
            var student = CreateDummyStudent();
            var course = new Course();

            var lesson1 = new Lesson();
            var task1 = new Task { MaxPoints = 100 };
            var resp1 = new StudentResponse(student, task1);
            resp1.ApplyGrade(new Grade(90, "Good", resp1));
            task1.Responses.Add(resp1);
            lesson1.Tasks.Add(task1);

            var lesson2 = new Lesson();
            var task2 = new Task { MaxPoints = 100 };
            var resp2 = new StudentResponse(student, task2);
            resp2.ApplyGrade(new Grade(100, "Perfect", resp2));
            task2.Responses.Add(resp2);
            lesson2.Tasks.Add(task2);

            var lesson3 = new Lesson();

            course.Lessons.Add(lesson1);
            course.Lessons.Add(lesson2);
            course.Lessons.Add(lesson3);

            double courseAverage = student.GetCourseAverage(course);

            Assert.AreEqual(95.0, courseAverage, 0.001);
        }

        [TestMethod]
        public void GetCourseAverage_NoGrades_ReturnsZero()
        {
            var student = CreateDummyStudent();
            var course = new Course();

            Assert.AreEqual(0, student.GetCourseAverage(course));
        }

        [TestMethod]
        public void GetTaskAnalytics_ReturnsCorrectStatusList()
        {
            var student = CreateDummyStudent();
            var course = new Course();
            var lesson = new Lesson();

            var task1 = new Task { Title = "Task 1", MaxPoints = 100 };

            var task2 = new Task { Title = "Task 2", MaxPoints = 100 };
            var resp2 = new StudentResponse(student, task2);
            task2.Responses.Add(resp2);

            var task3 = new Task { Title = "Task 3", MaxPoints = 100 };
            var resp3 = new StudentResponse(student, task3);
            resp3.ApplyGrade(new Grade(95, "Ok", resp3));
            task3.Responses.Add(resp3);

            lesson.Tasks.Add(task1);
            lesson.Tasks.Add(task2);
            lesson.Tasks.Add(task3);
            course.Lessons.Add(lesson);

            var analytics = student.GetTaskAnalytics(course);

            Assert.AreEqual(3, analytics.Count);
            Assert.AreEqual("Ще не здано", analytics.Find(a => a.TaskTitle == "Task 1")?.Status);
            Assert.AreEqual("На перевірці", analytics.Find(a => a.TaskTitle == "Task 2")?.Status);
            Assert.AreEqual("Оцінено (95)", analytics.Find(a => a.TaskTitle == "Task 3")?.Status);
        }
    }
}