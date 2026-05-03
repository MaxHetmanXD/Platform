using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;
using System.Collections.Generic;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class TaskTests
    {
        private Student CreateDummyStudent(string login = "test_user")
        {
            return new Student(login, "pass123", $"{login}@test.com", "Tester", "avatar.png");
        }

        private FileModel CreateDummyFile(Student uploader)
        {
            return new FileModel("document.pdf", 1024, uploader);
        }

        [TestMethod]
        public void UpdateTaskInfo_ValidData_UpdatesProperties()
        {
            var task = new Task();
            var deadline = DateTime.Now.AddDays(7);
            var attachments = new List<FileModel> { CreateDummyFile(CreateDummyStudent()) };

            task.UpdateTaskInfo("New Title", "New Theory", 95, deadline, attachments);

            Assert.AreEqual("New Title", task.Title);
            Assert.AreEqual("New Theory", task.TheoryContent);
            Assert.AreEqual(95, task.MaxPoints);
            Assert.AreEqual(deadline, task.Deadline);
            Assert.AreEqual(1, task.Attachments.Count);
        }

        [TestMethod]
        public void UpdateTaskInfo_EmptyTitle_ThrowsArgumentException()
        {
            var task = new Task();
            Assert.ThrowsExactly<ArgumentException>(() =>
                task.UpdateTaskInfo("", "Theory", 100, null, null!));

            Assert.ThrowsExactly<ArgumentException>(() =>
                task.UpdateTaskInfo("   ", "Theory", 100, null, null!));

            Assert.ThrowsExactly<ArgumentException>(() =>
                task.UpdateTaskInfo(null!, "Theory", 100, null, null!));
        }

        [TestMethod]
        public void UpdateTaskInfo_NegativeMaxPoints_ThrowsArgumentException()
        {
            var task = new Task();
            Assert.ThrowsExactly<ArgumentException>(() =>
                task.UpdateTaskInfo("Title", "Theory", -5, null, null!));
        }

        [TestMethod]
        public void UpdateTaskInfo_MaxPointsGreaterThan100_ThrowsArgumentException()
        {
            var task = new Task();
            Assert.ThrowsExactly<ArgumentException>(() =>
                task.UpdateTaskInfo("Title", "Theory", 105, null, null!));
        }

        [TestMethod]
        public void UpdateTaskInfo_NullAttachments_SetsEmptyList()
        {
            var task = new Task();
            task.UpdateTaskInfo("Title", "Theory", 100, null, null!);
            Assert.IsNotNull(task.Attachments);
            Assert.AreEqual(0, task.Attachments.Count);
        }

        [TestMethod]
        public void SetAccessibility_ValidList_SetsAllowedStudents()
        {
            var task = new Task();
            var student = CreateDummyStudent();
            var list = new List<Student> { student };

            task.SetAccessibility(list);

            Assert.IsTrue(task.AllowedStudents.Contains(student));
        }

        [TestMethod]
        public void SetAccessibility_NullList_SetsEmptyList()
        {
            var task = new Task();
            task.SetAccessibility(null!);

            Assert.IsNotNull(task.AllowedStudents);
            Assert.AreEqual(0, task.AllowedStudents.Count);
        }

        [TestMethod]
        public void CheckSubmissionEligibility_StudentInList_ReturnsTrue()
        {
            var task = new Task();
            var student = CreateDummyStudent();
            task.SetAccessibility(new List<Student> { student });

            Assert.IsTrue(task.CheckSubmissionEligibility(student));
        }

        [TestMethod]
        public void CheckSubmissionEligibility_StudentNotInList_ReturnsFalse()
        {
            var task = new Task();
            var student1 = CreateDummyStudent("user1");
            var student2 = CreateDummyStudent("user2");
            task.SetAccessibility(new List<Student> { student1 });

            Assert.IsFalse(task.CheckSubmissionEligibility(student2));
        }

        [TestMethod]
        public void IsOverdue_NoDeadline_ReturnsFalse()
        {
            var task = new Task { Deadline = null };
            Assert.IsFalse(task.IsOverdue());
        }

        [TestMethod]
        public void IsOverdue_FutureDeadline_ReturnsFalse()
        {
            var task = new Task { Deadline = DateTime.Now.AddDays(1) };
            Assert.IsFalse(task.IsOverdue());
        }

        [TestMethod]
        public void IsOverdue_PastDeadline_ReturnsTrue()
        {
            var task = new Task { Deadline = DateTime.Now.AddDays(-1) };
            Assert.IsTrue(task.IsOverdue());
        }

        [TestMethod]
        public void GetTaskStatus_CheckedResponse_ReturnsОцінено()
        {
            var task = new Task();
            var student = CreateDummyStudent();
            var response = new StudentResponse(student, task) { Status = SubmissionStatus.Checked };
            task.Responses.Add(response);

            Assert.AreEqual("Оцінено", task.GetTaskStatus(student));
        }

        [TestMethod]
        public void GetTaskStatus_PendingResponse_ReturnsНаПеревірці()
        {
            var task = new Task();
            var student = CreateDummyStudent();
            var response = new StudentResponse(student, task) { Status = SubmissionStatus.Pending };
            task.Responses.Add(response);

            Assert.AreEqual("На перевірці", task.GetTaskStatus(student));
        }

        [TestMethod]
        public void GetTaskStatus_NoResponseOverdue_ReturnsПрострочено()
        {
            var task = new Task { Deadline = DateTime.Now.AddDays(-1) };
            var student = CreateDummyStudent();

            Assert.AreEqual("Прострочено", task.GetTaskStatus(student));
        }

        [TestMethod]
        public void GetTaskStatus_NoResponseNotOverdue_ReturnsНеЗдано()
        {
            var task = new Task { Deadline = DateTime.Now.AddDays(1) };
            var student = CreateDummyStudent();

            Assert.AreEqual("Не здано", task.GetTaskStatus(student));
        }

        [TestMethod]
        public void GetStudentGrade_HasGrade_ReturnsGrade()
        {
            var task = new Task { MaxPoints = 100 };
            var student = CreateDummyStudent();
            var response = new StudentResponse(student, task);
            var grade = new Grade(90, "Good", response);
            response.ApplyGrade(grade);
            task.Responses.Add(response);

            var actualGrade = task.GetStudentGrade(student);

            Assert.IsNotNull(actualGrade);
            Assert.AreEqual(90, actualGrade.Value);
        }

        [TestMethod]
        public void GetStudentGrade_NoResponseOrNoGrade_ReturnsNull()
        {
            var task = new Task();
            var student = CreateDummyStudent();

            Assert.IsNull(task.GetStudentGrade(student));
        }

        [TestMethod]
        public void AttachFile_ValidFile_AddsToList()
        {
            var task = new Task();
            var file = CreateDummyFile(CreateDummyStudent());

            task.AttachFile(file);

            Assert.IsTrue(task.Attachments.Contains(file));
            Assert.IsTrue(task.HasAttachments);
        }

        [TestMethod]
        public void AttachFile_NullFile_DoesNotAdd()
        {
            var task = new Task();
            task.AttachFile(null!);

            Assert.AreEqual(0, task.Attachments.Count);
            Assert.IsFalse(task.HasAttachments);
        }

        [TestMethod]
        public void RemoveFile_ExistingFile_RemovesAndReturnsTrue()
        {
            var task = new Task();
            var file = CreateDummyFile(CreateDummyStudent());
            task.AttachFile(file);

            bool result = task.RemoveFile(file);

            Assert.IsTrue(result);
            Assert.IsFalse(task.Attachments.Contains(file));
        }

        [TestMethod]
        public void RemoveFile_NonExistingFile_ReturnsFalse()
        {
            var task = new Task();
            var file = CreateDummyFile(CreateDummyStudent());

            bool result = task.RemoveFile(file);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetAttachments_ReturnsCorrectList()
        {
            var task = new Task();
            var file = CreateDummyFile(CreateDummyStudent());
            task.AttachFile(file);

            var list = task.GetAttachments();

            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(file, list[0]);
        }

        [TestMethod]
        public void IsAttachmentAllowed_ReturnsTrue()
        {
            var task = new Task();
            Assert.IsTrue(task.IsAttachmentAllowed());
        }
    }
}