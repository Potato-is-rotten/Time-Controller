using NUnit.Framework;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class LockModeTests
    {
        [Test]
        public void LockMode_None_HasCorrectValue()
        {
            Assert.That(LockMode.None, Is.EqualTo(0));
        }

        [Test]
        public void LockMode_FullScreen_HasCorrectValue()
        {
            Assert.That(LockMode.FullScreen, Is.EqualTo(1));
        }

        [Test]
        public void LockMode_AppLock_HasCorrectValue()
        {
            Assert.That(LockMode.AppLock, Is.EqualTo(2));
        }

        [Test]
        public void LockMode_AllValues_AreDefined()
        {
            var values = System.Enum.GetValues<LockMode>();

            Assert.That(values, Has.Length.EqualTo(3));
            Assert.That(values, Contains.Item(LockMode.None));
            Assert.That(values, Contains.Item(LockMode.FullScreen));
            Assert.That(values, Contains.Item(LockMode.AppLock));
        }

        [Test]
        public void LockMode_CanConvertToInt()
        {
            int noneValue = (int)LockMode.None;
            int fullScreenValue = (int)LockMode.FullScreen;
            int appLockValue = (int)LockMode.AppLock;

            Assert.That(noneValue, Is.EqualTo(0));
            Assert.That(fullScreenValue, Is.EqualTo(1));
            Assert.That(appLockValue, Is.EqualTo(2));
        }

        [Test]
        public void LockMode_CanConvertFromInt()
        {
            LockMode mode = (LockMode)1;

            Assert.That(mode, Is.EqualTo(LockMode.FullScreen));
        }

        [Test]
        public void LockMode_InvalidInt_ThrowsException()
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                LockMode mode = (LockMode)999;
                if (!System.Enum.IsDefined(typeof(LockMode), mode))
                {
                    throw new System.ArgumentException();
                }
            });
        }
    }
}
