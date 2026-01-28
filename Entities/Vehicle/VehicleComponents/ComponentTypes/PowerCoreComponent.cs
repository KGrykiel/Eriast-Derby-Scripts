using Assets.Scripts.Core;
using Assets.Scripts.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes
{
    /// <summary>
    /// Power Core component - the energy source of a vehicle.
    /// MANDATORY: Every vehicle must have exactly one power core.
    /// Stores and manages the vehicle's energy system.
    /// </summary>
    public class PowerCoreComponent : VehicleComponent
    {
        [Header("Energy System")]
        [Tooltip("Current energy available")]
        public int currentEnergy = 50;
        
        [SerializeField]
        [Tooltip("Maximum energy capacity (base value before modifiers)")]
        private int baseMaxEnergy = 50;
        
        [SerializeField]
        [Tooltip("Energy regenerated per turn (base value before modifiers)")]
        private float baseEnergyRegen = 5f;
        
        [Header("Power Distribution Limits (Optional)")]
        [SerializeField]
        [Tooltip("Maximum total power that can be drawn per turn by all components combined (0 = no limit) (base value before modifiers)")]
        private int baseMaxPowerDrawPerTurn = 0;  // Default 0 = unlimited, for marathon resource model
        
        [Header("Runtime State")]
        [ReadOnly]
        [Tooltip("Total power drawn this turn (resets at start of turn)")]
        public int currentTurnPowerDraw = 0;
        
        /// <summary>
        /// Called when component is first added or reset in Editor.
        /// Sets default values that appear immediately in Inspector.
        /// </summary>
        void Reset()
        {
            // Set GameObject name (shows in hierarchy)
            gameObject.name = "Power Core";
            
            // Set component identity
            componentType = ComponentType.PowerCore;
            
            // Set component base stats using Entity fields
            baseMaxHealth = 75;      // Moderately durable
            health = 75;         // Start at full HP
            baseArmorClass = 20;     // Well-protected critical component
            baseComponentSpace = 0;  // Power cores don't consume space
            basePowerDrawPerTurn = 0;  // Generates power, doesn't consume it
            
            // Set energy system defaults
            currentEnergy = 50;
            baseMaxEnergy = 50;
            baseEnergyRegen = 5f;
            
            // Power core does NOT enable a role
            roleType = RoleType.None;
        }
        
        void Awake()
        {
            // Set component type (in case Reset wasn't called)
            componentType = ComponentType.PowerCore;
            
            // Initialize energy to max
            currentEnergy = GetMaxEnergy();
            
            // Ensure role settings
            roleType = RoleType.None;
        }
        
        // ==================== STAT ACCESSORS ====================
        // These methods provide the single source of truth for stat values with modifiers.
        // UI and game logic should use these instead of accessing fields directly.
        
        // Runtime state accessors (not stats, just current values)
        public int GetCurrentEnergy() => currentEnergy;
        public int GetCurrentTurnPowerDraw() => currentTurnPowerDraw;
        
        // Base value accessors (return raw field values without modifiers)
        public int GetBaseMaxEnergy() => baseMaxEnergy;
        public float GetBaseEnergyRegen() => baseEnergyRegen;
        public int GetBaseMaxPowerDrawPerTurn() => baseMaxPowerDrawPerTurn;
        
        // Modified value accessors (return values with all modifiers applied via StatCalculator)
        public int GetMaxEnergy() => Mathf.RoundToInt(StatCalculator.GatherAttributeValue(this, Attribute.MaxEnergy, baseMaxEnergy));
        public float GetEnergyRegen() => StatCalculator.GatherAttributeValue(this, Attribute.EnergyRegen, baseEnergyRegen);
        
        // MaxPowerDrawPerTurn is a configuration setting, not a modified stat
        public int GetMaxPowerDrawPerTurn() => baseMaxPowerDrawPerTurn;
        
        /// <summary>
        /// Regenerates energy at the start of turn.
        /// Cannot regenerate if power core is destroyed.
        /// Uses StatCalculator for modifier-adjusted values.
        /// </summary>
        public void RegenerateEnergy()
        {
            if (isDestroyed)
            {
                // Cannot regenerate when destroyed
                return;
            }
            
            // Use accessor methods for modified values
            float regenRate = GetEnergyRegen();
            int maxCap = GetMaxEnergy();
            
            int oldEnergy = currentEnergy;
            currentEnergy = Mathf.Min(currentEnergy + Mathf.RoundToInt(regenRate), maxCap);
            
            int regenAmount = currentEnergy - oldEnergy;
            
            if (regenAmount > 0 && parentVehicle != null)
            {
                RaceHistory.Log(
                    Logging.EventType.Resource,
                    EventImportance.Debug,
                    $"{parentVehicle.vehicleName} regenerated {regenAmount} energy ({currentEnergy}/{maxCap})",
                    parentVehicle.currentStage,
                    parentVehicle
                ).WithMetadata("regenAmount", regenAmount)
                 .WithMetadata("currentEnergy", currentEnergy)
                 .WithMetadata("maxEnergy", maxCap);
            }
        }
        
        /// <summary>
        /// Get the stats to display in the UI for this power core.
        /// Uses StatCalculator for modified values.
        /// </summary>
        public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
        {
            var stats = new List<VehicleComponentUI.DisplayStat>();
            
            // Get modified values using accessor methods
            float modifiedMaxEnergy = GetMaxEnergy();
            float modifiedRegen = GetEnergyRegen();
            
            // Energy bar with tooltip for max energy modifiers
            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Energy", "EN", Attribute.MaxEnergy, currentEnergy, baseMaxEnergy, modifiedMaxEnergy));
            
            // Regen with tooltip for regen modifiers
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Regen", "REGEN", Attribute.EnergyRegen, baseEnergyRegen, modifiedRegen, "/turn"));
            
            // Don't add base class stats - power core generates power, doesn't consume it
            
            return stats;
        }
        
        /// <summary>
        /// Called when power core is destroyed.
        /// This is catastrophic - without power, the vehicle cannot function.
        /// Vehicle loses all energy and cannot regenerate it.
        /// </summary>
        protected override void OnComponentDestroyed()
        {
            base.OnComponentDestroyed();
            
            if (parentVehicle == null) return;
            
            // Drain all energy immediately
            currentEnergy = 0;
            
            // Log catastrophic failure
            Debug.LogError($"[PowerCore] CRITICAL: {parentVehicle.vehicleName}'s {name} destroyed! Vehicle has no power!");
            
            RaceHistory.Log(
                Logging.EventType.Combat,
                EventImportance.Critical,
                $"[CRITICAL] {parentVehicle.vehicleName}'s Power Core destroyed! Vehicle is powerless!",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("componentName", name)
             .WithMetadata("componentType", "PowerCore")
             .WithMetadata("currentEnergy", 0)
             .WithMetadata("catastrophicFailure", true);
        }
        
        // ==================== POWER MANAGEMENT METHODS ====================
        
        /// <summary>
        /// Check if there's enough energy available for a draw request.
        /// Validates both total energy and optional per-turn limit.
        /// </summary>
        public bool CanDrawPower(int amount, VehicleComponent requester = null)
        {
            // Primary constraint: Total energy available
            if (currentEnergy < amount) return false;
            
            // Optional constraint: Per-turn limit (usually 0 for unlimited)
            int maxPerTurn = GetMaxPowerDrawPerTurn();
            if (maxPerTurn > 0 && currentTurnPowerDraw + amount > maxPerTurn)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Consume power. Returns true if successful.
        /// Automatically logs power draw for debugging.
        /// </summary>
        public bool DrawPower(int amount, VehicleComponent requester, string reason)
        {
            if (!CanDrawPower(amount, requester)) return false;
            
            currentEnergy -= amount;
            currentTurnPowerDraw += amount;
            
            // Log power draw (debug level)
            if (parentVehicle != null)
            {
                string requesterName = requester != null ? requester.name : "Unknown";
                RaceHistory.Log(
                    Logging.EventType.Resource,
                    EventImportance.Debug,
                    $"{parentVehicle.vehicleName}: {requesterName} drew {amount} power ({reason})",
                    parentVehicle.currentStage,
                    parentVehicle
                ).WithMetadata("powerDrawn", amount)
                 .WithMetadata("remainingEnergy", currentEnergy)
                 .WithMetadata("turnDrawTotal", currentTurnPowerDraw);
            }
            
            return true;
        }
        
        /// <summary>
        /// Reset per-turn tracking. Called at start of vehicle's turn.
        /// </summary>
        public void ResetTurnPowerTracking()
        {
            currentTurnPowerDraw = 0;
        }
    }
}

