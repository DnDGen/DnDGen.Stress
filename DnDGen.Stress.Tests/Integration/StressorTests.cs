using NUnit.Framework;
using System.Threading.Tasks;

namespace DnDGen.Stress.Tests.Integration
{
    internal class StressorTests
    {
        private Stressor _stressor;

        [OneTimeSetUp]
        public void Setup()
        {
            var options = new StressorOptions();
            options.TestCount = 6;

#if STRESS
            options.IsFullStress = true;
#else
            options.IsFullStress = false;
#endif

            _stressor = new Stressor(options);
        }

        [Test]
        public void Stress()
        {
            _stressor.Stress(TestToRun);
        }

        private void TestSetup()
        {
            Assert.That(2 * 2, Is.EqualTo(4));
        }

        private void TestToRun()
        {
            Assert.That(1 + 1, Is.EqualTo(2));
        }

        private async Task TestToRunAsync()
        {
            Assert.That(1 + 1, Is.EqualTo(2));
        }

        private void TestTeardown()
        {
            Assert.That(1 + 2, Is.EqualTo(3));
        }

        [Test]
        public void Stress_WithSetupAndTeardown()
        {
            _stressor.Stress(TestSetup, TestToRun, TestTeardown);
        }

        [Test]
        public async Task StressAsync()
        {
            await _stressor.StressAsync(TestToRunAsync);
        }

        [Test]
        public async Task StressAsync_WithSetupAndTeardown()
        {
            await _stressor.StressAsync(TestSetup, TestToRunAsync, TestTeardown);
        }

        [Test]
        public void Generate()
        {
            var count = 0;
            var generated = _stressor.Generate(() => count++, c => c == 10);
            Assert.That(generated, Is.EqualTo(10));
            Assert.That(count, Is.EqualTo(11));
        }

        [Test]
        public void GenerateOrFail()
        {
            var count = 0;
            var generated = _stressor.GenerateOrFail(() => count++, c => c == 10);
            Assert.That(generated, Is.EqualTo(10));
            Assert.That(count, Is.EqualTo(11));
        }
    }
}
