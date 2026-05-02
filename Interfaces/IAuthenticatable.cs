namespace Platform.Interfaces
{
    public interface IAuthenticatable
    {
        string Login { get; set; }

        string Password { get; set; }

        DateTime LastLogin { get; set; }

        string GetPasswordHash();

        void UpdateLastLogin(DateTime timestamp);

        bool IsAccountActive();

        Guid GetIdentity();
    }
}