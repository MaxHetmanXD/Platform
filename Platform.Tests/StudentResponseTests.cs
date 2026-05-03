using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;
using System.Reflection;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class StudentResponseTests
    {
        private Student CreateDummyStudent()
        {
            return new Student("student", "pass123", "student@test.com", "Student", "avatar.png");
        }

        private Task CreateDummyTask(DateTime? deadline = null)
        {
            return new Task
            {
                Title = "Test Task",
                MaxPoints = 100,
                Deadline = deadline
            };
        }

        private FileModel CreateDummyFile(Student uploader)
        {
            return new FileModel("homework.pdf", 1024, uploader);
        }

        [TestMethod]
        public void Constructor_ValidData_SetsPropertiesCorrectly()
        {
            var author = CreateDummyStudent();
            var task = CreateDummyTask();

            var response = new StudentResponse(author, task);

            Assert.AreNotEqual(Guid.Empty, response.Id);
            Assert.AreEqual(author, response.Author);
            Assert.AreEqual(task, response.TargetTask);
            Assert.AreEqual(SubmissionStatus.Pending, response.Status, "Статус за замовчуванням має бути Pending.");
            Assert.IsNotNull(response.AttachedFiles);
            Assert.AreEqual(0, response.AttachedFiles.Count);
            Assert.IsNull(response.FinalGrade);
            Assert.IsTrue((DateTime.Now - response.SubmissionDate).TotalSeconds < 1, "Дата здачі має бути поточною.");
        }

        [TestMethod]
        public void Constructor_NullAuthor_ThrowsArgumentNullException()
        {
            var task = CreateDummyTask();
            Assert.ThrowsExactly<ArgumentNullException>(() => new StudentResponse(null!, task));
        }

        [TestMethod]
        public void Constructor_NullTask_ThrowsArgumentNullException()
        {
            var author = CreateDummyStudent();
            Assert.ThrowsExactly<ArgumentNullException>(() => new StudentResponse(author, null!));
        }

        [TestMethod]
        public void SubmissionDate_SetToPastDate_ThrowsArgumentException()
        {
            var response = new StudentResponse(CreateDummyStudent(), CreateDummyTask());
            var propertyInfo = typeof(StudentResponse).GetProperty("SubmissionDate");

            var exception = Assert.ThrowsExactly<TargetInvocationException>(() =>
            {
                propertyInfo!.SetValue(response, DateTime.Now.AddDays(-1));
            });

            Assert.IsInstanceOfType(exception.InnerException, typeof(ArgumentException));
            StringAssert.Contains(exception.InnerException!.Message, "не може бути в минулому");
        }

        [TestMethod]
        public void ModifyFiles_AddFile_AddsToListAndUpdatesDate()
        {
            var author = CreateDummyStudent();
            var response = new StudentResponse(author, CreateDummyTask());
            var file = CreateDummyFile(author);

            System.Threading.Thread.Sleep(10);
            var initialDate = response.SubmissionDate;

            response.ModifyFiles(file, isAdding: true);

            Assert.IsTrue(response.AttachedFiles.Contains(file));
            Assert.AreEqual(1, response.AttachedFiles.Count);
            Assert.IsTrue(response.SubmissionDate > initialDate, "Дата SubmissionDate не оновилася після додавання файлу.");
        }

        [TestMethod]
        public void ModifyFiles_RemoveFile_RemovesFromListAndUpdatesDate()
        {
            var author = CreateDummyStudent();
            var response = new StudentResponse(author, CreateDummyTask());
            var file = CreateDummyFile(author);

            response.ModifyFiles(file, isAdding: true);

            System.Threading.Thread.Sleep(10);
            var dateAfterAdd = response.SubmissionDate;

            response.ModifyFiles(file, isAdding: false);

            Assert.IsFalse(response.AttachedFiles.Contains(file));
            Assert.AreEqual(0, response.AttachedFiles.Count);
            Assert.IsTrue(response.SubmissionDate > dateAfterAdd, "Дата SubmissionDate не оновилася після видалення файлу.");
        }

        [TestMethod]
        public void ModifyFiles_NullFile_DoesNothing()
        {
            var response = new StudentResponse(CreateDummyStudent(), CreateDummyTask());

            response.ModifyFiles(null!, isAdding: true);

            Assert.AreEqual(0, response.AttachedFiles.Count);
        }

        [TestMethod]
        public void Submit_ChangesStatusToPending_AndInvokesOnSubmittedEvent()
        {
            var response = new StudentResponse(CreateDummyStudent(), CreateDummyTask());
            response.Status = SubmissionStatus.Overdue;

            bool eventFired = false;
            response.OnSubmitted += (sender, args) =>
            {
                eventFired = true;
                Assert.AreEqual(response, args);
            };

            response.Submit();

            Assert.AreEqual(SubmissionStatus.Pending, response.Status);
            Assert.IsTrue(eventFired, "Подія OnSubmitted не спрацювала.");
        }

        [TestMethod]
        public void RefreshStatus_NoDeadline_DoesNotChangeStatus()
        {
            var response = new StudentResponse(CreateDummyStudent(), CreateDummyTask(deadline: null));
            response.Status = SubmissionStatus.Pending;

            response.RefreshStatus();

            Assert.AreEqual(SubmissionStatus.Pending, response.Status);
        }

        [TestMethod]
        public void RefreshStatus_PastDeadline_SetsStatusToOverdue()
        {
            var task = CreateDummyTask(deadline: DateTime.Now.AddDays(-1));
            var response = new StudentResponse(CreateDummyStudent(), task);

            response.RefreshStatus();

            Assert.AreEqual(SubmissionStatus.Overdue, response.Status, "Статус не змінився на Overdue для протермінованого завдання.");
        }

        [TestMethod]
        public void RefreshStatus_FutureDeadline_DoesNotChangeStatus()
        {
            var task = CreateDummyTask(deadline: DateTime.Now.AddDays(1));
            var response = new StudentResponse(CreateDummyStudent(), task);

            response.RefreshStatus();

            Assert.AreEqual(SubmissionStatus.Pending, response.Status);
        }

        [TestMethod]
        public void RefreshStatus_WasOverdueButDeadlineExtended_SetsToPending()
        {
            var task = CreateDummyTask(deadline: DateTime.Now.AddDays(1));
            var response = new StudentResponse(CreateDummyStudent(), task);
            response.Status = SubmissionStatus.Overdue;

            response.RefreshStatus();

            Assert.AreEqual(SubmissionStatus.Pending, response.Status, "Статус не повернувся до Pending після продовження дедлайну.");
        }

        [TestMethod]
        public void ApplyGrade_ValidGrade_SetsCheckedStatusAndInvokesEvent()
        {
            var response = new StudentResponse(CreateDummyStudent(), CreateDummyTask());
            var grade = new Grade(90, "Good", response);

            bool eventFired = false;
            response.OnGradeApplied += (sender, args) => eventFired = true;

            response.ApplyGrade(grade);

            Assert.AreEqual(grade, response.FinalGrade);
            Assert.AreEqual(SubmissionStatus.Checked, response.Status);
            Assert.IsTrue(eventFired, "Подія OnGradeApplied не спрацювала.");
        }

        [TestMethod]
        public void ApplyGrade_NullGrade_ThrowsArgumentNullException()
        {
            var response = new StudentResponse(CreateDummyStudent(), CreateDummyTask());

            Assert.ThrowsExactly<ArgumentNullException>(() => response.ApplyGrade(null!));
        }
    }
}