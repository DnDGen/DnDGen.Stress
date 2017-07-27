using NUnit.Framework;
using System;
using System.Reflection;

namespace DnDGen.Stress.Tests.Few
{
    [TestFixture]
    public class StressorTests
    {
        private Stressor stressor;

        [SetUp]
        public void Setup()
        {
            stressor = new Stressor(false, Assembly.GetExecutingAssembly());
        }

        [Test]
        public void DurationIs10MinutesMinus10Seconds()
        {
            stressor = new Stressor(true, Assembly.GetExecutingAssembly());
            var expectedTimeLimit = new TimeSpan(0, 9, 50);

            Assert.That(stressor.IsFullStress, Is.True);
            Assert.That(stressor.TimeLimit, Is.EqualTo(expectedTimeLimit));
        }
    }
}
