using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.VehicleComponents
{
    /// <summary>
    /// Power Core component - the energy source of a vehicle.
    /// MANDATORY: Every vehicle must have exactly one power core.
    /// Provides: Power Capacity (total power storage) and Power Discharge (power available per turn).
    /// Does NOT enable any role (it's just power supply).
    /// </summary>
    public class PowerCoreComponent : VehicleComponent
    {
        [Header("Power Core Stats")]
        [Tooltip("Power Capacity - total power storage")]
        public int powerCapacity = 1000;
        
        [Tooltip("Power Discharge - power available per turn")]
        public int powerDischarge = 50;
        
        /// <summary>
        /// Called when component is first added or reset in Editor.
        /// Sets default values that appear immediately in Inspector.
        /// </summary>
        void Reset()
        {
            // Set component identity
            componentType = ComponentType.PowerCore;
            componentName = "Standard Power Core";
            
            // Set component base stats
            componentHP = 75;   // Moderately durable
            componentAC = 20;   // Well-protected critical component
            componentSpaceRequired = 0;  // Power cores don't consume space
            powerDrawPerTurn = 0;  // Generates power, doesn't consume it
            
            // Set power core-specific stats (already have defaults in field declarations)
            // powerCapacity = 1000;
            // powerDischarge = 50;
            
            // Power core does NOT enable a role
            enablesRole = false;
            roleName = "";
        }
        
        void Awake()
        {
            // Set component type (in case Reset wasn't called)
            componentType = ComponentType.PowerCore;
            
            // Initialize current HP
            currentHP = componentHP;
            
            // Ensure role settings
            enablesRole = false;
            roleName = "";
        }
        
        /// <summary>
        /// Power core provides Power Capacity and Power Discharge to the vehicle.
        /// </summary>
        public override VehicleStatModifiers GetStatModifiers()
        {
            // If power core is destroyed or disabled, it contributes nothing
            if (isDestroyed || isDisabled)
                return VehicleStatModifiers.Zero;
            
            // Create modifiers using the flexible stat system
            var modifiers = new VehicleStatModifiers();
            modifiers.PowerCapacity = powerCapacity;
            modifiers.PowerDischarge = powerDischarge;
            
            return modifiers;
        }
        
        /// <summary>
        /// Called when power core is destroyed.
        /// This is catastrophic - without power, the vehicle cannot function.
        /// </summary>
        protected override void OnComponentDestroyed()
        {
            base.OnComponentDestroyed();
            
            // Power core destruction is catastrophic
            Debug.LogError($"[PowerCore] CRITICAL: {componentName} destroyed! Vehicle has no power!");
            
            // TODO: In future, trigger vehicle shutdown if power core is destroyed
            // For now, vehicle can continue with no power regeneration
        }
    }
}
