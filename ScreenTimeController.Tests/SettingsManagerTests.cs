using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class SettingsManagerTests : TestBase
    {
        private SettingsManager _settingsManager = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _settingsManager = new SettingsManager();
        }

        [TearDown]
        public override void Teardown()
        {
            base.Teardown();
        }

        [Test]
        public void Constructor_CreatesInstance()
        {
            Assert.That(_settingsManager, Is.Not.Null);
        }

        [Test]
        public void SundayLimit_CanBeSet()
        {
            TimeSpan newLimit = TimeSpan.FromHours(3);

            _settingsManager.SundayLimit = newLimit;

            Assert.That(_settingsManager.SundayLimit, Is.EqualTo(newLimit));
        }

        [Test]
        public void MondayLimit_CanBeSet()
        {
            TimeSpan newLimit = TimeSpan.FromHours(3);

            _settingsManager.MondayLimit = newLimit;

            Assert.That(_settingsManager.MondayLimit, Is.EqualTo(newLimit));
        }

        [Test]
        public void EnablePasswordLock_CanBeSet()
        {
            _settingsManager.EnablePasswordLock = false;

            Assert.That(_settingsManager.EnablePasswordLock, Is.False);
        }

        [Test]
        public void Language_CanBeSet()
        {
            _settingsManager.Language = Language.SimplifiedChinese;

            Assert.That(_settingsManager.Language, Is.EqualTo(Language.SimplifiedChinese));
        }

        [Test]
        public void CurrentLockMode_CanBeSet()
        {
            _settingsManager.CurrentLockMode = LockMode.PerApp;

            Assert.That(_settingsManager.CurrentLockMode, Is.EqualTo(LockMode.PerApp));
        }

        [Test]
        public void AppTimeLimits_CanBeSet()
        {
            var limits = new List<AppTimeLimit>
            {
                new AppTimeLimit { AppIdentifier = "App1", DailyLimit = TimeSpan.FromHours(1) },
                new AppTimeLimit { AppIdentifier = "App2", DailyLimit = TimeSpan.FromHours(2) }
            };

            _settingsManager.AppTimeLimits = limits;

            Assert.That(_settingsManager.AppTimeLimits, Has.Count.EqualTo(2));
        }

        [Test]
        public void AddAppTimeLimit_ValidLimit_AddsSuccessfully()
        {
            string uniqueAppId = "TestApp_" + Guid.NewGuid().ToString();
            var limit = new AppTimeLimit
            {
                AppIdentifier = uniqueAppId,
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppTimeLimit(limit);

            var limits = _settingsManager.AppTimeLimits;
            Assert.That(limits.Exists(l => l.AppIdentifier == uniqueAppId), Is.True);
        }

        [Test]
        public void RemoveAppTimeLimit_ExistingLimit_RemovesSuccessfully()
        {
            string uniqueAppId = "TestApp_" + Guid.NewGuid().ToString();
            var limit = new AppTimeLimit
            {
                AppIdentifier = uniqueAppId,
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppTimeLimit(limit);
            _settingsManager.RemoveAppTimeLimit(uniqueAppId);

            var limits = _settingsManager.AppTimeLimits;
            Assert.That(limits.Exists(l => l.AppIdentifier == uniqueAppId), Is.False);
        }

        [Test]
        public void GetDailyLimit_ReturnsValidValue()
        {
            TimeSpan limit = _settingsManager.GetDailyLimit();

            Assert.That(limit, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
            Assert.That(limit, Is.LessThanOrEqualTo(TimeSpan.FromHours(24)));
        }

        [Test]
        public void SaveSettings_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _settingsManager.SaveSettings());
        }

        [Test]
        public void SetPassword_ValidPassword_SetsSuccessfully()
        {
            string password = "TestPassword123!" + Guid.NewGuid().ToString();

            _settingsManager.SetPassword(password);

            Assert.That(_settingsManager.HasPassword(), Is.True);
        }

        [Test]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            string password = "TestPassword123!" + Guid.NewGuid().ToString();

            _settingsManager.SetPassword(password);
            bool result = _settingsManager.VerifyPassword(password);

            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            string password = "TestPassword123!" + Guid.NewGuid().ToString();

            _settingsManager.SetPassword(password);
            bool result = _settingsManager.VerifyPassword("WrongPassword");

            Assert.That(result, Is.False);
        }
    }
}
