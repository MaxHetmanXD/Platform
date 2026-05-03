using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Models;
using System;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class GradeTests
    {
        private StudentResponse CreateDummyResponse(int maxPoints)
        {
            var task = new Task { Title = "Test Task", MaxPoints = maxPoints, IsVisible = true };
            var student = new Student("test_user", "pass123", "test@test.com", "Tester", "avatar.png");

            return new StudentResponse(student, task);
        }

        [TestMethod]
        public void Constructor_ValidData_CreatesGradeSuccessfully()
        {
            var response = CreateDummyResponse(100);
            int validScore = 85;
            string comment = "Відмінна робота!";

            var grade = new Grade(validScore, comment, response);

            Assert.AreEqual(validScore, grade.Value, "Оцінка не збігається.");
            Assert.AreEqual(comment, grade.Comment, "Коментар не збігається.");
            Assert.AreEqual(response, grade.Response, "Посилання на відповідь не збереглося.");
            Assert.AreEqual(response.Id, grade.ResponseId, "ID відповіді не збігається.");
            Assert.IsTrue((DateTime.Now - grade.DateSet).TotalSeconds < 1);
        }

        [TestMethod]
        public void Constructor_NegativeValue_ThrowsArgumentException()
        {
            var response = CreateDummyResponse(100);
            int negativeScore = -5;

            var exception = Assert.ThrowsExactly<ArgumentException>(() =>
                new Grade(negativeScore, "Bad", response)
            );

            StringAssert.Contains(exception.Message, "не може бути від'ємною");
        }

        [TestMethod]
        public void Constructor_ValueGreaterThanMaxPoints_ThrowsArgumentException()
        {
            var response = CreateDummyResponse(50);
            int tooBigScore = 60;

            Assert.ThrowsExactly<ArgumentException>(() =>
                new Grade(tooBigScore, "Too much!", response)
            );
        }

        [TestMethod]
        public void Constructor_NullResponse_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new Grade(10, "Comment", null!)
            );
        }

        [TestMethod]
        public void Constructor_NullComment_SetsToEmptyString()
        {
            var response = CreateDummyResponse(10);

            var grade = new Grade(5, null!, response);

            Assert.AreEqual(string.Empty, grade.Comment, "Null коментар не замінився на порожній рядок.");
        }

        [TestMethod]
        public void UpdateGrade_ValidData_UpdatesSuccessfully()
        {
            var response = CreateDummyResponse(100);
            var grade = new Grade(50, "Ок", response);

            System.Threading.Thread.Sleep(10);
            var oldDate = grade.DateSet;

            grade.UpdateGrade(90, "Виправив помилки, молодець");

            Assert.AreEqual(90, grade.Value);
            Assert.AreEqual("Виправив помилки, молодець", grade.Comment);
            Assert.IsTrue(grade.DateSet > oldDate, "Дата оновлення не змінилася.");
        }

        [TestMethod]
        public void UpdateGrade_NegativeValue_ThrowsArgumentException()
        {
            var response = CreateDummyResponse(100);
            var grade = new Grade(50, "Ок", response);

            Assert.ThrowsExactly<ArgumentException>(() => grade.UpdateGrade(-10, "Погано"));
        }

        [TestMethod]
        public void FormatFeedback_WithComment_ReturnsFullString()
        {
            var response = CreateDummyResponse(12);
            var grade = new Grade(10, "Гарно", response);

            string feedback = grade.FormatFeedback();

            Assert.AreEqual("Оцінка: 10/12. Коментар: Гарно", feedback);
        }

        [TestMethod]
        public void FormatFeedback_EmptyComment_ReturnsShortString()
        {
            var response = CreateDummyResponse(12);
            var grade = new Grade(10, "", response);

            string feedback = grade.FormatFeedback();

            Assert.AreEqual("Оцінка: 10/12.", feedback);
        }

        [TestMethod]
        public void ValidateValue_ValidScore_ReturnsTrue()
        {
            var response = CreateDummyResponse(100);
            var grade = new Grade(50, "Ок", response);

            bool isValid = grade.ValidateValue(85, 100);

            Assert.IsTrue(isValid, "Валідне значення не пройшло перевірку.");
        }

        [TestMethod]
        public void ValidateValue_NegativeScore_ReturnsFalse()
        {
            var response = CreateDummyResponse(100);
            var grade = new Grade(50, "Ок", response);

            bool isValid = grade.ValidateValue(-5, 100);

            Assert.IsFalse(isValid, "Від'ємне значення пройшло перевірку.");
        }

        [TestMethod]
        public void ValidateValue_ScoreGreaterThanMax_ReturnsFalse()
        {
            var response = CreateDummyResponse(100);
            var grade = new Grade(50, "Ок", response);

            bool isValid = grade.ValidateValue(105, 100);

            Assert.IsFalse(isValid, "Значення, більше за максимум, пройшло перевірку.");
        }

        [TestMethod]
        public void UpdateGrade_ValueGreaterThanMaxPoints_ThrowsArgumentException()
        {
            var response = CreateDummyResponse(50);
            var grade = new Grade(40, "Ок", response);

            Assert.ThrowsExactly<ArgumentException>(() => grade.UpdateGrade(60, "Забагато"));
        }
    }
}