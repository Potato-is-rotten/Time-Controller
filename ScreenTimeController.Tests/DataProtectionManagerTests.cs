using NUnit.Framework;
using System;
using System.IO;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class DataProtectionManagerTests : TestBase
    {
        private DataProtectionManager _dataProtectionManager = null!;
        private string _testDataPath = string.Empty;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _testDataPath = GetTestFilePath("protected_data.json");
            _dataProtectionManager = new DataProtectionManager(_testDataPath);
        }

        [TearDown]
        public override void Teardown()
        {
            base.Teardown();
        }

        [Test]
        public void Constructor_ValidPath_CreatesInstance()
        {
            Assert.That(_dataProtectionManager, Is.Not.Null);
        }

        [Test]
        public void ProtectData_ValidData_ProtectsSuccessfully()
        {
            string testData = "Test data to protect";

            bool result = _dataProtectionManager.ProtectData(testData);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(_testDataPath), Is.True);
        }

        [Test]
        public void ProtectData_EmptyData_ReturnsFalse()
        {
            string testData = "";

            bool result = _dataProtectionManager.ProtectData(testData);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ProtectData_NullData_ReturnsFalse()
        {
            string? testData = null;

            bool result = _dataProtectionManager.ProtectData(testData!);

            Assert.That(result, Is.False);
        }

        [Test]
        public void UnprotectData_ProtectedData_UnprotectsSuccessfully()
        {
            string originalData = "Test data to protect";

            _dataProtectionManager.ProtectData(originalData);
            string? unprotectedData = _dataProtectionManager.UnprotectData();

            Assert.That(unprotectedData, Is.Not.Null);
            Assert.That(unprotectedData, Is.EqualTo(originalData));
        }

        [Test]
        public void UnprotectData_NoProtectedData_ReturnsNull()
        {
            string? result = _dataProtectionManager.UnprotectData();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void UnprotectData_CorruptedData_ReturnsNull()
        {
            File.WriteAllText(_testDataPath, "corrupted data");

            string? result = _dataProtectionManager.UnprotectData();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void VerifyIntegrity_ValidData_ReturnsTrue()
        {
            string testData = "Test data to protect";

            _dataProtectionManager.ProtectData(testData);
            bool result = _dataProtectionManager.VerifyIntegrity();

            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyIntegrity_NoData_ReturnsFalse()
        {
            bool result = _dataProtectionManager.VerifyIntegrity();

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyIntegrity_CorruptedData_ReturnsFalse()
        {
            File.WriteAllText(_testDataPath, "corrupted data");

            bool result = _dataProtectionManager.VerifyIntegrity();

            Assert.That(result, Is.False);
        }

        [Test]
        public void DeleteData_ExistingData_DeletesSuccessfully()
        {
            string testData = "Test data to protect";

            _dataProtectionManager.ProtectData(testData);
            bool result = _dataProtectionManager.DeleteData();

            Assert.That(result, Is.True);
            Assert.That(File.Exists(_testDataPath), Is.False);
        }

        [Test]
        public void DeleteData_NoData_ReturnsFalse()
        {
            bool result = _dataProtectionManager.DeleteData();

            Assert.That(result, Is.False);
        }

        [Test]
        public void DataExists_ExistingData_ReturnsTrue()
        {
            string testData = "Test data to protect";

            _dataProtectionManager.ProtectData(testData);

            Assert.That(_dataProtectionManager.DataExists(), Is.True);
        }

        [Test]
        public void DataExists_NoData_ReturnsFalse()
        {
            Assert.That(_dataProtectionManager.DataExists(), Is.False);
        }

        [Test]
        public void ProtectData_OverwriteExistingData_UpdatesSuccessfully()
        {
            string originalData = "Original data";
            string newData = "New data";

            _dataProtectionManager.ProtectData(originalData);
            _dataProtectionManager.ProtectData(newData);

            string? unprotectedData = _dataProtectionManager.UnprotectData();
            Assert.That(unprotectedData, Is.EqualTo(newData));
        }

        [Test]
        public void ProtectAndUnprotect_SpecialCharacters_PreservesData()
        {
            string testData = "Test with special characters: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            _dataProtectionManager.ProtectData(testData);
            string? unprotectedData = _dataProtectionManager.UnprotectData();

            Assert.That(unprotectedData, Is.EqualTo(testData));
        }

        [Test]
        public void ProtectAndUnprotect_UnicodeCharacters_PreservesData()
        {
            string testData = "测试数据 Test 数据 🔒🛡️";

            _dataProtectionManager.ProtectData(testData);
            string? unprotectedData = _dataProtectionManager.UnprotectData();

            Assert.That(unprotectedData, Is.EqualTo(testData));
        }

        [Test]
        public void ProtectAndUnprotect_LongData_PreservesData()
        {
            string testData = new string('A', 10000);

            _dataProtectionManager.ProtectData(testData);
            string? unprotectedData = _dataProtectionManager.UnprotectData();

            Assert.That(unprotectedData, Is.EqualTo(testData));
        }

        [Test]
        public void ProtectAndUnprotect_MultipleOperations_PreservesData()
        {
            for (int i = 0; i < 5; i++)
            {
                string testData = $"Test data {i}";
                _dataProtectionManager.ProtectData(testData);
                string? unprotectedData = _dataProtectionManager.UnprotectData();
                Assert.That(unprotectedData, Is.EqualTo(testData));
            }
        }

        [Test]
        public void GetFileSize_ExistingData_ReturnsCorrectSize()
        {
            string testData = "Test data to protect";

            _dataProtectionManager.ProtectData(testData);
            long size = _dataProtectionManager.GetFileSize();

            Assert.That(size, Is.GreaterThan(0));
        }

        [Test]
        public void GetFileSize_NoData_ReturnsZero()
        {
            long size = _dataProtectionManager.GetFileSize();

            Assert.That(size, Is.EqualTo(0));
        }

        [Test]
        public void BackupData_ExistingData_CreatesBackup()
        {
            string testData = "Test data to protect";
            string backupPath = GetTestFilePath("backup.json");

            _dataProtectionManager.ProtectData(testData);
            bool result = _dataProtectionManager.BackupData(backupPath);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(backupPath), Is.True);
        }

        [Test]
        public void BackupData_NoData_ReturnsFalse()
        {
            string backupPath = GetTestFilePath("backup.json");

            bool result = _dataProtectionManager.BackupData(backupPath);

            Assert.That(result, Is.False);
        }

        [Test]
        public void RestoreData_ValidBackup_RestoresSuccessfully()
        {
            string testData = "Test data to protect";
            string backupPath = GetTestFilePath("backup.json");

            _dataProtectionManager.ProtectData(testData);
            _dataProtectionManager.BackupData(backupPath);
            _dataProtectionManager.DeleteData();

            bool result = _dataProtectionManager.RestoreData(backupPath);
            string? restoredData = _dataProtectionManager.UnprotectData();

            Assert.That(result, Is.True);
            Assert.That(restoredData, Is.EqualTo(testData));
        }

        [Test]
        public void RestoreData_InvalidBackup_ReturnsFalse()
        {
            string backupPath = GetTestFilePath("backup.json");
            File.WriteAllText(backupPath, "invalid backup");

            bool result = _dataProtectionManager.RestoreData(backupPath);

            Assert.That(result, Is.False);
        }
    }
}
