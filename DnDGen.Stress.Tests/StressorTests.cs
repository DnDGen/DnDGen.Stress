using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class StressorTests
    {
        private const int TestCount = 68;
        private const int TestCaseCount = 49;

        private Stressor stressor;
        private StressorOptions options;
        private Mock<ILogger> mockLogger;

        [SetUp]
        public void Setup()
        {
            options = new StressorOptions();
            options.RunningAssembly = Assembly.GetExecutingAssembly();

            mockLogger = new Mock<ILogger>();

            stressor = new Stressor(options, mockLogger.Object);
        }

        [Test]
        public void TestCountIsCorrect()
        {
            var types = options.RunningAssembly.GetTypes();
            var methods = types.SelectMany(t => t.GetMethods());
            var activeStressTests = methods.Where(m => IsActiveTest(m));
            var stressTestsCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestAttribute>(true).Count());

            Assert.That(TestCount, Is.EqualTo(stressTestsCount));
        }

        [Test]
        public void TestCaseCountIsCorrect()
        {
            var types = options.RunningAssembly.GetTypes();
            var methods = types.SelectMany(t => t.GetMethods());
            var activeStressTests = methods.Where(m => IsActiveTest(m));
            var stressTestCasesCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestCaseAttribute>().Count(tc => TestCaseIsActive(tc)));

            Assert.That(TestCaseCount, Is.EqualTo(stressTestCasesCount));
        }

        private static bool IsActiveTest(MethodInfo method)
        {
            if (method.GetCustomAttributes<IgnoreAttribute>(true).Any())
                return false;

            if (method.GetCustomAttributes<TestAttribute>(true).Any())
                return true;

            return method.GetCustomAttributes<TestCaseAttribute>(true).Any(tc => TestCaseIsActive(tc));
        }

        private static bool TestCaseIsActive(TestCaseAttribute testCase)
        {
            return string.IsNullOrEmpty(testCase.Ignore) && string.IsNullOrEmpty(testCase.IgnoreReason);
        }

        [TestCase(Stressor.ConfidentIterations, 1000000)]
        [TestCase(Stressor.TravisJobBuildTimeLimit, 50 * 60)]
        [TestCase(Stressor.TravisJobOutputTimeLimit, 10 * 60)]
        public void StressorConstant(double constant, double value)
        {
            Assert.That(constant, Is.EqualTo(value));
        }

        [Test]
        public void ThrowExceptionIfOptionsAreNotValid()
        {
            options.RunningAssembly = null;
            Assert.That(() => new Stressor(options), Throws.ArgumentException.With.Message.EqualTo("Stressor Options are not valid"));
        }

        [Test]
        public void SetPercentageViaOptions()
        {
            options.TimeLimitPercentage = .9266;
            stressor = new Stressor(options);
            Assert.That(stressor.TimeLimitPercentage, Is.EqualTo(.9266));
        }

        [Test]
        public void SetMaxAsyncBatchViaOptions()
        {
            options.MaxAsyncBatch = 42;
            stressor = new Stressor(options);
            Assert.That(stressor.MaxAsyncBatch, Is.EqualTo(42));
        }

        [Test]
        public void WhenFullStress_DurationIsLong()
        {
            options.IsFullStress = true;
            stressor = new Stressor(options);
            var oneSecondTimeLimit = new TimeSpan(0, 0, 1);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.GreaterThan(oneSecondTimeLimit));
        }

        [Test]
        public void WhenNotFullStress_DurationIs1Second()
        {
            var oneSecondTimeLimit = new TimeSpan(0, 0, 1);

            Assert.That(stressor.IsFullStress, Is.False);
            Assert.That(stressor.TimeLimit, Is.EqualTo(oneSecondTimeLimit));
        }

        [TestCase(1)]
        [TestCase(.9)]
        public void ConstructWithSetTestCount(double percentage)
        {
            options.IsFullStress = true;
            options.TestCount = 100;
            options.RunningAssembly = null;
            options.TimeLimitPercentage = percentage;

            stressor = new Stressor(options, mockLogger.Object);

            var seconds = Stressor.TravisJobBuildTimeLimit * stressor.TimeLimitPercentage / 100;
            var ticks = seconds * TimeSpan.TicksPerSecond;
            var expectedTimeLimit = new TimeSpan((long)ticks);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [TestCase(1)]
        [TestCase(.9)]
        public void ConstructWithCountFromRunningAssembly(double percentage)
        {
            options.IsFullStress = true;
            options.TimeLimitPercentage = percentage;

            stressor = new Stressor(options, mockLogger.Object);

            var seconds = Stressor.TravisJobBuildTimeLimit * stressor.TimeLimitPercentage / (TestCount + TestCaseCount);
            var ticks = seconds * TimeSpan.TicksPerSecond;
            var expectedTimeLimit = new TimeSpan((long)ticks);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void CountDoesNotIncludeIgnoredTests()
        {
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount + TestCaseCount));
        }

        [Test]
        [Ignore("Ignoring to verify count")]
        public void IgnoredTest()
        {
            Assert.Fail("This test should be ignored");
        }

        [Test]
        public void CountIncludesTests()
        {
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount + TestCaseCount));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CountIncludesTestCases(int caseNumber)
        {
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount + TestCaseCount));
        }

        [TestCase(1, Ignore = "Ignoring this case")]
        [TestCase(2, IgnoreReason = "Ignoring this case")]
        [TestCase(3)]
        public void CountDoesNotIncludeIgnoredTestCases(int caseNumber)
        {
            Assert.That(caseNumber, Is.EqualTo(3));
        }

        [TestCase(1)]
        [TestCase(2)]
        [Ignore("Ignoring to verify count")]
        public void CountDoesNotIncludeAnyIgnoredTestCases(int caseNumber)
        {
            Assert.Fail("This test should be ignored");
        }

        [TestCase(1)]
        [TestCase(.9)]
        public void DurationIsTotalDurationDividedByTestCount(double percentage)
        {
            options.IsFullStress = true;
            options.TimeLimitPercentage = percentage;

            stressor = new Stressor(options, mockLogger.Object);

            var seconds = Stressor.TravisJobBuildTimeLimit * stressor.TimeLimitPercentage / (TestCount + TestCaseCount);
            var ticks = seconds * TimeSpan.TicksPerSecond;
            var expectedTimeLimit = new TimeSpan((long)ticks);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [TestCase(1)]
        [TestCase(.9)]
        public void DurationIsPercentageOfOutputLimit(double percentage)
        {
            options.IsFullStress = true;
            options.TimeLimitPercentage = percentage;
            options.RunningAssembly = null;
            options.TestCount = 5;

            stressor = new Stressor(options, mockLogger.Object);
            var seconds = Convert.ToInt32(Stressor.TravisJobOutputTimeLimit * percentage);
            var expectedTimeLimit = new TimeSpan(0, 0, seconds);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void ThrowExceptionIfNoTestsDetected()
        {
            options.RunningAssembly = Assembly.GetAssembly(typeof(int));
            options.IsFullStress = true;

            Assert.That(() => stressor = new Stressor(options, mockLogger.Object), Throws.ArgumentException.With.Message.EqualTo("No tests were detected in the running assembly"));
        }

        [Test]
        public void ThrowExceptionIfNoTestsDetectedAndNotFullStress()
        {
            options.RunningAssembly = Assembly.GetAssembly(typeof(int));
            options.IsFullStress = false;

            Assert.That(() => stressor = new Stressor(options, mockLogger.Object), Throws.ArgumentException.With.Message.EqualTo("No tests were detected in the running assembly"));
        }

        [Test]
        public void GetActiveTestCount()
        {
            var count = Stressor.CountStressTestsIn(options.RunningAssembly);
            Assert.That(count, Is.EqualTo(TestCount + TestCaseCount));
        }
    }
}
