using Platform.Data;
using Platform.Models;
using System.Linq;

namespace Platform.Data
{
    public static class DatabaseInitializer
    {
        public static void SeedData()
        {
            using (var db = new PlatformDbContext())
            {
                if (!db.Users.Any())
                {
                    var firstAdmin = new Admin(
                        login: "admin",
                        password: "password123",
                        nickname: "SuperAdmin",
                        email: "admin@platform.com"
                    );

                    db.Users.Add(firstAdmin);

                    db.SaveChanges();
                }
            }
        }
    }
}