using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;
using System.Collections.Generic;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class TeacherTests
    {
        private Teacher CreateDummyTeacher(string login = "teacher1")
        {
            return new Teacher(login, "Pass1234", "Docent", $"{login}@test.com", "Професор");
        }

        private Student CreateDummyStudent(string login = "student1")
        {
            return new Student(login, "Pass1234", $"{login}@test.com", "Stud", "КН-21");
        }

        private FileModel CreateDummyFile(User uploader)
        {
            return new FileModel("file.pdf", 1024, uploader);
        }

        [TestMethod]
        public void Constructor_ValidData_SetsPropertiesAndRole()
        {
            var teacher = new Teacher("t1", "pass", "Nick", "t@t.com", "Доцент");

            Assert.AreEqual(UserRole.Teacher, teacher.Role);
            Assert.AreEqual("Доцент", teacher.Position);
            Assert.IsNotNull(teacher.OwnCourses);
            Assert.AreEqual(0, teacher.OwnCourses.Count);
        }

        [TestMethod]
        public void Constructor_EmptyPosition_ThrowsArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new Teacher("t1", "pass", "Nick", "t@t.com", ""));

            Assert.ThrowsExactly<ArgumentException>(() =>
                new Teacher("t1", "pass", "Nick", "t@t.com", null!));
        }

        [TestMethod]
        public void CreateCourse_WithoutPassword_CreatesPublicCourse()
        {
            var teacher = CreateDummyTeacher();

            var course = teacher.CreateCourse("C# Basics", "Intro", CourseCategory.Programming, null);

            Assert.IsTrue(course.IsPublic);
            Assert.IsNull(course.Pass);
            Assert.AreEqual(teacher.Id, course.OwnerId);
            Assert.AreEqual("C# Basics", course.Title);
            Assert.AreEqual(CourseCategory.Programming, course.Category);
            Assert.IsTrue(teacher.OwnCourses.Contains(course));
        }

        [TestMethod]
        public void CreateCourse_WithPassword_CreatesPrivateCourse()
        {
            var teacher = CreateDummyTeacher();

            var course = teacher.CreateCourse("Secret Course", "Desc", CourseCategory.Design, "secret123");

            Assert.IsFalse(course.IsPublic);
            Assert.AreEqual("secret123", course.Pass);
        }

        [TestMethod]
        public void EditCourse_Owner_UpdatesSuccessfully()
        {
            var teacher = CreateDummyTeacher();
            var course = teacher.CreateCourse("Old Title", "Old Desc", CourseCategory.Programming, null);
            var banner = CreateDummyFile(teacher);

            teacher.EditCourse(course, "New Title", "New Desc", banner);

            Assert.AreEqual("New Title", course.Title);
            Assert.AreEqual("New Desc", course.Description);
            Assert.AreEqual(banner, course.Banner);
        }

        [TestMethod]
        public void EditCourse_NotOwner_ThrowsUnauthorizedAccessException()
        {
            var teacher1 = CreateDummyTeacher("teacher1");
            var teacher2 = CreateDummyTeacher("teacher2");
            var course = teacher1.CreateCourse("Title", "Desc", CourseCategory.Programming, null);

            var exception = Assert.ThrowsExactly<UnauthorizedAccessException>(() =>
                teacher2.EditCourse(course, "Hacked", "Hacked", null));

            StringAssert.Contains(exception.Message, "не є власником");
        }

        [TestMethod]
        public void AcceptStudent_PendingStudent_MovesToActiveAndUpdatesEnrolled()
        {
            var teacher = CreateDummyTeacher();
            var course = teacher.CreateCourse("Course", "Desc", CourseCategory.Programming, null);
            var student = CreateDummyStudent();

            course.PendingStudents.Add(student);

            teacher.AcceptStudent(student, course);

            Assert.IsFalse(course.PendingStudents.Contains(student));
            Assert.IsTrue(course.Students.Contains(student));
            Assert.IsTrue(student.EnrolledCourses.Contains(course));
        }

        [TestMethod]
        public void BanStudent_ActiveStudent_MovesToBanListAndRemovesFromEnrolled()
        {
            var teacher = CreateDummyTeacher();
            var course = teacher.CreateCourse("Course", "Desc", CourseCategory.Programming, null);
            var student = CreateDummyStudent();

            course.Students.Add(student);
            student.EnrolledCourses.Add(course);

            teacher.BanStudent(student, course);

            Assert.IsFalse(course.Students.Contains(student));
            Assert.IsFalse(course.PendingStudents.Contains(student));
            Assert.IsTrue(course.BannedStudents.Contains(student));
            Assert.IsFalse(student.EnrolledCourses.Contains(course));
        }

        [TestMethod]
        public void RemoveStudent_ActiveStudent_RemovesFromStudentsAndEnrolled()
        {
            var teacher = CreateDummyTeacher();
            var course = teacher.CreateCourse("Course", "Desc", CourseCategory.Programming, null);
            var student = CreateDummyStudent();

            course.Students.Add(student);
            student.EnrolledCourses.Add(course);

            teacher.RemoveStudent(student, course);

            Assert.IsFalse(course.Students.Contains(student));
            Assert.IsFalse(student.EnrolledCourses.Contains(course));
        }

        [TestMethod]
        public void CreateLesson_AddsLessonToCourse()
        {
            var teacher = CreateDummyTeacher();
            var course = new Course();
            var files = new List<FileModel> { CreateDummyFile(teacher) };

            var lesson = teacher.CreateLesson(course, "Lesson 1", "Theory", files);

            Assert.IsTrue(course.Lessons.Contains(lesson));
            Assert.AreEqual("Lesson 1", lesson.Title);
            Assert.AreEqual(1, lesson.Attachments.Count);
        }

        [TestMethod]
        public void EditLesson_UpdatesProperties()
        {
            var teacher = CreateDummyTeacher();
            var lesson = new Lesson { Title = "Title", TheoryContent = "Old" };

            teacher.EditLesson(lesson, "New Theory", null!);

            Assert.AreEqual("New Theory", lesson.TheoryContent);
            Assert.IsNotNull(lesson.Attachments);
            Assert.AreEqual(0, lesson.Attachments.Count);
        }

        [TestMethod]
        public void DeleteLesson_RemovesFromCourse()
        {
            var teacher = CreateDummyTeacher();
            var course = new Course();
            var lesson = new Lesson();
            course.Lessons.Add(lesson);

            teacher.DeleteLesson(course, lesson);

            Assert.IsFalse(course.Lessons.Contains(lesson));
        }

        [TestMethod]
        public void CreateTask_AddsTaskToLesson()
        {
            var teacher = CreateDummyTeacher();
            var lesson = new Lesson();
            var deadline = DateTime.Now.AddDays(1);

            var task = teacher.CreateTask(lesson, "Task", "Theory", 100, deadline, null!);

            Assert.IsTrue(lesson.Tasks.Contains(task));
            Assert.AreEqual("Task", task.Title);
            Assert.AreEqual(100, task.MaxPoints);
            Assert.AreEqual(deadline, task.Deadline);
        }

        [TestMethod]
        public void EditTask_UpdatesProperties()
        {
            var teacher = CreateDummyTeacher();
            var task = new Task();
            var newDeadline = DateTime.Now.AddDays(5);

            teacher.EditTask(task, "New Title", "New Theory", 50, newDeadline, null!);

            Assert.AreEqual("New Title", task.Title);
            Assert.AreEqual(50, task.MaxPoints);
            Assert.AreEqual(newDeadline, task.Deadline);
        }

        [TestMethod]
        public void DeleteTask_RemovesFromLesson()
        {
            var teacher = CreateDummyTeacher();
            var lesson = new Lesson();
            var task = new Task();
            lesson.Tasks.Add(task);

            teacher.DeleteTask(lesson, task);

            Assert.IsFalse(lesson.Tasks.Contains(task));
        }

        [TestMethod]
        public void GradeSubmission_AppliesGradeToResponse()
        {
            var teacher = CreateDummyTeacher();
            var student = CreateDummyStudent();
            var task = new Task { MaxPoints = 100 };
            var response = new StudentResponse(student, task);

            var grade = teacher.GradeSubmission(response, 95, "Good job");

            Assert.IsNotNull(response.FinalGrade);
            Assert.AreEqual(95, response.FinalGrade.Value);
            Assert.AreEqual("Good job", response.FinalGrade.Comment);
            Assert.AreEqual(grade, response.FinalGrade);
            Assert.AreEqual(SubmissionStatus.Checked, response.Status);
        }

        [TestMethod]
        public void GetStudentDetails_ReturnsAnalyticsList()
        {
            var teacher = CreateDummyTeacher();
            var student = CreateDummyStudent();
            var course = new Course();

            var details = teacher.GetStudentDetails(student, course);

            Assert.IsNotNull(details);
        }
    }
}