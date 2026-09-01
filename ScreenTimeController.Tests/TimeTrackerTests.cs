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

        [Test]
        public void AddAppBonusTime_UnderCap_DecreasesUsage()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();
            _timeTracker.RecordUsage(TimeSpan.FromMinutes(60), appName);
            TimeSpan before = _timeTracker.GetAppUsageToday(appName);

            bool granted = _timeTracker.AddAppBonusTime(appName, TimeSpan.FromMinutes(10));

            Assert.That(granted, Is.True);
            TimeSpan after = _timeTracker.GetAppUsageToday(appName);
            Assert.That(after, Is.LessThan(before));
        }

        [Test]
        public void AddAppBonusTime_ExceedsCap_RefusesGrant()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();
            _timeTracker.RecordUsage(TimeSpan.FromMinutes(60), appName);
            _timeTracker.AddAppBonusTime(appName, TimeSpan.FromMinutes(60));
            TimeSpan beforeSecond = _timeTracker.GetAppUsageToday(appName);

            bool secondGranted = _timeTracker.AddAppBonusTime(appName, TimeSpan.FromMinutes(5));

            Assert.That(secondGranted, Is.False);
            TimeSpan afterSecond = _timeTracker.GetAppUsageToday(appName);
            Assert.That(afterSecond, Is.EqualTo(beforeSecond));
        }

        [Test]
        public void AddAppBonusTime_PerAppCap_IndependentAcrossApps()
        {
            string appA = "TestAppA_" + Guid.NewGuid().ToString();
            string appB = "TestAppB_" + Guid.NewGuid().ToString();
            _timeTracker.RecordUsage(TimeSpan.FromMinutes(60), appA);
            _timeTracker.RecordUsage(TimeSpan.FromMinutes(60), appB);
            _timeTracker.AddAppBonusTime(appA, TimeSpan.FromMinutes(60));

            bool grantedForB = _timeTracker.AddAppBonusTime(appB, TimeSpan.FromMinutes(10));

            Assert.That(grantedForB, Is.True);
        }

        [Test]
        public void AddAppBonusTime_EmptyOrNullIdentifier_DoesNotGrant()
        {
            Assert.That(_timeTracker.AddAppBonusTime("", TimeSpan.FromMinutes(5)), Is.False);
            Assert.That(_timeTracker.AddAppBonusTime(null!, TimeSpan.FromMinutes(5)), Is.False);
        }

        [Test]
        public void AddAppBonusTime_NonPositiveBonus_DoesNotGrant()
        {
            string appName = "TestApp_" + Guid.NewGuid().ToString();
            _timeTracker.RecordUsage(TimeSpan.FromMinutes(60), appName);

            Assert.That(_timeTracker.AddAppBonusTime(appName, TimeSpan.Zero), Is.False);
            Assert.That(_timeTracker.AddAppBonusTime(appName, TimeSpan.FromMinutes(-1)), Is.False);
        }
    }
}
