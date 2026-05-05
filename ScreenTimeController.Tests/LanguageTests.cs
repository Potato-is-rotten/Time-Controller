using NUnit.Framework;
using System;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class LanguageTests
    {
        [Test]
        public void Language_SimplifiedChinese_IsDefined()
        {
            Assert.That(Enum.IsDefined(typeof(Language), Language.SimplifiedChinese), Is.True);
        }

        [Test]
        public void Language_English_IsDefined()
        {
            Assert.That(Enum.IsDefined(typeof(Language), Language.English), Is.True);
        }

        [Test]
        public void Language_AllValues_AreDefined()
        {
            var values = Enum.GetValues<Language>();

            Assert.That(values.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(values, Contains.Item(Language.SimplifiedChinese));
            Assert.That(values, Contains.Item(Language.English));
        }
    }

    [TestFixture]
    public class LanguageManagerTests
    {
        [SetUp]
        public void Setup()
        {
            LanguageManager.CurrentLanguage = Language.SimplifiedChinese;
        }

        [TearDown]
        public void Teardown()
        {
            LanguageManager.CurrentLanguage = Language.SimplifiedChinese;
        }

        [Test]
        public void CurrentLanguage_CanBeSet()
        {
            LanguageManager.CurrentLanguage = Language.English;

            Assert.That(LanguageManager.CurrentLanguage, Is.EqualTo(Language.English));
        }

        [Test]
        public void GetLanguageName_SimplifiedChinese_ReturnsCorrectName()
        {
            string name = LanguageManager.GetLanguageName(Language.SimplifiedChinese);

            Assert.That(name, Is.EqualTo("简体中文"));
        }

        [Test]
        public void GetLanguageName_English_ReturnsCorrectName()
        {
            string name = LanguageManager.GetLanguageName(Language.English);

            Assert.That(name, Is.EqualTo("English"));
        }

        [Test]
        public void GetString_ValidKey_ReturnsNonEmptyString()
        {
            string key = "AppTitle";

            string result = LanguageManager.GetString(key);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void GetString_InvalidKey_ReturnsKey()
        {
            string key = "NonExistentKey12345";

            string result = LanguageManager.GetString(key);

            Assert.That(result, Is.EqualTo(key));
        }

        [Test]
        public void GetString_DifferentLanguages_ReturnsDifferentStrings()
        {
            string key = "AppTitle";

            LanguageManager.CurrentLanguage = Language.SimplifiedChinese;
            string chineseResult = LanguageManager.GetString(key);

            LanguageManager.CurrentLanguage = Language.English;
            string englishResult = LanguageManager.GetString(key);

            Assert.That(chineseResult, Is.Not.EqualTo(englishResult));
        }

        [Test]
        public void LanguageChanged_EventFires_WhenLanguageChanges()
        {
            bool eventFired = false;
            LanguageManager.LanguageChanged += (s, e) => eventFired = true;

            LanguageManager.CurrentLanguage = Language.English;

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void LanguageChanged_EventDoesNotFire_WhenSameLanguage()
        {
            int fireCount = 0;
            LanguageManager.CurrentLanguage = Language.SimplifiedChinese;
            LanguageManager.LanguageChanged += (s, e) => fireCount++;

            LanguageManager.CurrentLanguage = Language.SimplifiedChinese;

            Assert.That(fireCount, Is.EqualTo(0));
        }
    }
}
