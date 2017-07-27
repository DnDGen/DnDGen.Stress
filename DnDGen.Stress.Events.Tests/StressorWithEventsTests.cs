using EventGen;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DnDGen.Stress.Events.Tests
{
    [TestFixture]
    public class StressorWithEventsTests
    {
        private const int TestCount = 27;
        private const int TestCaseCount = 2;

        private StressorWithEvents stressor;
        private Assembly runningAssembly;
        private Stopwatch stopwatch;
        private StringBuilder console;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;
        private Guid clientId;

        [SetUp]
        public void Setup()
        {
            mockClientIdManager = new Mock<ClientIDManager>();
            mockEventQueue = new Mock<GenEventQueue>();
            runningAssembly = Assembly.GetExecutingAssembly();
            stressor = new StressorWithEvents(false, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object);
            stopwatch = new Stopwatch();
            console = new StringBuilder();
            var writer = new StringWriter(console);

            Console.SetOut(writer);

            clientId = Guid.Empty;
            mockClientIdManager.Setup(m => m.SetClientID(It.IsAny<Guid>())).Callback((Guid g) => clientId = g);
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns((Guid g) => new[]
            {
                new GenEvent("Unit Tests", $"Event 1 for {g}"),
                new GenEvent("Unit Tests", $"Event 2 for {g}"),
                new GenEvent("Unit Tests", $"Event 3 for {g}"),
                new GenEvent("Unit Tests", $"Event 4 for {g}"),
                new GenEvent("Unit Tests", $"Event 5 for {g}"),
                new GenEvent("Unit Tests", $"Event 6 for {g}"),
                new GenEvent("Unit Tests", $"Event 7 for {g}"),
                new GenEvent("Unit Tests", $"Event 8 for {g}"),
                new GenEvent("Unit Tests", $"Event 9 for {g}"),
                new GenEvent("Unit Tests", $"Event 10 for {g}"),
                new GenEvent("Unit Tests", $"Event 11 for {g}"),
                new GenEvent("Unit Tests", $"Event 12 for {g}"),
            });
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
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object);
            var expectedTimeLimit = new TimeSpan(0, 1, 22);

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
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object);
            var seconds = Stressor.TravisJobBuildTimeLimit / (TestCount + TestCaseCount);
            var expectedTimeLimit = new TimeSpan(0, 0, seconds);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void ThrowExceptionIfNoTestsDetected()
        {
            runningAssembly = Assembly.GetAssembly(typeof(int));
            Assert.That(() => stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object), Throws.ArgumentException.With.Message.EqualTo("No tests were detected in the running assembly"));
        }

        [Test]
        public void StopsWhenTimeLimitHit()
        {
            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => SlowTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));
        }

        private void SlowTest(ref int count)
        {
            FastTest(ref count);
            Thread.Sleep(1);
        }

        private void FastTest(ref int count)
        {
            count++;
            Assert.That(count, Is.Positive);
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            //INFO: Need longer timeout to hit confidence iterations, as event logging takes more time
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object);

            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => FastTest(ref count));
            stopwatch.Stop();

            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations));
            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: 1"));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: 1"));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            try
            {
                stressor.Stress(FailedTest);
            }
            catch { }

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(lines[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
            Assert.That(lines[4], Is.EqualTo($"\tIterations Per Second: 0"));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: FAILED"));
        }

        private void FailedTest()
        {
            Assert.Fail("This test should fail");
        }

        [Test]
        public void WritesStressSlowSummaryToConsole()
        {
            var count = 0;
            stressor.Stress(() => SlowTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: 5"));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: 5"));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void StressATest()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100000));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.EndWith($"] Unit Test: Event 12 for {clientId}"));
            Assert.That(lines[7], Does.EndWith($"] Unit Test: Event 11 for {clientId}"));
            Assert.That(lines[8], Does.EndWith($"] Unit Test: Event 10 for {clientId}"));
            Assert.That(lines[9], Does.EndWith($"] Unit Test: Event 9 for {clientId}"));
            Assert.That(lines[10], Does.EndWith($"] Unit Test: Event 8 for {clientId}"));
            Assert.That(lines[11], Does.EndWith($"] Unit Test: Event 7 for {clientId}"));
            Assert.That(lines[12], Does.EndWith($"] Unit Test: Event 6 for {clientId}"));
            Assert.That(lines[13], Does.EndWith($"] Unit Test: Event 5 for {clientId}"));
            Assert.That(lines[14], Does.EndWith($"] Unit Test: Event 4 for {clientId}"));
            Assert.That(lines[15], Does.EndWith($"] Unit Test: Event 3 for {clientId}"));
        }

        [Test]
        public void StressATestWithSetupAndTeardown()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(() => TestSetup(ref count, ref setup), () => FastTest(ref count), () => TestTeardown(ref count, ref teardown));
            Assert.That(count, Is.EqualTo(2));
            Assert.That(setup, Is.AtLeast(100000));
            Assert.That(teardown, Is.AtLeast(100000));

            Assert.Fail("Assert events are logged");
        }

        private void TestSetup(ref int count, ref int setup)
        {
            count = 0;
            setup++;
        }

        private void TestTeardown(ref int count, ref int teardown)
        {
            if (count == 1)
                count++;

            teardown++;
        }

        [Test]
        public void Generate()
        {
            var count = 0;
            var result = stressor.Generate(() => count++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));

            Assert.Fail("Assert events are logged");
        }

        [Test]
        public void GenerateBeyondTimeout()
        {
            var count = 0;

            stopwatch.Start();
            var result = stressor.Generate(() => count++, c => c == 9266);
            stopwatch.Stop();

            Assert.That(result, Is.EqualTo(9266));
            Assert.That(stopwatch.Elapsed, Is.GreaterThan(stressor.TimeLimit));

            Assert.Fail("Assert events are logged");
        }

        [Test]
        public void GenerateAndSucceed()
        {
            var count = 0;
            var result = stressor.GenerateOrFail(() => count++, c => c == 9266);
            Assert.That(result, Is.EqualTo(9266));

            Assert.Fail("Assert events are logged");
        }

        [Test]
        public void GenerateAndFail()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c == 90210), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

            Assert.Fail("Assert events are logged");
        }

        [Test]
        public void WriteEventSummaryToConsole()
        {
            Assert.Fail();
        }

        [Test]
        public void WriteFewEventSummaryToConsole()
        {
            Assert.Fail();
        }

        [Test]
        public void WriteNoEventSummaryToConsole()
        {
            Assert.Fail();
        }

        [Test]
        public void WriteFailedEventSummaryToConsole()
        {
            Assert.Fail();
        }

        [Test]
        public void EventSpacingIsWithin1SecondOfEachOther()
        {
            Assert.Fail();
        }

        [Test]
        public void EventSpacingIsNotWithin1SecondOfEachOther()
        {
            Assert.Fail();
        }

        [Test]
        public void ReEnqueueEventsAfterAssertingSpacing()
        {
            Assert.Fail();
        }

        [Test]
        public void ReEnqueueEventsAfterAssertingFailedSpacing()
        {
            Assert.Fail();
        }

        [Test]
        public void EventsAreOrderedChronoligcally()
        {
            Assert.Fail();
        }

        [Test]
        public void EventsAreNotOrderedChronoligcally()
        {
            Assert.Fail();
        }

        [Test]
        public void DoNotAssertEventsBetweenGenerations()
        {
            Assert.Fail();
        }
    }
}
