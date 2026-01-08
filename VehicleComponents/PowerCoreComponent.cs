using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.VehicleComponents
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
        /// Get maximum energy capacity (with modifiers applied).
        /// </summary>
        public int GetMaxEnergy()
        {
            return Mathf.RoundToInt(ApplyModifiers(Attribute.MaxEnergy, maxEnergy));
        }
        
        /// <summary>
        /// Get energy regeneration rate (with modifiers applied).
        /// </summary>
        public float GetEnergyRegen()
        {
            return ApplyModifiers(Attribute.EnergyRegen, energyRegen);
        }
        
        /// <summary>
        /// Regenerates energy at the start of turn.
        /// Cannot regenerate if power core is destroyed.
        /// Uses modifier-adjusted max energy and regen rate.
        /// </summary>
        public void RegenerateEnergy()
        {
            if (isDestroyed)
            {
                // Cannot regenerate when destroyed
                return;
            }
            
            // Use modified regen rate and max capacity
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
        /// Power core does not contribute stats via GetStatModifiers.
        /// Energy is accessed directly through powerCore.currentEnergy.
        /// </summary>
        public override VehicleStatModifiers GetStatModifiers()
        {
            // Power core doesn't contribute stats through the modifier system
            // Energy is a resource managed directly by the power core
            return VehicleStatModifiers.Zero;
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
