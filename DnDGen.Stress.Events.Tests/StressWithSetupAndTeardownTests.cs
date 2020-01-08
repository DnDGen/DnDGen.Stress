using EventGen;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DnDGen.Stress.Events.Tests
{
    [TestFixture]
    public class StressWithSetupAndTeardownTests
    {
        private StressorWithEvents stressor;
        private Mock<ILogger> mockLogger;
        private List<string> output;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;
        private Guid clientId;
        private Stopwatch stopwatch;
        private StressorWithEventsOptions options;

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

            output = new List<string>();
            mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(l => l.Log(It.IsAny<string>()))
                .Callback((string m) => output.Add(m));

            stressor = new StressorWithEvents(options, mockLogger.Object);

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
        public void TearDown()
        {
            //HACK: Need to do this since tests take longer than 10 minutes to run, and Travis cuts the build after that long without activity
            Console.WriteLine($"Test completed at {DateTime.Now}");
        }

        [Test]
        public void StopsWhenTimeLimitHit()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stopwatch.Start();

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(setup, Is.LessThan(StressorWithEvents.ConfidentIterations));
            Assert.That(teardown, Is.LessThan(StressorWithEvents.ConfidentIterations));
            Assert.That(count, Is.EqualTo(setup).And.EqualTo(teardown).And.LessThan(StressorWithEvents.ConfidentIterations));
        }

        private void SlowTest(ref int count)
        {
            FastTest(ref count);
            Thread.Sleep(1);
        }

        private void FastTest(ref int count, int failLimit = int.MaxValue)
        {
            count++;
            Assert.That(count, Is.Positive.And.LessThan(failLimit));
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            //Returning no events, as that makes the generation go faster, and is more likely to actually hit the iteration limit within the time frame
            //Separate tests exist that verify that event spacing and order are asserted for this method
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns(Enumerable.Empty<GenEvent>());

            options.IsFullStress = true;
            options.RunningAssembly = null;
            options.TestCount = 1;

            stressor = new StressorWithEvents(options, mockLogger.Object);

            var count = 0;
            var setup = 0;
            var teardown = 0;

            stopwatch.Start();

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(StressorWithEvents.ConfidentIterations));
            Assert.That(setup, Is.EqualTo(StressorWithEvents.ConfidentIterations));
            Assert.That(teardown, Is.EqualTo(StressorWithEvents.ConfidentIterations));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => setup++, FailedTest, () => teardown++), Throws.InstanceOf<AssertionException>());
            Assert.That(setup, Is.EqualTo(1));
            Assert.That(teardown, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(7));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
            Assert.That(output[4], Is.EqualTo($"\tIterations Per Second: 0"));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
        }

        private void FailedTest()
        {
            Assert.Fail("This test should fail");
        }

        [Test]
        public void WritesStressSlowSummaryToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesEventSummaryToConsole()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));
            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Does.EndWith($" from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain(clientId.ToString()));
            Assert.That(output[11], Does.Contain(clientId.ToString()));
            Assert.That(output[12], Does.Contain(clientId.ToString()));
            Assert.That(output[13], Does.Contain(clientId.ToString()));
            Assert.That(output[14], Does.Contain(clientId.ToString()));
            Assert.That(output[15], Does.Contain(clientId.ToString()));
            Assert.That(output[16], Does.Contain(clientId.ToString()));
            Assert.That(output[17], Does.Contain(clientId.ToString()));
            Assert.That(output[18], Does.Contain(clientId.ToString()));
            Assert.That(output[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void ClearsRemainingEvents()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));
            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Does.EndWith($" from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain(clientId.ToString()));
            Assert.That(output[11], Does.Contain(clientId.ToString()));
            Assert.That(output[12], Does.Contain(clientId.ToString()));
            Assert.That(output[13], Does.Contain(clientId.ToString()));
            Assert.That(output[14], Does.Contain(clientId.ToString()));
            Assert.That(output[15], Does.Contain(clientId.ToString()));
            Assert.That(output[16], Does.Contain(clientId.ToString()));
            Assert.That(output[17], Does.Contain(clientId.ToString()));
            Assert.That(output[18], Does.Contain(clientId.ToString()));
            Assert.That(output[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void ClearsRemainingEventsOnFailure()
        {
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => setup++, FailedTest, () => teardown++), Throws.InstanceOf<AssertionException>());
            Assert.That(setup, Is.EqualTo(1));
            Assert.That(teardown, Is.EqualTo(1));

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(7));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00."));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Is.EqualTo($"0 events were logged in total"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void StressATestWithSetupAndTeardown()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count), () => TestTeardown(ref teardown));
            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Does.EndWith($" from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain(clientId.ToString()));
            Assert.That(output[11], Does.Contain(clientId.ToString()));
            Assert.That(output[12], Does.Contain(clientId.ToString()));
            Assert.That(output[13], Does.Contain(clientId.ToString()));
            Assert.That(output[14], Does.Contain(clientId.ToString()));
            Assert.That(output[15], Does.Contain(clientId.ToString()));
            Assert.That(output[16], Does.Contain(clientId.ToString()));
            Assert.That(output[17], Does.Contain(clientId.ToString()));
            Assert.That(output[18], Does.Contain(clientId.ToString()));
            Assert.That(output[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        private void TestSetup(ref int setup)
        {
            setup++;
        }

        private void TestTeardown(ref int teardown)
        {
            teardown++;
        }

        [Test]
        public void StressAFailedTestWithSetupAndTeardown()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(() => TestSetup(ref setup), () => FastTest(ref count, 42), () => TestTeardown(ref teardown)), Throws.InstanceOf<AssertionException>());
            Assert.That(count, Is.EqualTo(42), "Count");
            Assert.That(setup, Is.EqualTo(42), "Setup");
            Assert.That(teardown, Is.EqualTo(42), "Tear Down");

            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00."));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Is.EqualTo($"123 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t82 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"\t41 from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain(clientId.ToString()));
            Assert.That(output[11], Does.Contain(clientId.ToString()));
            Assert.That(output[12], Does.Contain(clientId.ToString()));
            Assert.That(output[13], Does.Contain(clientId.ToString()));
            Assert.That(output[14], Does.Contain(clientId.ToString()));
            Assert.That(output[15], Does.Contain(clientId.ToString()));
            Assert.That(output[16], Does.Contain(clientId.ToString()));
            Assert.That(output[17], Does.Contain(clientId.ToString()));
            Assert.That(output[18], Does.Contain(clientId.ToString()));
            Assert.That(output[19], Does.Contain(clientId.ToString()));
            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GenerateWithinStressWithSetupAndTeardownHonorsTimeLimit()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stopwatch.Start();

            stressor.Stress(
                () => TestSetup(ref setup),
                () => TestWithGenerate(ref count),
                () => TestTeardown(ref teardown));

            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(1).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations), "Count");
            Assert.That(setup, Is.LessThan(Stressor.ConfidentIterations), "Setup");
            Assert.That(teardown, Is.LessThan(Stressor.ConfidentIterations), "Tear Down");
            Assert.That(count, Is.AtLeast(10), "Count");
            Assert.That(setup, Is.AtLeast(10), "Setup");
            Assert.That(teardown, Is.AtLeast(10), "Tear Down");
        }

        private void TestWithGenerate(ref int count)
        {
            var originalCount = count;
            var innerCount = count;

            var newCount = stressor.Generate(() => innerCount++, c => c > originalCount && c % 42 == 0);
            var difference = newCount - count;
            count += difference;

            Assert.That(count % 42, Is.EqualTo(0));
            Assert.That(count, Is.GreaterThan(originalCount));
        }

        [Test]
        public void SetsClientIdForEvents()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingIsWithin1SecondOfEachOther()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            mockEventQueue
                .Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(() => new[]
                {
                    new GenEvent("Unit Test", $"First Message {count}"),
                    new GenEvent("Unit Test", $"Last Message {count}"),
                });

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");

            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: {count}"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[9], Does.Contain($"Unit Test: First Message {count - 4}"));
            Assert.That(output[10], Does.Contain($"Unit Test: Last Message {count - 4}"));
            Assert.That(output[11], Does.Contain($"Unit Test: First Message {count - 3}"));
            Assert.That(output[12], Does.Contain($"Unit Test: Last Message {count - 3}"));
            Assert.That(output[13], Does.Contain($"Unit Test: First Message {count - 2}"));
            Assert.That(output[14], Does.Contain($"Unit Test: Last Message {count - 2}"));
            Assert.That(output[15], Does.Contain($"Unit Test: First Message {count - 1}"));
            Assert.That(output[16], Does.Contain($"Unit Test: Last Message {count - 1}"));
            Assert.That(output[17], Does.Contain($"Unit Test: First Message {count}"));
            Assert.That(output[18], Does.Contain($"Unit Test: Last Message {count}"));
            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(19));
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
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown)), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Is.EqualTo($"2 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(output[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingIsWithin1SecondOfEachOtherWithNonSourceEvents()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            mockEventQueue
                .Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(() => new[]
                {
                    new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-1000) },
                    new GenEvent("Wrong Source", "Wrong Message") { When = DateTime.Now.AddMilliseconds(-500) },
                    new GenEvent("Unit Test", "Last Message") { When = DateTime.Now },
                });

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");

            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: {count}"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Does.EndWith($" from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[11], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[12], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[14], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[15], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[16], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[17], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[18], Does.Contain("Unit Test: Last Message"));
            Assert.That(output[19], Does.Contain("Unit Test: Last Message"));
            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreOrderedChronologically()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(count, Is.AtLeast(100), "Count");
            Assert.That(setup, Is.AtLeast(100), "Setup");
            Assert.That(teardown, Is.AtLeast(100), "Tear Down");

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Does.EndWith($" from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain(clientId.ToString()));
            Assert.That(output[11], Does.Contain(clientId.ToString()));
            Assert.That(output[12], Does.Contain(clientId.ToString()));
            Assert.That(output[14], Does.Contain(clientId.ToString()));
            Assert.That(output[15], Does.Contain(clientId.ToString()));
            Assert.That(output[16], Does.Contain(clientId.ToString()));
            Assert.That(output[17], Does.Contain(clientId.ToString()));
            Assert.That(output[18], Does.Contain(clientId.ToString()));
            Assert.That(output[19], Does.Contain(clientId.ToString()));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreNotOrderedChronologically()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now.AddMilliseconds(10) });
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var count = 0;
            var setup = 0;
            var teardown = 0;

            Assert.That(() => stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown)), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Is.EqualTo($"2 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(output[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
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

            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => FastTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(count, Is.AtLeast(1000), "Count");
            Assert.That(setup, Is.AtLeast(1000), "Setup");
            Assert.That(teardown, Is.AtLeast(1000), "Tear Down");

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(19));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Does.EndWith($" events were logged in total"));
            Assert.That(output[7], Does.EndWith($" from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[9], Does.EndWith($"] Unit Test: First message"));
            Assert.That(output[10], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(output[11], Does.EndWith($"] Unit Test: First message"));
            Assert.That(output[12], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(output[13], Does.EndWith($"] Unit Test: First message"));
            Assert.That(output[14], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(output[15], Does.EndWith($"] Unit Test: First message"));
            Assert.That(output[16], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(output[17], Does.EndWith($"] Unit Test: First message"));
            Assert.That(output[18], Does.EndWith($"] Unit Test: Last message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [TestCase(65)]
        [TestCase(70)]
        public void PercentageIsAccurate(int testCount)
        {
            options.IsFullStress = true;
            options.TestCount = testCount;
            options.RunningAssembly = null;

            stressor = new StressorWithEvents(options, mockLogger.Object);

            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            var time = stressor.TimeLimit.ToString().Substring(0, 10);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: {time}"));
            Assert.That(output[2], Does.Contain($"(100"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [TestCase(0, IgnoreReason = "This method is not currently used in event-based stress tests")]
        [TestCase(1, IgnoreReason = "This method is not currently used in event-based stress tests")]
        [TestCase(10, IgnoreReason = "This method is not currently used in event-based stress tests")]
        [TestCase(100, IgnoreReason = "This method is not currently used in event-based stress tests")]
        public void IterationsWithEventsCanComplete(int eventCount)
        {
            options.IsFullStress = true;

            stressor = new StressorWithEvents(options, mockLogger.Object);

            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns(() => GetEvents(eventCount));

            var count = 0;
            var setup = 0;
            var teardown = 0;

            stressor.Stress(
                () => TestSetup(ref setup),
                () => SlowTest(ref count),
                () => TestTeardown(ref teardown));

            Assert.That(count, Is.AtLeast(1));
            Assert.That(count, Is.AtLeast(10));
            Assert.That(count, Is.AtLeast(100));
            Assert.That(count, Is.AtLeast(1000));
            Assert.That(count, Is.AtLeast(10000));
            Assert.That(setup, Is.AtLeast(1));
            Assert.That(setup, Is.AtLeast(10));
            Assert.That(setup, Is.AtLeast(100));
            Assert.That(setup, Is.AtLeast(1000));
            Assert.That(setup, Is.AtLeast(10000));
            Assert.That(teardown, Is.AtLeast(1));
            Assert.That(teardown, Is.AtLeast(10));
            Assert.That(teardown, Is.AtLeast(100));
            Assert.That(teardown, Is.AtLeast(1000));
            Assert.That(teardown, Is.AtLeast(10000));
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

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;
            var setup = 0;
            var teardown = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.Stress(() => TestSetup(ref setup), () => FailStress(count++), () => TestTeardown(ref teardown)));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(setup, Is.EqualTo(12));
            Assert.That(teardown, Is.EqualTo(12));
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunAction(Action setup, Action action, Action teardown)"));
        }

        public void FailStress(int count)
        {
            if (count > 10)
                throw new ArgumentException();
        }
    }
}
