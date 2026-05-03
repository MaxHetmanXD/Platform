using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using Platform.Services;
using System;

namespace Platform.Tests
{
    [TestClass]
    public class AuthServiceTests
    {
        private PlatformData CreatePlatformWithTestUser()
        {
            var platform = new PlatformData();
            var user = new Student("test_user", "ValidPass123", "Nick", "test@domain.com", "Група-1");
            platform.AllUsers.Add(user);
            return platform;
        }

        [TestMethod]
        public void Constructor_ValidPlatform_CreatesInstance()
        {
            var platform = new PlatformData();
            var authService = new AuthService(platform);

            Assert.IsNotNull(authService);
            Assert.IsNull(authService.CurrentUser);
            Assert.IsNull(authService.SessionStartTime);
            Assert.AreEqual(8, authService.MinPasswordLength);
        }

        [TestMethod]
        public void Constructor_NullPlatform_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new AuthService(null!));
        }

        [TestMethod]
        public void Login_ValidCredentials_SetsSessionAndReturnsUser()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            var result = authService.Login("test_user", "ValidPass123");

            Assert.IsNotNull(result);
            Assert.AreEqual("test_user", result.Login);
            Assert.AreEqual(result, authService.CurrentUser, "CurrentUser має бути встановлений.");
            Assert.IsNotNull(authService.SessionStartTime, "SessionStartTime має бути встановлений.");
            Assert.IsTrue((DateTime.Now - result.LastLogin).TotalSeconds < 2);
        }

        [TestMethod]
        public void Login_InvalidLogin_ReturnsNullAndDoesNotSetSession()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            var result = authService.Login("wrong_user", "ValidPass123");

            Assert.IsNull(result);
            Assert.IsNull(authService.CurrentUser);
            Assert.IsNull(authService.SessionStartTime);
        }

        [TestMethod]
        public void Login_InvalidPassword_ReturnsNullAndDoesNotSetSession()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            var result = authService.Login("test_user", "WrongPass123");

            Assert.IsNull(result);
            Assert.IsNull(authService.CurrentUser);
            Assert.IsNull(authService.SessionStartTime);
        }

        [TestMethod]
        public void Logout_ClearsSessionData()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            authService.Login("test_user", "ValidPass123");
            Assert.IsNotNull(authService.CurrentUser);

            authService.Logout();

            Assert.IsNull(authService.CurrentUser, "CurrentUser має бути null після виходу.");
            Assert.IsNull(authService.SessionStartTime, "SessionStartTime має бути null після виходу.");
        }

        [TestMethod]
        public void ChangePassword_ValidData_ReturnsTrueAndInvokesEvent()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);
            var user = platform.AllUsers[0];

            bool eventFired = false;
            authService.OnPasswordChanged += (sender, message) =>
            {
                eventFired = true;
                StringAssert.Contains(message, user.Login);
            };

            bool result = authService.ChangePassword(user, "ValidPass123", "NewSecurePass888");

            Assert.IsTrue(result);
            Assert.IsTrue(eventFired, "Подія OnPasswordChanged не спрацювала.");
            Assert.AreEqual("NewSecurePass888", user.Password);
        }

        [TestMethod]
        public void ChangePassword_InvalidOldPassword_ReturnsFalseAndDoesNotInvokeEvent()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);
            var user = platform.AllUsers[0];

            bool eventFired = false;
            authService.OnPasswordChanged += (sender, message) => eventFired = true;

            bool result = authService.ChangePassword(user, "WrongPass", "NewSecurePass888");

            Assert.IsFalse(result);
            Assert.IsFalse(eventFired, "Подія не повинна спрацьовувати при невдалій спробі.");
            Assert.AreEqual("ValidPass123", user.Password, "Пароль не повинен змінитися.");
        }

        [TestMethod]
        public void ChangePassword_NullUser_ReturnsFalse()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            bool result = authService.ChangePassword(null!, "Old", "New");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckUniqueness_UniqueData_ReturnsTrue()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            bool result = authService.CheckUniqueness("new_login", "new_email@domain.com");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CheckUniqueness_DuplicateLogin_ReturnsFalse()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            bool result = authService.CheckUniqueness("TEST_USER", "new_email@domain.com");

            Assert.IsFalse(result, "Має повернути false, бо логін вже існує.");
        }

        [TestMethod]
        public void CheckUniqueness_DuplicateEmail_ReturnsFalse()
        {
            var platform = CreatePlatformWithTestUser();
            var authService = new AuthService(platform);

            bool result = authService.CheckUniqueness("new_login", "TEST@domain.com");

            Assert.IsFalse(result, "Має повернути false, бо email вже існує.");
        }
    }
}