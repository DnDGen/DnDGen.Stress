using NUnit.Framework;
using System;
using System.Reflection;

namespace StressGen.Tests
{
    [TestFixture]
    public class StressorTests
    {
        private const int TestCount = 21;

        private Stressor stressor;

        [SetUp]
        public void Setup()
        {
            stressor = new Stressor(false, Assembly.GetExecutingAssembly());
        }

        [Test]
        public void Confidence()
        {
            Assert.That(Stressor.ConfidentIterations, Is.EqualTo(1000000));
        }

        [Test]
        public void TravisBuildLimit()
        {
            Assert.That(Stressor.TravisJobBuildTimeLimit, Is.EqualTo(50 * 60 - 3 * 60));
        }

        [Test]
        public void TravisOutputLimit()
        {
            Assert.That(Stressor.TravisJobOutputTimeLimit, Is.EqualTo(10 * 60));
        }

        [Test]
        public void WhenFullStress_DurationIsLong()
        {
            stressor = new Stressor(true, Assembly.GetExecutingAssembly());
            var expectedTimeLimit = new TimeSpan(0, 2, 14);

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
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount));
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
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CountIncludesTestCases(int caseNumber)
        {
            Assert.That(stressor.StressTestCount, Is.EqualTo(TestCount));
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
            stressor = new Stressor(true, Assembly.GetExecutingAssembly());
            var expectedTimeLimit = new TimeSpan(0, 2, 14);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void DurationIs10MinutesMinus10Seconds()
        {
            stressor = new Stressor(true, Assembly.GetExecutingAssembly());
            var expectedTimeLimit = new TimeSpan(0, 9, 50);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void StopsWhenShortDurationHit()
        {
            Assert.Fail();
        }

        [Test]
        public void StopsWhenLongDurationHit()
        {
            Assert.Fail();
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            Assert.Fail();
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            Assert.Fail();
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            Assert.Fail();
        }

        [Test]
        public void StressATest()
        {
            Assert.Fail();
        }

        [Test]
        public void StressATestWithSetupAndTeardown()
        {
            Assert.Fail();
        }

        [Test]
        public void Generate()
        {
            Assert.Fail();
        }

        [Test]
        public void GenerateAndSucceed()
        {
            Assert.Fail();
        }

        [Test]
        public void GenerateAndFail()
        {
            Assert.Fail();
        }
    }
}
