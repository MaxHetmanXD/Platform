using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Models;
using Platform.Models.ViewModels;
using Platform.Enums;
using Platform.Services;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using Task = Platform.Models.Task;

namespace Platform.Controllers
{
    public class CourseController : Controller
    {
        private readonly PlatformDbContext _context;
        private readonly FileManager _fileManager;
        private readonly NotificationService _notificationService;

        public CourseController(PlatformDbContext context, FileManager fileManager, NotificationService notificationService)
        {
            _context = context;
            _fileManager = fileManager;
            _notificationService = notificationService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Banner)
                .Include(c => c.BannedStudents)
                .Include(c => c.PendingStudents)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var vm = new CourseDetailsViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                BannerId = course.Banner?.Id,
                Category = course.Category,
                IsPublic = course.IsPublic,
                IsGuest = !User.Identity!.IsAuthenticated
            };

            if (!vm.IsGuest)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out Guid userId);

                vm.IsOwnerOrAdmin = (course.OwnerId == userId) || User.IsInRole("Admin");

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser is Student student)
                {
                    vm.IsUserBanned = course.BannedStudents.Contains(student);
                    vm.IsUserPending = course.PendingStudents.Contains(student);
                    vm.IsUserEnrolled = course.Students.Contains(student);
                    vm.IsStudent = true;
                }
            }

            return View(vm);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> Enroll(Guid id, string? coursePassword)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var student = await _context.Users.OfType<Student>().FirstOrDefaultAsync(s => s.Id == userId);
            var course = await _context.Courses
                .Include(c => c.BannedStudents)
                .Include(c => c.PendingStudents)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (student == null || course == null) return NotFound();

            if (course.BannedStudents.Contains(student))
            {
                TempData["Error"] = "Ви не можете вступити, оскільки вас заблоковано на цьому курсі.";
                return RedirectToAction("Details", new { id = id });
            }

            bool success = student.RequestEnrollment(course, coursePassword);

            if (success)
            {
                await _context.SaveChangesAsync();
                TempData["Message"] = "Заявку на вступ успішно надіслано! Очікуйте підтвердження викладача.";
            }
            else
            {
                TempData["Error"] = "Неправильний пароль або вступ наразі неможливий.";
            }

            return RedirectToAction("Details", new { id = id });
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> AutoCreate()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var teacher = await _context.Users.OfType<Teacher>().FirstOrDefaultAsync(t => t.Id == userId);
            if (teacher == null) return Forbid();

            string randomId = Guid.NewGuid().ToString().Substring(0, 4);
            var newCourse = teacher.CreateCourse($"Новий курс {randomId}", "Додайте опис вашого курсу тут...", CourseCategory.Programming, null);

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();

            return RedirectToAction("Manage", new { id = newCourse.Id });
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses.Include(c => c.Banner).FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var vm = new CourseManagementViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Pass = course.Pass,
                IsPublic = course.IsPublic,
                Category = course.Category,
                BannerId = course.Banner?.Id
            };

            return View(vm);
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> Manage(CourseManagementViewModel model, IFormFile? bannerFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses.Include(c => c.Banner).FirstOrDefaultAsync(c => c.Id == model.Id);
            if (course == null) return NotFound();

            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            FileModel? newBanner = course.Banner;
            if (bannerFile != null && currentUser != null)
            {
                newBanner = await _fileManager.SaveFileAsync(bannerFile, currentUser);
                if (newBanner != null) _context.Files.Add(newBanner);
            }

            bool nameExists = await _context.Courses.AnyAsync(c => c.Title.ToLower() == model.Title.ToLower() && c.Id != model.Id);
            if (nameExists)
            {
                TempData["Error"] = "Курс із такою назвою вже існує. Оберіть іншу.";

                return View("Manage", model);
            }

            if (isAdmin)
            {
                ((Admin)currentUser!).EditCourse(course, model.Title, model.Description, newBanner, model.Category, model.Pass);
            }
            else
            {
                ((Teacher)currentUser!).EditCourse(course, model.Title, model.Description, newBanner, model.Category, model.Pass);
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Налаштування курсу успішно збережено!";

            return RedirectToAction("Manage", new { id = model.Id });
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (isAdmin) ((Admin)currentUser!).DeleteCourse(course);
            else ((Teacher)currentUser!).DeleteCourse(course);

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return RedirectToAction(isAdmin ? "Courses" : "MyCourses", isAdmin ? "Admin" : "Home");
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Participants(Guid id, string? searchString)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses
                .Include(c => c.Students)
                .Include(c => c.PendingStudents)
                .Include(c => c.BannedStudents)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var owner = await _context.Users.OfType<Teacher>().FirstOrDefaultAsync(u => u.Id == course.OwnerId);
            if (owner == null) return NotFound();

            bool isStudentEnrolled = course.Students.Any(s => s.Id == userId);
            bool isOwner = (course.OwnerId == userId);

            if (!isStudentEnrolled && !isOwner && !isAdmin) return Forbid();

            var vm = new CourseParticipantsViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                IsOwnerOrAdmin = isOwner || isAdmin,
                Owner = new ParticipantItemViewModel
                {
                    Id = owner.Id,
                    Nickname = owner.Nickname,
                    AvatarId = owner.AvatarId,
                    SpecialField = owner.Position
                }
            };

            var enrolled = course.Students.AsEnumerable();
            var pending = course.PendingStudents.AsEnumerable();
            var banned = course.BannedStudents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var q = searchString.ToLower();
                enrolled = enrolled.Where(s => s.Nickname.ToLower().Contains(q) || s.Group.ToLower().Contains(q));
                pending = pending.Where(s => s.Nickname.ToLower().Contains(q) || s.Group.ToLower().Contains(q));
                banned = banned.Where(s => s.Nickname.ToLower().Contains(q) || s.Group.ToLower().Contains(q));
            }

            vm.EnrolledStudents = enrolled.Select(s => new ParticipantItemViewModel { Id = s.Id, Nickname = s.Nickname, AvatarId = s.AvatarId, SpecialField = s.Group }).ToList();

            if (vm.IsOwnerOrAdmin)
            {
                vm.PendingStudents = pending.Select(s => new ParticipantItemViewModel { Id = s.Id, Nickname = s.Nickname, AvatarId = s.AvatarId, SpecialField = s.Group }).ToList();
                vm.BannedStudents = banned.Select(s => new ParticipantItemViewModel { Id = s.Id, Nickname = s.Nickname, AvatarId = s.AvatarId, SpecialField = s.Group }).ToList();
            }

            ViewData["CurrentSearch"] = searchString;
            return View(vm);
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> ManageParticipant(Guid courseId, Guid studentId, string actionType)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses
                .Include(c => c.Students).Include(c => c.PendingStudents).Include(c => c.BannedStudents)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();
            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var student = await _context.Users.OfType<Student>().FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return NotFound();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (isAdmin)
            {
                var admin = (Admin)currentUser!;
                switch (actionType)
                {
                    case "Accept": admin.AcceptStudent(student, course); break;
                    case "Reject": admin.RejectStudent(student, course); break;
                    case "Ban": admin.BanStudent(student, course); break;
                    case "Remove": admin.RemoveStudent(student, course); break;
                    case "Unban": admin.UnbanStudent(student, course); break;
                }
            }
            else
            {
                var teacher = (Teacher)currentUser!;
                switch (actionType)
                {
                    case "Accept": teacher.AcceptStudent(student, course); break;
                    case "Reject": teacher.RejectStudent(student, course); break;
                    case "Ban": teacher.BanStudent(student, course); break;
                    case "Remove": teacher.RemoveStudent(student, course); break;
                    case "Unban": teacher.UnbanStudent(student, course); break;
                }
            }

            string actionWord = actionType switch
            {
                "Accept" => "enrolled",
                "Reject" => "excluded",
                "Ban" => "excluded",
                "Remove" => "excluded",
                "Unban" => "enrolled",
                _ => ""
            };

            if (!string.IsNullOrEmpty(actionWord))
            {
                _notificationService.NotifyEnrollmentChange(student, course, actionWord);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Participants", new { id = courseId });
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Lessons(Guid id, string? searchString, string? sortOrder)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses
                .Include(c => c.Students)
                .Include(c => c.Lessons).ThenInclude(l => l.AllowedStudents)
                .Include(c => c.Lessons).ThenInclude(l => l.Attachments)
                .Include(c => c.Lessons).ThenInclude(l => l.Tasks).ThenInclude(t => t.AllowedStudents)
                .Include(c => c.Lessons).ThenInclude(l => l.Tasks).ThenInclude(t => t.Responses).ThenInclude(r => r.FinalGrade)
                .Include(c => c.Lessons).ThenInclude(l => l.Tasks).ThenInclude(t => t.Responses).ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            bool isStudentEnrolled = course.Students.Any(s => s.Id == userId);
            bool isOwner = (course.OwnerId == userId);

            if (!isStudentEnrolled && !isOwner && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var student = currentUser as Student;

            var vm = new CourseLessonsViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                IsOwnerOrAdmin = isOwner || isAdmin
            };

            IEnumerable<Lesson> availableLessons = course.Lessons;

            if (student != null)
            {
                availableLessons = student.GetAvailableContent(course);
            }

            foreach (var lesson in availableLessons)
            {
                var lessonVm = new LessonItemViewModel
                {
                    Id = lesson.Id,
                    Title = lesson.Title
                };

                if (student != null) lessonVm.AverageGrade = student.GetLessonAverage(lesson);

                lessonVm.Files = lesson.Attachments.Select(f => new FileItemViewModel
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    ReadableSize = f.GetReadableSize()
                }).ToList();

                IEnumerable<Platform.Models.Task> availableTasks = lesson.Tasks;

                if (student != null)
                {
                    availableTasks = lesson.Tasks.Where(t => t.IsVisible || t.AllowedStudents.Any(s => s.Id == userId));
                }

                foreach (var task in availableTasks)
                {
                    var taskVm = new TaskItemViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Deadline = task.Deadline,
                        MaxPoints = task.MaxPoints
                    };

                    if (student != null)
                    {
                        taskVm.Status = task.GetTaskStatus(student);
                        var grade = task.GetStudentGrade(student);
                        taskVm.GradeString = grade != null ? $"{grade.Value}/{task.MaxPoints}" : $"-/{task.MaxPoints}";
                    }

                    lessonVm.Tasks.Add(taskVm);
                }

                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    var q = searchString.ToLower();
                    bool matchLesson = lessonVm.Title.ToLower().Contains(q);
                    lessonVm.Tasks = lessonVm.Tasks.Where(t => t.Title.ToLower().Contains(q)).ToList();

                    if (!matchLesson && !lessonVm.Tasks.Any()) continue;
                }

                vm.Lessons.Add(lessonVm);
            }

            if (sortOrder == "alphabetical") vm.Lessons = vm.Lessons.OrderBy(l => l.Title).ToList();
            else if (sortOrder == "grade" && student != null) vm.Lessons = vm.Lessons.OrderByDescending(l => l.AverageGrade ?? 0).ToList();

            foreach (var l in vm.Lessons)
            {
                if (sortOrder == "deadline") l.Tasks = l.Tasks.OrderBy(t => t.Deadline ?? DateTime.MaxValue).ToList();
                else if (sortOrder == "status" && student != null) l.Tasks = l.Tasks.OrderBy(t => t.Status).ToList();
                else if (sortOrder == "alphabetical") l.Tasks = l.Tasks.OrderBy(t => t.Title).ToList();
            }

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            return View(vm);
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> AutoCreateLesson(Guid courseId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();
            if (course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            Lesson newLesson;
            string randomId = Guid.NewGuid().ToString().Substring(0, 4);

            if (isAdmin) newLesson = ((Admin)currentUser!).CreateLesson(course, $"Новий урок {randomId}", "Опис вашого уроку...");
            else newLesson = ((Teacher)currentUser!).CreateLesson(course, $"Новий урок {randomId}", "Опис вашого уроку...");

            newLesson.IsPublic = false;
            newLesson.SetAccessibility(new List<Student>());

            course.AddLesson(newLesson);
            _context.Lessons.Add(newLesson);

            await _context.SaveChangesAsync();
            return RedirectToAction("Lesson", new { id = newLesson.Id });
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Lesson(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var lesson = await _context.Lessons
                .Include(l => l.Course).ThenInclude(c => c.Students)
                .Include(l => l.Attachments)
                .Include(l => l.Tasks).ThenInclude(t => t.Responses).ThenInclude(r => r.FinalGrade)
                .Include(l => l.AllowedStudents)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            bool isStudentEnrolled = lesson.Course.Students.Any(s => s.Id == userId);
            bool isOwner = (lesson.Course.OwnerId == userId);
            if (!isStudentEnrolled && !isOwner && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var student = currentUser as Student;

            if (student != null && !lesson.IsPublic && !lesson.AllowedStudents.Any(s => s.Id == student.Id))
            {
                return Forbid();
            }

            var vm = new LessonDetailsViewModel
            {
                CourseId = lesson.Course.Id,
                CourseTitle = lesson.Course.Title,
                LessonId = lesson.Id,
                LessonTitle = lesson.Title,
                LessonInfo = lesson.TheoryContent,
                IsPublic = lesson.IsPublic,
                IsOwnerOrAdmin = isOwner || isAdmin
            };

            if (student != null) vm.AverageGrade = student.GetLessonAverage(lesson);

            vm.Files = lesson.GetAttachments().Select(f => new FileItemViewModel { Id = f.Id, FileName = f.FileName, ReadableSize = f.GetReadableSize() }).ToList();

            IEnumerable<Task> availableTasks = lesson.Tasks;
            if (student != null) availableTasks = lesson.GetAccessibleTasks(student);

            foreach (var task in availableTasks)
            {
                var taskVm = new TaskItemViewModel { Id = task.Id, Title = task.Title, Deadline = task.Deadline, MaxPoints = task.MaxPoints };
                if (student != null)
                {
                    taskVm.Status = task.GetTaskStatus(student);
                    var grade = task.GetStudentGrade(student);
                    taskVm.GradeString = grade != null ? $"{grade.Value}/{task.MaxPoints}" : $"-/{task.MaxPoints}";
                }
                vm.Tasks.Add(taskVm);
            }

            if (vm.IsOwnerOrAdmin)
            {
                vm.CourseStudents = lesson.Course.Students.Select(s => new ParticipantItemViewModel { Id = s.Id, Nickname = s.Nickname, SpecialField = s.Group }).ToList();
                vm.AllowedStudentIds = lesson.AllowedStudents.Select(s => s.Id).ToList();

                var stats = lesson.GetSubmissionStats();
                ViewData["CheckedCount"] = stats["Checked"];
                ViewData["PendingCount"] = stats["Pending"];
            }

            return View(vm);
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> EditLesson(Guid lessonId, string title, string info, bool isPublic, List<Guid> allowedStudentIds, List<IFormFile> newFiles, List<Guid> filesToRemove)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var lesson = await _context.Lessons
                .Include(l => l.Course).ThenInclude(c => c.Students)
                .Include(l => l.Attachments)
                .Include(l => l.AllowedStudents)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();
            if (lesson.Course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (filesToRemove != null && filesToRemove.Any())
            {
                var filesToDelete = lesson.Attachments.Where(f => filesToRemove.Contains(f.Id)).ToList();
                foreach (var file in filesToDelete)
                {
                    lesson.RemoveFile(file);
                    _fileManager.DeleteFile(file.LocalPath);
                    _context.Files.Remove(file);
                }
            }

            if (newFiles != null && newFiles.Any())
            {
                string[] allowedExtensions = { ".pdf", ".docx", ".doc", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".png", ".jpg", ".txt" };
                foreach (var file in newFiles)
                {
                    var tempFileModel = new FileModel(file.FileName, file.Length, currentUser!);
                    if (!tempFileModel.Validate(_fileManager.MaxFileSize, allowedExtensions))
                    {
                        TempData["Error"] = $"Файл '{file.FileName}' не завантажено: неприпустимий формат або перевищено ліміт розміру.";
                        continue;
                    }

                    try
                    {
                        var savedFile = await _fileManager.SaveFileAsync(file, currentUser!);
                        if (savedFile != null)
                        {
                            lesson.AttachFile(savedFile);
                            _context.Files.Add(savedFile);
                        }
                    }
                    catch (InvalidOperationException) { }
                }
            }

            try
            {
                lesson.UpdateContent(title, info ?? string.Empty, lesson.Attachments.ToList());
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Lesson", new { id = lessonId });
            }

            bool finalIsPublic = isPublic;

            List<Student> finalStudentsList = new List<Student>();
            if (finalIsPublic)
            {
                finalStudentsList = lesson.Course.Students.ToList();
            }
            else if (allowedStudentIds != null && allowedStudentIds.Any())
            {
                finalStudentsList = lesson.Course.Students.Where(s => allowedStudentIds.Contains(s.Id)).ToList();
            }

            if (isAdmin) ((Admin)currentUser!).EditLesson(lesson, lesson.Title, lesson.TheoryContent, finalIsPublic, finalStudentsList);
            else ((Teacher)currentUser!).EditLesson(lesson, lesson.Title, lesson.TheoryContent, finalIsPublic, finalStudentsList);

            if (isAdmin)
            {
                var currentAdmin = currentUser as Admin;
                currentAdmin?.SetAccessibility(lesson, finalStudentsList);
            }
            else
            {
                lesson.SetAccessibility(finalStudentsList);
            }

            lesson.IsPublic = finalIsPublic;

            if (finalStudentsList.Any())
            {
                _notificationService.NotifyNewContent(lesson.Course, lesson, finalStudentsList);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Lesson", new { id = lessonId });
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteLesson(Guid lessonId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var lesson = await _context.Lessons.Include(l => l.Course).ThenInclude(c => c.Lessons).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return NotFound();
            if (lesson.Course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Guid courseId = lesson.Course.Id;

            lesson.Course.RemoveLesson(lesson);

            if (isAdmin) ((Admin)currentUser!).DeleteLesson(lesson.Course, lesson);
            else ((Teacher)currentUser!).DeleteLesson(lesson.Course, lesson);

            _context.Lessons.Remove(lesson);

            await _context.SaveChangesAsync();
            return RedirectToAction("Lessons", new { id = courseId });
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Grades(Guid id, string? searchString, string? sortOrder, Guid? selectedStudentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses
                .Include(c => c.Students)
                .Include(c => c.Lessons).ThenInclude(l => l.AllowedStudents)
                .Include(c => c.Lessons).ThenInclude(l => l.Tasks).ThenInclude(t => t.AllowedStudents)
                .Include(c => c.Lessons).ThenInclude(l => l.Tasks).ThenInclude(t => t.Responses).ThenInclude(r => r.FinalGrade)
                .Include(c => c.Lessons).ThenInclude(l => l.Tasks).ThenInclude(t => t.Responses).ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            bool isStudentEnrolled = course.Students.Any(s => s.Id == userId);
            bool isOwner = (course.OwnerId == userId);
            if (!isStudentEnrolled && !isOwner && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            var vm = new CourseGradesViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                IsOwnerOrAdmin = isOwner || isAdmin,
                SelectedStudentId = selectedStudentId
            };

            List<Student> studentsToShow = new List<Student>();
            if (vm.IsOwnerOrAdmin)
            {
                vm.AllCourseStudents = course.Students.Select(s => new ParticipantItemViewModel { Id = s.Id, Nickname = s.Nickname }).ToList();

                if (selectedStudentId.HasValue && selectedStudentId.Value != Guid.Empty)
                {
                    var target = course.Students.FirstOrDefault(s => s.Id == selectedStudentId.Value);
                    if (target != null) studentsToShow.Add((Student)target);
                }
                else
                {
                    studentsToShow = course.Students.Select(s => (Student)s).ToList();
                }
            }
            else
            {
                var me = course.Students.FirstOrDefault(s => s.Id == userId);
                if (me != null) studentsToShow.Add((Student)me);
            }

            foreach (var student in studentsToShow)
            {
                var studentVm = new StudentGradesViewModel
                {
                    StudentId = student.Id,
                    Nickname = student.Nickname,
                    AvatarId = student.AvatarId
                };

                List<Platform.DTOs.TaskStatusDTO>? basicAnalytics = null;
                if (currentUser is Teacher teacher)
                {
                    basicAnalytics = teacher.GetStudentDetails(student, course);
                }
                else if (currentUser is Student currentStudent && currentStudent.Id == student.Id)
                {
                    basicAnalytics = currentStudent.GetTaskAnalytics(course);
                }

                double totalGrade = 0;
                int gradedTasksCount = 0;

                var availableLessons = student.GetAvailableContent(course);
                foreach (var lesson in availableLessons)
                {
                    var availableTasks = lesson.GetAccessibleTasks(student);
                    foreach (var task in availableTasks)
                    {
                        if (!string.IsNullOrWhiteSpace(searchString) && !task.Title.ToLower().Contains(searchString.ToLower()))
                            continue;

                        var taskVm = new TaskGradeViewModel
                        {
                            TaskId = task.Id,
                            Title = task.Title,
                            Deadline = task.Deadline
                        };

                        var analyticsItem = basicAnalytics?.FirstOrDefault(a => a.TaskTitle == task.Title);
                        if (analyticsItem != null)
                        {
                            taskVm.Status = analyticsItem.Status;
                        }
                        else
                        {
                            taskVm.Status = task.GetTaskStatus(student);
                        }

                        var grade = task.GetStudentGrade(student);

                        if (grade != null)
                        {
                            taskVm.GradeValue = grade.Value;
                            taskVm.GradeString = $"{grade.Value}/{task.MaxPoints}";
                            totalGrade += grade.Value;
                            gradedTasksCount++;
                        }
                        else
                        {
                            taskVm.GradeValue = null;
                            taskVm.GradeString = $"-/{task.MaxPoints}";
                        }

                        studentVm.Tasks.Add(taskVm);
                    }
                }

                studentVm.AverageGrade = gradedTasksCount > 0 ? totalGrade / gradedTasksCount : 0;

                if (sortOrder == "deadline") studentVm.Tasks = studentVm.Tasks.OrderBy(t => t.Deadline ?? DateTime.MaxValue).ToList();
                else if (sortOrder == "alphabetical") studentVm.Tasks = studentVm.Tasks.OrderBy(t => t.Title).ToList();
                else if (sortOrder == "status") studentVm.Tasks = studentVm.Tasks.OrderBy(t => t.Status).ToList();
                else if (sortOrder == "grade") studentVm.Tasks = studentVm.Tasks.OrderByDescending(t => t.GradeValue ?? -1).ToList();
                else studentVm.Tasks = studentVm.Tasks.OrderByDescending(t => t.TaskId).ToList();

                if (studentVm.Tasks.Any() || string.IsNullOrWhiteSpace(searchString))
                {
                    vm.Students.Add(studentVm);
                }
            }

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            return View(vm);
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> AutoCreateTask(Guid lessonId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var lesson = await _context.Lessons
                .Include(l => l.Course).ThenInclude(c => c.Students)
                .Include(l => l.Tasks)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();
            if (lesson.Course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            Platform.Models.Task newTask;
            string randomId = Guid.NewGuid().ToString().Substring(0, 4);

            if (isAdmin) newTask = ((Admin)currentUser!).CreateTask(lesson, $"Нове завдання {randomId}", "Опис...", 100, DateTime.Now.AddDays(7), new List<FileModel>());
            else newTask = ((Teacher)currentUser!).CreateTask(lesson, $"Нове завдання {randomId}", "Опис...", 100, DateTime.Now.AddDays(7), new List<FileModel>());

            newTask.IsVisible = false;
            newTask.SetAccessibility(new List<Student>());

            lesson.AddTask(newTask);
            _context.Tasks.Add(newTask);

            await _context.SaveChangesAsync();
            return RedirectToAction("TaskDetails", new { id = newTask.Id });
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> EditTask(Guid taskId, string title, string info, int maxPoints, DateTime? deadline, bool isVisible, List<Guid> allowedStudentIds, List<IFormFile> newFiles, List<Guid> filesToRemove)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var task = await _context.Tasks
                .Include(t => t.Lesson).ThenInclude(l => l.Course).ThenInclude(c => c.Students)
                .Include(t => t.Attachments)
                .Include(t => t.AllowedStudents)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return NotFound();
            if (task.Lesson.Course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (filesToRemove != null && filesToRemove.Any())
            {
                var filesToDelete = task.Attachments.Where(f => filesToRemove.Contains(f.Id)).ToList();
                foreach (var file in filesToDelete)
                {
                    task.RemoveFile(file);
                    _fileManager.DeleteFile(file.LocalPath);
                    _context.Files.Remove(file);
                }
            }

            if (newFiles != null && newFiles.Any())
            {
                string[] allowedExtensions = { ".pdf", ".docx", ".doc", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".png", ".jpg", ".txt" };
                foreach (var file in newFiles)
                {
                    var tempFileModel = new FileModel(file.FileName, file.Length, currentUser!);
                    if (!tempFileModel.Validate(_fileManager.MaxFileSize, allowedExtensions))
                    {
                        TempData["Error"] = $"Файл '{file.FileName}' не завантажено: неприпустимий формат або перевищено ліміт розміру.";
                        continue;
                    }

                    try
                    {
                        var savedFile = await _fileManager.SaveFileAsync(file, currentUser!);
                        if (savedFile != null)
                        {
                            task.AttachFile(savedFile);
                            _context.Files.Add(savedFile);
                        }
                    }
                    catch (InvalidOperationException) { }
                }
            }

            DateTime finalDeadline = deadline ?? DateTime.MaxValue;

            bool finalIsVisible = isVisible;
            task.IsVisible = finalIsVisible;

            List<Student> finalStudentsList = new List<Student>();
            if (finalIsVisible)
            {
                finalStudentsList = task.Lesson.Course.Students.ToList();
            }
            else if (allowedStudentIds != null && allowedStudentIds.Any())
            {
                finalStudentsList = task.Lesson.Course.Students.Where(s => allowedStudentIds.Contains(s.Id)).ToList();
            }

            try
            {
                if (isAdmin)
                {
                    ((Admin)currentUser!).EditTask(task, title, info ?? string.Empty, maxPoints, deadline, task.Attachments.ToList());

                    var currentAdmin = currentUser as Admin;
                    currentAdmin?.SetAccessibility(task, finalStudentsList);
                }
                else
                {
                    ((Teacher)currentUser!).EditTask(task, title, info ?? string.Empty, maxPoints, deadline, task.Attachments.ToList());

                    task.SetAccessibility(finalStudentsList);
                }
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("TaskDetails", new { id = taskId });
            }

            if (finalStudentsList.Any())
            {
                _notificationService.NotifyNewContent(task.Lesson.Course, task, finalStudentsList);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteTask(Guid taskId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var task = await _context.Tasks.Include(t => t.Lesson).ThenInclude(l => l.Course).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return NotFound();
            if (task.Lesson.Course.OwnerId != userId && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Guid lessonId = task.Lesson.Id;

            if (isAdmin) ((Admin)currentUser!).DeleteTask(task.Lesson, task);
            else ((Teacher)currentUser!).DeleteTask(task.Lesson, task);

            await _context.SaveChangesAsync();
            return RedirectToAction("Lesson", new { id = lessonId });
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> TaskDetails(Guid id, Guid? studentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);
            var isAdmin = User.IsInRole("Admin");

            var task = await _context.Tasks
                .Include(t => t.Lesson).ThenInclude(l => l.Course).ThenInclude(c => c.Students)
                .Include(t => t.Lesson).ThenInclude(l => l.AllowedStudents)
                .Include(t => t.Attachments)
                .Include(t => t.AllowedStudents)
                .Include(t => t.Responses).ThenInclude(r => r.Author)
                .Include(t => t.Responses).ThenInclude(r => r.AttachedFiles)
                .Include(t => t.Responses).ThenInclude(r => r.FinalGrade)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            bool isStudentEnrolled = task.Lesson.Course.Students.Any(s => s.Id == userId);
            bool isOwner = (task.Lesson.Course.OwnerId == userId);
            if (!isStudentEnrolled && !isOwner && !isAdmin) return Forbid();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var student = currentUser as Student;

            if (student != null)
            {
                if (!task.IsVisible && !task.CheckSubmissionEligibility(student)) return Forbid();

                studentId = student.Id;
            }

            if (isAdmin)
            {
                studentId = null;
            }

            var vm = new TaskDetailsViewModel
            {
                CourseId = task.Lesson.Course.Id,
                CourseTitle = task.Lesson.Course.Title,
                LessonId = task.Lesson.Id,
                TaskId = task.Id,
                TaskTitle = task.Title,
                TaskInfo = task.TheoryContent,
                MaxPoints = task.MaxPoints,
                Deadline = task.Deadline,
                IsPublic = task.IsVisible,
                IsOwnerOrAdmin = isOwner || isAdmin,
                IsStudent = (student != null)
            };

            vm.TaskFiles = task.GetAttachments().Select(f => new FileItemViewModel { Id = f.Id, FileName = f.FileName, ReadableSize = f.GetReadableSize() }).ToList();

            if (studentId.HasValue)
            {
                var response = task.Responses.FirstOrDefault(r => r.Author.Id == studentId.Value);
                if (response == null)
                {
                    var targetStudent = await _context.Users.OfType<Student>().FirstOrDefaultAsync(s => s.Id == studentId.Value);
                    if (targetStudent != null)
                    {
                        response = new StudentResponse(targetStudent, task);
                        _context.Responses.Add(response);
                        await _context.SaveChangesAsync();
                    }
                }

                if (response != null)
                {
                    vm.ResponseStudentId = response.Author.Id;
                    vm.ResponseStudentNickname = response.Author.Nickname;
                    vm.ResponseStatus = response.Status.ToString();
                    vm.CurrentGrade = response.FinalGrade?.Value;
                    vm.ResponseFiles = response.AttachedFiles.Select(f => new FileItemViewModel { Id = f.Id, FileName = f.FileName, ReadableSize = f.GetReadableSize() }).ToList();
                }
            }

            if (vm.IsOwnerOrAdmin)
            {
                var studentsWhoCanAccessLesson = task.Lesson.IsPublic ? task.Lesson.Course.Students : task.Lesson.AllowedStudents;
                vm.AvailableStudentsForTask = studentsWhoCanAccessLesson.Select(s => new ParticipantItemViewModel { Id = s.Id, Nickname = s.Nickname }).ToList();
                vm.AllowedStudentIds = task.AllowedStudents.Select(s => s.Id).ToList();
            }

            return View(vm);
        }


        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> ToggleTaskStatus(Guid taskId, string action)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var response = await _context.Responses
                .Include(r => r.TargetTask)
                    .ThenInclude(t => t.Lesson)
                        .ThenInclude(l => l.Course)
                            .ThenInclude(c => c.Owner)
                .Include(r => r.FinalGrade)
                .Include(r => r.Author)
                .FirstOrDefaultAsync(r => r.TargetTask.Id == taskId && r.Author.Id == userId);

            if (response == null) return NotFound("Відповідь не знайдена.");

            var student = response.Author as Student;
            if (student == null) return BadRequest("Користувач не є студентом.");

            if (action == "Send")
            {
                try
                {
                    student.SubmitTask(response);

                    if (response.TargetTask.IsOverdue())
                    {
                        var currentCourse = response.TargetTask.Lesson.Course;
                        _notificationService.NotifyTeacherOfSubmission(student, response.TargetTask, currentCourse);
                        TempData["Warning"] = "Завдання відправлено, але дедлайн вже минув!";
                    }
                    else
                    {
                        var currentCourse = response.TargetTask.Lesson.Course;
                        _notificationService.NotifyTeacherOfSubmission(student, response.TargetTask, currentCourse);
                        TempData["Message"] = "Завдання успішно відправлено на перевірку!";
                    }
                }
                catch (InvalidOperationException ex)
                {
                    TempData["Error"] = ex.Message;
                }
            }
            else if (action == "Return")
            {
                response.Status = SubmissionStatus.Rejected;
                if (response.FinalGrade != null)
                {
                    _context.Grades.Remove(response.FinalGrade);
                    response.FinalGrade = null;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<IActionResult> GradeTask(Guid taskId, Guid studentId, int gradeValue, string action)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var response = await _context.Responses
                .Include(r => r.TargetTask)
                .Include(r => r.FinalGrade)
                .Include(r => r.Author)
                .FirstOrDefaultAsync(r => r.TargetTask.Id == taskId && r.Author.Id == studentId);

            if (response == null) return NotFound();

            var teacher = await _context.Users.OfType<Teacher>().FirstOrDefaultAsync(u => u.Id == userId);
            if (teacher == null) return Forbid();

            if (action == "Grade")
            {
                try
                {
                    if (response.FinalGrade != null)
                    {
                        if (!response.FinalGrade.ValidateValue(gradeValue, response.TargetTask.MaxPoints))
                        {
                            TempData["Error"] = $"Помилка: Оцінка ({gradeValue}) не може перевищувати максимум завдання ({response.TargetTask.MaxPoints}).";
                            return RedirectToAction("TaskDetails", new { id = taskId, studentId = studentId });
                        }

                        response.FinalGrade.UpdateGrade(gradeValue, "Оцінено успішно");
                    }
                    else
                    {
                        teacher.GradeSubmission(response, gradeValue, "Оцінено успішно");
                        _context.Add(response.FinalGrade);
                    }

                    if (response.Author is Student studentObject)
                    {
                        _notificationService.HandleGradeEvent(response.FinalGrade, studentObject);
                    }

                    TempData["Message"] = $"Успішно збережено! {response.FinalGrade.FormatFeedback()}";
                }
                catch (ArgumentException ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("TaskDetails", new { id = taskId, studentId = studentId });
                }
            }
            else if (action == "Return")
            {
                response.Status = SubmissionStatus.Rejected;
                if (response.FinalGrade != null)
                {
                    _context.Grades.Remove(response.FinalGrade);
                    response.FinalGrade = null;
                }
                TempData["Message"] = "Роботу повернуто студенту на доопрацювання.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("TaskDetails", new { id = taskId, studentId = studentId });
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> AddResponseFile(Guid taskId, IFormFile file)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var response = await _context.Responses
                .Include(r => r.TargetTask)
                .Include(r => r.AttachedFiles)
                .FirstOrDefaultAsync(r => r.TargetTask.Id == taskId && r.Author.Id == userId);

            if (response == null) return NotFound();
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            var tempFileModel = new FileModel(file.FileName, file.Length, currentUser!);
            string[] allowedExtensions = { ".pdf", ".docx", ".doc", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".png", ".jpg", ".txt" };

            if (!tempFileModel.Validate(_fileManager.MaxFileSize, allowedExtensions))
            {
                TempData["Error"] = "Файл перевищує допустимий розмір або має незареєстроване розширення!";
                return RedirectToAction("TaskDetails", new { id = taskId });
            }

            try
            {
                var savedFile = await _fileManager.SaveFileAsync(file, currentUser!);
                if (savedFile != null)
                {
                    _context.Files.Add(savedFile);
                    response.ModifyFiles(savedFile, true);
                    response.RefreshStatus();
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> RemoveResponseFile(Guid taskId, Guid fileId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var response = await _context.Responses
                .Include(r => r.TargetTask)
                .Include(r => r.AttachedFiles)
                .FirstOrDefaultAsync(r => r.TargetTask.Id == taskId && r.Author.Id == userId);

            if (response == null) return NotFound();

            var fileToRemove = response.AttachedFiles.FirstOrDefault(f => f.Id == fileId);
            if (fileToRemove != null)
            {
                response.ModifyFiles(fileToRemove, false);
                response.RefreshStatus();

                _fileManager.DeleteFile(fileToRemove.LocalPath);

                _context.Files.Remove(fileToRemove);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> Leave(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out Guid userId);

            var student = await _context.Users.OfType<Student>()
                .Include(s => s.EnrolledCourses)
                .FirstOrDefaultAsync(s => s.Id == userId);

            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (student == null || course == null) return NotFound();

            student.Unenroll(course);

            _notificationService.NotifyEnrollmentChange(student, course, "excluded");

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Ви успішно покинули курс '{course.Title}'.";
            return RedirectToAction("MyCourses", "Home");
        }
    }
}