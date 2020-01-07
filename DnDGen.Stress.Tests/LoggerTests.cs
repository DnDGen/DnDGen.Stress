using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class LoggerTests
    {
        private StringBuilder console;
        private ILogger logger;

        [SetUp]
        public void Setup()
        {
            logger = new Logger();

            console = new StringBuilder();
            var writer = new StringWriter(console);

            Console.SetOut(writer);
        }

        [Test]
        public void LogWritesMessageToConsole()
        {
            logger.Log("Hello world!");

            var output = console.ToString();
            Assert.That(output, Is.EqualTo($"Hello world!{Environment.NewLine}"));
        }

        [Test]
        public void LogWritesMessagesToConsole()
        {
            logger.Log("Hello world!");
            logger.Log("Goodbye world!");

            var output = console.ToString();
            Assert.That(output, Is.EqualTo($"Hello world!{Environment.NewLine}Goodbye world!{Environment.NewLine}"));
        }
    }
}
