using NUnit.Framework;
using System;
using System.IO;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class DataProtectionManagerTests
    {
        private string _testFileName = "test_data_" + Guid.NewGuid().ToString() + ".json";

        [TearDown]
        public void Teardown()
        {
            try
            {
                string dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "ScreenTimeController");
                string filePath = Path.Combine(dataDir, _testFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }

        [Test]
        public void SaveFast_ValidContent_SavesSuccessfully()
        {
            string content = "Test content " + Guid.NewGuid().ToString();

            Assert.DoesNotThrow(() => DataProtectionManager.SaveFast(_testFileName, content));
        }

        [Test]
        public void SaveWithProtection_ValidContent_SavesSuccessfully()
        {
            string content = "Test content " + Guid.NewGuid().ToString();

            Assert.DoesNotThrow(() => DataProtectionManager.SaveWithProtection(_testFileName, content));
        }

        [Test]
        public void SaveWithEncryption_ValidContent_SavesSuccessfully()
        {
            string content = "Test content " + Guid.NewGuid().ToString();

            Assert.DoesNotThrow(() => DataProtectionManager.SaveWithEncryption(_testFileName, content));
        }

        [Test]
        public void LoadWithDecryption_AfterEncryption_ReturnsOriginalContent()
        {
            string originalContent = "Test content " + Guid.NewGuid().ToString();

            DataProtectionManager.SaveWithEncryption(_testFileName, originalContent);
            string? loadedContent = DataProtectionManager.LoadWithDecryption(_testFileName);

            Assert.That(loadedContent, Is.EqualTo(originalContent));
        }

        [Test]
        public void LoadWithProtection_AfterSave_ReturnsOriginalContent()
        {
            string originalContent = "Test content " + Guid.NewGuid().ToString();

            DataProtectionManager.SaveWithProtection(_testFileName, originalContent);
            string? loadedContent = DataProtectionManager.LoadWithProtection(_testFileName);

            Assert.That(loadedContent, Is.EqualTo(originalContent));
        }

        [Test]
        public void LoadWithDecryption_NonExistentFile_ReturnsNull()
        {
            string? result = DataProtectionManager.LoadWithDecryption("non_existent_file_" + Guid.NewGuid().ToString() + ".json");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void LoadWithProtection_NonExistentFile_ReturnsNull()
        {
            string? result = DataProtectionManager.LoadWithProtection("non_existent_file_" + Guid.NewGuid().ToString() + ".json");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void SaveWithEncryption_SpecialCharacters_PreservesData()
        {
            string originalContent = "Test with special characters: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            DataProtectionManager.SaveWithEncryption(_testFileName, originalContent);
            string? loadedContent = DataProtectionManager.LoadWithDecryption(_testFileName);

            Assert.That(loadedContent, Is.EqualTo(originalContent));
        }

        [Test]
        public void SaveWithEncryption_UnicodeCharacters_PreservesData()
        {
            string originalContent = "测试数据 Test 数据 🔒🛡️";

            DataProtectionManager.SaveWithEncryption(_testFileName, originalContent);
            string? loadedContent = DataProtectionManager.LoadWithDecryption(_testFileName);

            Assert.That(loadedContent, Is.EqualTo(originalContent));
        }

        [Test]
        public void SaveWithEncryption_LongContent_PreservesData()
        {
            string originalContent = new string('A', 10000);

            DataProtectionManager.SaveWithEncryption(_testFileName, originalContent);
            string? loadedContent = DataProtectionManager.LoadWithDecryption(_testFileName);

            Assert.That(loadedContent, Is.EqualTo(originalContent));
        }

        [Test]
        public void SaveWithProtection_Overwrite_UpdatesContent()
        {
            string originalContent = "Original content";
            string newContent = "New content";

            DataProtectionManager.SaveWithProtection(_testFileName, originalContent);
            DataProtectionManager.SaveWithProtection(_testFileName, newContent);
            string? loadedContent = DataProtectionManager.LoadWithProtection(_testFileName);

            Assert.That(loadedContent, Is.EqualTo(newContent));
        }
    }
}
