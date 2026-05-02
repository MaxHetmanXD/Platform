using System;
using System.Text.RegularExpressions;
using Platform.Interfaces;
using Platform.Enums;

namespace Platform.Models
{
    public class User : IAuthenticatable
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Login { get; set; }

        public string Password { get; set; }
        public string Nickname { get; set; }
        public string Email { get; set; }
        public FileModel? Avatar { get; set; }
        public string Info { get; set; }
        public UserRole Role { get; set; }

        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        public event EventHandler<string>? OnProfileUpdated;

        public User(string login, string password, string nickname, string email, UserRole role)
        {
            Login = login;
            Password = password;
            Nickname = nickname;
            Email = email;
            Role = role;
            Info = string.Empty;
        }

        public void UpdateProfile(string nickname, string email, FileModel? avatar)
        {
            if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Нікнейм та Email не можуть бути порожніми.");
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException("Невірний формат електронної пошти.");
            }

            if (avatar != null)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

                string fileExtension = Path.GetExtension(avatar.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Аватар має бути зображенням (jpg, jpeg, png, webp, gif).");
                }
            }

            Nickname = nickname;
            Email = email;
            Avatar = avatar;

            OnProfileUpdated?.Invoke(this, $"Користувач {Login} оновив свій профіль.");
        }

        public bool ChangePassword(string oldPass, string newPass)
        {
            if (newPass.Length < 8) return false;

            if (Password != oldPass) return false;

            Password = newPass;
            return true;
        }

        public bool Authenticate(string login, string password)
        {
            if (Login == login && Password == password)
            {
                UpdateLastLogin(DateTime.Now);
                return true;
            }
            return false;
        }

        public void Logout()
        {
            Role = UserRole.Guest;
        }

        public string GetPasswordHash()
        {
            return Password;
        }

        public void UpdateLastLogin(DateTime timestamp)
        {
            LastLogin = timestamp;
        }

        public bool IsAccountActive()
        {
            return IsActive;
        }

        public Guid GetIdentity()
        {
            return Id;
        }
    }
}