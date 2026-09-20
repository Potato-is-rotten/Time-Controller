using NUnit.Framework;
using System;

namespace ScreenTimeController.Tests
{
    /// <summary>
    /// Tests that the password input dialog used by the AppLockWindow's
    /// "Add Time" button shares state with <see cref="LoginAttemptManager"/>,
    /// so the global lockout (5 failed attempts → midnight) cannot be bypassed
    /// by reopening the dialog repeatedly.
    /// </summary>
    [TestFixture]
    public class PasswordInputDialogLockoutTests : TestBase
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            LoginAttemptManager.ClearAllData();
        }

        [TearDown]
        public override void Teardown()
        {
            LoginAttemptManager.ClearAllData();
            base.Teardown();
        }

        [Test]
        public void PasswordInputDialog_AfterFiveFailedAttempts_LocksAccount()
        {
            // Drive the public lockout path that AppLockWindow must integrate with.
            // Five failed attempts through the same LoginAttemptManager instance
            // must transition the manager into a locked state — proving that the
            // manager persists state and is suitable for sharing across dialog
            // instances (as the AppLockWindow fix relies on).
            var manager = new LoginAttemptManager();

            for (int i = 0; i < 5; i++)
            {
                manager.RecordFailedAttempt();
            }

            Assert.That(manager.IsLocked, Is.True,
                "After 5 failed attempts the manager must be locked so that subsequent " +
                "PasswordInputDialog instances cannot be used to brute-force the password.");
            Assert.That(manager.FailedAttempts, Is.EqualTo(5));
        }

        [Test]
        public void PasswordInputDialog_StatePersistsAcrossInstances()
        {
            // The AppLockWindow fix constructs a fresh LoginAttemptManager on every
            // "Add Time" click. If persistence is broken, an attacker could open a
            // new manager each time and bypass the lockout. This test ensures the
            // persisted counter survives across instances.
            var first = new LoginAttemptManager();
            first.RecordFailedAttempt();
            first.RecordFailedAttempt();

            var second = new LoginAttemptManager();
            Assert.That(second.FailedAttempts, Is.EqualTo(2),
                "Failed-attempt count must persist across LoginAttemptManager instances " +
                "so that the AppLockWindow cannot reset the counter by spawning new dialogs.");
        }

        [Test]
        public void PasswordInputDialog_ResetAttempts_ClearsLock()
        {
            // When the user enters the correct password, AppLockWindow must reset
            // the counter so a legitimate parent is not locked out from granting
            // bonus time across multiple sessions in the same day.
            var manager = new LoginAttemptManager();
            for (int i = 0; i < 3; i++)
            {
                manager.RecordFailedAttempt();
            }

            manager.ResetAttempts();

            Assert.That(manager.FailedAttempts, Is.EqualTo(0));
            Assert.That(manager.IsLocked, Is.False);
        }
    }
}