using NUnit.Framework;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class LockModeTests
    {
        [Test]
        public void LockMode_FullScreen_IsDefined()
        {
            Assert.That(System.Enum.IsDefined(typeof(LockMode), LockMode.FullScreen), Is.True);
        }

        [Test]
        public void LockMode_PerApp_IsDefined()
        {
            Assert.That(System.Enum.IsDefined(typeof(LockMode), LockMode.PerApp), Is.True);
        }

        [Test]
        public void LockMode_AllValues_AreDefined()
        {
            var values = System.Enum.GetValues<LockMode>();

            Assert.That(values, Has.Length.EqualTo(2));
            Assert.That(values, Contains.Item(LockMode.FullScreen));
            Assert.That(values, Contains.Item(LockMode.PerApp));
        }

        [Test]
        public void LockMode_CanConvertFromInt()
        {
            LockMode mode = (LockMode)0;

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
