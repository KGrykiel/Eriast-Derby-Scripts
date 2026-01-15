using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Entities.Vehicle.VehicleComponents.ComponentTypes
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
        
        [Tooltip("Maximum energy capacity")]
        public int maxEnergy = 50;
        
        [Tooltip("Energy regenerated per turn")]
        public float energyRegen = 5f;
        
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
            maxHealth = 75;      // Moderately durable
            health = 75;         // Start at full HP
            armorClass = 20;     // Well-protected critical component
            componentSpace = 0;  // Power cores don't consume space
            powerDrawPerTurn = 0;  // Generates power, doesn't consume it
            
            // Set energy system defaults
            currentEnergy = 50;
            maxEnergy = 50;
            energyRegen = 5f;
            
            // Power core does NOT enable a role
            enablesRole = false;
            roleName = "";
        }
        
        void Awake()
        {
            // Set component type (in case Reset wasn't called)
            componentType = ComponentType.PowerCore;
            
            // Initialize energy to max
            currentEnergy = maxEnergy;
            
            // Ensure role settings
            enablesRole = false;
            roleName = "";
        }
        
        /// <summary>
        /// Get maximum energy capacity (with modifiers from StatCalculator).
        /// </summary>
        public int GetMaxEnergy()
        {
            float modified = Core.StatCalculator.GatherAttributeValue(this, Attribute.MaxEnergy, maxEnergy);
            return Mathf.RoundToInt(modified);
        }
        
        /// <summary>
        /// Get energy regeneration rate (with modifiers from StatCalculator).
        /// </summary>
        public float GetEnergyRegen()
        {
            return Core.StatCalculator.GatherAttributeValue(this, Attribute.EnergyRegen, energyRegen);
        }
        
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
            
            // Use StatCalculator for modified regen rate and max capacity
            float regenRate = GetEnergyRegen();
            int maxCap = GetMaxEnergy();
            
            int oldEnergy = currentEnergy;
            currentEnergy = Mathf.Min(currentEnergy + Mathf.RoundToInt(regenRate), maxCap);
            
            int regenAmount = currentEnergy - oldEnergy;
            
            if (regenAmount > 0 && parentVehicle != null)
            {
                RacingGame.Events.RaceHistory.Log(
                    RacingGame.Events.EventType.Resource,
                    RacingGame.Events.EventImportance.Debug,
                    $"{parentVehicle.vehicleName} regenerated {regenAmount} energy ({currentEnergy}/{maxCap})",
                    parentVehicle.currentStage,
                    parentVehicle
                ).WithMetadata("regenAmount", regenAmount)
                 .WithMetadata("currentEnergy", currentEnergy)
                 .WithMetadata("maxEnergy", maxCap);
            }
        }
        
        /// <summary>
        /// Consumes energy. Returns true if successful, false if insufficient energy.
        /// </summary>
        public bool ConsumeEnergy(int amount)
        {
            if (isDestroyed) return false;
            if (currentEnergy < amount) return false;
            
            currentEnergy -= amount;
            return true;
        }
        
        /// <summary>
        /// Get the stats to display in the UI for this power core.
        /// Uses StatCalculator for modified values.
        /// </summary>
        public override List<VehicleComponentUI.DisplayStat> GetDisplayStats()
        {
            var stats = new List<VehicleComponentUI.DisplayStat>();
            
            // Get modified values from StatCalculator
            float modifiedMaxEnergy = GetMaxEnergy();
            float modifiedRegen = GetEnergyRegen();
            
            // Energy bar with tooltip for max energy modifiers
            stats.Add(VehicleComponentUI.DisplayStat.BarWithTooltip("Energy", "EN", Attribute.MaxEnergy, currentEnergy, maxEnergy, modifiedMaxEnergy));
            
            // Regen with tooltip for regen modifiers
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Regen", "REGEN", Attribute.EnergyRegen, energyRegen, modifiedRegen, "/turn"));
            
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
            
            RacingGame.Events.RaceHistory.Log(
                RacingGame.Events.EventType.Combat,
                RacingGame.Events.EventImportance.Critical,
                $"[CRITICAL] {parentVehicle.vehicleName}'s Power Core destroyed! Vehicle is powerless!",
                parentVehicle.currentStage,
                parentVehicle
            ).WithMetadata("componentName", name)
             .WithMetadata("componentType", "PowerCore")
             .WithMetadata("currentEnergy", 0)
             .WithMetadata("catastrophicFailure", true);
        }
    }
}
