using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Logging.Results;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    internal class RaceManagementTests
    {
        private readonly List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();
            TurnEventBus.ClearAllSubscribers();
        }

        // ==================== HELPERS ====================

        private Vehicle CreateVehicle(string name = "TestVehicle")
        {
            var vehicle = TestVehicleBuilder.CreateEmpty();
            vehicle.vehicleName = name;
            cleanup.Add(vehicle.gameObject);
            return vehicle;
        }

        private RaceCompletionTracker CreateTracker(IEnumerable<Vehicle> vehicles, int maxRounds = 10)
        {
            var stateMachine = new TurnStateMachine();
            var tracker = new RaceCompletionTracker(stateMachine, vehicles, maxRounds);
            tracker.Subscribe();
            return tracker;
        }

        // ==================== VEHICLE STATUS ====================

        [Test]
        public void MarkAsFinished_SetsStatusToFinished()
        {
            var vehicle = CreateVehicle();

            vehicle.MarkAsFinished();

            Assert.AreEqual(VehicleStatus.Finished, vehicle.Status);
        }

        [Test]
        public void MarkAsFinished_IsIdempotent()
        {
            var vehicle = CreateVehicle();

            vehicle.MarkAsFinished();
            vehicle.MarkAsFinished();

            Assert.AreEqual(VehicleStatus.Finished, vehicle.Status);
        }

        [Test]
        public void MarkAsFinished_EmitsOnVehicleFinished()
        {
            var vehicle = CreateVehicle();
            Vehicle received = null;
            TurnEventBus.OnVehicleFinished += v => received = v;

            vehicle.MarkAsFinished();

            Assert.AreEqual(vehicle, received);
        }

        [Test]
        public void MarkAsFinished_DoesNotEmitTwiceWhenCalledTwice()
        {
            var vehicle = CreateVehicle();
            int emitCount = 0;
            TurnEventBus.OnVehicleFinished += _ => emitCount++;

            vehicle.MarkAsFinished();
            vehicle.MarkAsFinished();

            Assert.AreEqual(1, emitCount);
        }

        // ==================== SHOULD SKIP TURN ====================

        [Test]
        public void ShouldSkipTurn_NullVehicle_ReturnsTrue()
        {
            var stateMachine = new TurnStateMachine();

            bool skip = stateMachine.ShouldSkipTurn(null);

            Assert.IsTrue(skip);
        }

        [Test]
        public void ShouldSkipTurn_VehicleWithNoStage_ReturnsTrue()
        {
            var vehicle = CreateVehicle();
            var stateMachine = new TurnStateMachine();

            bool skip = stateMachine.ShouldSkipTurn(vehicle);

            Assert.IsTrue(skip);
        }

        [Test]
        public void ShouldSkipTurn_ActiveVehicleWithStage_ReturnsFalse()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            var stateMachine = new TurnStateMachine();

            bool skip = stateMachine.ShouldSkipTurn(vehicle);

            Assert.IsFalse(skip);
        }

        [Test]
        public void ShouldSkipTurn_FinishedVehicle_ReturnsTrue()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            vehicle.MarkAsFinished();
            var stateMachine = new TurnStateMachine();

            bool skip = stateMachine.ShouldSkipTurn(vehicle);

            Assert.IsTrue(skip);
        }

        [Test]
        public void ShouldSkipTurn_DestroyedVehicle_ReturnsTrue()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            vehicle.MarkAsDestroyed();
            var stateMachine = new TurnStateMachine();

            bool skip = stateMachine.ShouldSkipTurn(vehicle);

            Assert.IsTrue(skip);
        }

        // ==================== FINISH LINE ====================

        [Test]
        public void FinishLineCrossed_MarksVehicleAsFinished()
        {
            var vehicle = CreateVehicle();
            var finishStage = TestStageFactory.CreateStage("Finish", out var stageObj);
            cleanup.Add(stageObj);
            CreateTracker(new[] { vehicle });

            TurnEventBus.EmitFinishLineCrossed(vehicle, finishStage);

            Assert.AreEqual(VehicleStatus.Finished, vehicle.Status);
        }

        [Test]
        public void FinishLineCrossed_RecordsVehicleInPosition1()
        {
            var vehicle = CreateVehicle();
            var finishStage = TestStageFactory.CreateStage("Finish", out var stageObj);
            cleanup.Add(stageObj);
            var tracker = CreateTracker(new[] { vehicle });

            TurnEventBus.EmitFinishLineCrossed(vehicle, finishStage);

            Assert.AreEqual(1, tracker.Finishers.Count);
            Assert.AreEqual(vehicle, tracker.Finishers[0].Vehicle);
            Assert.AreEqual(1, tracker.Finishers[0].Position);
        }

        [Test]
        public void FinishLineCrossed_MultipleVehicles_CorrectFinishOrder()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var finishStage = TestStageFactory.CreateStage("Finish", out var stageObj);
            cleanup.Add(stageObj);
            var tracker = CreateTracker(new[] { vehicleA, vehicleB });

            TurnEventBus.EmitFinishLineCrossed(vehicleA, finishStage);
            TurnEventBus.EmitFinishLineCrossed(vehicleB, finishStage);

            Assert.AreEqual(vehicleA, tracker.Finishers[0].Vehicle);
            Assert.AreEqual(1, tracker.Finishers[0].Position);
            Assert.AreEqual(vehicleB, tracker.Finishers[1].Vehicle);
            Assert.AreEqual(2, tracker.Finishers[1].Position);
        }

        [Test]
        public void FinishLineCrossed_AllVehiclesFinished_EmitsRaceOver()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var finishStage = TestStageFactory.CreateStage("Finish", out var stageObj);
            cleanup.Add(stageObj);
            CreateTracker(new[] { vehicleA, vehicleB });
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitFinishLineCrossed(vehicleA, finishStage);
            TurnEventBus.EmitFinishLineCrossed(vehicleB, finishStage);

            Assert.IsNotNull(result);
        }

        [Test]
        public void FinishLineCrossed_PartiallyFinished_DoesNotEmitRaceOver()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var finishStage = TestStageFactory.CreateStage("Finish", out var stageObj);
            cleanup.Add(stageObj);
            CreateTracker(new[] { vehicleA, vehicleB });
            bool raceOverFired = false;
            TurnEventBus.OnRaceOver += _ => raceOverFired = true;

            TurnEventBus.EmitFinishLineCrossed(vehicleA, finishStage);

            Assert.IsFalse(raceOverFired);
        }

        // ==================== ELIMINATION ====================

        [Test]
        public void VehicleDestroyed_RecordedInRaceResult_DidNotFinish()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            CreateTracker(new[] { vehicle });
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitVehicleDestroyed(vehicle);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.DidNotFinish.Count);
            Assert.AreEqual(vehicle, result.DidNotFinish[0].Vehicle);
        }

        [Test]
        public void VehicleDestroyed_RecordsCorrectEliminationStage()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            CreateTracker(new[] { vehicle });
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitVehicleDestroyed(vehicle);

            Assert.AreEqual(stage, result.DidNotFinish[0].EliminatedAt);
        }

        [Test]
        public void VehicleDestroyed_WithFinisher_EmitsRaceOver_WhenAllAccountedFor()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicleB, stage);
            CreateTracker(new[] { vehicleA, vehicleB });
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitFinishLineCrossed(vehicleA, stage);
            TurnEventBus.EmitVehicleDestroyed(vehicleB);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Finishers.Count);
            Assert.AreEqual(1, result.DidNotFinish.Count);
        }

        // ==================== ROUND CAP ====================

        [Test]
        public void RoundCap_ActiveVehicles_ClassifiedAsDNF()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicleA, stage);
            RacePositionTracker.SetStage(vehicleB, stage);
            CreateTracker(new[] { vehicleA, vehicleB }, maxRounds: 5);
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitRoundEnded(5);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.DidNotFinish.Count);
        }

        [Test]
        public void RoundCap_EmitsRaceOver()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            CreateTracker(new[] { vehicle }, maxRounds: 5);
            bool raceOverFired = false;
            TurnEventBus.OnRaceOver += _ => raceOverFired = true;

            TurnEventBus.EmitRoundEnded(5);

            Assert.IsTrue(raceOverFired);
        }

        [Test]
        public void RoundCap_BelowMaxRounds_DoesNotTrigger()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            CreateTracker(new[] { vehicle }, maxRounds: 5);
            bool raceOverFired = false;
            TurnEventBus.OnRaceOver += _ => raceOverFired = true;

            TurnEventBus.EmitRoundEnded(3);

            Assert.IsFalse(raceOverFired);
        }

        [Test]
        public void RoundCap_AlreadyFinishedVehicle_ExcludedFromDNF()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicleB, stage);
            CreateTracker(new[] { vehicleA, vehicleB }, maxRounds: 5);
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitFinishLineCrossed(vehicleA, stage);
            TurnEventBus.EmitRoundEnded(5);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Finishers.Count);
            Assert.AreEqual(vehicleA, result.Finishers[0].Vehicle);
            Assert.AreEqual(1, result.DidNotFinish.Count);
            Assert.AreEqual(vehicleB, result.DidNotFinish[0].Vehicle);
        }

        // ==================== RACE RESULT ====================

        [Test]
        public void RaceResult_Winner_IsFirstFinisher()
        {
            var vehicleA = CreateVehicle("VehicleA");
            var vehicleB = CreateVehicle("VehicleB");
            var stage = TestStageFactory.CreateStage("Finish", out var stageObj);
            cleanup.Add(stageObj);
            CreateTracker(new[] { vehicleA, vehicleB });
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitFinishLineCrossed(vehicleA, stage);
            TurnEventBus.EmitFinishLineCrossed(vehicleB, stage);

            Assert.AreEqual(vehicleA, result.Winner);
        }

        [Test]
        public void RaceResult_Winner_IsNull_WhenAllDNF()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            CreateTracker(new[] { vehicle }, maxRounds: 5);
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitRoundEnded(5);

            Assert.IsFalse(result.HasFinishers);
            Assert.IsNull(result.Winner);
        }

        [Test]
        public void RaceResult_TotalParticipants_MatchesRosterSize()
        {
            var vehicles = new[]
            {
                CreateVehicle("A"),
                CreateVehicle("B"),
                CreateVehicle("C")
            };
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            foreach (var v in vehicles) RacePositionTracker.SetStage(v, stage);
            CreateTracker(vehicles, maxRounds: 5);
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitRoundEnded(5);

            Assert.AreEqual(3, result.TotalParticipants);
        }

        [Test]
        public void RaceResult_TotalRounds_MatchesRoundAtCompletion()
        {
            var vehicle = CreateVehicle();
            var stage = TestStageFactory.CreateStage("Stage1", out var stageObj);
            cleanup.Add(stageObj);
            RacePositionTracker.SetStage(vehicle, stage);
            CreateTracker(new[] { vehicle }, maxRounds: 7);
            RaceResult result = null;
            TurnEventBus.OnRaceOver += r => result = r;

            TurnEventBus.EmitRoundEnded(7);

            Assert.AreEqual(7, result.TotalRounds);
        }
    }
}

