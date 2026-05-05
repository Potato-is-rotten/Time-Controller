using NUnit.Framework;
using System;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class AppTimeLimitTests
    {
        [Test]
        public void Constructor_DefaultValues_SetsCorrectly()
        {
            var limit = new AppTimeLimit();

            Assert.That(limit.AppIdentifier, Is.EqualTo(string.Empty));
            Assert.That(limit.DailyLimit, Is.EqualTo(TimeSpan.Zero));
            Assert.That(limit.IsEnabled, Is.True);
        }

        [Test]
        public void Constructor_WithValues_SetsCorrectly()
        {
            string appIdentifier = "TestApp";
            TimeSpan dailyLimit = TimeSpan.FromHours(2);

            var limit = new AppTimeLimit
            {
                AppIdentifier = appIdentifier,
                DailyLimit = dailyLimit
            };

            Assert.That(limit.AppIdentifier, Is.EqualTo(appIdentifier));
            Assert.That(limit.DailyLimit, Is.EqualTo(dailyLimit));
        }

        [Test]
        public void IsEnabled_SetToFalse_UpdatesCorrectly()
        {
            var limit = new AppTimeLimit();

            limit.IsEnabled = false;

            Assert.That(limit.IsEnabled, Is.False);
        }

        [Test]
        public void CheckLimit_WithinLimit_ReturnsFalse()
        {
            var limit = new AppTimeLimit
            {
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(1);

            bool exceedsLimit = usage > limit.DailyLimit;

            Assert.That(exceedsLimit, Is.False);
        }

        [Test]
        public void CheckLimit_ExceedsLimit_ReturnsTrue()
        {
            var limit = new AppTimeLimit
            {
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(3);

            bool exceedsLimit = usage > limit.DailyLimit;

            Assert.That(exceedsLimit, Is.True);
        }

        [Test]
        public void GetRemainingTime_WithinLimit_ReturnsCorrectTime()
        {
            var limit = new AppTimeLimit
            {
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(1);

            TimeSpan remaining = limit.DailyLimit - usage;

            Assert.That(remaining, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void GetRemainingTime_ExceedsLimit_ReturnsNegative()
        {
            var limit = new AppTimeLimit
            {
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(3);

            TimeSpan remaining = limit.DailyLimit - usage;

            Assert.That(remaining, Is.EqualTo(TimeSpan.FromHours(-1)));
        }

        [Test]
        public void GetRemainingTime_ExactlyAtLimit_ReturnsZero()
        {
            var limit = new AppTimeLimit
            {
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(2);

            TimeSpan remaining = limit.DailyLimit - usage;

            Assert.That(remaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Equals_SameIdentifier_ReturnsTrue()
        {
            var limit1 = new AppTimeLimit { AppIdentifier = "TestApp" };
            var limit2 = new AppTimeLimit { AppIdentifier = "TestApp" };

            bool areEqual = limit1.AppIdentifier == limit2.AppIdentifier;

            Assert.That(areEqual, Is.True);
        }

        [Test]
        public void Equals_DifferentIdentifier_ReturnsFalse()
        {
            var limit1 = new AppTimeLimit { AppIdentifier = "App1" };
            var limit2 = new AppTimeLimit { AppIdentifier = "App2" };

            bool areEqual = limit1.AppIdentifier == limit2.AppIdentifier;

            Assert.That(areEqual, Is.False);
        }

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };

            string result = limit.ToString();

            Assert.That(result, Does.Contain("TestApp"));
            Assert.That(result, Does.Contain("2"));
        }
    }

    [TestFixture]
    public class AppTimeLimitResultTests
    {
        [Test]
        public void IsExceeded_WhenExceeded_ReturnsTrue()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(1)
            };
            TimeSpan usage = TimeSpan.FromHours(2);

            bool isExceeded = usage > limit.DailyLimit;

            Assert.That(isExceeded, Is.True);
        }

        [Test]
        public void IsExceeded_WhenNotExceeded_ReturnsFalse()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(1);

            bool isExceeded = usage > limit.DailyLimit;

            Assert.That(isExceeded, Is.False);
        }

        [Test]
        public void RemainingTime_WhenWithinLimit_ReturnsCorrectTime()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(2)
            };
            TimeSpan usage = TimeSpan.FromHours(1);

            TimeSpan remaining = limit.DailyLimit - usage;

            Assert.That(remaining, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void RemainingTime_WhenExceeded_ReturnsZeroOrNegative()
        {
            var limit = new AppTimeLimit
            {
                AppIdentifier = "TestApp",
                DailyLimit = TimeSpan.FromHours(1)
            };
            TimeSpan usage = TimeSpan.FromHours(2);

            TimeSpan remaining = limit.DailyLimit - usage;

            Assert.That(remaining, Is.LessThanOrEqualTo(TimeSpan.Zero));
        }
    }
}
