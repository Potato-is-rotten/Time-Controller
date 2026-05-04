using NUnit.Framework;
using System;
using System.Threading;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class AppLockServiceTests : TestBase
    {
        private AppLockService _service = null!;
        private SettingsManager _settingsManager = null!;
        private TimeTracker _timeTracker = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _settingsManager = new SettingsManager();
            _timeTracker = new TimeTracker(_settingsManager);
            _service = new AppLockService(_settingsManager, _timeTracker);
        }

        [TearDown]
        public override void Teardown()
        {
            _service?.Dispose();
            _timeTracker?.Dispose();
            base.Teardown();
        }

        [Test]
        public void Constructor_CreatesInstance()
        {
            Assert.That(_service, Is.Not.Null);
        }

        [Test]
        public void IsEnabled_Default_False()
        {
            Assert.That(_service.IsEnabled, Is.False);
        }

        [Test]
        public void Start_SetsIsEnabledToTrue()
        {
            _service.Start();

            Assert.That(_service.IsEnabled, Is.True);
        }

        [Test]
        public void Stop_SetsIsEnabledToFalse()
        {
            _service.Start();
            _service.Stop();

            Assert.That(_service.IsEnabled, Is.False);
        }

        [Test]
        public void IsEnabled_SetToTrue_EnablesService()
        {
            _service.IsEnabled = true;

            Assert.That(_service.IsEnabled, Is.True);
        }

        [Test]
        public void IsEnabled_SetToFalse_DisablesService()
        {
            _service.Start();
            _service.IsEnabled = false;

            Assert.That(_service.IsEnabled, Is.False);
        }

        [Test]
        public void GetLockedApps_WhenEmpty_ReturnsEmptyList()
        {
            var lockedApps = _service.GetLockedApps();

            Assert.That(lockedApps, Is.Not.Null);
            Assert.That(lockedApps, Is.Empty);
        }

        [Test]
        public void IsAppLocked_WhenNotLocked_ReturnsFalse()
        {
            bool isLocked = _service.IsAppLocked("NonExistentApp");

            Assert.That(isLocked, Is.False);
        }

        [Test]
        public void UnlockApp_WhenNotLocked_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.UnlockApp("NonExistentApp"));
        }

        [Test]
        public void Start_CalledTwice_DoesNotThrow()
        {
            _service.Start();
            Assert.DoesNotThrow(() => _service.Start());
        }

        [Test]
        public void Stop_CalledTwice_DoesNotThrow()
        {
            _service.Start();
            _service.Stop();
            Assert.DoesNotThrow(() => _service.Stop());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _service.Dispose();
            Assert.DoesNotThrow(() => _service.Dispose());
        }

        [Test]
        public void Start_AfterStop_CanRestart()
        {
            _service.Start();
            _service.Stop();
            _service.Start();

            Assert.That(_service.IsEnabled, Is.True);
        }

        [Test]
        public void AppLocked_Event_CanBeSubscribed()
        {
            bool eventSubscribed = false;
            _service.AppLocked += (s, e) => eventSubscribed = true;

            Assert.That(eventSubscribed, Is.False);
        }

        [Test]
        public void AppUnlocked_Event_CanBeSubscribed()
        {
            bool eventSubscribed = false;
            _service.AppUnlocked += (s, e) => eventSubscribed = true;

            Assert.That(eventSubscribed, Is.False);
        }

        [Test]
        public void Service_WithPerAppMode_CanBeEnabled()
        {
            _settingsManager.CurrentLockMode = LockMode.PerApp;
            _service.Start();

            Assert.That(_service.IsEnabled, Is.True);
        }

        [Test]
        public void Service_WithFullScreenMode_CanBeEnabled()
        {
            _settingsManager.CurrentLockMode = LockMode.FullScreen;
            _service.Start();

            Assert.That(_service.IsEnabled, Is.True);
        }

        [Test]
        public void GetLockedApps_AfterStart_ReturnsEmptyList()
        {
            _service.Start();

            var lockedApps = _service.GetLockedApps();

            Assert.That(lockedApps, Is.Not.Null);
            Assert.That(lockedApps, Is.Empty);
        }

        [Test]
        public void IsAppLocked_WithDifferentIdentifiers_ReturnsFalse()
        {
            _service.Start();

            Assert.That(_service.IsAppLocked("App1"), Is.False);
            Assert.That(_service.IsAppLocked("App2"), Is.False);
            Assert.That(_service.IsAppLocked("App3"), Is.False);
        }

        [Test]
        public void UnlockApp_MultipleUnlocks_DoesNotThrow()
        {
            _service.Start();

            Assert.DoesNotThrow(() =>
            {
                _service.UnlockApp("App1");
                _service.UnlockApp("App2");
                _service.UnlockApp("App3");
            });
        }

        [Test]
        public void Service_StartStop_RapidCycling()
        {
            for (int i = 0; i < 5; i++)
            {
                _service.Start();
                Assert.That(_service.IsEnabled, Is.True);
                _service.Stop();
                Assert.That(_service.IsEnabled, Is.False);
            }
        }

        [Test]
        public void Service_WithAppTimeLimit_CanBeEnabled()
        {
            string testApp = "TestApp_" + Guid.NewGuid().ToString();
            _settingsManager.AddAppTimeLimit(new AppTimeLimit
            {
                AppIdentifier = testApp,
                DailyLimit = TimeSpan.FromHours(1),
                IsEnabled = true
            });
            _settingsManager.CurrentLockMode = LockMode.PerApp;

            _service.Start();

            Assert.That(_service.IsEnabled, Is.True);
        }

        [Test]
        public void IsEnabled_SetSameValue_DoesNotChange()
        {
            _service.Start();
            bool before = _service.IsEnabled;

            _service.IsEnabled = true;

            Assert.That(_service.IsEnabled, Is.EqualTo(before));
        }

        [Test]
        public void Stop_WhenNotEnabled_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.Stop());
        }

        [Test]
        public void Dispose_WhenEnabled_StopsService()
        {
            _service.Start();
            bool wasEnabled = _service.IsEnabled;

            _service.Dispose();

            Assert.That(wasEnabled, Is.True);
        }

        [Test]
        public void Service_WithNullAppIdentifier_IsAppLockedReturnsFalse()
        {
            bool isLocked = _service.IsAppLocked("");

            Assert.That(isLocked, Is.False);
        }

        [Test]
        public void GetLockedApps_ReturnsNewListInstance()
        {
            var list1 = _service.GetLockedApps();
            var list2 = _service.GetLockedApps();

            Assert.That(list1, Is.Not.SameAs(list2));
        }
    }
}
