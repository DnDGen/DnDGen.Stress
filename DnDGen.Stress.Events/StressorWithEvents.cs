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
        private readonly List<GenEvent> summaryEvents;
        private readonly Dictionary<string, int> sourceCounts;

        private Guid clientId;

        public StressorWithEvents(StressorWithEventsOptions options)
            : base(options)
        {
            clientIdManager = options.ClientIdManager;
            eventQueue = options.EventQueue;
            source = options.Source;

            summaryEvents = new List<GenEvent>();
            sourceCounts = new Dictionary<string, int>();
        }

        public StressorWithEvents(StressorWithEventsOptions options, ILogger logger)
            : base(options, logger)
        {
            clientIdManager = options.ClientIdManager;
            eventQueue = options.EventQueue;
            source = options.Source;

            summaryEvents = new List<GenEvent>();
            sourceCounts = new Dictionary<string, int>();
        }

        protected override void StressSetup()
        {
            clientId = Guid.NewGuid();
            clientIdManager.SetClientID(clientId);

            summaryEvents.Clear();
            sourceCounts.Clear();

            base.StressSetup();
        }

        protected override void StressTearDown()
        {
            base.StressTearDown();

            WriteEventSummary();
            ClearEvents();
        }

        private void ClearEvents()
        {
            eventQueue.Clear(clientId);
        }

        private void WriteEventSummary()
        {
            var eventTotal = sourceCounts.Values.Sum();
            logger.Log($"{eventTotal} events were logged in total");

            foreach (var kvp in sourceCounts)
                logger.Log($"\t{kvp.Value} from {kvp.Key}");

            if (!summaryEvents.Any())
                return;

            logger.Log($"Last {summaryEvents.Count} events from {source}:");

            foreach (var genEvent in summaryEvents)
                logger.Log(GetMessage(genEvent));
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
            var dequeuedEvents = GetDequeuedEventsAndAddToEvents();

            if (!dequeuedEvents.Any())
                return;

            Assert.That(dequeuedEvents, Is.Ordered.By("When"));
            Assert.That(summaryEvents, Is.Ordered.By("When"));

            var times = dequeuedEvents.Select(e => e.When);
            var checkpointEvent = dequeuedEvents.First();
            var checkpoint = checkpointEvent.When;
            var finalEvent = dequeuedEvents.Last();
            var finalCheckPoint = finalEvent.When;

            while (finalCheckPoint > checkpoint)
            {
                var oneSecondAfterCheckpoint = checkpoint.AddSeconds(1);

                var failedEvent = dequeuedEvents.First(e => e.When > checkpoint);
                var failureMessage = $"{GetMessage(checkpointEvent)}\n{GetMessage(failedEvent)}";
                Assert.That(times, Has.Some.InRange(checkpoint.AddTicks(1), oneSecondAfterCheckpoint), failureMessage);

                checkpointEvent = dequeuedEvents.Last(e => e.When <= oneSecondAfterCheckpoint);
                checkpoint = checkpointEvent.When;
            }
        }

        private IEnumerable<GenEvent> GetDequeuedEventsAndAddToEvents()
        {
            var dequeuedEvents = eventQueue.DequeueAll(clientId);

            UpdateSummaryEventsWith(dequeuedEvents);

            return dequeuedEvents;
        }

        private void UpdateSummaryEventsWith(IEnumerable<GenEvent> newEvents)
        {
            var eventGroups = newEvents.GroupBy(e => e.Source);

            foreach (var group in eventGroups)
            {
                var source = group.Key;
                var count = group.Count();

                if (!sourceCounts.ContainsKey(source))
                    sourceCounts[source] = 0;

                sourceCounts[source] += count;
            }

            var filteredEvents = newEvents.Where(e => e.Source == source);
            var newSummaryEvents = GetMostRecentEvents(filteredEvents);

            var tempSummary = summaryEvents.Union(newSummaryEvents);
            newSummaryEvents = GetMostRecentEvents(tempSummary).ToArray();

            summaryEvents.Clear();
            summaryEvents.AddRange(newSummaryEvents);
        }

        private IEnumerable<GenEvent> GetMostRecentEvents(IEnumerable<GenEvent> source)
        {
            var mostRecentEvents = source
                .OrderByDescending(e => e.When)
                .Take(EventSummaryCount)
                .Reverse();

            return mostRecentEvents;
        }
    }
}
