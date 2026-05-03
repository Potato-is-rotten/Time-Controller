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
        public void Constructor_DefaultValues_SetsCorrectly()
        {
            var result = new AppTimeLimitResult();

            Assert.That(result.AppIdentifier, Is.EqualTo(string.Empty));
            Assert.That(result.IsWithinLimit, Is.True);
            Assert.That(result.RemainingTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(result.UsageTime, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Constructor_WithValues_SetsCorrectly()
        {
            string appIdentifier = "TestApp";
            bool isWithinLimit = false;
            TimeSpan remainingTime = TimeSpan.FromMinutes(30);
            TimeSpan usageTime = TimeSpan.FromHours(1);

            var result = new AppTimeLimitResult
            {
                AppIdentifier = appIdentifier,
                IsWithinLimit = isWithinLimit,
                RemainingTime = remainingTime,
                UsageTime = usageTime
            };

            Assert.That(result.AppIdentifier, Is.EqualTo(appIdentifier));
            Assert.That(result.IsWithinLimit, Is.EqualTo(isWithinLimit));
            Assert.That(result.RemainingTime, Is.EqualTo(remainingTime));
            Assert.That(result.UsageTime, Is.EqualTo(usageTime));
        }

        [Test]
        public void IsWithinLimit_SetToFalse_UpdatesCorrectly()
        {
            var result = new AppTimeLimitResult();

            result.IsWithinLimit = false;

            Assert.That(result.IsWithinLimit, Is.False);
        }

        [Test]
        public void GetUsagePercentage_ReturnsCorrectPercentage()
        {
            var result = new AppTimeLimitResult
            {
                UsageTime = TimeSpan.FromMinutes(30),
                RemainingTime = TimeSpan.FromMinutes(30)
            };

            double percentage = (result.UsageTime.TotalMinutes / (result.UsageTime.TotalMinutes + result.RemainingTime.TotalMinutes)) * 100;

            Assert.That(percentage, Is.EqualTo(50.0));
        }
    }
}
