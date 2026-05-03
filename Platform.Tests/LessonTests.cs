using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class LessonTests
    {
        private Student CreateDummyStudent(string login = "test_user")
        {
            return new Student(login, "pass123", $"{login}@test.com", "Tester", "avatar.png");
        }

        private FileModel CreateDummyFile(Student uploader)
        {
            return new FileModel("material.pdf", 1024, uploader);
        }

        [TestMethod]
        public void UpdateContent_ValidData_UpdatesProperties()
        {
            var lesson = new Lesson();
            var materials = new List<FileModel> { CreateDummyFile(CreateDummyStudent()) };

            lesson.UpdateContent("New Title", "New Theory", materials);

            Assert.AreEqual("New Title", lesson.Title);
            Assert.AreEqual("New Theory", lesson.TheoryContent);
            Assert.AreEqual(1, lesson.Attachments.Count);
        }

        [TestMethod]
        public void UpdateContent_EmptyTitle_ThrowsArgumentException()
        {
            var lesson = new Lesson();
            Assert.ThrowsExactly<ArgumentException>(() =>
                lesson.UpdateContent("", "Theory", null!));
        }

        [TestMethod]
        public void UpdateContent_NullMaterials_SetsEmptyList()
        {
            var lesson = new Lesson();
            lesson.UpdateContent("Title", "Theory", null!);
            Assert.IsNotNull(lesson.Attachments);
            Assert.AreEqual(0, lesson.Attachments.Count);
        }

        [TestMethod]
        public void AddTask_ValidTask_AddsToListAndIgnoresDuplicates()
        {
            var lesson = new Lesson();
            var task = new Task { Title = "Task 1" };

            lesson.AddTask(task);
            lesson.AddTask(task);
            lesson.AddTask(null!);

            Assert.AreEqual(1, lesson.Tasks.Count);
            Assert.IsTrue(lesson.Tasks.Contains(task));
        }

        [TestMethod]
        public void RemoveTask_ExistingTask_RemovesFromList()
        {
            var lesson = new Lesson();
            var task = new Task { Title = "To Delete" };
            lesson.AddTask(task);

            lesson.RemoveTask(task);

            Assert.IsFalse(lesson.Tasks.Contains(task));
            Assert.AreEqual(0, lesson.Tasks.Count);
        }

        [TestMethod]
        public void GetAccessibleTasks_StudentNotAllowedInLesson_ReturnsEmptyList()
        {
            var lesson = new Lesson();
            var student = CreateDummyStudent();
            var task = new Task();
            task.AllowedStudents.Add(student);
            lesson.AddTask(task);

            var accessible = lesson.GetAccessibleTasks(student);

            Assert.AreEqual(0, accessible.Count);
        }

        [TestMethod]
        public void GetAccessibleTasks_StudentAllowedInLesson_ReturnsOnlyAllowedTasks()
        {
            var lesson = new Lesson();
            var student = CreateDummyStudent();
            lesson.AllowedStudents.Add(student);

            var task1 = new Task { Title = "Allowed" };
            task1.AllowedStudents.Add(student);

            var task2 = new Task { Title = "Forbidden" };

            lesson.AddTask(task1);
            lesson.AddTask(task2);

            var accessible = lesson.GetAccessibleTasks(student);

            Assert.AreEqual(1, accessible.Count);
            Assert.AreEqual("Allowed", accessible[0].Title);
        }

        [TestMethod]
        public void CalculateAverage_WithGradedResponses_ReturnsCorrectAverage()
        {
            var student = CreateDummyStudent();
            var lesson = new Lesson();

            var task1 = new Task { MaxPoints = 100 };
            var response1 = new StudentResponse(student, task1);
            response1.ApplyGrade(new Grade(80, "Good", response1));
            task1.Responses.Add(response1);

            var task2 = new Task { MaxPoints = 100 };
            var response2 = new StudentResponse(student, task2);
            response2.ApplyGrade(new Grade(100, "Perfect", response2));
            task2.Responses.Add(response2);

            lesson.AddTask(task1);
            lesson.AddTask(task2);

            double average = lesson.CalculateAverage(student);

            Assert.AreEqual(90.0, average, 0.001);
        }

        [TestMethod]
        public void CalculateAverage_NoGrades_ReturnsZero()
        {
            var student = CreateDummyStudent();
            var lesson = new Lesson();
            var task = new Task();
            lesson.AddTask(task);

            Assert.AreEqual(0, lesson.CalculateAverage(student));
        }

        [TestMethod]
        public void GetSubmissionStats_CalculatesCorrectly()
        {
            var lesson = new Lesson();
            var student = CreateDummyStudent();

            var task = new Task();
            task.Responses.Add(new StudentResponse(student, task) { Status = SubmissionStatus.Checked });
            task.Responses.Add(new StudentResponse(student, task) { Status = SubmissionStatus.Checked });
            task.Responses.Add(new StudentResponse(student, task) { Status = SubmissionStatus.Pending });
            task.Responses.Add(new StudentResponse(student, task) { Status = SubmissionStatus.Overdue });

            lesson.AddTask(task);

            var stats = lesson.GetSubmissionStats();

            Assert.AreEqual(2, stats["Checked"]);
            Assert.AreEqual(1, stats["Pending"]);
            Assert.AreEqual(1, stats["Overdue"]);
        }

        [TestMethod]
        public void AttachFile_ValidFile_AddsToList()
        {
            var lesson = new Lesson();
            var file = CreateDummyFile(CreateDummyStudent());

            lesson.AttachFile(file);

            Assert.IsTrue(lesson.Attachments.Contains(file));
            Assert.IsTrue(lesson.HasAttachments);
        }

        [TestMethod]
        public void RemoveFile_ExistingFile_RemovesAndReturnsTrue()
        {
            var lesson = new Lesson();
            var file = CreateDummyFile(CreateDummyStudent());
            lesson.AttachFile(file);

            bool result = lesson.RemoveFile(file);

            Assert.IsTrue(result);
            Assert.IsFalse(lesson.Attachments.Contains(file));
        }

        [TestMethod]
        public void GetAttachments_ReturnsCorrectList()
        {
            var lesson = new Lesson();
            var file = CreateDummyFile(CreateDummyStudent());
            lesson.AttachFile(file);

            var list = lesson.GetAttachments();

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(file, list[0]);
        }

        [TestMethod]
        public void SetAccessibility_ValidList_SetsAllowedStudents()
        {
            var lesson = new Lesson();
            var student = CreateDummyStudent();
            var list = new List<Student> { student };

            lesson.SetAccessibility(list);

            Assert.IsTrue(lesson.AllowedStudents.Contains(student));
            Assert.AreEqual(1, lesson.AllowedStudents.Count);
        }

        [TestMethod]
        public void SetAccessibility_NullList_SetsEmptyList()
        {
            var lesson = new Lesson();
            lesson.SetAccessibility(null!);

            Assert.IsNotNull(lesson.AllowedStudents);
            Assert.AreEqual(0, lesson.AllowedStudents.Count);
        }

        [TestMethod]
        public void IsAttachmentAllowed_ReturnsTrue()
        {
            var lesson = new Lesson();
            Assert.IsTrue(lesson.IsAttachmentAllowed());
        }
    }
}