using System;
using System.Linq;
using Platform.Models;

namespace Platform.Services
{
    public class AuthService
    {
        public User? CurrentUser { get; private set; }
        public DateTime? SessionStartTime { get; private set; }
        public int MinPasswordLength { get; private set; } = 8;

        private readonly PlatformData _platform;

        public event EventHandler<string>? OnPasswordChanged;

        public AuthService(PlatformData platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        public User? Login(string login, string password)
        {
            var user = _platform.AllUsers.FirstOrDefault(u => u.Login == login);

            if (user == null)
            {
                return null;
            }

            if (user.GetPasswordHash() == password)
            {
                user.UpdateLastLogin(DateTime.Now);
                CurrentUser = user;
                SessionStartTime = DateTime.Now;

                return user;
            }

            return null;
        }

        public void Logout()
        {
            CurrentUser = null;
            SessionStartTime = null;
        }

        public bool ChangePassword(User user, string oldPass, string newPass)
        {
            if (user == null) return false;

            Platform.Interfaces.IAuthenticatable authUser = user;
            if (authUser.GetPasswordHash() != oldPass)
            {
                return false;
            }

            try
            {
                user.ChangePassword(newPass);

                OnPasswordChanged?.Invoke(this, $"Користувач {user.Login} успішно змінив пароль.");
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public bool CheckUniqueness(string login, string email)
        {
            bool isDuplicate = _platform.AllUsers.Any(u =>
                u.Login.Equals(login, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(u.Email) && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            );

            return !isDuplicate;
        }
    }
}