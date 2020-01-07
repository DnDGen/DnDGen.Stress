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
    public class GenerateOrFailTests
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

        [Test]
        public void StopsWhenTimeLimitHit()
        {
            var count = 0;

            stopwatch.Start();
            Assert.That(() => stressor.GenerateOrFail(() => SlowGenerate(ref count), c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.01).Seconds);
            Assert.That(count, Is.LessThan(Stressor.ConfidentIterations));
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
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations));
        }

        private int SlowGenerate(ref int count)
        {
            count++;
            Thread.Sleep(1);
            return count;
        }

        [Test]
        public void StopsWhenGenerated()
        {
            var count = 0;

            stopwatch.Start();
            var result = stressor.GenerateOrFail(() => count++, c => c == 42);
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(43));
        }

        [Test]
        public void WritesStressDurationToConsole()
        {
            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c == 42);
            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(43));

            Assert.That(output, Is.Not.Empty);
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
        }

        [Test]
        public void WritesStressSummaryToConsole()
        {
            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c == 42);
            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(43));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00."));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 43 (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesFailedStressSummaryToConsole()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01.0"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
        }

        [Test]
        public void WritesShortSummaryToConsole()
        {
            var count = 0;

            var result = stressor.GenerateOrFail(() => count++, c => c > 2);
            Assert.That(result, Is.EqualTo(3));
            Assert.That(count, Is.EqualTo(4));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(18));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: {count} (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
        }

        [Test]
        public void WritesEventSummaryToConsole()
        {
            var count = 0;
            var result = stressor.GenerateOrFail(() => count++, c => c == 42);
            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(43));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: {count} (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"129 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t86 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"\t43 from Wrong Source"));
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
            var result = stressor.GenerateOrFail(() => count++, c => c == 42);
            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(43));

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: {count} (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"129 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t86 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"\t43 from Wrong Source"));
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
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c == 90210), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: {count} ("));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
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
        public void GenerateAndSucceed()
        {
            var count = 0;
            var result = stressor.GenerateOrFail(() => count++, c => c == 42);
            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(43));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: {count} (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"129 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t86 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"\t43 from Wrong Source"));
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
        public void GenerateAndFail()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c == 90210), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: {count} ("));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
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
        public void GenerateWithinGenerateOrFailHonorsTimeLimit()
        {
            var count = 0;

            stopwatch.Start();
            Assert.That(() => stressor.GenerateOrFail(() => stressor.Generate(() => count++, c => c % 42 == 0), c => c < 0), Throws.InstanceOf<AssertionException>());
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.1).Seconds);
            Assert.That(count, Is.AtLeast(42));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
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
        public void SetsClientIdForEvents()
        {
            var result = stressor.GenerateOrFail(() => 1, i => i > 0);
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

            var result = stressor.GenerateOrFail(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 1 (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"2 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(output[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
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

            Assert.That(() => stressor.GenerateOrFail(() => 1, i => i > 0), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
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
            var events = new List<GenEvent>();
            events.Add(new GenEvent("Unit Test", "First Message") { When = DateTime.Now.AddMilliseconds(-1001) });
            events.Add(new GenEvent("Wrong Source", "Wrong Message") { When = DateTime.Now.AddMilliseconds(-999) });
            events.Add(new GenEvent("Unit Test", "Last Message") { When = DateTime.Now });

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            var result = stressor.GenerateOrFail(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(12));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 1 (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"3 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"\t1 from Wrong Source"));
            Assert.That(output[9], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(output[10], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[11], Is.EqualTo($"[{events[2].When.ToLongTimeString()}] Unit Test: Last Message"));
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

            var result = stressor.GenerateOrFail(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 1 (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"2 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t2 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(output[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
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

            Assert.That(() => stressor.GenerateOrFail(() => 1, i => i > 0), Throws.InstanceOf<AssertionException>());

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
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
            var result = stressor.GenerateOrFail(() => count++, i => i == 1);
            Assert.That(result, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(13));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: {count} (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"4 events were logged in total"));
            Assert.That(output[7], Is.EqualTo($"\t4 from Unit Test"));
            Assert.That(output[8], Is.EqualTo($"Last 4 events from Unit Test:"));
            Assert.That(output[9], Is.EqualTo($"[{earlyEvents[0].When.ToLongTimeString()}] Unit Test: First early message"));
            Assert.That(output[10], Is.EqualTo($"[{earlyEvents[1].When.ToLongTimeString()}] Unit Test: Last early message"));
            Assert.That(output[11], Is.EqualTo($"[{lateEvents[0].When.ToLongTimeString()}] Unit Test: First late message"));
            Assert.That(output[12], Is.EqualTo($"[{lateEvents[1].When.ToLongTimeString()}] Unit Test: Last late message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
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
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

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

            var exception = Assert.Throws<ArgumentException>(() => stressor.GenerateOrFail(() => FailGeneration(count++), c => c > 9266));
            Assert.That(count, Is.EqualTo(12));
            Assert.That(exception.StackTrace, Contains.Substring("FailGeneration"));
        }

        public int FailGeneration(int count)
        {
            if (count > 10)
                throw new ArgumentException();

            return count;
        }
    }
}
