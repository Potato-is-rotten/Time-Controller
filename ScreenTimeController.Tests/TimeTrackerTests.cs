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
        private SettingsManager _settingsManager = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _settingsManager = new SettingsManager();
            _timeTracker = new TimeTracker(_settingsManager);
        }

        [TearDown]
        public override void Teardown()
        {
            _timeTracker?.Dispose();
            base.Teardown();
        }

        [Test]
        public void Constructor_ValidSettingsManager_CreatesInstance()
        {
            Assert.That(_timeTracker, Is.Not.Null);
        }

        [Test]
        public void RecordUsage_ValidApp_RecordsTime()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();
            TimeSpan duration = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);

            TimeSpan todayUsage = _timeTracker.GetAppUsageToday(appName);
            Assert.That(todayUsage, Is.GreaterThanOrEqualTo(duration));
        }

        [Test]
        public void GetAppUsageToday_NoUsage_ReturnsZero()
        {
            string appName = "NonExistentApp_" + Guid.NewGuid().ToString();

            TimeSpan usage = _timeTracker.GetAppUsageToday(appName);

            Assert.That(usage, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void TotalUsage_AfterRecord_HasValue()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();
            TimeSpan duration = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);

            Assert.That(_timeTracker.TotalUsage, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        }

        [Test]
        public void AddBonusTime_IncreasesBonusTime()
        {
            TimeSpan bonus = TimeSpan.FromMinutes(10);

            _timeTracker.AddBonusTime(bonus);

            Assert.That(_timeTracker.BonusTime, Is.GreaterThanOrEqualTo(bonus));
        }

        [Test]
        public void AppUsage_AfterRecord_ContainsApp()
        {
            string app1 = "App1_" + Guid.NewGuid().ToString();
            TimeSpan duration1 = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration1, app1);

            var appUsage = _timeTracker.AppUsage;

            Assert.That(appUsage, Contains.Key(app1));
        }

        [Test]
        public void GetDailyLimit_ReturnsValidValue()
        {
            TimeSpan limit = _timeTracker.GetDailyLimit();

            Assert.That(limit, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
            Assert.That(limit, Is.LessThanOrEqualTo(TimeSpan.FromHours(24)));
        }

        [Test]
        public void GetExceededApps_ReturnsList()
        {
            var exceeded = _timeTracker.GetExceededApps();

            Assert.That(exceeded, Is.Not.Null);
        }

        [Test]
        public void ForceSave_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _timeTracker.ForceSave());
        }

        [Test]
        public void MarkCleanExit_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _timeTracker.MarkCleanExit());
        }

        [Test]
        public void Reset_ClearsData()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();
            TimeSpan duration = TimeSpan.FromMinutes(5);

            _timeTracker.RecordUsage(duration, appName);
            _timeTracker.Reset();

            TimeSpan usage = _timeTracker.GetAppUsageToday(appName);
            Assert.That(usage, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void GetRemainingTime_ReturnsValue()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();

            TimeSpan remaining = _timeTracker.GetRemainingTime(appName);

            Assert.That(remaining, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        }
    }
}
