using NUnit.Framework;
using System.IO;

namespace ScreenTimeController.Tests
{
    [TestFixture]
    public class LanguageTests
    {
        [Test]
        public void Language_Chinese_HasCorrectValue()
        {
            Assert.That(Language.Chinese, Is.EqualTo(0));
        }

        [Test]
        public void Language_English_HasCorrectValue()
        {
            Assert.That(Language.English, Is.EqualTo(1));
        }

        [Test]
        public void Language_AllValues_AreDefined()
        {
            var values = System.Enum.GetValues<Language>();

            Assert.That(values, Has.Length.EqualTo(2));
            Assert.That(values, Contains.Item(Language.Chinese));
            Assert.That(values, Contains.Item(Language.English));
        }
    }

    [TestFixture]
    public class LanguageManagerTests : TestBase
    {
        private LanguageManager _languageManager = null!;
        private string _testLanguagePath = string.Empty;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _testLanguagePath = GetTestFilePath("language.json");
            _languageManager = new LanguageManager(_testLanguagePath);
        }

        [TearDown]
        public override void Teardown()
        {
            base.Teardown();
        }

        [Test]
        public void Constructor_ValidPath_CreatesInstance()
        {
            Assert.That(_languageManager, Is.Not.Null);
        }

        [Test]
        public void GetCurrentLanguage_NoLanguage_ReturnsDefault()
        {
            Language language = _languageManager.GetCurrentLanguage();

            Assert.That(language, Is.EqualTo(Language.Chinese));
        }

        [Test]
        public void SetLanguage_ValidLanguage_SetsSuccessfully()
        {
            _languageManager.SetLanguage(Language.English);

            Language language = _languageManager.GetCurrentLanguage();
            Assert.That(language, Is.EqualTo(Language.English));
        }

        [Test]
        public void SetLanguage_Chinese_SetsSuccessfully()
        {
            _languageManager.SetLanguage(Language.Chinese);

            Language language = _languageManager.GetCurrentLanguage();
            Assert.That(language, Is.EqualTo(Language.Chinese));
        }

        [Test]
        public void GetString_ValidKey_ReturnsCorrectString()
        {
            string key = "AppName";

            string result = _languageManager.GetString(key);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.EqualTo(key));
        }

        [Test]
        public void GetString_InvalidKey_ReturnsKey()
        {
            string key = "NonExistentKey";

            string result = _languageManager.GetString(key);

            Assert.That(result, Is.EqualTo(key));
        }

        [Test]
        public void GetString_NullKey_ReturnsEmpty()
        {
            string result = _languageManager.GetString(null!);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetString_EmptyKey_ReturnsEmpty()
        {
            string result = _languageManager.GetString("");

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetString_WithLanguage_ReturnsCorrectLanguage()
        {
            string key = "AppName";

            string chineseResult = _languageManager.GetString(key, Language.Chinese);
            string englishResult = _languageManager.GetString(key, Language.English);

            Assert.That(chineseResult, Is.Not.EqualTo(englishResult));
        }

        [Test]
        public void SaveAndLoad_PersistsLanguage()
        {
            _languageManager.SetLanguage(Language.English);

            var newManager = new LanguageManager(_testLanguagePath);
            Language language = newManager.GetCurrentLanguage();

            Assert.That(language, Is.EqualTo(Language.English));
        }

        [Test]
        public void GetAvailableLanguages_ReturnsAllLanguages()
        {
            var languages = _languageManager.GetAvailableLanguages();

            Assert.That(languages, Is.Not.Null);
            Assert.That(languages, Has.Length.EqualTo(2));
            Assert.That(languages, Contains.Item(Language.Chinese));
            Assert.That(languages, Contains.Item(Language.English));
        }

        [Test]
        public void GetLanguageName_Chinese_ReturnsCorrectName()
        {
            string name = _languageManager.GetLanguageName(Language.Chinese);

            Assert.That(name, Is.EqualTo("中文"));
        }

        [Test]
        public void GetLanguageName_English_ReturnsCorrectName()
        {
            string name = _languageManager.GetLanguageName(Language.English);

            Assert.That(name, Is.EqualTo("English"));
        }

        [Test]
        public void Reload_ReloadsLanguageFile()
        {
            _languageManager.SetLanguage(Language.English);
            _languageManager.Reload();

            Language language = _languageManager.GetCurrentLanguage();
            Assert.That(language, Is.EqualTo(Language.English));
        }

        [Test]
        public void GetString_WithFormat_ReturnsFormattedString()
        {
            string key = "TimeRemaining";
            object[] args = new object[] { 30 };

            string result = _languageManager.GetString(key, args);

            Assert.That(result, Does.Contain("30"));
        }

        [Test]
        public void HasKey_ExistingKey_ReturnsTrue()
        {
            string key = "AppName";

            bool hasKey = _languageManager.HasKey(key);

            Assert.That(hasKey, Is.True);
        }

        [Test]
        public void HasKey_NonExistentKey_ReturnsFalse()
        {
            string key = "NonExistentKey";

            bool hasKey = _languageManager.HasKey(key);

            Assert.That(hasKey, Is.False);
        }

        [Test]
        public void GetAllKeys_ReturnsAllKeys()
        {
            var keys = _languageManager.GetAllKeys();

            Assert.That(keys, Is.Not.Null);
            Assert.That(keys, Is.Not.Empty);
        }

        [Test]
        public void GetString_WithNullArgs_ReturnsString()
        {
            string key = "AppName";

            string result = _languageManager.GetString(key, null!);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetString_WithEmptyArgs_ReturnsString()
        {
            string key = "AppName";

            string result = _languageManager.GetString(key, new object[0]);

            Assert.That(result, Is.Not.Null);
        }
    }
}
