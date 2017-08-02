using EventGen;
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
    public class GenerateOrFailTests
    {
        private Stressor stressor;
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
            stressor = new StressorWithEvents(true, runningAssembly, mockClientIdManager.Object, mockEventQueue.Object, "Unit Test");
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

            Console.WriteLine("Test for GenerateOrFail() on StressorWithEvents with few tests completed.");
        }

        [Test]
        public void StopsWhenConfidenceIterationsHit()
        {
            var count = 0;

            stopwatch.Start();
            Assert.That(() => stressor.GenerateOrFail(() => count++, c => c < 0), Throws.InstanceOf<AssertionException>().With.Message.EqualTo("Generation timed out"));
            stopwatch.Stop();

            Assert.That(stopwatch.Elapsed, Is.LessThan(stressor.TimeLimit));
            Assert.That(count, Is.EqualTo(Stressor.ConfidentIterations));
        }
    }
}
