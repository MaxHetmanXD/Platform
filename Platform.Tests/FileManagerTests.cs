using Microsoft.VisualStudio.TestTools.UnitTesting;
using Platform.Enums;
using Platform.Models;
using Platform.Services;
using System;
using System.Collections.Generic;
using System.IO;
using Task = Platform.Models.Task;

namespace Platform.Tests
{
    [TestClass]
    public class FileManagerTests
    {
        private string _tempPath = string.Empty;
        private PlatformData _platform = null!;
        private Student _dummyUploader = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "PlatformTests_" + Guid.NewGuid().ToString());
            _platform = new PlatformData();
            _dummyUploader = new Student("user1", "pass", "Nick", "u@test.com", "Група-1");
            _platform.AllUsers.Add(_dummyUploader);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }

        private FileManager CreateManager(long maxSize = 10 * 1024 * 1024)
        {
            return new FileManager(_tempPath, new List<string> { ".jpg", ".png", ".pdf" }, maxSize, _platform);
        }

        [TestMethod]
        public void Constructor_ValidData_CreatesDirectory()
        {
            var manager = CreateManager();

            Assert.IsTrue(Directory.Exists(_tempPath), "Конструктор має створити кореневу папку.");
            Assert.AreEqual(_tempPath, manager.RootFolderPath);
        }

        [TestMethod]
        public void Constructor_InvalidArguments_ThrowsExceptions()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new FileManager("", new List<string>(), 1000, _platform));

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new FileManager(_tempPath, new List<string>(), 1000, null!));
        }

        [TestMethod]
        public void SaveFile_ValidData_SavesToDiskAndUpdatesLocalPath()
        {
            var manager = CreateManager();
            var metadata = new FileModel("test.jpg", 100, _dummyUploader);
            byte[] content = new byte[] { 1, 2, 3, 4, 5 };

            string resultPath = manager.SaveFile(metadata, content);

            Assert.IsTrue(File.Exists(resultPath), "Файл не був збережений на диск.");
            Assert.AreEqual(resultPath, metadata.LocalPath, "LocalPath у метаданих не оновився.");
        }

        [TestMethod]
        public void SaveFile_FileExceedsLimit_ThrowsInvalidOperationException()
        {
            var manager = CreateManager(maxSize: 3);
            var metadata = new FileModel("test.jpg", 5, _dummyUploader);
            byte[] content = new byte[] { 1, 2, 3, 4, 5 };

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                manager.SaveFile(metadata, content));

            StringAssert.Contains(ex.Message, "перевищує допустимі ліміти");
        }

        [TestMethod]
        public void SaveFile_NullOrEmptyContent_ThrowsArgumentException()
        {
            var manager = CreateManager();
            var metadata = new FileModel("test.jpg", 0, _dummyUploader);

            Assert.ThrowsExactly<ArgumentException>(() => manager.SaveFile(metadata, new byte[0]));
            Assert.ThrowsExactly<ArgumentException>(() => manager.SaveFile(metadata, null!));
            Assert.ThrowsExactly<ArgumentNullException>(() => manager.SaveFile(null!, new byte[] { 1 }));
        }

        [TestMethod]
        public void DeleteFile_ExistingFile_ReturnsTrueAndDeletes()
        {
            var manager = CreateManager();
            string testFile = Path.Combine(_tempPath, "to_delete.txt");
            File.WriteAllText(testFile, "Hello");

            bool result = manager.DeleteFile(testFile);

            Assert.IsTrue(result);
            Assert.IsFalse(File.Exists(testFile));
        }

        [TestMethod]
        public void DeleteFile_NonExistingFile_ReturnsFalse()
        {
            var manager = CreateManager();
            string testFile = Path.Combine(_tempPath, "non_existing.txt");

            bool result = manager.DeleteFile(testFile);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetDownloadUrl_ValidPath_ReturnsFormattedString()
        {
            var manager = CreateManager();
            string path = @"C:\Storage\Files\image.png";

            string url = manager.GetDownloadUrl(path);

            Assert.AreEqual("/files/image.png", url);
        }

        [TestMethod]
        public void CheckSystemLimits_ChecksMaxFileSize()
        {
            var manager = CreateManager(maxSize: 100);

            Assert.IsTrue(manager.CheckSystemLimits(50));
            Assert.IsFalse(manager.CheckSystemLimits(150));
        }

        [TestMethod]
        public void CleanOrphanedFiles_DeletesOnlyUnreferencedFiles()
        {
            var manager = CreateManager();

            string activeFile = Path.Combine(_tempPath, "active.jpg");
            string orphanedFile = Path.Combine(_tempPath, "orphan.jpg");
            File.WriteAllText(activeFile, "I am used");
            File.WriteAllText(orphanedFile, "I am forgotten");

            var fileModel = new FileModel("active.jpg", 10, _dummyUploader) { LocalPath = activeFile };
            _dummyUploader.UpdateProfile("New Nick", "u@test.com", fileModel);

            manager.CleanOrphanedFiles();

            Assert.IsTrue(File.Exists(activeFile), "Активний файл був помилково видалений!");
            Assert.IsFalse(File.Exists(orphanedFile), "Файл-сирота не був видалений!");
        }
    }
}