﻿using EventGen;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace DnDGen.Stress.Events.Tests.Few
{
    [TestFixture]
    public class StressorWithEventsTests
    {
        private Stressor stressor;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;
        private Assembly runningAssembly;
        private Stopwatch stopwatch;
        private StringBuilder console;
        private Guid clientId;

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
            });
        }

        [TearDown]
        public void Teardown()
        {
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Console.WriteLine("Test for base stressor properties on StressorWithEvents with few tests completed.");
        }

        [Test]
        public void DurationIs90PercentOf10Minutes()
        {
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");
            var expectedTimeLimit = new TimeSpan(0, 9, 0);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }
    }
}
