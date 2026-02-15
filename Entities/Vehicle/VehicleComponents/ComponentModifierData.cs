using System;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicle.VehicleComponents
{
    /// <summary>
    /// Cross-component modifier data (e.g. Advanced Armor gives +2 AC to Chassis).
    /// Not to be confused with AttributeModifier that is used for game-time modifiers.
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
