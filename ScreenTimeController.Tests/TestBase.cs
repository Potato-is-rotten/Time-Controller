using NUnit.Framework;
using System.IO;

namespace ScreenTimeController.Tests
{
    [SetUpFixture]
    public class TestSetup
    {
        public static string TestDirectory { get; private set; } = string.Empty;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            TestDirectory = Path.Combine(Path.GetTempPath(), "ScreenTimeControllerTests");
            if (!Directory.Exists(TestDirectory))
            {
                Directory.CreateDirectory(TestDirectory);
            }
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            if (Directory.Exists(TestDirectory))
            {
                try
                {
                    Directory.Delete(TestDirectory, true);
                }
                catch
                {
                }
            }
        }
    }

    public abstract class TestBase
    {
        protected string TestFolderPath { get; private set; } = string.Empty;

        [SetUp]
        public virtual void Setup()
        {
            TestFolderPath = Path.Combine(TestSetup.TestDirectory, TestContext.CurrentContext.Test.Name);
            if (!Directory.Exists(TestFolderPath))
            {
                Directory.CreateDirectory(TestFolderPath);
            }
        }

        [TearDown]
        public virtual void Teardown()
        {
            if (Directory.Exists(TestFolderPath))
            {
                try
                {
                    Directory.Delete(TestFolderPath, true);
                }
                catch
                {
                }
            }
        }

        protected string GetTestFilePath(string fileName)
        {
            return Path.Combine(TestFolderPath, fileName);
        }
    }
}
