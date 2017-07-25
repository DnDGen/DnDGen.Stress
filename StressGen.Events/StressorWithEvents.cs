using EventGen;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace StressGen.Events
{
    [TestFixture]
    public class StressorWithEvents : Stressor
    {
        private readonly ClientIDManager clientIdManager;
        private readonly GenEventQueue eventQueue;
        private readonly string source;

        private Guid clientId;
        private DateTime eventCheckpoint;

        public StressorWithEvents(bool isFullStress, Assembly runningAssembly, ClientIDManager clientIdManager, GenEventQueue eventQueue)
            : base(isFullStress, runningAssembly)
        {
            var types = runningAssembly.GetTypes();
            source = types.First().AssemblyQualifiedName.Split('.')[0];
        }

        protected override void StressSetup()
        {
            clientId = Guid.NewGuid();
            clientIdManager.SetClientID(clientId);

            eventCheckpoint = new DateTime();

            base.StressSetup();
        }

        protected override void StressTearDown()
        {
            base.StressTearDown();

            WriteEventSummary();
        }

        private void WriteEventSummary()
        {
            var events = eventQueue.DequeueAll(clientId);

            //INFO: Get the 10 most recent events for the source.  We assume the events are ordered chronologically already
            events = events.Where(e => e.Source == source);
            events = events.Reverse();
            events = events.Take(10);
            events = events.Reverse();

            foreach (var genEvent in events)
                Console.WriteLine(GetMessage(genEvent));
        }

        private string GetMessage(GenEvent genEvent)
        {
            return $"[{genEvent.When.ToLongTimeString()}] {genEvent.Source}: {genEvent.Message}";
        }

        protected override T RunGenerate<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return base.RunGenerate(() => GenerateAndAssertEvent(generate), isValid);
        }

        protected override T RunGenerateOrFail<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return base.RunGenerateOrFail(() => GenerateAndAssertEvent(generate), isValid);
        }

        private T GenerateAndAssertEvent<T>(Func<T> generate)
        {
            var generatedObject = generate();
            AssertEventSpacing();

            return generatedObject;
        }

        private void RunAndAssertEvent(Action test)
        {
            test();
            AssertEventSpacing();
        }

        protected override void RunStress(Action setup, Action test, Action teardown)
        {
            base.RunStress(setup, () => RunAndAssertEvent(test), teardown);
        }

        protected override void RunStress(Action test)
        {
            base.RunStress(() => RunAndAssertEvent(test));
        }

        private void AssertEventSpacing()
        {
            var events = eventQueue.DequeueAll(clientId);

            //INFO: Have to put the events back in the queue for the summary at the end of the test
            foreach (var genEvent in events)
                eventQueue.Enqueue(genEvent);

            Assert.That(events, Is.Ordered.By("When"));

            var newEvents = events.Where(e => e.When > eventCheckpoint).ToArray();

            Assert.That(newEvents, Is.Ordered.By("When"));

            for (var i = 1; i < newEvents.Length; i++)
            {
                var failureMessage = $"{GetMessage(newEvents[i - 1])}\n{GetMessage(newEvents[i])}";
                Assert.That(newEvents[i].When, Is.EqualTo(newEvents[i - 1].When).Within(1).Seconds, failureMessage);
            }

            if (newEvents.Any())
                eventCheckpoint = newEvents.Last().When;
        }
    }
}
