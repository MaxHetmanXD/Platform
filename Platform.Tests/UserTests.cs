using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using System;

namespace Platform.Tests
{
    [TestClass]
    public class UserTests
    {
        private User CreateTestUser()
        {
            return new User("john_doe", "SecretPass123", "JohnD", "john@test.com", UserRole.Student);
        }

        private FileModel CreateDummyAvatar(User uploader, string fileName)
        {
            return new FileModel(fileName, 2048, uploader);
        }

        [TestMethod]
        public void Constructor_ValidData_SetsPropertiesCorrectly()
        {
            var user = new User("admin_user", "pass", "Admin", "admin@test.com", UserRole.Admin);

            Assert.AreNotEqual(Guid.Empty, user.Id);
            Assert.AreEqual("admin_user", user.Login);
            Assert.AreEqual("pass", user.Password);
            Assert.AreEqual("Admin", user.Nickname);
            Assert.AreEqual("admin@test.com", user.Email);
            Assert.AreEqual(UserRole.Admin, user.Role);
            Assert.AreEqual(string.Empty, user.Info);
            Assert.IsTrue(user.IsActive, "Новий користувач має бути активним за замовчуванням.");
        }

        [TestMethod]
        public void UpdateProfile_ValidData_UpdatesPropertiesAndInvokesEvent()
        {
            var user = CreateTestUser();
            bool eventFired = false;
            user.OnProfileUpdated += (sender, message) =>
            {
                eventFired = true;
                StringAssert.Contains(message, user.Login);
            };

            user.UpdateProfile("NewNick", "new.email@test.com", null);

            Assert.AreEqual("NewNick", user.Nickname);
            Assert.AreEqual("new.email@test.com", user.Email);
            Assert.IsNull(user.Avatar);
            Assert.IsTrue(eventFired, "Подія OnProfileUpdated не спрацювала.");
        }

        [TestMethod]
        public void UpdateProfile_EmptyNickname_ThrowsArgumentException()
        {
            var user = CreateTestUser();
            Assert.ThrowsExactly<ArgumentException>(() =>
                user.UpdateProfile("", "test@test.com", null));
            Assert.ThrowsExactly<ArgumentException>(() =>
                user.UpdateProfile("   ", "test@test.com", null));
            Assert.ThrowsExactly<ArgumentException>(() =>
                user.UpdateProfile(null!, "test@test.com", null));
        }

        [TestMethod]
        public void UpdateProfile_EmptyEmail_ThrowsArgumentException()
        {
            var user = CreateTestUser();
            Assert.ThrowsExactly<ArgumentException>(() =>
                user.UpdateProfile("Nick", "", null));
        }

        [TestMethod]
        public void UpdateProfile_InvalidEmailFormat_ThrowsArgumentException()
        {
            var user = CreateTestUser();

            Assert.ThrowsExactly<ArgumentException>(() => user.UpdateProfile("Nick", "invalid_email.com", null));
            Assert.ThrowsExactly<ArgumentException>(() => user.UpdateProfile("Nick", "test@domain", null));
            Assert.ThrowsExactly<ArgumentException>(() => user.UpdateProfile("Nick", "te st@domain.com", null));
        }

        [TestMethod]
        public void UpdateProfile_ValidAvatar_UpdatesSuccessfully()
        {
            var user = CreateTestUser();
            var avatar = CreateDummyAvatar(user, "photo.jpg");

            user.UpdateProfile("Nick", "test@test.com", avatar);

            Assert.AreEqual(avatar, user.Avatar);
        }

        [TestMethod]
        public void UpdateProfile_InvalidAvatarExtension_ThrowsArgumentException()
        {
            var user = CreateTestUser();
            var badAvatar = CreateDummyAvatar(user, "document.pdf");

            var exception = Assert.ThrowsExactly<ArgumentException>(() =>
                user.UpdateProfile("Nick", "test@test.com", badAvatar));

            StringAssert.Contains(exception.Message, "Аватар має бути зображенням");
        }

        [TestMethod]
        public void ChangePassword_ValidOldPasswordAndLength_ChangesPasswordAndReturnsTrue()
        {
            var user = CreateTestUser();

            bool result = user.ChangePassword("SecretPass123", "NewSecurePass888");

            Assert.IsTrue(result);
            Assert.AreEqual("NewSecurePass888", user.Password);
        }

        [TestMethod]
        public void ChangePassword_WrongOldPassword_ReturnsFalseAndKeepsOldPassword()
        {
            var user = CreateTestUser();

            bool result = user.ChangePassword("WrongPass", "NewSecurePass888");

            Assert.IsFalse(result);
            Assert.AreEqual("SecretPass123", user.Password);
        }

        [TestMethod]
        public void ChangePassword_NewPasswordTooShort_ReturnsFalse()
        {
            var user = CreateTestUser();

            bool result = user.ChangePassword("SecretPass123", "1234567");

            Assert.IsFalse(result, "Пароль коротший за 8 символів був прийнятий.");
            Assert.AreEqual("SecretPass123", user.Password);
        }

        [TestMethod]
        public void Authenticate_ValidCredentials_ReturnsTrueAndUpdatesLastLogin()
        {
            var user = CreateTestUser();
            var oldDate = user.LastLogin;
            System.Threading.Thread.Sleep(10);

            bool result = user.Authenticate("john_doe", "SecretPass123");

            Assert.IsTrue(result);
            Assert.IsTrue(user.LastLogin > oldDate, "LastLogin не оновився після успішної авторизації.");
        }

        [TestMethod]
        public void Authenticate_InvalidLogin_ReturnsFalse()
        {
            var user = CreateTestUser();
            bool result = user.Authenticate("wrong_login", "SecretPass123");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Authenticate_InvalidPassword_ReturnsFalse()
        {
            var user = CreateTestUser();
            bool result = user.Authenticate("john_doe", "wrong_password");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Logout_ChangesRoleToGuest()
        {
            var user = CreateTestUser();
            user.Logout();

            Assert.AreEqual(UserRole.Guest, user.Role, "Після виходу роль не змінилася на Guest.");
        }

        [TestMethod]
        public void GetPasswordHash_ReturnsCurrentPassword()
        {
            var user = CreateTestUser();
            Assert.AreEqual("SecretPass123", user.GetPasswordHash());
        }

        [TestMethod]
        public void UpdateLastLogin_SetsNewDate()
        {
            var user = CreateTestUser();
            var expectedDate = new DateTime(2025, 1, 1);

            user.UpdateLastLogin(expectedDate);

            Assert.AreEqual(expectedDate, user.LastLogin);
        }

        [TestMethod]
        public void IsAccountActive_ReturnsIsActiveStatus()
        {
            var user = CreateTestUser();
            Assert.IsTrue(user.IsAccountActive());

            user.IsActive = false;
            Assert.IsFalse(user.IsAccountActive());
        }

        [TestMethod]
        public void GetIdentity_ReturnsUserId()
        {
            var user = CreateTestUser();
            Assert.AreEqual(user.Id, user.GetIdentity());
        }
    }
}