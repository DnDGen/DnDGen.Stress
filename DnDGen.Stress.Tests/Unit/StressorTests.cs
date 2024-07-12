using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace DnDGen.Stress.Tests.Unit
{
    [TestFixture]
    public class StressorTests
    {
        //Unit + Integration
        public const int TestCount = 84 + 6;
        public const int TestCaseCount = 149 + 0;

        private Stressor stressor;
        private StressorOptions options;
        private Mock<ILogger> mockLogger;

        [SetUp]
        public void Setup()
        {
            options = new StressorOptions();
            options.RunningAssembly = Assembly.GetExecutingAssembly();
            options.ConfidenceIterations = 10_000;
            options.BuildTimeLimitInSeconds = 1_000;
            options.OutputTimeLimitInSeconds = 100;

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

        [Test]
        public void ThrowExceptionIfOptionsAreNotValid()
        {
            options.RunningAssembly = null;
            Assert.That(() => new Stressor(options), Throws.ArgumentException.With.Message.EqualTo("Stressor Options are not valid"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SetOptions_WithTestCount(bool isFull)
        {
            options.TimeLimitPercentage = .9266;
            options.MaxAsyncBatch = 42;
            options.BuildTimeLimitInSeconds = 90210;
            options.ConfidenceIterations = 600;
            options.IsFullStress = isFull;
            options.OutputTimeLimitInSeconds = 1337;
            options.TestCount = 1336;
            options.RunningAssembly = null;

            Assert.That(options.AreValid, Is.True);

            stressor = new Stressor(options);
            Assert.That(stressor.Options, Is.EqualTo(options));
            Assert.That(stressor.Options.BuildTimeLimitInSeconds, Is.EqualTo(90210));
            Assert.That(stressor.Options.ConfidenceIterations, Is.EqualTo(600));
            Assert.That(stressor.Options.IsFullStress, Is.EqualTo(isFull));
            Assert.That(stressor.Options.MaxAsyncBatch, Is.EqualTo(42));
            Assert.That(stressor.Options.OutputTimeLimitInSeconds, Is.EqualTo(1337));
            Assert.That(stressor.Options.TestCount, Is.EqualTo(1336));
            Assert.That(stressor.StressTestCount, Is.EqualTo(1336));
            Assert.That(stressor.Options.TimeLimitPercentage, Is.EqualTo(.9266));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SetOptions_WithAssembly(bool isFull)
        {
            options.TimeLimitPercentage = .9266;
            options.MaxAsyncBatch = 42;
            options.BuildTimeLimitInSeconds = 90210;
            options.ConfidenceIterations = 600;
            options.IsFullStress = isFull;
            options.OutputTimeLimitInSeconds = 1337;
            options.TestCount = 0;
            options.RunningAssembly = Assembly.GetExecutingAssembly();

            Assert.That(options.AreValid, Is.True);

            stressor = new Stressor(options);
            Assert.That(stressor.Options, Is.EqualTo(options));
            Assert.That(stressor.Options.BuildTimeLimitInSeconds, Is.EqualTo(90210));
            Assert.That(stressor.Options.ConfidenceIterations, Is.EqualTo(600));
            Assert.That(stressor.Options.IsFullStress, Is.EqualTo(isFull));
            Assert.That(stressor.Options.MaxAsyncBatch, Is.EqualTo(42));
            Assert.That(stressor.Options.OutputTimeLimitInSeconds, Is.EqualTo(1337));
            Assert.That(stressor.Options.TestCount, Is.Zero);
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount + TestCaseCount));
            Assert.That(stressor.Options.TimeLimitPercentage, Is.EqualTo(.9266));
        }

        [Test]
        public void WhenFullStress_DurationIsLong()
        {
            options.IsFullStress = true;
            stressor = new Stressor(options);
            var oneSecondTimeLimit = new TimeSpan(0, 0, 1);

            Assert.That(stressor.Options.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.GreaterThan(oneSecondTimeLimit));
        }

        [Test]
        public void WhenNotFullStress_DurationIs1Second()
        {
            var oneSecondTimeLimit = new TimeSpan(0, 0, 1);

            Assert.That(stressor.Options.IsFullStress, Is.False);
            Assert.That(stressor.TimeLimit, Is.EqualTo(oneSecondTimeLimit));
        }

        [TestCase(1, 1, 100)]
        [TestCase(1, 2, 100)]
        [TestCase(1, 10, 100)]
        [TestCase(1, 100, 10)]
        [TestCase(1, 1000, 1)]
        [TestCase(0.95, 1, 95)]
        [TestCase(0.95, 2, 95)]
        [TestCase(0.95, 10, 95)]
        [TestCase(0.95, 100, 9.5)]
        [TestCase(0.95, 1000, .95)]
        [TestCase(0.9, 1, 90)]
        [TestCase(0.9, 2, 90)]
        [TestCase(0.9, 10, 90)]
        [TestCase(0.9, 100, 9)]
        [TestCase(0.9, 1000, .9)]
        [TestCase(0.8, 1, 80)]
        [TestCase(0.8, 2, 80)]
        [TestCase(0.8, 10, 80)]
        [TestCase(0.8, 100, 8)]
        [TestCase(0.8, 1000, .8)]
        [TestCase(0.5, 1, 50)]
        [TestCase(0.5, 2, 50)]
        [TestCase(0.5, 10, 50)]
        [TestCase(0.5, 100, 5)]
        [TestCase(0.5, 1000, 0.5)]
        public void ConstructWithSetTestCount(double percentage, int testCount, double timeLimitSeconds)
        {
            options.IsFullStress = true;
            options.TestCount = testCount;
            options.RunningAssembly = null;
            options.TimeLimitPercentage = percentage;

            stressor = new Stressor(options, mockLogger.Object);

            Assert.That(stressor.Options.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(timeLimitSeconds)).Within(1).Seconds);
        }

        [TestCase(1, 5)]
        [TestCase(.95, 4.9)]
        [TestCase(.9, 4.5)]
        [TestCase(.8, 4)]
        [TestCase(.5, 2.5)]
        public void ConstructWithCountFromRunningAssembly(double percentage, double timeLimitSeconds)
        {
            options.IsFullStress = true;
            options.TimeLimitPercentage = percentage;

            stressor = new Stressor(options, mockLogger.Object);

            Assert.That(stressor.Options.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(timeLimitSeconds)).Within(1).Seconds);
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

        [TestCase(1, 5.1)]
        [TestCase(.95, 4.9)]
        [TestCase(.9, 4.6)]
        [TestCase(.8, 4.1)]
        [TestCase(.5, 2.5)]
        public void DurationIsTotalDurationDividedByTestCount(double percentage, double timeLimitSeconds)
        {
            options.IsFullStress = true;
            options.TimeLimitPercentage = percentage;

            stressor = new Stressor(options, mockLogger.Object);

            Assert.That(stressor.Options.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(TimeSpan.FromSeconds(timeLimitSeconds)).Within(1).Seconds);
        }

        [TestCase(1)]
        [TestCase(.95)]
        [TestCase(.9)]
        [TestCase(.8)]
        [TestCase(.5)]
        public void DurationIsPercentageOfOutputLimit(double percentage)
        {
            options.IsFullStress = true;
            options.TimeLimitPercentage = percentage;
            options.RunningAssembly = null;
            options.TestCount = 5;

            stressor = new Stressor(options, mockLogger.Object);

            Assert.That(stressor.Options.IsFullStress, Is.True);

            var expected = TimeSpan.FromSeconds(options.OutputTimeLimitInSeconds * percentage);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expected).Within(1).Seconds);
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
