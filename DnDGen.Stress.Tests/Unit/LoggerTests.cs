using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace DnDGen.Stress.Tests.Unit
{
    [TestFixture]
    [Ignore("might not be needed, let's see how integration tests do in pipeline")]
    public class LoggerTests
    {
        private StringBuilder console;
        private ILogger logger;

        [SetUp]
        public void Setup()
        {
            using var factory = LoggerFactory.Create(builder => builder.AddConsole());
            logger = factory.CreateLogger<Stressor>();

            console = new StringBuilder();
            var writer = new StringWriter(console);

            Console.SetOut(writer);
        }

        [Test]
        public void EXTERNAL_LogWritesMessageToConsole()
        {
            logger.LogInformation("Hello world!");

            var output = console.ToString();
            Assert.That(output, Is.EqualTo($"Hello world!{Environment.NewLine}"));
        }

        [Test]
        public void EXTERNAL_LogWritesMessagesToConsole()
        {
            logger.LogInformation("Hello world!");
            logger.LogInformation("Goodbye world!");

            var output = console.ToString();
            Assert.That(output, Is.EqualTo($"Hello world!{Environment.NewLine}Goodbye world!{Environment.NewLine}"));
        }
    }
}
