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
        private string _testSettingsPath = string.Empty;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _testSettingsPath = GetTestFilePath("settings.json");
            _settingsManager = new SettingsManager(_testSettingsPath);
        }

        [TearDown]
        public override void Teardown()
        {
            base.Teardown();
        }

        [Test]
        public void Constructor_ValidPath_CreatesInstance()
        {
            Assert.That(_settingsManager, Is.Not.Null);
        }

        [Test]
        public void DailyLimit_DefaultValue_ReturnsCorrectValue()
        {
            TimeSpan limit = _settingsManager.DailyLimit;

            Assert.That(limit, Is.EqualTo(TimeSpan.FromHours(8)));
        }

        [Test]
        public void DailyLimit_SetValue_UpdatesCorrectly()
        {
            TimeSpan newLimit = TimeSpan.FromHours(6);

            _settingsManager.DailyLimit = newLimit;

            Assert.That(_settingsManager.DailyLimit, Is.EqualTo(newLimit));
        }

        [Test]
        public void IsPasswordEnabled_DefaultValue_ReturnsFalse()
        {
            bool isEnabled = _settingsManager.IsPasswordEnabled;

            Assert.That(isEnabled, Is.False);
        }

        [Test]
        public void IsPasswordEnabled_SetValue_UpdatesCorrectly()
        {
            _settingsManager.IsPasswordEnabled = true;

            Assert.That(_settingsManager.IsPasswordEnabled, Is.True);
        }

        [Test]
        public void Language_DefaultValue_ReturnsChinese()
        {
            Language language = _settingsManager.Language;

            Assert.That(language, Is.EqualTo(Language.Chinese));
        }

        [Test]
        public void Language_SetValue_UpdatesCorrectly()
        {
            _settingsManager.Language = Language.English;

            Assert.That(_settingsManager.Language, Is.EqualTo(Language.English));
        }

        [Test]
        public void LockMode_DefaultValue_ReturnsNone()
        {
            LockMode mode = _settingsManager.LockMode;

            Assert.That(mode, Is.EqualTo(LockMode.None));
        }

        [Test]
        public void LockMode_SetValue_UpdatesCorrectly()
        {
            _settingsManager.LockMode = LockMode.FullScreen;

            Assert.That(_settingsManager.LockMode, Is.EqualTo(LockMode.FullScreen));
        }

        [Test]
        public void AppTimeLimits_DefaultValue_ReturnsEmptyList()
        {
            List<AppTimeLimit> limits = _settingsManager.AppTimeLimits;

            Assert.That(limits, Is.Not.Null);
            Assert.That(limits, Is.Empty);
        }

        [Test]
        public void AddAppLimit_ValidLimit_AddsSuccessfully()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppLimit(limit);

            List<AppTimeLimit> limits = _settingsManager.AppTimeLimits;
            Assert.That(limits, Has.Count.EqualTo(1));
            Assert.That(limits[0].AppIdentifier, Is.EqualTo("TestApp"));
        }

        [Test]
        public void AddAppLimit_DuplicateApp_UpdatesExisting()
        {
            var limit1 = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };
            var limit2 = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(3)
            };

            _settingsManager.AddAppLimit(limit1);
            _settingsManager.AddAppLimit(limit2);

            List<AppTimeLimit> limits = _settingsManager.AppTimeLimits;
            Assert.That(limits, Has.Count.EqualTo(1));
            Assert.That(limits[0].DailyLimit, Is.EqualTo(TimeSpan.FromHours(3)));
        }

        [Test]
        public void RemoveAppLimit_ExistingLimit_RemovesSuccessfully()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppLimit(limit);
            bool result = _settingsManager.RemoveAppLimit("TestApp");

            Assert.That(result, Is.True);
            Assert.That(_settingsManager.AppTimeLimits, Is.Empty);
        }

        [Test]
        public void RemoveAppLimit_NonExistentLimit_ReturnsFalse()
        {
            bool result = _settingsManager.RemoveAppLimit("NonExistentApp");

            Assert.That(result, Is.False);
        }

        [Test]
        public void GetAppLimit_ExistingLimit_ReturnsCorrectLimit()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppLimit(limit);
            AppTimeLimit? retrievedLimit = _settingsManager.GetAppLimit("TestApp");

            Assert.That(retrievedLimit, Is.Not.Null);
            Assert.That(retrievedLimit!.AppIdentifier, Is.EqualTo("TestApp"));
            Assert.That(retrievedLimit.DailyLimit, Is.EqualTo(TimeSpan.FromHours(2)));
        }

        [Test]
        public void GetAppLimit_NonExistentLimit_ReturnsNull()
        {
            AppTimeLimit? limit = _settingsManager.GetAppLimit("NonExistentApp");

            Assert.That(limit, Is.Null);
        }

        [Test]
        public void UpdateAppLimit_ExistingLimit_UpdatesSuccessfully()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppLimit(limit);
            limit.DailyLimit = TimeSpan.FromHours(3);
            bool result = _settingsManager.UpdateAppLimit(limit);

            Assert.That(result, Is.True);
            AppTimeLimit? updatedLimit = _settingsManager.GetAppLimit("TestApp");
            Assert.That(updatedLimit!.DailyLimit, Is.EqualTo(TimeSpan.FromHours(3)));
        }

        [Test]
        public void UpdateAppLimit_NonExistentLimit_ReturnsFalse()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "NonExistentApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            bool result = _settingsManager.UpdateAppLimit(limit);

            Assert.That(result, Is.False);
        }

        [Test]
        public void SaveSettings_ValidSettings_SavesSuccessfully()
        {
            _settingsManager.DailyLimit = TimeSpan.FromHours(6);
            _settingsManager.IsPasswordEnabled = true;
            _settingsManager.Language = Language.English;

            bool result = _settingsManager.SaveSettings();

            Assert.That(result, Is.True);
            Assert.That(File.Exists(_testSettingsPath), Is.True);
        }

        [Test]
        public void LoadSettings_NoSettingsFile_ReturnsDefaults()
        {
            var newManager = new SettingsManager(_testSettingsPath);

            Assert.That(newManager.DailyLimit, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(newManager.IsPasswordEnabled, Is.False);
            Assert.That(newManager.Language, Is.EqualTo(Language.Chinese));
        }

        [Test]
        public void LoadSettings_WithSettingsFile_LoadsCorrectly()
        {
            _settingsManager.DailyLimit = TimeSpan.FromHours(6);
            _settingsManager.IsPasswordEnabled = true;
            _settingsManager.Language = Language.English;
            _settingsManager.SaveSettings();

            var newManager = new SettingsManager(_testSettingsPath);

            Assert.That(newManager.DailyLimit, Is.EqualTo(TimeSpan.FromHours(6)));
            Assert.That(newManager.IsPasswordEnabled, Is.True);
            Assert.That(newManager.Language, Is.EqualTo(Language.English));
        }

        [Test]
        public void SaveAndLoad_WithAppLimits_PersistsCorrectly()
        {
            var limit1 = new AppTimeLimit
            {
                AppIdentifier = "App1",
                DailyLimit = TimeSpan.FromHours(2)
            };
            var limit2 = new AppTimeLimit
            {
                AppIdentifier = "App2",
                DailyLimit = TimeSpan.FromHours(3)
            };

            _settingsManager.AddAppLimit(limit1);
            _settingsManager.AddAppLimit(limit2);
            _settingsManager.SaveSettings();

            var newManager = new SettingsManager(_testSettingsPath);
            List<AppTimeLimit> limits = newManager.AppTimeLimits;

            Assert.That(limits, Has.Count.EqualTo(2));
            Assert.That(limits[0].AppIdentifier, Is.EqualTo("App1"));
            Assert.That(limits[1].AppIdentifier, Is.EqualTo("App2"));
        }

        [Test]
        public void ClearAppLimits_RemovesAllLimits()
        {
            var limit1 = new AppTimeLimit
            {
                AppIdentifier = "App1",
                DailyLimit = TimeSpan.FromHours(2)
            };
            var limit2 = new AppTimeLimit
            {
                AppIdentifier = "App2",
                DailyLimit = TimeSpan.FromHours(3)
            };

            _settingsManager.AddAppLimit(limit1);
            _settingsManager.AddAppLimit(limit2);
            _settingsManager.ClearAppLimits();

            Assert.That(_settingsManager.AppTimeLimits, Is.Empty);
        }

        [Test]
        public void ResetToDefaults_ResetsAllSettings()
        {
            _settingsManager.DailyLimit = TimeSpan.FromHours(6);
            _settingsManager.IsPasswordEnabled = true;
            _settingsManager.Language = Language.English;
            _settingsManager.LockMode = LockMode.FullScreen;

            _settingsManager.ResetToDefaults();

            Assert.That(_settingsManager.DailyLimit, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(_settingsManager.IsPasswordEnabled, Is.False);
            Assert.That(_settingsManager.Language, Is.EqualTo(Language.Chinese));
            Assert.That(_settingsManager.LockMode, Is.EqualTo(LockMode.None));
        }

        [Test]
        public void HasAppLimit_ExistingLimit_ReturnsTrue()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            _settingsManager.AddAppLimit(limit);

            Assert.That(_settingsManager.HasAppLimit("TestApp"), Is.True);
        }

        [Test]
        public void HasAppLimit_NonExistentLimit_ReturnsFalse()
        {
            Assert.That(_settingsManager.HasAppLimit("NonExistentApp"), Is.False);
        }

        [Test]
        public void GetAppIdentifiers_ReturnsAllIdentifiers()
        {
            var limit1 = new AppTimeLimit
            {
                AppIdentifier = "App1",
                DailyLimit = TimeSpan.FromHours(2)
            };
            var limit2 = new AppTimeLimit
            {
                AppIdentifier = "App2",
                DailyLimit = TimeSpan.FromHours(3)
            };

            _settingsManager.AddAppLimit(limit1);
            _settingsManager.AddAppLimit(limit2);

            List<string> identifiers = _settingsManager.GetAppIdentifiers();

            Assert.That(identifiers, Has.Count.EqualTo(2));
            Assert.That(identifiers, Contains.Item("App1"));
            Assert.That(identifiers, Contains.Item("App2"));
        }
    }
}
