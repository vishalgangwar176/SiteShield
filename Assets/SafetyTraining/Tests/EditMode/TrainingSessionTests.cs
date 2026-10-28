using System;
using NUnit.Framework;
using SafetyTraining.Core;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class TrainingSessionTests
    {
        [Test]
        public void Inspect_CompletesSession_WhenAllHazardsAreFound()
        {
            // Given
            var session = CreateSession();

            // When
            session.Inspect("fall-edge");
            var result = session.Inspect("blocked-access");

            // Then
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Score, Is.EqualTo(200));
        }

        [Test]
        public void Inspect_PenalizesFirstSafeSelection_WithoutAllowingRepeatedPenalty()
        {
            // Given
            var session = CreateSession();
            session.Inspect("fall-edge");

            // When
            var first = session.Inspect("guarded-edge");
            var repeated = session.Inspect("guarded-edge");

            // Then
            Assert.That(first.Outcome, Is.EqualTo(InspectionOutcome.SafeObjectSelected));
            Assert.That(first.Score, Is.EqualTo(75));
            Assert.That(repeated.Outcome, Is.EqualTo(InspectionOutcome.AlreadyInspected));
            Assert.That(repeated.Score, Is.EqualTo(75));
        }

        [Test]
        public void Inspect_DoesNotChangeState_WhenTargetIsUnknown()
        {
            // Given
            var session = CreateSession();

            // When
            var result = session.Inspect("not-in-scene");

            // Then
            Assert.That(result.Outcome, Is.EqualTo(InspectionOutcome.UnknownTarget));
            Assert.That(result.Score, Is.Zero);
            Assert.That(result.HazardsFound, Is.Zero);
        }

        [Test]
        public void Constructor_RejectsDuplicateTargetIdentifiers()
        {
            // Given, When, Then
            Assert.Throws<ArgumentException>(() => new TrainingSession(
                TrainingSiteId.Warehouse,
                new[] { new InspectionTargetSpec("spill", true), new InspectionTargetSpec("spill", false) }));
        }

        [Test]
        public void Constructor_RejectsSiteWithoutHazards()
        {
            // Given, When, Then
            Assert.Throws<ArgumentException>(() => new TrainingSession(
                TrainingSiteId.FireResponse,
                new[] { new InspectionTargetSpec("clear-exit", false) }));
        }

        [Test]
        public void GuidedPlan_RemainsIncompleteBeforeTwentyMinutes()
        {
            var plan = CreateCompletedActivityPlan(239f);

            Assert.That(plan.IsComplete, Is.False);
            Assert.That(plan.ElapsedSeconds, Is.EqualTo(1195f).Within(0.01f));
        }

        [Test]
        public void GuidedPlan_CompletesAfterFourMinutesAndTwoCoachTurnsPerSite()
        {
            var plan = CreateCompletedActivityPlan(GuidedSessionPlan.MinimumSiteSeconds);

            Assert.That(plan.IsComplete, Is.True);
            Assert.That(plan.ElapsedSeconds, Is.EqualTo(GuidedSessionPlan.MinimumSessionSeconds).Within(0.01f));
            Assert.That(plan.CompletedCoachTurns, Is.EqualTo(10));
        }

        [Test]
        public void GuidedPlan_DoesNotCreditTimeWithoutAnActiveSite()
        {
            var plan = new GuidedSessionPlan();

            plan.Advance(null, 300f);

            Assert.That(plan.ElapsedSeconds, Is.Zero);
        }

        static GuidedSessionPlan CreateCompletedActivityPlan(float secondsPerSite)
        {
            var plan = new GuidedSessionPlan();
            foreach (var site in (TrainingSiteId[])Enum.GetValues(typeof(TrainingSiteId)))
            {
                plan.Advance(site, secondsPerSite);
                plan.RecordCoachTurn(site);
                plan.RecordCoachTurn(site);
            }
            return plan;
        }

        static TrainingSession CreateSession()
        {
            return new TrainingSession(TrainingSiteId.Construction, new[]
            {
                new InspectionTargetSpec("fall-edge", true),
                new InspectionTargetSpec("blocked-access", true),
                new InspectionTargetSpec("guarded-edge", false)
            });
        }
    }
}
