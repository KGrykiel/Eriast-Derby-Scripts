using System;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Modifiers;
using UnityEngine;

namespace Assets.Scripts.Entities.Vehicles.VehicleComponents
{
    /// <summary>
    /// Cross-component modifier data (e.g. Advanced Armor gives +2 AC to Chassis).
    /// Not to be confused with AttributeModifier that is used for game-time modifiers.
    /// </summary>
    [Serializable]
    public class ComponentModifierData
    {
        [Tooltip("Modifier to apply to the target component (attribute, type, value, and optional label)")]
        public EntityAttributeModifier modifier;

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
        SpecificComponent,

        [Tooltip("Targets only this component itself")]
        Self
    }

    /// <summary>
    /// Cross-component advantage grant data (e.g. Enhanced Sensors gives advantage on all vehicle checks).
    /// Mirrors ComponentModifierData but for advantage/disadvantage grants.
    /// </summary>
    [Serializable]
    public class ComponentAdvantageGrantData
    {
        [Tooltip("Advantage grant to apply to the target component's rolls")]
        public AdvantageGrant grant;

        [Header("Target Selection")]
        [Tooltip("Which component(s) receive this advantage grant on their rolls")]
        public ComponentTargetMode targetMode = ComponentTargetMode.AllComponents;

        [Tooltip("Specific target (only used when targetMode is SpecificComponent)")]
        public VehicleComponent specificTarget;
    }
}
