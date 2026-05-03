using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Models;
using System;

namespace Platform.Tests
{
    [TestClass]
    public class FileModelTests
    {
        // Допоміжний метод для створення користувача-власника файлу
        private User CreateDummyUser()
        {
            return new Student("uploader_user", "pass123", "uploader@test.com", "Uploader", "avatar.png");
        }

        [TestMethod]
        public void Constructor_ValidData_SetsPropertiesCorrectly()
        {
            var uploader = CreateDummyUser();
            string fileName = "document.pdf";
            long size = 2048;

            var file = new FileModel(fileName, size, uploader);

            Assert.AreEqual(fileName, file.FileName);
            Assert.AreEqual(".pdf", file.Extension);
            Assert.AreEqual(size, file.SizeInBytes);
            Assert.AreEqual(uploader, file.Uploader);
            Assert.AreNotEqual(Guid.Empty, file.Id);
            Assert.IsTrue((DateTime.Now - file.UploadDate).TotalSeconds < 1, "Дата завантаження має бути поточною.");
        }

        [TestMethod]
        public void Constructor_FileNameWithoutExtension_SetsEmptyExtension()
        {
            var uploader = CreateDummyUser();
            var file = new FileModel("just_a_name", 100, uploader);

            Assert.AreEqual(string.Empty, file.Extension, "Для файлів без розширення має бути порожній рядок.");
        }

        [TestMethod]
        public void Constructor_UpperCaseExtension_ConvertsToLowercase()
        {
            var uploader = CreateDummyUser();
            var file = new FileModel("image.PNG", 100, uploader);

            Assert.AreEqual(".png", file.Extension, "Розширення має автоматично приводитися до нижнього регістру.");
        }

        [TestMethod]
        public void Constructor_NullFileName_ThrowsArgumentException()
        {
            var uploader = CreateDummyUser();
            Assert.ThrowsExactly<ArgumentException>(() => new FileModel(null!, 100, uploader));
        }

        [TestMethod]
        public void Constructor_EmptyOrWhitespaceFileName_ThrowsArgumentException()
        {
            var uploader = CreateDummyUser();
            Assert.ThrowsExactly<ArgumentException>(() => new FileModel("", 100, uploader));
            Assert.ThrowsExactly<ArgumentException>(() => new FileModel("   ", 100, uploader));
        }

        [TestMethod]
        public void Constructor_NullUploader_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new FileModel("test.txt", 100, null!));
        }

        [TestMethod]
        public void Validate_SizeExceedsMax_ReturnsFalse()
        {
            var file = new FileModel("test.txt", 5000, CreateDummyUser());

            bool isValid = file.Validate(4000, new[] { ".txt" });

            Assert.IsFalse(isValid, "Файл, що перевищує ліміт розміру, не пройшов перевірку.");
        }

        [TestMethod]
        public void Validate_ExtensionNotAllowed_ReturnsFalse()
        {
            var file = new FileModel("virus.exe", 100, CreateDummyUser());

            bool isValid = file.Validate(1000, new[] { ".pdf", ".docx" });

            Assert.IsFalse(isValid, "Файл з недозволеним розширенням пройшов перевірку.");
        }

        [TestMethod]
        public void Validate_ValidSizeAndExtension_ReturnsTrue()
        {
            var file = new FileModel("doc.pdf", 500, CreateDummyUser());
            bool isValid = file.Validate(1000, new[] { ".txt", ".pdf" });
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Validate_AllowedExtIsNull_IgnoresExtensionCheck()
        {
            var file = new FileModel("test.txt", 500, CreateDummyUser());
            bool isValid = file.Validate(1000, null!);
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Validate_AllowedExtIsEmpty_IgnoresExtensionCheck()
        {
            var file = new FileModel("test.txt", 500, CreateDummyUser());
            bool isValid = file.Validate(1000, Array.Empty<string>());
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Validate_ExtensionCaseInsensitive_ReturnsTrue()
        {
            var file = new FileModel("photo.jpg", 500, CreateDummyUser());
            bool isValid = file.Validate(1000, new[] { ".JPG", ".PNG" });

            Assert.IsTrue(isValid, "Перевірка розширень має бути нечутливою до регістру.");
        }

        [TestMethod]
        public void GenerateStorageName_ReturnsIdWithExtension()
        {
            var file = new FileModel("data.csv", 100, CreateDummyUser());
            string expectedName = $"{file.Id}.csv";

            Assert.AreEqual(expectedName, file.GenerateStorageName());
        }

        [TestMethod]
        public void GetReadableSize_Bytes_ReturnsB()
        {
            var file = new FileModel("1.txt", 500, CreateDummyUser());
            Assert.AreEqual("500 B", file.GetReadableSize());
        }

        [TestMethod]
        public void GetReadableSize_Kilobytes_ReturnsKB()
        {
            var file = new FileModel("1.txt", 1024, CreateDummyUser());
            Assert.AreEqual("1 KB", file.GetReadableSize());
        }

        [TestMethod]
        public void GetReadableSize_Megabytes_ReturnsMB()
        {
            var file = new FileModel("1.txt", 1048576, CreateDummyUser());
            Assert.AreEqual("1 MB", file.GetReadableSize());
        }

        [TestMethod]
        public void GetAccessPath_LocalPathSet_ReturnsLocalPath()
        {
            var file = new FileModel("test.txt", 100, CreateDummyUser());
            file.LocalPath = "C:\\Temp\\test.txt";

            Assert.AreEqual("C:\\Temp\\test.txt", file.GetAccessPath());
        }

        [TestMethod]
        public void GetAccessPath_LocalPathNotSet_ReturnsDefaultUploadPath()
        {
            var file = new FileModel("test.txt", 100, CreateDummyUser());
            string expectedPath = $"/uploads/{file.Id}.txt";

            Assert.AreEqual(expectedPath, file.GetAccessPath());
        }
    }
}