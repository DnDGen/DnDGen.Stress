using EventGen;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDGen.Stress.Events
{
    [TestFixture]
    public class StressorWithEvents : Stressor
    {
        private const int EventSummaryCount = 10;

        private readonly ClientIDManager clientIdManager;
        private readonly GenEventQueue eventQueue;
        private readonly string source;
        private readonly List<GenEvent> events;

        private Guid clientId;

        public StressorWithEvents(StressorWithEventsOptions options)
            : base(options)
        {
            clientIdManager = options.ClientIdManager;
            eventQueue = options.EventQueue;
            source = options.Source;

            events = new List<GenEvent>();
        }

        protected override void StressSetup()
        {
            clientId = Guid.NewGuid();
            clientIdManager.SetClientID(clientId);

            events.Clear();
            base.StressSetup();
        }

        protected override void StressTearDown()
        {
            base.StressTearDown();

            WriteEventSummary();
        }

        private void WriteEventSummary()
        {
            GetDequeuedEventsAndAddToEvents();

            foreach (var genEvent in events)
                Console.WriteLine(GetMessage(genEvent));
        }

        private string GetMessage(GenEvent genEvent)
        {
            return $"[{genEvent.When.ToLongTimeString()}] {genEvent.Source}: {genEvent.Message}";
        }

        public override T Generate<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return base.Generate(() => GenerateAndAssertEvent(generate), isValid);
        }

        public override T GenerateOrFail<T>(Func<T> generate, Func<T, bool> isValid)
        {
            return base.GenerateOrFail(() => GenerateAndAssertEvent(generate), isValid);
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

        public override void Stress(Action setup, Action test, Action teardown)
        {
            base.Stress(setup, () => RunAndAssertEvent(test), teardown);
        }

        public override void Stress(Action test)
        {
            base.Stress(() => RunAndAssertEvent(test));
        }

        private void AssertEventSpacing()
        {
            var dequeuedEvents = GetDequeuedEventsAndAddToEvents().ToArray();

            Assert.That(dequeuedEvents, Is.Ordered.By("When"));
            Assert.That(events, Is.Ordered.By("When"));

            for (var i = 1; i < dequeuedEvents.Length; i++)
            {
                var failureMessage = $"{GetMessage(dequeuedEvents[i - 1])}\n{GetMessage(dequeuedEvents[i])}";
                Assert.That(dequeuedEvents[i].When, Is.EqualTo(dequeuedEvents[i - 1].When).Within(1).Seconds, failureMessage);
            }
        }

        private IEnumerable<GenEvent> GetDequeuedEventsAndAddToEvents()
        {
            //INFO: Get the 10 most recent events for the source.  We assume the events are ordered chronologically already

            var dequeuedEvents = eventQueue.DequeueAll(clientId);
            var filteredDequeuedEvents = dequeuedEvents.Where(e => e.Source == source);

            events.AddRange(filteredDequeuedEvents);
            var skipTotal = Math.Max(events.Count - EventSummaryCount, 0);

            //INFO: Execute immediately, so that the items are preserved after we clear the list
            var summaryEvents = events.Skip(skipTotal).Take(EventSummaryCount).ToArray();

            events.Clear();
            events.AddRange(summaryEvents);

            return dequeuedEvents;
        }
    }
}
