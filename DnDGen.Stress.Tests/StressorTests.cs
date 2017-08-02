using NUnit.Framework;
using System;
using System.Reflection;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class StressorTests
    {
        private const int TestCount = 39;
        private const int TestCaseCount = 6;

        private Stressor stressor;
        private Assembly runningAssembly;

        [SetUp]
        public void Setup()
        {
            runningAssembly = Assembly.GetExecutingAssembly();
            stressor = new Stressor(false, runningAssembly);
        }

        [TestCase(Stressor.ConfidentIterations, 1000000)]
        [TestCase(Stressor.TravisJobBuildTimeLimit, 50 * 60)]
        [TestCase(Stressor.TravisJobOutputTimeLimit, 10 * 60)]
        [TestCase(Stressor.TimeLimitPercentage, .9)]
        public void StressorConstant(double constant, double value)
        {
            Assert.That(constant, Is.EqualTo(value));
        }

        [Test]
        public void WhenFullStress_DurationIsLong()
        {
            stressor = new Stressor(true, runningAssembly);
            var expectedTimeLimit = new TimeSpan(0, 1, 0);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void WhenNotFullStress_DurationIs1Second()
        {
            var expectedTimeLimit = new TimeSpan(0, 0, 1);

            Assert.That(stressor.IsFullStress, Is.False);
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
        public void CountDoesNotIncludeIgnoredTestCases(int caseNumber)
        {
            Assert.Fail("This test should be ignored");
        }

        [TestCase(1)]
        [TestCase(2)]
        [Ignore("Ignoring to verify count")]
        public void CountDoesNotIncludeAnyIgnoredTestCases(int caseNumber)
        {
            Assert.Fail("This test should be ignored");
        }

        [Test]
        public void DurationIsTotalDurationDividedByTestCount()
        {
            stressor = new Stressor(true, runningAssembly);
            var seconds = Stressor.TravisJobBuildTimeLimit * Stressor.TimeLimitPercentage / (TestCount + TestCaseCount);
            var expectedTimeLimit = new TimeSpan((long)seconds * TimeSpan.TicksPerSecond);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void ThrowExceptionIfNoTestsDetected()
        {
            runningAssembly = Assembly.GetAssembly(typeof(int));
            Assert.That(() => stressor = new Stressor(true, runningAssembly), Throws.ArgumentException.With.Message.EqualTo("No tests were detected in the running assembly"));
        }
    }
}
