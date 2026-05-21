using Microsoft.EntityFrameworkCore;
using Platform.Models;
using Task = Platform.Models.Task;

namespace Platform.Data
{
    public class PlatformDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<StudentResponse> Responses { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<FileModel> Files { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=PlatformDb;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Admin>("Admin")
                .HasValue<Teacher>("Teacher")
                .HasValue<Student>("Student");

            modelBuilder.Entity<Student>()
                .HasMany(s => s.EnrolledCourses)
                .WithMany(c => c.Students)
                .UsingEntity(j => j.ToTable("StudentCourseEnrollments"));

            modelBuilder.Entity<Teacher>()
                .HasMany(t => t.OwnCourses)
                .WithOne(c => c.Owner)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Lessons)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Tasks)
                .WithOne(t => t.Lesson)
                .HasForeignKey(t => t.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId);

            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.AllowedStudents)
                .WithMany()
                .UsingEntity(j => j.ToTable("LessonAllowedStudents"));

            modelBuilder.Entity<Platform.Models.Task>()
                .HasMany(t => t.AllowedStudents)
                .WithMany()
                .UsingEntity(j => j.ToTable("TaskAllowedStudents"));

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .HasMany(c => c.BannedStudents)
                .WithMany()
                .UsingEntity(j => j.ToTable("CourseBannedStudents"));

            modelBuilder.Entity<Course>()
                .HasMany(c => c.PendingStudents)
                .WithMany()
                .UsingEntity(j => j.ToTable("CoursePendingStudents"));


            modelBuilder.Entity<User>()
                .HasOne(u => u.Avatar)
                .WithMany() 
                .HasForeignKey("AvatarId") 
                .OnDelete(DeleteBehavior.SetNull); 

            modelBuilder.Entity<FileModel>()
                .HasOne(f => f.Uploader)
                .WithMany() 
                .HasForeignKey("UploaderId")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}