using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;

namespace Assets.Scripts.AI
{
    /// <summary>
    /// Plain data snapshot of what a vehicle's crew collectively knows when a
    /// seat is about to act. Rebuilt fresh by <see cref="VehicleAIComponent"/>
    /// before each seat acts — never mutated mid-seat.
    ///
    /// Inter-vehicle information is observable only: enemy HP is only available
    /// as the approximate percentage derived from their chassis, and vehicles
    /// in other stages are intentionally absent.
    /// </summary>
    public class VehicleAISharedContext
    {
        // ==================== OWN VEHICLE ====================

        public Vehicle Self;
        public TurnService TurnService;

        // Own position
        public Stage CurrentStage;
        public StageLane CurrentLane;
        public int CurrentProgress;

        // Own vehicle resources (normalised 0..1 where applicable)
        public float ChassisHealthPercent;
        public float EnergyPercent;
        public float SpeedPercent;

        // ==================== OBSERVABLE OTHERS ====================

        /// <summary>Hostile vehicles in the same stage. Active vehicles only.</summary>
        public List<Vehicle> EnemiesInStage = new();

        /// <summary>Allied vehicles in the same stage. Active vehicles only.</summary>
        public List<Vehicle> AlliesInStage = new();
    }
}
