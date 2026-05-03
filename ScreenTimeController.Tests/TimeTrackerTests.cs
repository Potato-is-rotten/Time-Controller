using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class TimeTrackerTests : TestBase
    {
        private TimeTracker _timeTracker = null!;
        private string _testDataPath = string.Empty;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _testDataPath = GetTestFilePath("timedata.json");
            _timeTracker = new TimeTracker(_testDataPath);
        }

        [TearDown]
        public override void Teardown()
        {
            _timeTracker?.Dispose();
            base.Teardown();
        }

        [Test]
        public void Constructor_ValidPath_CreatesInstance()
        {
            Assert.That(_timeTracker, Is.Not.Null);
        }

        [Test]
        public void RecordUsage_ValidApp_RecordsTime()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);

            TimeSpan todayUsage = _timeTracker.GetTodayUsage(appName);
            Assert.That(todayUsage, Is.EqualTo(duration));
        }

        [Test]
        public void RecordUsage_MultipleApps_RecordsAll()
        {
            string app1 = "App1";
            string app2 = "App2";
            TimeSpan duration1 = TimeSpan.FromMinutes(5);
            TimeSpan duration2 = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration1, app1);
            _timeTracker.RecordUsage(duration2, app2);

            TimeSpan usage1 = _timeTracker.GetTodayUsage(app1);
            TimeSpan usage2 = _timeTracker.GetTodayUsage(app2);

            Assert.That(usage1, Is.EqualTo(duration1));
            Assert.That(usage2, Is.EqualTo(duration2));
        }

        [Test]
        public void RecordUsage_SameAppMultipleTimes_AccumulatesTime()
        {
            string appName = "TestApp";
            TimeSpan duration1 = TimeSpan.FromMinutes(5);
            TimeSpan duration2 = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration1, appName);
            _timeTracker.RecordUsage(duration2, appName);

            TimeSpan totalUsage = _timeTracker.GetTodayUsage(appName);
            Assert.That(totalUsage, Is.EqualTo(duration1 + duration2));
        }

        [Test]
        public void GetTodayUsage_NoUsage_ReturnsZero()
        {
            string appName = "NonExistentApp";

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);

            Assert.That(usage, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void GetTodayUsage_WithUsage_ReturnsCorrectTime()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(15);

            _timeTracker.RecordUsage(duration, appName);

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);
            Assert.That(usage, Is.EqualTo(duration));
        }

        [Test]
        public void ResetDailyUsage_ClearsAllData()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.ResetDailyUsage();

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);
            Assert.That(usage, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void CheckTimeLimit_WithinLimit_ReturnsFalse()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);
            TimeSpan limit = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration, appName);
            bool exceedsLimit = _timeTracker.CheckTimeLimit(appName, limit);

            Assert.That(exceedsLimit, Is.False);
        }

        [Test]
        public void CheckTimeLimit_ExceedsLimit_ReturnsTrue()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(15);
            TimeSpan limit = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration, appName);
            bool exceedsLimit = _timeTracker.CheckTimeLimit(appName, limit);

            Assert.That(exceedsLimit, Is.True);
        }

        [Test]
        public void CheckTimeLimit_ExactlyAtLimit_ReturnsFalse()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(10);
            TimeSpan limit = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration, appName);
            bool exceedsLimit = _timeTracker.CheckTimeLimit(appName, limit);

            Assert.That(exceedsLimit, Is.False);
        }

        [Test]
        public void AddAppBonusTime_ReducesUsage()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(15);
            TimeSpan bonus = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.AddAppBonusTime(appName, bonus);

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);
            Assert.That(usage, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        [Test]
        public void AddAppBonusTime_MoreThanUsage_SetsToZero()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);
            TimeSpan bonus = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.AddAppBonusTime(appName, bonus);

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);
            Assert.That(usage, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void AddAppBonusTime_ZeroBonus_DoesNotChangeUsage()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);
            TimeSpan bonus = TimeSpan.Zero;

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.AddAppBonusTime(appName, bonus);

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);
            Assert.That(usage, Is.EqualTo(duration));
        }

        [Test]
        public void AddAppBonusTime_NegativeBonus_DoesNotChangeUsage()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);
            TimeSpan bonus = TimeSpan.FromMinutes(-5);

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.AddAppBonusTime(appName, bonus);

            TimeSpan usage = _timeTracker.GetTodayUsage(appName);
            Assert.That(usage, Is.EqualTo(duration));
        }

        [Test]
        public void AppUsage_AfterRecord_ReturnsCorrectDictionary()
        {
            string app1 = "App1";
            string app2 = "App2";
            TimeSpan duration1 = TimeSpan.FromMinutes(5);
            TimeSpan duration2 = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration1, app1);
            _timeTracker.RecordUsage(duration2, app2);

            var appUsage = _timeTracker.AppUsage;

            Assert.That(appUsage, Contains.Key(app1));
            Assert.That(appUsage, Contains.Key(app2));
            Assert.That(appUsage[app1], Is.EqualTo(duration1));
            Assert.That(appUsage[app2], Is.EqualTo(duration2));
        }

        [Test]
        public void SaveAndLoad_PersistsData()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.Dispose();

            var newTimeTracker = new TimeTracker(_testDataPath);
            TimeSpan usage = newTimeTracker.GetTodayUsage(appName);

            Assert.That(usage, Is.EqualTo(duration));
            newTimeTracker.Dispose();
        }

        [Test]
        public void RecordUsage_ConcurrentAccess_ThreadSafe()
        {
            string appName = "TestApp";
            int threadCount = 10;
            TimeSpan durationPerThread = TimeSpan.FromMinutes(1);

            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    _timeTracker.RecordUsage(durationPerThread, appName);
                });
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            TimeSpan totalUsage = _timeTracker.GetTodayUsage(appName);
            Assert.That(totalUsage, Is.EqualTo(TimeSpan.FromMinutes(threadCount)));
        }

        [Test]
        public void GetRemainingTime_WithinLimit_ReturnsCorrectTime()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(5);
            TimeSpan limit = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration, appName);
            TimeSpan remaining = limit - _timeTracker.GetTodayUsage(appName);

            Assert.That(remaining, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void GetRemainingTime_ExceedsLimit_ReturnsNegativeOrZero()
        {
            string appName = "TestApp";
            TimeSpan duration = TimeSpan.FromMinutes(15);
            TimeSpan limit = TimeSpan.FromMinutes(10);

            _timeTracker.RecordUsage(duration, appName);
            TimeSpan remaining = limit - _timeTracker.GetTodayUsage(appName);

            Assert.That(remaining, Is.LessThanOrEqualTo(TimeSpan.Zero));
        }
    }
}
