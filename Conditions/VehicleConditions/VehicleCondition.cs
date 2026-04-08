using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Conditions.EntityConditions;
using SerializeReferenceEditor;

namespace Assets.Scripts.Conditions.VehicleConditions
{
    /// <summary>
    /// Vehicle-domain condition template. Lives on the vehicle as a whole, not on any individual component.
    /// Attribute modifiers are routed to the correct components on activation and removed as a unit on expiry.
    /// Periodic effects (DoTs, HoTs) apply to all components each tick.
    /// Use EntityCondition for component-specific effects.
    /// </summary>
    [CreateAssetMenu(menuName = "Racing/Vehicle Condition", fileName = "New Vehicle Condition")]
    public class VehicleCondition : ConditionBase
    {
        [Header("Stat Modifiers")]
        [Tooltip("Attribute modifiers routed to their owning components while this condition is active")]
        public List<EntityModifierData> modifiers = new();

        [Header("Periodic Effects")]
        [Tooltip("Effects applied to all components each turn (DoT hits all components — use EntityCondition for component-specific DoTs)")]
        [SerializeReference, SR]
        public List<IPeriodicEffect> periodicEffects = new();
    }
}
