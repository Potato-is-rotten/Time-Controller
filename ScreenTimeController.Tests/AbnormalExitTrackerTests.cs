using NUnit.Framework;
using System;
using System.IO;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class AbnormalExitTrackerTests : TestBase
    {
        private string _testDataDir = "";
        private string _testFilePath = "";

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _testDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ScreenTimeController");
            _testFilePath = Path.Combine(_testDataDir, "abnormal_exits.txt");
            CleanupTestFile();
        }

        [TearDown]
        public override void Teardown()
        {
            CleanupTestFile();
            base.Teardown();
        }

        private void CleanupTestFile()
        {
            try
            {
                if (File.Exists(_testFilePath))
                {
                    File.Delete(_testFilePath);
                }
            }
            catch { }
        }

        [Test]
        public void GetTodayAbnormalExitCount_NoFile_ReturnsZero()
        {
            CleanupTestFile();

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void IncrementAbnormalExitCount_NoFile_CreatesFileWithCountOne()
        {
            CleanupTestFile();

            AbnormalExitTracker.IncrementAbnormalExitCount();

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void IncrementAbnormalExitCount_ExistingFile_IncrementsCount()
        {
            CleanupTestFile();

            AbnormalExitTracker.IncrementAbnormalExitCount();
            AbnormalExitTracker.IncrementAbnormalExitCount();
            AbnormalExitTracker.IncrementAbnormalExitCount();

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void ResetTodayCount_NoFile_CreatesFileWithZero()
        {
            CleanupTestFile();

            AbnormalExitTracker.ResetTodayCount();

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void ResetTodayCount_ExistingCount_ResetsToZero()
        {
            CleanupTestFile();

            AbnormalExitTracker.IncrementAbnormalExitCount();
            AbnormalExitTracker.IncrementAbnormalExitCount();
            AbnormalExitTracker.ResetTodayCount();

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetHistory_ReturnsValidHistory()
        {
            CleanupTestFile();

            AbnormalExitTracker.IncrementAbnormalExitCount();

            var history = AbnormalExitTracker.GetHistory();

            Assert.That(history, Is.Not.Null);
            Assert.That(history.TodayCount, Is.EqualTo(1));
            Assert.That(history.LastCheck, Is.GreaterThanOrEqualTo(DateTime.Now.AddSeconds(-5)));
        }

        [Test]
        public void GetTodayAbnormalExitCount_OldDateFile_ReturnsZero()
        {
            CleanupTestFile();

            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            File.WriteAllLines(_testFilePath, new string[] { yesterday, "5" });

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void IncrementAbnormalExitCount_OldDateFile_ResetsDateAndCount()
        {
            CleanupTestFile();

            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            File.WriteAllLines(_testFilePath, new string[] { yesterday, "5" });

            AbnormalExitTracker.IncrementAbnormalExitCount();

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void GetTodayAbnormalExitCount_InvalidFileContent_ReturnsZero()
        {
            CleanupTestFile();

            File.WriteAllLines(_testFilePath, new string[] { "invalid", "data" });

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetTodayAbnormalExitCount_EmptyFile_ReturnsZero()
        {
            CleanupTestFile();

            File.WriteAllText(_testFilePath, "");

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetTodayAbnormalExitCount_SingleLineFile_ReturnsZero()
        {
            CleanupTestFile();

            File.WriteAllLines(_testFilePath, new string[] { DateTime.Today.ToString("yyyy-MM-dd") });

            int count = AbnormalExitTracker.GetTodayAbnormalExitCount();

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetHistory_DefaultParameters_ReturnsValidHistory()
        {
            CleanupTestFile();

            var history = AbnormalExitTracker.GetHistory(7);

            Assert.That(history, Is.Not.Null);
            Assert.That(history.TodayCount, Is.EqualTo(0));
        }

        [Test]
        public void MultipleOperations_Sequential_IncrementAndReset()
        {
            CleanupTestFile();

            AbnormalExitTracker.IncrementAbnormalExitCount();
            Assert.That(AbnormalExitTracker.GetTodayAbnormalExitCount(), Is.EqualTo(1));

            AbnormalExitTracker.IncrementAbnormalExitCount();
            Assert.That(AbnormalExitTracker.GetTodayAbnormalExitCount(), Is.EqualTo(2));

            AbnormalExitTracker.ResetTodayCount();
            Assert.That(AbnormalExitTracker.GetTodayAbnormalExitCount(), Is.EqualTo(0));

            AbnormalExitTracker.IncrementAbnormalExitCount();
            Assert.That(AbnormalExitTracker.GetTodayAbnormalExitCount(), Is.EqualTo(1));
        }
    }
}
