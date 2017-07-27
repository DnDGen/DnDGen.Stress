using EventGen;
using Moq;
using NUnit.Framework;
using System;
using System.Reflection;

namespace DnDGen.Stress.Events.Tests.Few
{
    [TestFixture]
    public class StressorWithEventsTests
    {
        private StressorWithEvents stressor;
        private Mock<ClientIDManager> mockClientIdManager;
        private Mock<GenEventQueue> mockEventQueue;

        [SetUp]
        public void Setup()
        {
            mockClientIdManager = new Mock<ClientIDManager>();
            mockEventQueue = new Mock<GenEventQueue>();
            stressor = new StressorWithEvents(false, Assembly.GetExecutingAssembly(), mockClientIdManager.Object, mockEventQueue.Object);
        }

        [Test]
        public void DurationIs10MinutesMinus10Seconds()
        {
            stressor = new StressorWithEvents(true, Assembly.GetExecutingAssembly(), mockClientIdManager.Object, mockEventQueue.Object);
            var expectedTimeLimit = new TimeSpan(0, 9, 50);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }
    }
}
