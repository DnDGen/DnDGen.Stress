using EventGen;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        private const int TestCount = 31;
        private const int TestCaseCount = 2;

        private StressorWithEvents stressor;
        private Assembly runningAssembly;
        private StringBuilder console;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;
        private Guid clientId;
        private Stopwatch stopwatch;

        [SetUp]
        public void Setup()
        {
            mockClientIdManager = new Mock<ClientIDManager>();
            mockEventQueue = new Mock<GenEventQueue>();
            runningAssembly = Assembly.GetExecutingAssembly();
            stressor = new StressorWithEvents(false, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");
            stopwatch = new Stopwatch();
            console = new StringBuilder();
            var writer = new StringWriter(console);

            Console.SetOut(writer);

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

        [TearDown]
        public void Teardown()
        {
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            var output = console.ToString();
            Console.WriteLine(output);
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
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");
            var expectedTimeLimit = new TimeSpan(0, 1, 25);

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
            var seconds = Stressor.TravisJobBuildTimeLimit / (TestCount + TestCaseCount);
            var expectedTimeLimit = new TimeSpan(0, 0, seconds);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }

        [Test]
        public void ThrowExceptionIfNoTestsDetected()
        {
            runningAssembly = Assembly.GetAssembly(typeof(int));
            Assert.That(() => stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test"), Throws.ArgumentException.With.Message.EqualTo("No tests were detected in the running assembly"));
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
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            Assert.That(() => stressor.Stress(FailedTest), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(8));
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
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void StressATest()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(1000));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.Contain(clientId.ToString()));
            Assert.That(lines[7], Does.Contain(clientId.ToString()));
            Assert.That(lines[8], Does.Contain(clientId.ToString()));
            Assert.That(lines[9], Does.Contain(clientId.ToString()));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void StressATestWithSetupAndTeardown()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(() => TestSetup(ref count, ref setup), () => FastTest(ref count), () => TestTeardown(ref count, ref teardown));
            Assert.That(count, Is.EqualTo(2));
            Assert.That(setup, Is.AtLeast(1000));
            Assert.That(teardown, Is.AtLeast(1000));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.Contain(clientId.ToString()));
            Assert.That(lines[7], Does.Contain(clientId.ToString()));
            Assert.That(lines[8], Does.Contain(clientId.ToString()));
            Assert.That(lines[9], Does.Contain(clientId.ToString()));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
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
            var result = stressor.Generate(() => count++, c => c == 42);
            Assert.That(result, Is.EqualTo(42));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.EndWith($"] Unit Test: Event 79 for {clientId}"));
            Assert.That(lines[7], Does.EndWith($"] Unit Test: Event 80 for {clientId}"));
            Assert.That(lines[8], Does.EndWith($"] Unit Test: Event 81 for {clientId}"));
            Assert.That(lines[9], Does.EndWith($"] Unit Test: Event 82 for {clientId}"));
            Assert.That(lines[10], Does.EndWith($"] Unit Test: Event 83 for {clientId}"));
            Assert.That(lines[11], Does.EndWith($"] Unit Test: Event 84 for {clientId}"));
            Assert.That(lines[12], Does.EndWith($"] Unit Test: Event 85 for {clientId}"));
            Assert.That(lines[13], Does.EndWith($"] Unit Test: Event 86 for {clientId}"));
            Assert.That(lines[14], Does.EndWith($"] Unit Test: Event 87 for {clientId}"));
            Assert.That(lines[15], Does.EndWith($"] Unit Test: Event 88 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
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

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.EndWith($"] Unit Test: Event 18527 for {clientId}"));
            Assert.That(lines[7], Does.EndWith($"] Unit Test: Event 18528 for {clientId}"));
            Assert.That(lines[8], Does.EndWith($"] Unit Test: Event 18529 for {clientId}"));
            Assert.That(lines[9], Does.EndWith($"] Unit Test: Event 18530 for {clientId}"));
            Assert.That(lines[10], Does.EndWith($"] Unit Test: Event 18531 for {clientId}"));
            Assert.That(lines[11], Does.EndWith($"] Unit Test: Event 18532 for {clientId}"));
            Assert.That(lines[12], Does.EndWith($"] Unit Test: Event 18533 for {clientId}"));
            Assert.That(lines[13], Does.EndWith($"] Unit Test: Event 18534 for {clientId}"));
            Assert.That(lines[14], Does.EndWith($"] Unit Test: Event 18535 for {clientId}"));
            Assert.That(lines[15], Does.EndWith($"] Unit Test: Event 18536 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GenerateAndSucceed()
        {
            var count = 0;
            var result = stressor.GenerateOrFail(() => count++, c => c == 1337);
            Assert.That(result, Is.EqualTo(1337));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.EndWith($"] Unit Test: Event 2669 for {clientId}"));
            Assert.That(lines[7], Does.EndWith($"] Unit Test: Event 2670 for {clientId}"));
            Assert.That(lines[8], Does.EndWith($"] Unit Test: Event 2671 for {clientId}"));
            Assert.That(lines[9], Does.EndWith($"] Unit Test: Event 2672 for {clientId}"));
            Assert.That(lines[10], Does.EndWith($"] Unit Test: Event 2673 for {clientId}"));
            Assert.That(lines[11], Does.EndWith($"] Unit Test: Event 2674 for {clientId}"));
            Assert.That(lines[12], Does.EndWith($"] Unit Test: Event 2675 for {clientId}"));
            Assert.That(lines[13], Does.EndWith($"] Unit Test: Event 2676 for {clientId}"));
            Assert.That(lines[14], Does.EndWith($"] Unit Test: Event 2677 for {clientId}"));
            Assert.That(lines[15], Does.EndWith($"] Unit Test: Event 2678 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GenerateAndFail()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c == 90210), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.Contain(clientId.ToString()));
            Assert.That(lines[7], Does.Contain(clientId.ToString()));
            Assert.That(lines[8], Does.Contain(clientId.ToString()));
            Assert.That(lines[9], Does.Contain(clientId.ToString()));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void WriteEventSummaryToConsole()
        {
            var count = 0;
            var result = stressor.Generate(() => count++, i => i == 6);
            Assert.That(result, Is.EqualTo(6));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.EndWith($"] Unit Test: Event 7 for {clientId}"));
            Assert.That(lines[7], Does.EndWith($"] Unit Test: Event 8 for {clientId}"));
            Assert.That(lines[8], Does.EndWith($"] Unit Test: Event 9 for {clientId}"));
            Assert.That(lines[9], Does.EndWith($"] Unit Test: Event 10 for {clientId}"));
            Assert.That(lines[10], Does.EndWith($"] Unit Test: Event 11 for {clientId}"));
            Assert.That(lines[11], Does.EndWith($"] Unit Test: Event 12 for {clientId}"));
            Assert.That(lines[12], Does.EndWith($"] Unit Test: Event 13 for {clientId}"));
            Assert.That(lines[13], Does.EndWith($"] Unit Test: Event 14 for {clientId}"));
            Assert.That(lines[14], Does.EndWith($"] Unit Test: Event 15 for {clientId}"));
            Assert.That(lines[15], Does.EndWith($"] Unit Test: Event 16 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void WriteFewEventSummaryToConsole()
        {
            var result = stressor.Generate(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(10));
            Assert.That(lines[6], Does.EndWith($"] Unit Test: Event 1 for {clientId}"));
            Assert.That(lines[7], Does.EndWith($"] Unit Test: Event 2 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void WriteNoEventSummaryToConsole()
        {
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns(Enumerable.Empty<GenEvent>());

            var result = stressor.Generate(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(6));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void WriteFailedEventSummaryToConsole()
        {
            Assert.That(() => stressor.GenerateOrFail(() => 1, i => i < 0), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(16));
            Assert.That(lines[6], Does.Contain(clientId.ToString()));
            Assert.That(lines[7], Does.Contain(clientId.ToString()));
            Assert.That(lines[8], Does.Contain(clientId.ToString()));
            Assert.That(lines[9], Does.Contain(clientId.ToString()));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void SetsClientIdForEvents()
        {
            var result = stressor.Generate(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingIsWithin1SecondOfEachOther()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-999) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var result = stressor.Generate(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(10));
            Assert.That(lines[6], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[7], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingIsNotWithin1SecondOfEachOther()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-1001) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            Assert.That(() => stressor.Generate(() => 1, i => i > 0), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(10));
            Assert.That(lines[6], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[7], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingIsWithin1SecondOfEachOtherWithNonSourceEvents()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-1001) });
            events.Add(new GenEvent("Wrong Source", "Wrong Message") { When = DateTime.Now.AddMilliseconds(-999) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var result = stressor.Generate(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(10));
            Assert.That(lines[6], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[7], Is.EqualTo($"[{events[2].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreOrderedChronologically()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-1) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var result = stressor.Generate(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(10));
            Assert.That(lines[6], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[7], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreNotOrderedChronologically()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(1) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            Assert.That(() => stressor.Generate(() => 1, i => i > 0), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(10));
            Assert.That(lines[6], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[7], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void DoNotAssertEventsBetweenGenerations()
        {
            var earlyEvents = new List<GenEvent>();
            earlyEvents.Add(new GenEvent("Unit Test", "First early message") { When = DateTime.Now.AddMilliseconds(-1000) });
            earlyEvents.Add(new GenEvent("Unit Test", "Last early message") { When = DateTime.Now.AddMilliseconds(-999) });

            var lateEvents = new List<GenEvent>();
            lateEvents.Add(new GenEvent("Unit Test", "First late message") { When = DateTime.Now.AddMilliseconds(999) });
            lateEvents.Add(new GenEvent("Unit Test", "Last late message") { When = DateTime.Now.AddMilliseconds(1000) });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(earlyEvents)
                .Returns(lateEvents);

            var count = 0;
            var result = stressor.Generate(() => count++, i => i == 1);
            Assert.That(result, Is.EqualTo(1));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(12));
            Assert.That(lines[6], Is.EqualTo($"[{earlyEvents[0].When.ToLongTimeString()}] Unit Test: First early message"));
            Assert.That(lines[7], Is.EqualTo($"[{earlyEvents[1].When.ToLongTimeString()}] Unit Test: Last early message"));
            Assert.That(lines[8], Is.EqualTo($"[{lateEvents[0].When.ToLongTimeString()}] Unit Test: First late message"));
            Assert.That(lines[9], Is.EqualTo($"[{lateEvents[1].When.ToLongTimeString()}] Unit Test: Last late message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }
    }
}
