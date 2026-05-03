using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Models;
using Platform.Services;
using System.Linq;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class NotificationServiceTests
    {
        private Student CreateDummyStudent(string login = "student1")
        {
            return new Student(login, "Pass1234", "Nick", $"{login}@test.com", "КН-21");
        }

        private Teacher CreateDummyTeacher(string login = "teacher1")
        {
            return new Teacher(login, "Pass1234", "TeacherNick", $"{login}@test.com", "Професор");
        }

        [TestMethod]
        public void Send_ValidData_AddsFormattedMessageToLog()
        {
            var service = new NotificationService();
            var student = CreateDummyStudent("test_user");

            service.Send(student, "Hello World");

            Assert.AreEqual(1, service.EventLog.Count);
            StringAssert.Contains(service.EventLog[0], "Для test_user: Hello World");
        }

        [TestMethod]
        public void Send_NullRecipientOrEmptyMessage_DoesNothing()
        {
            var service = new NotificationService();
            var student = CreateDummyStudent();

            service.Send(null!, "Hello");
            service.Send(student, "");
            service.Send(student, "   ");
            service.Send(student, null!);

            Assert.AreEqual(0, service.EventLog.Count, "Порожні або невалідні сповіщення не повинні додаватися до логу.");
        }

        [TestMethod]
        public void HandleGradeEvent_ValidGrade_SendsNotification()
        {
            var service = new NotificationService();
            var student = CreateDummyStudent();
            var task = new Task { Title = "Лабораторна 1", MaxPoints = 100 };
            var response = new StudentResponse(student, task);
            var grade = new Grade(95, "Гарно", response);

            service.HandleGradeEvent(grade, student);

            Assert.AreEqual(1, service.EventLog.Count);
            StringAssert.Contains(service.EventLog[0], "Ви отримали 95 за завдання 'Лабораторна 1'");
        }

        [TestMethod]
        public void HandleGradeEvent_NullArguments_DoesNothing()
        {
            var service = new NotificationService();
            var student = CreateDummyStudent();
            var grade = new Grade(100, "Ok", new StudentResponse(student, new Task { MaxPoints = 100 }));

            service.HandleGradeEvent(null!, student);
            service.HandleGradeEvent(grade, null!);

            Assert.AreEqual(0, service.EventLog.Count);
        }

        [TestMethod]
        public void HandleSecurityEvent_SendsFormattedSecurityWarning()
        {
            var service = new NotificationService();
            var student = CreateDummyStudent();

            service.HandleSecurityEvent(student, "Зміна пароля");

            Assert.AreEqual(1, service.EventLog.Count);
            StringAssert.Contains(service.EventLog[0], "Критичне сповіщення: Зміна пароля");
            StringAssert.Contains(service.EventLog[0], "негайно зверніться до підтримки");
        }

        [TestMethod]
        public void NotifyTeacherOfSubmission_FindsTeacher_SendsNotification()
        {
            var service = new NotificationService();
            var platform = new PlatformData();

            var teacher = CreateDummyTeacher("teacher_owner");
            platform.AllUsers.Add(teacher);

            var course = new Course { Title = "C# Course", OwnerId = teacher.Id };
            var student = CreateDummyStudent();
            var task = new Task { Title = "Project" };

            service.NotifyTeacherOfSubmission(student, task, course, platform);

            Assert.AreEqual(1, service.EventLog.Count);
            StringAssert.Contains(service.EventLog[0], $"Для teacher_owner");
            StringAssert.Contains(service.EventLog[0], $"здав роботу до завдання 'Project'");
        }

        [TestMethod]
        public void NotifyTeacherOfSubmission_TeacherNotFound_DoesNothing()
        {
            var service = new NotificationService();
            var platform = new PlatformData();

            var course = new Course { Title = "C# Course", OwnerId = Guid.NewGuid() };
            var student = CreateDummyStudent();
            var task = new Task { Title = "Project" };

            service.NotifyTeacherOfSubmission(student, task, course, platform);

            Assert.AreEqual(0, service.EventLog.Count);
        }

        [TestMethod]
        public void NotifyNewContent_Lesson_NotifiesAllStudents()
        {
            var service = new NotificationService();
            var course = new Course { Title = "Math" };
            course.Students.Add(CreateDummyStudent("stud1"));
            course.Students.Add(CreateDummyStudent("stud2"));

            var lesson = new Lesson { Title = "Derivatives" };

            service.NotifyNewContent(course, lesson);

            Assert.AreEqual(2, service.EventLog.Count);
            Assert.IsTrue(service.EventLog.All(log => log.Contains("опубліковано новий матеріал: 'Derivatives'")));
        }

        [TestMethod]
        public void NotifyNewContent_Task_NotifiesAllStudents()
        {
            var service = new NotificationService();
            var course = new Course { Title = "Math" };
            course.Students.Add(CreateDummyStudent("stud1"));

            var task = new Task { Title = "Homework 1" };

            service.NotifyNewContent(course, task);

            Assert.AreEqual(1, service.EventLog.Count);
            StringAssert.Contains(service.EventLog[0], "опубліковано новий матеріал: 'Homework 1'");
        }

        [TestMethod]
        public void NotifyNewContent_UnknownContent_DoesNothing()
        {
            var service = new NotificationService();
            var course = new Course { Title = "Math" };
            course.Students.Add(CreateDummyStudent("stud1"));

            service.NotifyNewContent(course, "Just a string");

            Assert.AreEqual(0, service.EventLog.Count, "Сповіщення для невідомого типу контенту не повинні надсилатися.");
        }

        [TestMethod]
        public void NotifyEnrollmentChange_DifferentActions_GeneratesCorrectMessages()
        {
            var service = new NotificationService();
            var student = CreateDummyStudent();
            var course = new Course { Title = "Test Course" };

            service.NotifyEnrollmentChange(student, course, "Accepted");
            StringAssert.Contains(service.EventLog[0], "Вітаємо! Вас зараховано");

            service.NotifyEnrollmentChange(student, course, "BANNED");
            StringAssert.Contains(service.EventLog[1], "Вас заблоковано в курсі");

            service.NotifyEnrollmentChange(student, course, "excluded");
            StringAssert.Contains(service.EventLog[2], "Вас було виключено з учасників");

            service.NotifyEnrollmentChange(student, course, "SomeCustomStatus");
            StringAssert.Contains(service.EventLog[3], "Ваш статус у курсі 'Test Course' змінено на: SomeCustomStatus");

            Assert.AreEqual(4, service.EventLog.Count);
        }
    }
}