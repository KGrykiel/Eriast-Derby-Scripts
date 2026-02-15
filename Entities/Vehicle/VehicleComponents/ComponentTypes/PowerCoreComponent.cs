using Assets.Scripts.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes
{
    /// <summary>Main energy source. Every vehicle needs one or else it's just a static lump of metal</summary>
    public class PowerCoreComponent : VehicleComponent
    {
        [Header("Energy System")]
        [Tooltip("Current energy available")]
        public int currentEnergy = 50;
        
        [SerializeField]
        [Tooltip("Maximum energy capacity (base value before modifiers)")]
        private int baseMaxEnergy = 50;
        
        [SerializeField]
        [Tooltip("Energy regenerated per turn. INTEGER-FIRST.")]
        private int baseEnergyRegen = 5;
        
        [Header("Power Distribution Limits (Optional)")]
        [SerializeField]
        [Tooltip("Maximum total power that can be drawn per turn by all components combined (0 = no limit) (base value before modifiers)")]
        private int baseMaxPowerDrawPerTurn = 0;  // Default 0 = unlimited, for marathon resource model
        
        [Header("Runtime State")]
        [Tooltip("Total power drawn this turn (resets at start of turn)")]
        public int currentTurnPowerDraw = 0;

        /// <summary>
        /// Default values for convenience, to be edited manually.
        /// </summary>
        void Reset()
        {
            gameObject.name = "Power Core";
            componentType = ComponentType.PowerCore;

            baseMaxHealth = 75;
            health = 75;
            baseArmorClass = 20;
            baseComponentSpace = 0;
            basePowerDrawPerTurn = 0;

            currentEnergy = 50;
            baseMaxEnergy = 50;
            baseEnergyRegen = 5;
            roleType = RoleType.None;
        }

        void Awake()
        {
            componentType = ComponentType.PowerCore;
            currentEnergy = GetMaxEnergy();
            roleType = RoleType.None;
        }
        
        public int GetCurrentEnergy() => currentEnergy;
        public int GetCurrentTurnPowerDraw() => currentTurnPowerDraw;

        public int GetBaseMaxEnergy() => baseMaxEnergy;
        public int GetBaseEnergyRegen() => baseEnergyRegen;
        public int GetBaseMaxPowerDrawPerTurn() => baseMaxPowerDrawPerTurn;

        public int GetMaxEnergy() => StatCalculator.GatherAttributeValue(this, Attribute.MaxEnergy, baseMaxEnergy);
        public int GetEnergyRegen() => StatCalculator.GatherAttributeValue(this, Attribute.EnergyRegen, baseEnergyRegen);
        public int GetMaxPowerDrawPerTurn() => baseMaxPowerDrawPerTurn;

        public void RegenerateEnergy()
        {
            if (isDestroyed) return;

            int regenRate = GetEnergyRegen();
            int maxCap = GetMaxEnergy();

            int oldEnergy = currentEnergy;
            currentEnergy = Mathf.Min(currentEnergy + regenRate, maxCap);
            int regenAmount = currentEnergy - oldEnergy;

            this.LogEnergyRegeneration(regenAmount, currentEnergy, maxCap);
        }

        public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
        {
            var stats = new List<VehicleComponentUI.DisplayStat>();

            int modifiedMaxEnergy = GetMaxEnergy();
            int modifiedRegen = GetEnergyRegen();

            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Energy", "EN", Attribute.MaxEnergy, currentEnergy, baseMaxEnergy, modifiedMaxEnergy));
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Regen", "REGEN", Attribute.EnergyRegen, baseEnergyRegen, modifiedRegen, "/turn"));

            return stats;
        }
        
        /// <summary>Power core destruction = vehicle loses all energy.</summary>
        protected override void OnComponentDestroyed()
        {
            base.OnComponentDestroyed();

            if (parentVehicle == null) return;

            currentEnergy = 0;

            this.LogPowerCoreDestroyed();
        }

        // ==================== POWER MANAGEMENT METHODS ====================

        public bool CanDrawPower(int amount, VehicleComponent requester = null)
        {
            if (currentEnergy < amount) return false;

            int maxPerTurn = GetMaxPowerDrawPerTurn();
            if (maxPerTurn > 0 && currentTurnPowerDraw + amount > maxPerTurn)
                return false;

            return true;
        }

        /// <summary>Returns false if insufficient energy.</summary>
        public bool DrawPower(int amount, VehicleComponent requester, string reason)
        {
            if (!CanDrawPower(amount, requester)) return false;

            currentEnergy -= amount;
            currentTurnPowerDraw += amount;

            this.LogPowerDraw(amount, requester, reason, currentEnergy, currentTurnPowerDraw);

            return true;
        }

        public void ResetTurnPowerTracking()
        {
            currentTurnPowerDraw = 0;
        }
    }
}

