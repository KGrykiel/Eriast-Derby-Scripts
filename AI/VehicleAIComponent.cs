using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;
using Assets.Scripts.Skills;
using UnityEngine;

namespace Assets.Scripts.AI
{
    /// <summary>
    /// Orchestrator MonoBehaviour added to AI vehicle prefabs in the Inspector.
    /// Owns the vehicle's per-seat AI instances, rebuilds the shared perception
    /// context before each seat acts, and iterates seats in the order set on
    /// <c>Vehicle.seats</c> (designer-controlled, no separate priority field).
    ///
    /// Holds no personality and makes no scoring decisions — it is a coordinator
    /// only. Cleanup is automatic via Unity's <c>OnDestroy</c>.
    /// </summary>
    [RequireComponent(typeof(Vehicle))]
    public class VehicleAIComponent : MonoBehaviour
    {
        private Vehicle vehicle;
        private readonly List<SeatAI> seatAIs = new();

        private void Awake()
        {
            vehicle = GetComponent<Vehicle>();
            if (vehicle == null)
                Debug.LogError($"[VehicleAIComponent] No Vehicle component on '{name}'.");
        }

        /// <summary>
        /// Selects and returns the next single action for this vehicle, advancing
        /// through seats in order. Returns null when all seats are exhausted.
        /// Called repeatedly by AITurnController until null.
        /// </summary>
        public SkillAction TakeOneStep(TurnService turnService)
        {
            if (vehicle == null || !vehicle.IsOperational()) return null;
            if (turnService == null) return null;

            SyncSeatAIs();

            foreach (var seatAI in seatAIs)
            {
                if (seatAI == null) continue;
                SkillAction action = seatAI.TrySelectAction(BuildContext(turnService));
                if (action != null)
                    return action;
            }

            return null;
        }

        // ==================== SEAT MANAGEMENT ====================

        private void SyncSeatAIs()
        {
            seatAIs.Clear();
            foreach (var seat in vehicle.seats)
            {
                if (seat != null) seatAIs.Add(new SeatAI(seat));
                else seatAIs.Add(null);
            }
        }

        // ==================== CONTEXT ASSEMBLY ====================

        private VehicleAISharedContext BuildContext(TurnService turnService)
        {
            var ctx = new VehicleAISharedContext
            {
                Self = vehicle,
                TurnService = turnService,
                CurrentStage = RacePositionTracker.GetStage(vehicle),
                CurrentLane = RacePositionTracker.GetLane(vehicle),
                CurrentProgress = RacePositionTracker.GetProgress(vehicle),
                ChassisHealthPercent = GetChassisHealthPercent(),
                EnergyPercent = GetEnergyPercent(),
                SpeedPercent = GetSpeedPercent()
            };

            foreach (var other in turnService.GetOtherVehiclesInStage(vehicle))
            {
                if (other == null) continue;
                if (TurnService.AreAllied(vehicle, other))
                    ctx.AlliesInStage.Add(other);
                else if (TurnService.AreHostile(vehicle, other))
                    ctx.EnemiesInStage.Add(other);
            }

            return ctx;
        }

        private float GetChassisHealthPercent()
        {
            if (vehicle.Chassis == null) return 0f;
            int max = vehicle.Chassis.GetMaxHealth();
            if (max <= 0) return 0f;
            return Mathf.Clamp01(vehicle.Chassis.GetCurrentHealth() / (float)max);
        }

        private float GetEnergyPercent()
        {
            if (vehicle.PowerCore == null) return 0f;
            int max = vehicle.PowerCore.GetMaxEnergy();
            if (max <= 0) return 0f;
            return Mathf.Clamp01(vehicle.PowerCore.GetCurrentEnergy() / (float)max);
        }

        private float GetSpeedPercent()
        {
            if (vehicle.Drive == null) return 0f;
            return Mathf.Clamp01(vehicle.Drive.GetCurrentSpeed() / 100f);
        }
    }
}
