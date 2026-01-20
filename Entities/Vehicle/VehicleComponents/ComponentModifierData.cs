using System;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle.VehicleComponents
{
    /// <summary>
    /// Defines a modifier that one component provides to another component.
    /// Used for cross-component bonuses (e.g., Advanced Armor gives +2 AC to Chassis).
    /// 
    /// This is SEPARATE from ModifierData (used by StatusEffects) to keep concerns distinct.
    /// Components should NOT use this to modify themselves - just set base values directly.
    /// 
    /// Example usage:
    /// - Advanced Armor Component provides +2 AC to Chassis
    /// - Nitro Booster provides +10 Speed to Drive
    /// - Targeting Computer provides +2 AttackBonus to AllWeapons
    /// </summary>
    [Serializable]
    public class ComponentModifierData
    {
        [Tooltip("Which attribute to modify")]
        public Attribute attribute;
        
        [Tooltip("Type of modification (Flat or Multiplier)")]
        public ModifierType type = ModifierType.Flat;
        
        [Tooltip("Value of modification (e.g., +2, -5, or 1.5 for multiplier)")]
        public float value;
        
        [Header("Target Selection")]
        [Tooltip("Which component(s) receive this modifier")]
        public ComponentTargetMode targetMode = ComponentTargetMode.Chassis;
        
        [Tooltip("Specific target (only used when targetMode is SpecificComponent)")]
        public VehicleComponent specificTarget;
    }

    /// <summary>
    /// Specifies which component(s) a modifier targets.
    /// </summary>
    public enum ComponentTargetMode
    {
        [Tooltip("Targets the chassis component")]
        Chassis,
        
        [Tooltip("Targets the power core component")]
        PowerCore,
        
        [Tooltip("Targets the drive component")]
        Drive,
        
        [Tooltip("Targets all weapon components")]
        AllWeapons,
        
        [Tooltip("Targets all components on the vehicle")]
        AllComponents,
        
        [Tooltip("Targets a specific component (set specificTarget field)")]
        SpecificComponent
    }
}
