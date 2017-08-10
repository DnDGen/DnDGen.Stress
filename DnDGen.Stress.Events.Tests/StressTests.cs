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
    public class StressTests
    {
        private StressorWithEvents stressor;
        private StringBuilder console;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;
        private Guid clientId;
        private Stopwatch stopwatch;
        private StressorWithEventsOptions options;
        private int runTestCount;
        private int runTestTotal;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            runTestCount = 0;
            runTestTotal = CountTotalTests();
        }

        private int CountTotalTests()
        {
            var type = GetType();
            var methods = type.GetMethods();
            var activeStressTests = methods.Where(m => IsActiveTest(m));
            var testsCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestAttribute>(true).Count());
            var testCasesCount = activeStressTests.Sum(m => m.GetCustomAttributes<TestCaseAttribute>().Count(tc => TestCaseIsActive(tc)));
            var testsTotal = testsCount + testCasesCount;

            return testsTotal;
        }

        private bool IsActiveTest(MethodInfo method)
        {
            if (method.GetCustomAttributes<IgnoreAttribute>(true).Any())
                return false;

            if (method.GetCustomAttributes<TestAttribute>(true).Any())
                return true;

            return method.GetCustomAttributes<TestCaseAttribute>(true).Any(tc => TestCaseIsActive(tc));
        }

        private bool TestCaseIsActive(TestCaseAttribute testCase)
        {
            return string.IsNullOrEmpty(testCase.Ignore) && string.IsNullOrEmpty(testCase.IgnoreReason);
        }

        [SetUp]
        public void Setup()
        {
            mockClientIdManager = new Mock<ClientIDManager>();
            mockEventQueue = new Mock<GenEventQueue>();

            options = new StressorWithEventsOptions();
            options.RunningAssembly = Assembly.GetExecutingAssembly();
            options.ClientIdManager = mockClientIdManager.Object;
            options.EventQueue = mockEventQueue.Object;
            options.Source = "Unit Test";

            stressor = new StressorWithEvents(options);

            console = new StringBuilder();
            var writer = new StringWriter(console);

            Console.SetOut(writer);

            stopwatch = new Stopwatch();
            clientId = Guid.Empty;
            var count = 1;

            mockClientIdManager.Setup(m => m.SetClientID(It.IsAny<Guid>())).Callback((Guid g) => clientId = g);
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns((Guid g) => new[]
            {
                new GenEvent(options.Source, $"Event {count++} for {g}"),
                new GenEvent(options.Source, $"Event {count++} for {g}"),
                new GenEvent("Wrong Source", $"Wrong event for {g}"),
            });
        }

        [TearDown]
        public void Teardown()
        {
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            runTestCount++;
            Console.WriteLine($"Test {runTestCount} of {runTestTotal} for Stress() method for StressorWithEvents completed");
        }

        [Test]
        public void StopsWhenTimeLimitHit()
        {
            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => SlowTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            options.IsFullStress = true;
            options.RunningAssembly = null;
            options.TestCount = 1;

            stressor = new StressorWithEvents(options);

            //Returning no events, as that makes the generation go faster, and is more likely to actually hit the iteration limit within the time frame
            //Separate tests exist that verify that event spacing and order are asserted for this method
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns(Enumerable.Empty<GenEvent>());

            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => FastTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations));
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
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:0"));
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
            Assert.That(lines.Length, Is.EqualTo(7));
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
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesEventSummaryToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void ClearsRemainingEvents()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void ClearsRemainingEventsOnFailure()
        {
            Assert.That(() => stressor.Stress(FailedTest), Throws.InstanceOf<AssertionException>());

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(7));
            Assert.That(lines[6], Is.EqualTo($"0 events were logged in total"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void StressATest()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GenerateWithinStressHonorsTimeLimit()
        {
            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => TestWithGenerate(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        private void TestWithGenerate(ref int count)
        {
            var innerCount = count;
            count = stressor.Generate(() => innerCount++, c => c > 1);
            Assert.That(count, Is.AtLeast(2));
        }

        [Test]
        public void SetsClientIdForEvents()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingForStressIsWithin1SecondOfEachOther()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-998) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
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

            var count = 0;
            Assert.That(() => stressor.Stress(() => FastTest(ref count)), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(11));
            Assert.That(lines[6], Is.EqualTo($"2 events were logged in total"));
            Assert.That(lines[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(lines[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(lines[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
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

            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreOrderedChronologically()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Does.EndWith($" from Wrong Source"));
            Assert.That(lines[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[10], Does.Contain(clientId.ToString()));
            Assert.That(lines[11], Does.Contain(clientId.ToString()));
            Assert.That(lines[12], Does.Contain(clientId.ToString()));
            Assert.That(lines[13], Does.Contain(clientId.ToString()));
            Assert.That(lines[14], Does.Contain(clientId.ToString()));
            Assert.That(lines[15], Does.Contain(clientId.ToString()));
            Assert.That(lines[16], Does.Contain(clientId.ToString()));
            Assert.That(lines[17], Does.Contain(clientId.ToString()));
            Assert.That(lines[18], Does.Contain(clientId.ToString()));
            Assert.That(lines[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreNotOrderedChronologically()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(10) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var count = 0;
            Assert.That(() => stressor.Stress(() => FastTest(ref count)), Throws.InstanceOf<AssertionException>());

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(11));
            Assert.That(lines[6], Is.EqualTo($"2 events were logged in total"));
            Assert.That(lines[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(lines[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(lines[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(lines[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void DoNotAssertEventsBetweenGenerations()
        {
            var count = 0;

            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(() => new[] {
                    new GenEvent("Unit Test", "First message") { When = DateTime.Now.AddMilliseconds(count * 1000) },
                    new GenEvent("Unit Test", "Last message") { When = DateTime.Now.AddMilliseconds(count * 1000 + 1) },
                });

            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(19));
            Assert.That(lines[6], Does.EndWith($" events were logged in total"));
            Assert.That(lines[7], Does.EndWith($" from Unit Test"));
            Assert.That(lines[8], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(lines[9], Does.EndWith($"] Unit Test: First message"));
            Assert.That(lines[10], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(lines[11], Does.EndWith($"] Unit Test: First message"));
            Assert.That(lines[12], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(lines[13], Does.EndWith($"] Unit Test: First message"));
            Assert.That(lines[14], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(lines[15], Does.EndWith($"] Unit Test: First message"));
            Assert.That(lines[16], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(lines[17], Does.EndWith($"] Unit Test: First message"));
            Assert.That(lines[18], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [TestCase(65)]
        [TestCase(70)]
        public void PercentageIsAccurate(int testCount)
        {
            options.IsFullStress = true;
            options.TestCount = testCount;
            options.RunningAssembly = null;

            stressor = new StressorWithEvents(options);

            var count = 0;
            stressor.Stress(() => SlowTest(ref count));

            var output = console.ToString();
            var lines = output.Split('\r', '\n').Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var time = stressor.TimeLimit.ToString().Substring(0, 10);

            Assert.That(output, Is.Not.Empty);
            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(20));
            Assert.That(lines[1], Is.EqualTo($"Stress test complete"));
            Assert.That(lines[2], Does.StartWith($"\tTime: {time}"));
            Assert.That(lines[2], Does.Contain($"(100"));
            Assert.That(lines[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(lines[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(lines[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void IterationsWithEventsCanComplete(int eventCount)
        {
            options.IsFullStress = true;

            stressor = new StressorWithEvents(options);

            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns(() => GetEvents(eventCount));

            var count = 0;
            stressor.Stress(() => FastTest(ref count));

            Assert.That(count, Is.AtLeast(1));
            Assert.That(count, Is.AtLeast(10));
            Assert.That(count, Is.AtLeast(100));
            Assert.That(count, Is.AtLeast(1000));

            if (eventCount < 1000)
                Assert.That(count, Is.AtLeast(10000));
        }

        private IEnumerable<GenEvent> GetEvents(int eventCount)
        {
            var events = new List<GenEvent>(eventCount);

            while (events.Capacity > events.Count)
            {
                var genEvent = new GenEvent("Unit Test", Guid.NewGuid().ToString());
                events.Add(genEvent);
            }

            return events;
        }
    }
}
