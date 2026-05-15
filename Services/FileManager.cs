using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platform.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Platform.Services
{
    public class FileManager
    {
        public string RootFolderPath { get; private set; }
        public List<string> AllowedExtensions { get; private set; }
        public long MaxFileSize { get; private set; }

        private readonly PlatformData _platform;


        public FileManager(string rootFolderPath, List<string> allowedExtensions, long maxFileSize, PlatformData platform)
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
                throw new ArgumentException("Шлях до кореневої папки не може бути порожнім.");

            RootFolderPath = rootFolderPath;
            AllowedExtensions = allowedExtensions ?? new List<string>();
            MaxFileSize = maxFileSize;
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));

            if (!Directory.Exists(RootFolderPath))
            {
                Directory.CreateDirectory(RootFolderPath);
            }
        }
        public async Task<FileModel> SaveFileAsync(IFormFile file, User uploader)
        {
            if (file == null || file.Length == 0) return null;

            var fileModel = new FileModel(file.FileName, file.Length, uploader);

            string storageName = fileModel.GenerateStorageName();
            string fullPath = Path.Combine(RootFolderPath, storageName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            fileModel.LocalPath = fullPath;

            return fileModel;
        }

        public string SaveFile(FileModel metadata, byte[] content)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (content == null || content.Length == 0) throw new ArgumentException("Файл порожній.");

            if (!CheckSystemLimits(content.Length))
            {
                throw new InvalidOperationException("Файл перевищує допустимі ліміти системи.");
            }

            string storageName = metadata.GenerateStorageName();
            string fullPath = Path.Combine(RootFolderPath, storageName);

            File.WriteAllBytes(fullPath, content);

            metadata.LocalPath = fullPath;

            return fullPath;
        }

        public bool DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public string GetDownloadUrl(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath)) return string.Empty;
            string fileName = Path.GetFileName(storagePath);
            return $"/files/{fileName}";
        }

        public bool CheckSystemLimits(long fileSize)
        {
            if (fileSize > MaxFileSize)
            {
                return false;
            }

            try
            {
                DriveInfo drive = new DriveInfo(Path.GetPathRoot(RootFolderPath) ?? "C:\\");
                if (drive.AvailableFreeSpace < fileSize)
                {
                    return false;
                }
            }
            catch
            {

            }

            return true;
        }

        public void CleanOrphanedFiles()
        {
            var physicalFiles = Directory.GetFiles(RootFolderPath).ToList();

            var activeFiles = new HashSet<string>();

            foreach (var user in _platform.AllUsers)
            {
                if (user.Avatar != null && !string.IsNullOrEmpty(user.Avatar.LocalPath))
                {
                    activeFiles.Add(user.Avatar.LocalPath);
                }
            }

            foreach (var course in _platform.AllCourses)
            {
                if (course.Banner != null && !string.IsNullOrEmpty(course.Banner.LocalPath))
                {
                    activeFiles.Add(course.Banner.LocalPath);
                }

                foreach (var lesson in course.Lessons)
                {
                    foreach (var attachment in lesson.Attachments)
                    {
                        if (!string.IsNullOrEmpty(attachment.LocalPath))
                            activeFiles.Add(attachment.LocalPath);
                    }

                    foreach (var task in lesson.Tasks)
                    {
                        foreach (var attachment in task.Attachments)
                        {
                            if (!string.IsNullOrEmpty(attachment.LocalPath))
                                activeFiles.Add(attachment.LocalPath);
                        }

                        foreach (var response in task.Responses)
                        {
                            foreach (var file in response.AttachedFiles)
                            {
                                if (!string.IsNullOrEmpty(file.LocalPath))
                                    activeFiles.Add(file.LocalPath);
                            }
                        }
                    }
                }
            }

            foreach (var filePath in physicalFiles)
            {
                if (!activeFiles.Contains(filePath))
                {
                    DeleteFile(filePath);
                }
            }
        }
    }
}