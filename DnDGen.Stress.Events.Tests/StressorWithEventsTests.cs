using EventGen;
using Moq;
using NUnit.Framework;
using System;
using System.Reflection;

namespace DnDGen.Stress.Events.Tests
{
    [TestFixture]
    public class StressorWithEventsTests
    {
        private const int TestCount = 65;
        private const int TestCaseCount = 6;

        private Stressor stressor;
        private Assembly runningAssembly;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;
        private Guid clientId;

        [SetUp]
        public void Setup()
        {
            mockClientIdManager = new Mock<ClientIDManager>();
            mockEventQueue = new Mock<GenEventQueue>();
            runningAssembly = Assembly.GetExecutingAssembly();
            stressor = new StressorWithEvents(false, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");

            clientId = Guid.Empty;
            var count = 1;

            mockClientIdManager.Setup(m => m.SetClientID(It.IsAny<Guid>())).Callback((Guid g) => clientId = g);
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns((Guid g) => new[]
            {
                new GenEvent("Unit Test", $"Event {count++} for {g}"),
                new GenEvent("Unit Test", $"Event {count++} for {g}"),
                new GenEvent("Wrong Source", $"Wrong event for {g}"),
            });
        }

        [TestCase(Stressor.ConfidentIterations, 1000000)]
        [TestCase(Stressor.TravisJobBuildTimeLimit, 50 * 60)]
        [TestCase(Stressor.TravisJobOutputTimeLimit, 10 * 60)]
        [TestCase(Stressor.TimeLimitPercentage, .9)]
        public void StressorWithEventsConstant(double constant, double value)
        {
            Assert.That(constant, Is.EqualTo(value));
        }

        [Test]
        public void WhenFullStress_DurationIsLong()
        {
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");
            var expectedTimeLimit = new TimeSpan(0, 0, 38);

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
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");
            var seconds = Stressor.TravisJobBuildTimeLimit * Stressor.TimeLimitPercentage / (TestCount + TestCaseCount);
            var expectedTimeLimit = new TimeSpan((long)seconds * TimeSpan.TicksPerSecond);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void ThrowExceptionIfNoTestsDetected()
        {
            runningAssembly = Assembly.GetAssembly(typeof(int));
            Assert.That(() => stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test"), Throws.ArgumentException.With.Message.EqualTo("No tests were detected in the running assembly"));
        }
    }
}
