using System;
using System.IO;
using System.Linq;

namespace Platform.Models
{
    public class FileModel
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string FileName { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public long SizeInBytes { get; set; }

        public string LocalPath { get; set; } = string.Empty;

        public User Uploader { get; private set; }

        public DateTime UploadDate { get; private set; }

        protected FileModel()
        {
        }

        public FileModel(string fileName, long sizeInBytes, User uploader)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Ім'я файлу не може бути порожнім.");

            FileName = fileName;
            Extension = Path.GetExtension(fileName).ToLower();
            SizeInBytes = sizeInBytes;
            Uploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
            UploadDate = DateTime.Now;
        }

        public bool Validate(long maxSize, string[] allowedExt)
        {
            if (SizeInBytes > maxSize)
            {
                return false;
            }
            if (allowedExt != null && allowedExt.Length > 0)
            {
                var lowerAllowed = allowedExt.Select(ext => ext.ToLower()).ToArray();
                if (!lowerAllowed.Contains(Extension))
                {
                    return false;
                }
            }

            return true;
        }

        public string GenerateStorageName()
        {
            return $"{Id}{Extension}";
        }

        public string GetReadableSize()
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = SizeInBytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public string GetAccessPath()
        {
            if (!string.IsNullOrWhiteSpace(LocalPath))
            {
                return LocalPath;
            }
            return $"/uploads/{GenerateStorageName()}";
        }
    }
}