using NUnit.Framework;
using System;
using System.Reflection;

namespace DnDGen.Stress.Tests
{
    [TestFixture]
    public class StressorOptionsTests
    {
        private StressorOptions options;

        [SetUp]
        public void Setup()
        {
            options = new StressorOptions();

            options.RunningAssembly = Assembly.GetExecutingAssembly();
        }

        [TestCase(StressorOptions.DefaultBuildTimeLimitInSeconds, 60 * 60)]
        [TestCase(StressorOptions.DefaultOutputTimeLimitInSeconds, 10 * 60)]
        [TestCase(StressorOptions.DefaultConfidenceIterations, 1_000_000)]
        public void DefaultConstants(int constant, int value)
        {
            Assert.That(constant, Is.EqualTo(value));
        }

        [Test]
        public void StressorOptionsAreInitialized()
        {
            options = new StressorOptions();

            Assert.That(options.IsFullStress, Is.False);
            Assert.That(options.RunningAssembly, Is.Null);
            Assert.That(options.TestCount, Is.Zero);
            Assert.That(options.TimeLimitPercentage, Is.EqualTo(1));
            Assert.That(options.OutputTimeLimitInSeconds, Is.EqualTo(StressorOptions.DefaultOutputTimeLimitInSeconds));
            Assert.That(options.BuildTimeLimitInSeconds, Is.EqualTo(StressorOptions.DefaultBuildTimeLimitInSeconds));
            Assert.That(options.ConfidenceIterations, Is.EqualTo(StressorOptions.DefaultConfidenceIterations));
            Assert.That(options.MaxAsyncBatch, Is.EqualTo(Environment.ProcessorCount));
        }

        [Test]
        public void OptionsAreValid()
        {
            Assert.That(options.AreValid, Is.True);
        }

        [TestCase(-.01)]
        [TestCase(0)]
        [TestCase(1.01)]
        public void TimeLimitPercentageIsInvalid(double percentage)
        {
            options.TimeLimitPercentage = percentage;
            Assert.That(options.AreValid, Is.False);
        }

        [TestCase(.01)]
        [TestCase(.05)]
        [TestCase(.10)]
        [TestCase(.1337)]
        [TestCase(.15)]
        [TestCase(.20)]
        [TestCase(.25)]
        [TestCase(.30)]
        [TestCase(.35)]
        [TestCase(.40)]
        [TestCase(.42)]
        [TestCase(.45)]
        [TestCase(.50)]
        [TestCase(.55)]
        [TestCase(.60)]
        [TestCase(.65)]
        [TestCase(.70)]
        [TestCase(.75)]
        [TestCase(.80)]
        [TestCase(.85)]
        [TestCase(.90)]
        [TestCase(.90210)]
        [TestCase(.9266)]
        [TestCase(.95)]
        [TestCase(.99)]
        [TestCase(1)]
        public void TimeLimitPercentageIsValid(double percentage)
        {
            options.TimeLimitPercentage = percentage;
            Assert.That(options.AreValid, Is.True);
        }

        [Test]
        public void ValidIfFullStress()
        {
            options.IsFullStress = true;
            Assert.That(options.AreValid, Is.True);
        }

        [Test]
        public void ValidIfTestCountSetAndAssemblyIsNot()
        {
            options.TestCount = 1;
            options.RunningAssembly = null;
            Assert.That(options.AreValid, Is.True);
        }

        [Test]
        public void ValidIfAssemblySetAndTestCountIsNot()
        {
            options.TestCount = 0;
            options.RunningAssembly = Assembly.GetExecutingAssembly();
            Assert.That(options.AreValid, Is.True);
        }

        [Test]
        public void NotValidIfBothTestCountAndAssemblySet()
        {
            options.TestCount = 1;
            options.RunningAssembly = Assembly.GetExecutingAssembly();
            Assert.That(options.AreValid, Is.False);
        }

        [Test]
        public void NotValidIfBothTestCountAndAssemblyNotSet()
        {
            options.TestCount = 0;
            options.RunningAssembly = null;
            Assert.That(options.AreValid, Is.False);
        }

        [Test]
        public void NotValidIfTestCountIsNegative()
        {
            options.TestCount = -1;
            Assert.That(options.AreValid, Is.False);
        }
    }
}
