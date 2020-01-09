﻿using EventGen;
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
            var eventCount = 1;

            mockClientIdManager.Setup(m => m.SetClientID(It.IsAny<Guid>())).Callback((Guid g) => clientId = g);
            mockEventQueue.Setup(q => q.DequeueAll(It.Is<Guid>(g => g == clientId))).Returns((Guid g) => new[]
            {
                new GenEvent(options.Source, $"Event {eventCount++} for {g}"),
                new GenEvent(options.Source, $"Event {eventCount++} for {g}"),
                new GenEvent("Wrong Source", $"Wrong event {eventCount++} for {g}"),
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
            Assert.That(() => stressor.GenerateOrFail(() => SlowGenerate(ref count), c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.5).Seconds);
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
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(StressorWithEvents.ConfidentIterations));
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
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:0")); //HACK: Travis runs slower, cannot be more precise than this
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
            Assert.That(output[6], Is.EqualTo($"Events: 129 (~3 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 86 (~2 per iteration)"));
            Assert.That(output[8], Is.EqualTo($"\tWrong Source: 43 (~1 per iteration)"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain($"] Unit Test: Event 115 for {clientId}"));
            Assert.That(output[11], Does.Contain($"] Unit Test: Event 116 for {clientId}"));
            Assert.That(output[12], Does.Contain($"] Unit Test: Event 118 for {clientId}"));
            Assert.That(output[13], Does.Contain($"] Unit Test: Event 119 for {clientId}"));
            Assert.That(output[14], Does.Contain($"] Unit Test: Event 121 for {clientId}"));
            Assert.That(output[15], Does.Contain($"] Unit Test: Event 122 for {clientId}"));
            Assert.That(output[16], Does.Contain($"] Unit Test: Event 124 for {clientId}"));
            Assert.That(output[17], Does.Contain($"] Unit Test: Event 125 for {clientId}"));
            Assert.That(output[18], Does.Contain($"] Unit Test: Event 127 for {clientId}"));
            Assert.That(output[19], Does.Contain($"] Unit Test: Event 128 for {clientId}"));
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
            Assert.That(output[6], Is.EqualTo($"Events: 129 (~3 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 86 (~2 per iteration)"));
            Assert.That(output[8], Is.EqualTo($"\tWrong Source: 43 (~1 per iteration)"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain($"] Unit Test: Event 115 for {clientId}"));
            Assert.That(output[11], Does.Contain($"] Unit Test: Event 116 for {clientId}"));
            Assert.That(output[12], Does.Contain($"] Unit Test: Event 118 for {clientId}"));
            Assert.That(output[13], Does.Contain($"] Unit Test: Event 119 for {clientId}"));
            Assert.That(output[14], Does.Contain($"] Unit Test: Event 121 for {clientId}"));
            Assert.That(output[15], Does.Contain($"] Unit Test: Event 122 for {clientId}"));
            Assert.That(output[16], Does.Contain($"] Unit Test: Event 124 for {clientId}"));
            Assert.That(output[17], Does.Contain($"] Unit Test: Event 125 for {clientId}"));
            Assert.That(output[18], Does.Contain($"] Unit Test: Event 127 for {clientId}"));
            Assert.That(output[19], Does.Contain($"] Unit Test: Event 128 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void ClearsRemainingEventsOnFailure()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

            mockEventQueue.Verify(q => q.Clear(clientId), Times.Once);

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: {count} ("));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Does.StartWith($"Events: {count * 3} (~3 per iteration)"));
            Assert.That(output[7], Does.StartWith($"\tUnit Test: {count * 2} (~2 per iteration)"));
            Assert.That(output[8], Does.StartWith($"\tWrong Source: {count} (~1 per iteration)"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain($"] Unit Test: Event {count * 3 - 14} for {clientId}"));
            Assert.That(output[11], Does.Contain($"] Unit Test: Event {count * 3 - 13} for {clientId}"));
            Assert.That(output[12], Does.Contain($"] Unit Test: Event {count * 3 - 11} for {clientId}"));
            Assert.That(output[13], Does.Contain($"] Unit Test: Event {count * 3 - 10} for {clientId}"));
            Assert.That(output[14], Does.Contain($"] Unit Test: Event {count * 3 - 8} for {clientId}"));
            Assert.That(output[15], Does.Contain($"] Unit Test: Event {count * 3 - 7} for {clientId}"));
            Assert.That(output[16], Does.Contain($"] Unit Test: Event {count * 3 - 5} for {clientId}"));
            Assert.That(output[17], Does.Contain($"] Unit Test: Event {count * 3 - 4} for {clientId}"));
            Assert.That(output[18], Does.Contain($"] Unit Test: Event {count * 3 - 2} for {clientId}"));
            Assert.That(output[19], Does.Contain($"] Unit Test: Event {count * 3 - 1} for {clientId}"));
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
            Assert.That(output[6], Is.EqualTo($"Events: 129 (~3 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 86 (~2 per iteration)"));
            Assert.That(output[8], Is.EqualTo($"\tWrong Source: 43 (~1 per iteration)"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain($"] Unit Test: Event 115 for {clientId}"));
            Assert.That(output[11], Does.Contain($"] Unit Test: Event 116 for {clientId}"));
            Assert.That(output[12], Does.Contain($"] Unit Test: Event 118 for {clientId}"));
            Assert.That(output[13], Does.Contain($"] Unit Test: Event 119 for {clientId}"));
            Assert.That(output[14], Does.Contain($"] Unit Test: Event 121 for {clientId}"));
            Assert.That(output[15], Does.Contain($"] Unit Test: Event 122 for {clientId}"));
            Assert.That(output[16], Does.Contain($"] Unit Test: Event 124 for {clientId}"));
            Assert.That(output[17], Does.Contain($"] Unit Test: Event 125 for {clientId}"));
            Assert.That(output[18], Does.Contain($"] Unit Test: Event 127 for {clientId}"));
            Assert.That(output[19], Does.Contain($"] Unit Test: Event 128 for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GenerateAndFail()
        {
            var count = 0;
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: {count} ("));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Does.StartWith($"Events: {count * 3} (~3 per iteration)"));
            Assert.That(output[7], Does.StartWith($"\tUnit Test: {count * 2} (~2 per iteration)"));
            Assert.That(output[8], Does.StartWith($"\tWrong Source: {count} (~1 per iteration)"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain($"] Unit Test: Event {count * 3 - 14} for {clientId}"));
            Assert.That(output[11], Does.Contain($"] Unit Test: Event {count * 3 - 13} for {clientId}"));
            Assert.That(output[12], Does.Contain($"] Unit Test: Event {count * 3 - 11} for {clientId}"));
            Assert.That(output[13], Does.Contain($"] Unit Test: Event {count * 3 - 10} for {clientId}"));
            Assert.That(output[14], Does.Contain($"] Unit Test: Event {count * 3 - 8} for {clientId}"));
            Assert.That(output[15], Does.Contain($"] Unit Test: Event {count * 3 - 7} for {clientId}"));
            Assert.That(output[16], Does.Contain($"] Unit Test: Event {count * 3 - 5} for {clientId}"));
            Assert.That(output[17], Does.Contain($"] Unit Test: Event {count * 3 - 4} for {clientId}"));
            Assert.That(output[18], Does.Contain($"] Unit Test: Event {count * 3 - 2} for {clientId}"));
            Assert.That(output[19], Does.Contain($"] Unit Test: Event {count * 3 - 1} for {clientId}"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void GenerateWithinGenerateOrFailHonorsTimeLimit()
        {
            var innerIteration = 0;
            var iteration = 0;

            stopwatch.Start();
            Assert.That(() => stressor.GenerateOrFail(() =>
                    {
                        var generated = stressor.Generate(() => innerIteration++, c => c % 42 == 0);
                        iteration++;
                        return generated;
                    },
                    c => c < 0),
                Throws.InstanceOf<AssertionException>());
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.EqualTo(stressor.TimeLimit).Within(.5).Seconds);
            Assert.That(iteration, Is.AtLeast(42));
            Assert.That(innerIteration, Is.AtLeast(42));

            var totalIterations = iteration + innerIteration;
            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(20));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:01"));
            Assert.That(output[3], Does.StartWith($"\tCompleted Iterations: "));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Does.StartWith($"Events: {totalIterations * 3} (~{totalIterations * 3 / iteration} per iteration)"));
            Assert.That(output[7], Does.StartWith($"\tUnit Test: {totalIterations * 2} (~{totalIterations / iteration * 2 + 1} per iteration)"));
            Assert.That(output[8], Does.StartWith($"\tWrong Source: {totalIterations} (~{totalIterations / iteration} per iteration)"));
            Assert.That(output[9], Is.EqualTo($"Last 10 events from Unit Test:"));
            Assert.That(output[10], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 14} for {clientId}"));
            Assert.That(output[11], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 13} for {clientId}"));
            Assert.That(output[12], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 11} for {clientId}"));
            Assert.That(output[13], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 10} for {clientId}"));
            Assert.That(output[14], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 8} for {clientId}"));
            Assert.That(output[15], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 7} for {clientId}"));
            Assert.That(output[16], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 5} for {clientId}"));
            Assert.That(output[17], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 4} for {clientId}"));
            Assert.That(output[18], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 2} for {clientId}"));
            Assert.That(output[19], Does.Contain($"] Unit Test: Event {totalIterations * 3 - 1} for {clientId}"));
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
            Assert.That(output[6], Is.EqualTo($"Events: 2 (~2 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 2 (~2 per iteration)"));
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
            Assert.That(output[6], Is.EqualTo($"Events: 2 (~2 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 2 (~2 per iteration)"));
            Assert.That(output[8], Is.EqualTo($"Last 2 events from Unit Test:"));
            Assert.That(output[9], Is.EqualTo($"[{events[0].When.ToLongTimeString()}] Unit Test: First Message"));
            Assert.That(output[10], Is.EqualTo($"[{events[1].When.ToLongTimeString()}] Unit Test: Last Message"));
            Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(0, 3)]
        [TestCase(0, 4)]
        [TestCase(0, 5)]
        [TestCase(0, 6)]
        [TestCase(0, 7)]
        [TestCase(0, 8)]
        [TestCase(0, 9)]
        [TestCase(0, 10)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(1, 4)]
        [TestCase(1, 5)]
        [TestCase(1, 6)]
        [TestCase(1, 7)]
        [TestCase(1, 8)]
        [TestCase(1, 9)]
        [TestCase(1, 10)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(2, 4)]
        [TestCase(2, 5)]
        [TestCase(2, 6)]
        [TestCase(2, 7)]
        [TestCase(2, 8)]
        [TestCase(2, 9)]
        [TestCase(2, 10)]
        [TestCase(3, 0)]
        [TestCase(3, 1)]
        [TestCase(3, 2)]
        [TestCase(3, 3)]
        [TestCase(3, 4)]
        [TestCase(3, 5)]
        [TestCase(3, 6)]
        [TestCase(3, 7)]
        [TestCase(3, 8)]
        [TestCase(3, 9)]
        [TestCase(3, 10)]
        [TestCase(4, 0)]
        [TestCase(4, 1)]
        [TestCase(4, 2)]
        [TestCase(4, 3)]
        [TestCase(4, 4)]
        [TestCase(4, 5)]
        [TestCase(4, 6)]
        [TestCase(4, 7)]
        [TestCase(4, 8)]
        [TestCase(4, 9)]
        [TestCase(4, 10)]
        [TestCase(5, 0)]
        [TestCase(5, 1)]
        [TestCase(5, 2)]
        [TestCase(5, 3)]
        [TestCase(5, 4)]
        [TestCase(5, 5)]
        [TestCase(5, 6)]
        [TestCase(5, 7)]
        [TestCase(5, 8)]
        [TestCase(5, 9)]
        [TestCase(5, 10)]
        [TestCase(6, 0)]
        [TestCase(6, 1)]
        [TestCase(6, 2)]
        [TestCase(6, 3)]
        [TestCase(6, 4)]
        [TestCase(6, 5)]
        [TestCase(6, 6)]
        [TestCase(6, 7)]
        [TestCase(6, 8)]
        [TestCase(6, 9)]
        [TestCase(6, 10)]
        [TestCase(7, 0)]
        [TestCase(7, 1)]
        [TestCase(7, 2)]
        [TestCase(7, 3)]
        [TestCase(7, 4)]
        [TestCase(7, 5)]
        [TestCase(7, 6)]
        [TestCase(7, 7)]
        [TestCase(7, 8)]
        [TestCase(7, 9)]
        [TestCase(7, 10)]
        [TestCase(8, 0)]
        [TestCase(8, 1)]
        [TestCase(8, 2)]
        [TestCase(8, 3)]
        [TestCase(8, 4)]
        [TestCase(8, 5)]
        [TestCase(8, 6)]
        [TestCase(8, 7)]
        [TestCase(8, 8)]
        [TestCase(8, 9)]
        [TestCase(8, 10)]
        [TestCase(9, 0)]
        [TestCase(9, 1)]
        [TestCase(9, 2)]
        [TestCase(9, 3)]
        [TestCase(9, 4)]
        [TestCase(9, 5)]
        [TestCase(9, 6)]
        [TestCase(9, 7)]
        [TestCase(9, 8)]
        [TestCase(9, 9)]
        [TestCase(9, 10)]
        [TestCase(10, 0)]
        [TestCase(10, 1)]
        [TestCase(10, 2)]
        [TestCase(10, 3)]
        [TestCase(10, 4)]
        [TestCase(10, 5)]
        [TestCase(10, 6)]
        [TestCase(10, 7)]
        [TestCase(10, 8)]
        [TestCase(10, 9)]
        [TestCase(10, 10)]
        public void EventSpacingIsNotWithin1SecondOfEachOther_FocusesOnErrorEvents(int precedingEvents, int followingEvents)
        {
            var events = new List<GenEvent>();
            var totalEvents = precedingEvents + 2 + followingEvents;

            while (events.Count < precedingEvents)
            {
                events.Add(new GenEvent("Unit Test", $"Preceding Message {events.Count + 1}") { When = DateTime.Now.AddMilliseconds(-1500 + events.Count) });
            }

            events.Add(new GenEvent("Unit Test", "Checkpoint Message") { When = DateTime.Now.AddMilliseconds(-1001) });
            events.Add(new GenEvent("Unit Test", "Failure Message") { When = DateTime.Now });

            while (events.Count < totalEvents)
            {
                events.Add(new GenEvent("Unit Test", $"Following Message {events.Count - precedingEvents - 1}"));
            }

            mockEventQueue.SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
                .Returns(events);

            Assert.That(() => stressor.GenerateOrFail(() => 1, i => i > 0), Throws.InstanceOf<AssertionException>());

            var summaryPreceding = Math.Min(precedingEvents, 4);
            var summaryFollowing = Math.Min(followingEvents, 4);
            var summaryCount = summaryPreceding + 2 + summaryFollowing;

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(9 + summaryCount));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00."));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 0 (0%)"));
            Assert.That(output[4], Is.EqualTo($"\tIterations Per Second: 0"));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: FAILED"));
            Assert.That(output[6], Is.EqualTo($"Events: {totalEvents} (~{totalEvents} per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: {totalEvents} (~{totalEvents} per iteration)"));
            Assert.That(output[8], Is.EqualTo($"Last {summaryCount} events from Unit Test:"));

            var index = 9;
            for (var i = 0; i < summaryPreceding; i++)
            {
                var eventIndex = i + precedingEvents - summaryPreceding;
                var time = events[eventIndex].When.ToLongTimeString();
                Assert.That(output[index++], Is.EqualTo($"[{time}] Unit Test: Preceding Message {eventIndex + 1}"), $"Index {index}, Event Index {eventIndex}");
            }

            Assert.That(output[index++], Is.EqualTo($"[{events[precedingEvents].When.ToLongTimeString()}] Unit Test: Checkpoint Message"));
            Assert.That(output[index++], Is.EqualTo($"[{events[precedingEvents + 1].When.ToLongTimeString()}] Unit Test: Failure Message"));

            for (var i = 0; i < summaryFollowing; i++)
            {
                var eventIndex = precedingEvents + 2 + i;
                var time = events[eventIndex].When.ToLongTimeString();
                Assert.That(output[index++], Is.EqualTo($"[{time}] Unit Test: Following Message {i + 1}"), $"Index {index}, Event Index {eventIndex}");
            }

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
            Assert.That(output[6], Is.EqualTo($"Events: 3 (~3 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 2 (~2 per iteration)"));
            Assert.That(output[8], Is.EqualTo($"\tWrong Source: 1 (~1 per iteration)"));
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
            Assert.That(output[6], Is.EqualTo($"Events: 2 (~2 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 2 (~2 per iteration)"));
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

            var result = stressor.GenerateOrFail(() => 1, i => i > 0);
            Assert.That(result, Is.EqualTo(1));

            Assert.That(output, Is.Not.Empty.And.Count.EqualTo(11));
            Assert.That(output[0], Is.EqualTo($"Stress timeout is {stressor.TimeLimit}"));
            Assert.That(output[1], Is.EqualTo($"Stress test complete"));
            Assert.That(output[2], Does.StartWith($"\tTime: 00:00:00.0"));
            Assert.That(output[3], Is.EqualTo($"\tCompleted Iterations: 1 (0%)"));
            Assert.That(output[4], Does.StartWith($"\tIterations Per Second: "));
            Assert.That(output[5], Is.EqualTo($"\tLikely Status: PASSED"));
            Assert.That(output[6], Is.EqualTo($"Events: 2 (~2 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 2 (~2 per iteration)"));
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

            mockEventQueue
                .SetupSequence(q => q.DequeueAll(It.Is<Guid>(g => g == clientId)))
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
            Assert.That(output[6], Is.EqualTo($"Events: 4 (~2 per iteration)"));
            Assert.That(output[7], Is.EqualTo($"\tUnit Test: 4 (~2 per iteration)"));
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
