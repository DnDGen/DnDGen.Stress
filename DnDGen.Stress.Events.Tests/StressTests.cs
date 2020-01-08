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
    public class StressTests
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

            stopwatch.Start();
            stressor.Stress(() => SlowTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(count, Is.LessThan(StressorWithEvents.ConfidentIterations));
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            options.IsFullStress = true;
            options.RunningAssembly = null;
            options.TestCount = 1;

            stressor = new StressorWithEvents(options, mockLogger.Object);

            //Returning no events, as that makes the generation go faster, and is more likely to actually hit the iteration limit within the time frame
            //Separate tests exist that verify that event spacing and order are asserted for this method
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns(Enumerable.Empty<GenEvent>());

            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => FastTest(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(StressorWithEvents.ConfidentIterations));
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

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));

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
            Assert.That(() => stressor.Stress(FailedTest), Throws.InstanceOf<AssertionException>());

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
            stressor.Stress(() => SlowTest(ref count));

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
            stressor.Stress(() => FastTest(ref count));

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
            stressor.Stress(() => FastTest(ref count));

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
            Assert.That(() => stressor.Stress(FailedTest), Throws.InstanceOf<AssertionException>());

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
        public void StressATest()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.")); //HACK: Travis runs slower, so cannot be more precise than this
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
        public void GenerateWithinStressHonorsTimeLimit()
        {
            var count = 0;

            stopwatch.Start();
            stressor.Stress(() => TestWithGenerate(ref count));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(StressorWithEvents.ConfidentIterations));

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
            var count = 0;

            mockEventQueue
                .Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(() => new[]
                {
                    new GenEvent("Unit Test", $"First Message {count}"),
                    new GenEvent("Unit Test", $"Last Message {count}"),
                });

            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

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
            Assert.That(() => stressor.Stress(() => FastTest(ref count)), Throws.InstanceOf<AssertionException>());

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
            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventSpacingIsWithin1SecondOfEachOtherWithNonSourceEvents()
        {
            mockEventQueue
                .Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(() => new[]
                {
                    new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-1000) },
                    new GenEvent("Wrong Source", "First Message") { When = DateTime.Now.AddMilliseconds(-500) },
                    new GenEvent("Unit Test", "Last Message") { When = DateTime.Now },
                });

            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

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
            Assert.That(output[10], Does.Contain("Last Message"));
            Assert.That(output[11], Does.Contain("Last Message"));
            Assert.That(output[12], Does.Contain("Last Message"));
            Assert.That(output[13], Does.Contain("Last Message"));
            Assert.That(output[14], Does.Contain("Last Message"));
            Assert.That(output[15], Does.Contain("Last Message"));
            Assert.That(output[16], Does.Contain("Last Message"));
            Assert.That(output[17], Does.Contain("Last Message"));
            Assert.That(output[18], Does.Contain("Last Message"));
            Assert.That(output[19], Does.Contain("Last Message"));
            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void EventsAreOrderedChronologically()
        {
            var count = 0;
            stressor.Stress(() => FastTest(ref count));
            Assert.That(count, Is.AtLeast(100));

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
        public void EventsAreNotOrderedChronologically()
        {
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now.AddMilliseconds(10) });
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var count = 0;
            Assert.That(() => stressor.Stress(() => FastTest(ref count)), Throws.InstanceOf<AssertionException>());

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
            Assert.That(output[9], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[10], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: Last Message"));
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
            stressor.Stress(() => SlowTest(ref count));

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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void IterationsWithEventsCanComplete(int eventCount)
        {
            options.IsFullStress = true;

            stressor = new StressorWithEvents(options, mockLogger.Object);

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

        [Test]
        public void PreserveStackTrace()
        {
            var count = 0;

            var exception = Assert.Throws<ArgumentException>(() => stressor.Stress(() => FailStress(count++)));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(exception.StackTrace.Trim(), Does.Not.StartsWith("at DnDGen.Stress.Stressor.RunAction(Action setup, Action action, Action teardown)"));
        }

        public void FailStress(int count)
        {
            if (count > 10)
                throw new ArgumentException();
        }
    }
}
