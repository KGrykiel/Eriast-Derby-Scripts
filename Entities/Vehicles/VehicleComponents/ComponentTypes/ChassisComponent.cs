using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Core;

namespace Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes
{
    /// <summary>Structural foundation. Mandatory. The chassis IS the vehicle's HP/AC. Determines size category.</summary>
    public class ChassisComponent : VehicleComponent
    {
        [Header("Chassis Stats")]
        [SerializeField]
        [Tooltip("Base mobility for saving throws (dodging, evasion). Higher = easier to dodge AOE/traps (base value before modifiers).")]
        private int baseMobility = 8;

        [SerializeField]
        [Tooltip("Structural quality of this vehicle's build. Cheap components = low, professionally built = high. " +
                 "Effective integrity also decreases as the chassis takes damage, modelling how a battered vehicle " +
                 "is less able to withstand further stress (base value before modifiers).")]
        private int baseIntegrity = 1;

        [SerializeField]
        [Tooltip("Resistance to being destabilised by terrain, collisions, and impacts. Higher = harder to knock off course (base value before modifiers).")]
        private int baseStability = 5;

        [Header("Chassis Size")]
        [Tooltip("Size category determines AC, mobility, and speed modifiers. Larger = easier to hit, harder to maneuver.")]
        public VehicleSizeCategory sizeCategory = VehicleSizeCategory.Medium;

        [Header("Aerodynamic Properties")]
        [SerializeField]
        [Tooltip("Aerodynamic drag coefficient as percentage (10 = 0.10, 15 = 0.15). Higher = more drag.")]
        private int baseDragCoefficientPercent = 10;

        [Header("Cargo")]
        [Tooltip("Bulk units the chassis provides without a dedicated cargo bay. Heavier chassis have higher values.")]
        public int baseCargoCapacity = 0;

        /// <summary>
        /// Default values for convenience, to be edited manually.
        /// </summary>
        void Reset()
        {
            gameObject.name = "Chassis";
            componentType = ComponentType.Chassis;

            baseMaxHealth = 100;
            health = 100;
            baseArmorClass = 18;
            baseMobility = 8;
            baseStability = 5;
            baseComponentSpace = -2000;
            sizeCategory = VehicleSizeCategory.Medium;
            basePowerDrawPerTurn = 0;
            roleType = RoleType.None;
            baseIntegrity = 1;
        }

        void Awake()
        {
            componentType = ComponentType.Chassis;
            roleType = RoleType.None;
        }

        // ==================== STAT ACCESSORS ====================

        public int GetBaseMobility() => baseMobility;
        public int GetBaseIntegrity() => baseIntegrity;
        public int GetBaseStability() => baseStability;
        public int GetBaseDragCoefficientPercent() => baseDragCoefficientPercent;

        public int GetMobility() => StatCalculator.GatherAttributeValue(this, EntityAttribute.Mobility);
        public int GetIntegrity() => StatCalculator.GatherAttributeValue(this, EntityAttribute.Integrity);
        public int GetStability() => StatCalculator.GatherAttributeValue(this, EntityAttribute.Stability);
        public int GetDragCoefficientPercent() => StatCalculator.GatherAttributeValue(this, EntityAttribute.DragCoefficient);

        public override int GetBaseValue(EntityAttribute attribute)
        {
            return attribute switch
            {
                EntityAttribute.Mobility => baseMobility,
                EntityAttribute.Integrity => baseIntegrity + GetIntegrityDamagePenalty(),
                EntityAttribute.Stability => baseStability,
                EntityAttribute.DragCoefficient => baseDragCoefficientPercent,
                EntityAttribute.CargoCapacity => baseCargoCapacity,
                _ => base.GetBaseValue(attribute)
            };
        }

        // Chassis takes a tiered penalty to integrity as it accumulates damage.
        // Models how a battered structure is less able to withstand further stress.
        // TODO: consider making it smarter, the below should be considered as a placeholder.
        private int GetIntegrityDamagePenalty()
        {
            if (baseMaxHealth <= 0) return 0;
            float ratio = (float)health / baseMaxHealth;
            if (ratio > 0.75f) return 0;
            if (ratio > 0.50f) return -1;
            if (ratio > 0.25f) return -3;
            return -5;
        }

        // ==================== STATS ====================

        public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
        {
            var stats = new List<VehicleComponentUI.DisplayStat>();

            // componentSpace is negative for chassis (provides space)
            int baseSpace = -GetBaseComponentSpace();
            int modifiedSpace = -GetComponentSpace();
            if (baseSpace > 0 || modifiedSpace > 0)
            {
                stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Capacity", "CAP", EntityAttribute.ComponentSpace, baseSpace, modifiedSpace));
            }

            int modifiedMobility = GetMobility();
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Mobility", "MBL", EntityAttribute.Mobility, baseMobility, modifiedMobility));

            int modifiedStability = GetStability();
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Stability", "STB", EntityAttribute.Stability, baseStability, modifiedStability));

            int modifiedIntegrity = GetIntegrity();
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Integrity", "INTG", EntityAttribute.Integrity, baseIntegrity, modifiedIntegrity));

            int modifiedDrag = GetDragCoefficientPercent();
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Drag", "DRAG", EntityAttribute.DragCoefficient, baseDragCoefficientPercent, modifiedDrag, "%"));

            int modifiedCargoCapacity = GetCargoCapacity();
            if (baseCargoCapacity > 0 || modifiedCargoCapacity > 0)
                stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Cargo", "CRGO", EntityAttribute.CargoCapacity, baseCargoCapacity, modifiedCargoCapacity));

            return stats;
        }

        /// <summary>Chassis destruction = vehicle destruction.</summary>
        protected override void OnComponentDestroyed()
        {
            base.OnComponentDestroyed();

            if (parentVehicle == null) return;

            this.LogChassisDestroyed();

            // Immediately mark vehicle as destroyed (fires event for immediate handling)
            parentVehicle.MarkAsDestroyed();
        }
    }
}