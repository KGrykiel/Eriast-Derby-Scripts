using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using Assets.Scripts.Skills;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Logging.Results;

namespace Assets.Scripts.Statistics
{
    /// <summary>
    /// Accumulates race statistics by subscribing to CombatEventBus.OnActionCompleted.
    /// Reads typed domain events directly from each completed CombatAction.
    /// No dependency on RaceHistory or any formatting/logging class.
    /// </summary>
    public class RaceStatsTracker : IDisposable
    {
        // Running accumulators
        private int totalAttacks;
        private int totalHits;
        private int totalMisses;
        private int totalNat20s;
        private int totalNat1s;
        private int totalDamageDealt;
        private int totalSavingThrows;
        private int savesPassed;
        private int savesFailed;
        private readonly Dictionary<string, int> skillUseCounts = new();
        private readonly Dictionary<string, int> conditionApplyCounts = new();
        private readonly Dictionary<string, VehicleMetrics> vehicleStats = new();

        public RaceStatsTracker()
        {
            CombatEventBus.OnActionCompleted += HandleActionCompleted;
            SkillPipeline.OnSkillUsed += HandleSkillUsed;
        }

        public void Dispose()
        {
            CombatEventBus.OnActionCompleted -= HandleActionCompleted;
            SkillPipeline.OnSkillUsed -= HandleSkillUsed;
        }

        // ==================== EVENT HANDLER ====================

        private void HandleActionCompleted(CombatAction action)
        {
            if (action == null) return;

            ProcessAttackRolls(action);
            ProcessSavingThrows(action);
            ProcessDamage(action);
            ProcessEntityConditions(action);
            ProcessVehicleConditions(action);
        }

        private void HandleSkillUsed(string skillName, Vehicle vehicle)
        {
            if (string.IsNullOrEmpty(skillName)) return;

            IncrementKey(skillUseCounts, skillName);

            if (vehicle != null)
                IncrementKey(GetOrCreate(vehicle.vehicleName).SkillUseCounts, skillName);
        }

        // ==================== PROCESSING ====================

        private void ProcessAttackRolls(CombatAction action)
        {
            foreach (var evt in action.Get<AttackRollEvent>())
            {
                totalAttacks++;

                bool hit = evt.Roll.Success;
                if (hit) totalHits++;
                else totalMisses++;

                if (evt.Roll.IsCriticalHit) totalNat20s++;
                if (evt.Roll.IsFumble) totalNat1s++;

                Vehicle attackerVehicle = evt.Actor != null ? evt.Actor.GetVehicle() : null;
                if (attackerVehicle != null)
                {
                    var vm = GetOrCreate(attackerVehicle.vehicleName);
                    vm.Attacks++;
                    if (hit) vm.Hits++;
                    else vm.Misses++;
                    if (evt.Roll.IsCriticalHit) vm.Nat20s++;
                    if (evt.Roll.IsFumble) vm.Nat1s++;
                }
            }
        }

        private void ProcessSavingThrows(CombatAction action)
        {
            foreach (var evt in action.Get<SavingThrowEvent>())
            {
                totalSavingThrows++;

                bool passed = evt.Roll.Success;
                if (passed) savesPassed++;
                else savesFailed++;

                Vehicle defenderVehicle = evt.Defender != null ? evt.Defender.GetVehicle() : null;
                if (defenderVehicle != null)
                {
                    var vm = GetOrCreate(defenderVehicle.vehicleName);
                    vm.SavingThrows++;
                    if (passed) vm.SavesPassed++;
                    else vm.SavesFailed++;
                }
            }
        }

        private void ProcessDamage(CombatAction action)
        {
            foreach (var (target, actor, causalSource, events) in action.GetDamageByTarget())
            {
                int total = events.Sum(e => e.Result.FinalDamage);
                totalDamageDealt += total;

                Vehicle attackerVehicle = actor != null ? actor.GetVehicle() : null;
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

                if (attackerVehicle != null)
                    GetOrCreate(attackerVehicle.vehicleName).DamageDealt += total;

                if (targetVehicle != null)
                    GetOrCreate(targetVehicle.vehicleName).DamageReceived += total;
            }
        }

        private void ProcessEntityConditions(CombatAction action)
        {
            foreach (var evt in action.Get<EntityConditionEvent>())
            {
                if (evt.Applied == null) continue;

                string conditionName = evt.Applied.template != null ? evt.Applied.template.effectName : null;
                if (string.IsNullOrEmpty(conditionName)) continue;

                IncrementKey(conditionApplyCounts, conditionName);

                Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
                Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

                if (sourceVehicle != null)
                {
                    var vm = GetOrCreate(sourceVehicle.vehicleName);
                    vm.ConditionsInflicted++;
                    IncrementKey(vm.ConditionsInflictedByName, conditionName);
                }

                if (targetVehicle != null)
                    GetOrCreate(targetVehicle.vehicleName).ConditionsReceived++;
            }
        }

        private void ProcessVehicleConditions(CombatAction action)
        {
            foreach (var evt in action.Get<VehicleConditionEvent>())
            {
                if (evt.Applied == null) continue;

                string conditionName = evt.Applied.template != null ? evt.Applied.template.effectName : null;
                if (string.IsNullOrEmpty(conditionName)) continue;

                IncrementKey(conditionApplyCounts, conditionName);

                Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
                Vehicle targetVehicle = evt.Target;

                if (sourceVehicle != null)
                {
                    var vm = GetOrCreate(sourceVehicle.vehicleName);
                    vm.ConditionsInflicted++;
                    IncrementKey(vm.ConditionsInflictedByName, conditionName);
                }

                if (targetVehicle != null)
                    GetOrCreate(targetVehicle.vehicleName).ConditionsReceived++;
            }
        }

        // ==================== SNAPSHOT ====================

        public RaceMetrics BuildSnapshot(
            int currentRound,
            int vehiclesTotal,
            int vehiclesActive,
            RaceResult raceResult)
        {
            var metrics = new RaceMetrics
            {
                CurrentRound      = currentRound,
                VehiclesTotal     = vehiclesTotal,
                VehiclesActive    = vehiclesActive,
                TotalAttacks      = totalAttacks,
                TotalHits         = totalHits,
                TotalMisses       = totalMisses,
                TotalNat20s       = totalNat20s,
                TotalNat1s        = totalNat1s,
                TotalDamageDealt  = totalDamageDealt,
                TotalSavingThrows = totalSavingThrows,
                SavesPassed       = savesPassed,
                SavesFailed       = savesFailed,
            };

            foreach (var kv in skillUseCounts)
                metrics.SkillUseCounts[kv.Key] = kv.Value;

            foreach (var kv in conditionApplyCounts)
                metrics.ConditionApplyCounts[kv.Key] = kv.Value;

            foreach (var kv in vehicleStats)
                metrics.VehicleStats[kv.Key] = kv.Value;

            if (raceResult != null)
            {
                metrics.VehiclesFinished  = raceResult.Finishers.Count;
                metrics.VehiclesEliminated = raceResult.DidNotFinish.Count;

                foreach (var record in raceResult.Finishers)
                {
                    string name = record.Vehicle != null ? record.Vehicle.vehicleName : "Unknown";
                    metrics.FinishOrder.Add((name, record.Position, record.Round));
                }

                foreach (var record in raceResult.DidNotFinish)
                {
                    string name = record.Vehicle != null ? record.Vehicle.vehicleName : "Unknown";
                    string stage = record.EliminatedAt != null ? record.EliminatedAt.stageName : "Unknown";
                    metrics.Eliminations.Add((name, stage, record.Round));
                }
            }

            return metrics;
        }

        // ==================== HELPERS ====================

        private VehicleMetrics GetOrCreate(string vehicleName)
        {
            if (!vehicleStats.TryGetValue(vehicleName, out var vm))
            {
                vm = new VehicleMetrics { VehicleName = vehicleName };
                vehicleStats[vehicleName] = vm;
            }
            return vm;
        }

        private static void IncrementKey(Dictionary<string, int> dict, string key)
        {
            if (!dict.TryGetValue(key, out int count))
                count = 0;
            dict[key] = count + 1;
        }
    }
}
