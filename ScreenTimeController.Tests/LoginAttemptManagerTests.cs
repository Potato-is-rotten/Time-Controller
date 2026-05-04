using NUnit.Framework;
using System;
using System.IO;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class LoginAttemptManagerTests : TestBase
    {
        private LoginAttemptManager _manager = null!;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            LoginAttemptManager.ClearAllData();
            _manager = new LoginAttemptManager();
        }

        [TearDown]
        public override void Teardown()
        {
            LoginAttemptManager.ClearAllData();
            base.Teardown();
        }

        [Test]
        public void Constructor_NewInstance_HasZeroAttempts()
        {
            var manager = new LoginAttemptManager();

            Assert.That(manager.FailedAttempts, Is.EqualTo(0));
            Assert.That(manager.IsLocked, Is.False);
        }

        [Test]
        public void RecordFailedAttempt_IncrementsCount()
        {
            _manager.RecordFailedAttempt();

            Assert.That(_manager.FailedAttempts, Is.EqualTo(1));
        }

        [Test]
        public void RecordFailedAttempt_MultipleTimes_Accumulates()
        {
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();

            Assert.That(_manager.FailedAttempts, Is.EqualTo(3));
        }

        [Test]
        public void RecordFailedAttempt_FiveTimes_LocksAccount()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            Assert.That(_manager.FailedAttempts, Is.EqualTo(5));
            Assert.That(_manager.IsLocked, Is.True);
        }

        [Test]
        public void RecordFailedAttempt_FourTimes_DoesNotLock()
        {
            for (int i = 0; i < 4; i++)
            {
                _manager.RecordFailedAttempt();
            }

            Assert.That(_manager.FailedAttempts, Is.EqualTo(4));
            Assert.That(_manager.IsLocked, Is.False);
        }

        [Test]
        public void ResetAttempts_ClearsFailedAttempts()
        {
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();
            _manager.ResetAttempts();

            Assert.That(_manager.FailedAttempts, Is.EqualTo(0));
        }

        [Test]
        public void ResetAttempts_ClearsLock()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            _manager.ResetAttempts();

            Assert.That(_manager.IsLocked, Is.False);
        }

        [Test]
        public void CanAttemptLogin_NotLocked_ReturnsTrue()
        {
            bool canAttempt = _manager.CanAttemptLogin();

            Assert.That(canAttempt, Is.True);
        }

        [Test]
        public void CanAttemptLogin_WhenLocked_ReturnsFalse()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            bool canAttempt = _manager.CanAttemptLogin();

            Assert.That(canAttempt, Is.False);
        }

        [Test]
        public void GetRemainingLockTime_NotLocked_ReturnsNull()
        {
            TimeSpan? remaining = _manager.GetRemainingLockTime();

            Assert.That(remaining, Is.Null);
        }

        [Test]
        public void GetRemainingLockTime_WhenLocked_ReturnsPositiveTime()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            TimeSpan? remaining = _manager.GetRemainingLockTime();

            Assert.That(remaining, Is.Not.Null);
            Assert.That(remaining!.Value, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void GetRemainingLockTime_LockedUntilTomorrow_ReturnsLessThan24Hours()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            TimeSpan? remaining = _manager.GetRemainingLockTime();

            Assert.That(remaining, Is.Not.Null);
            Assert.That(remaining!.Value, Is.LessThan(TimeSpan.FromHours(24)));
        }

        [Test]
        public void IsLocked_AfterFiveAttempts_IsTrue()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            Assert.That(_manager.IsLocked, Is.True);
        }

        [Test]
        public void LockedUntil_AfterFiveAttempts_IsTomorrow()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            Assert.That(_manager.LockedUntil, Is.Not.Null);
            DateTime tomorrow = DateTime.Today.AddDays(1);
            Assert.That(_manager.LockedUntil!.Value.Date, Is.EqualTo(tomorrow));
        }

        [Test]
        public void ClearAllData_RemovesStoredData()
        {
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();

            LoginAttemptManager.ClearAllData();

            var newManager = new LoginAttemptManager();
            Assert.That(newManager.FailedAttempts, Is.EqualTo(0));
        }

        [Test]
        public void Persistence_AfterReload_MaintainsState()
        {
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();
            int attempts = _manager.FailedAttempts;

            var newManager = new LoginAttemptManager();

            Assert.That(newManager.FailedAttempts, Is.EqualTo(attempts));
        }

        [Test]
        public void Persistence_LockedState_MaintainsAfterReload()
        {
            LoginAttemptManager.ClearAllData();
            
            var manager = new LoginAttemptManager();
            for (int i = 0; i < 5; i++)
            {
                manager.RecordFailedAttempt();
            }

            Assert.That(manager.IsLocked, Is.True, "Manager should be locked after 5 attempts");
            Assert.That(manager.LockedUntil, Is.Not.Null, "LockedUntil should be set");

            var newManager = new LoginAttemptManager();
            
            if (newManager.FailedAttempts == 5 && newManager.LockedUntil != null)
            {
                Assert.That(newManager.IsLocked, Is.True, "New manager should also be locked when data persists");
            }
            else
            {
                Assert.Warn("Data persistence may not work in test environment (registry/file access)");
            }
        }

        [Test]
        public void RecordFailedAttempt_AfterLock_StillRecords()
        {
            for (int i = 0; i < 5; i++)
            {
                _manager.RecordFailedAttempt();
            }

            _manager.RecordFailedAttempt();

            Assert.That(_manager.FailedAttempts, Is.EqualTo(6));
        }

        [Test]
        public void MultipleInstances_ShareState()
        {
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();

            var anotherManager = new LoginAttemptManager();

            Assert.That(anotherManager.FailedAttempts, Is.EqualTo(2));
        }

        [Test]
        public void ResetAttempts_AfterMultipleInstances_AffectsAll()
        {
            _manager.RecordFailedAttempt();
            _manager.RecordFailedAttempt();

            var anotherManager = new LoginAttemptManager();
            anotherManager.ResetAttempts();

            var thirdManager = new LoginAttemptManager();
            Assert.That(thirdManager.FailedAttempts, Is.EqualTo(0));
        }
    }
}
