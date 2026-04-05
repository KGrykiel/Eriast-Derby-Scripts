using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Modifiers;
using SerializeReferenceEditor;
using System;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    /// <summary>
    /// Permanent invisible stat modifier — no duration, no status icon.
    /// Dual-interface: use in EntityEffectInvocation to apply to a specific component,
    /// or in VehicleEffectInvocation to let the vehicle route based on the modifier's attribute.
    /// </summary>
    [Serializable]
    [SRName("Modifier")]
    public class EntityModifierEffect : IEntityEffect, IVehicleEffect
    {
        [Header("Modifier Configuration")]
        public EntityAttribute attribute;
        public ModifierType type;
        public float value;

        [Tooltip("Label shown in logs and tooltips for this modifier")]
        public string label = "Effect Modifier";

        public EntityAttributeModifier ToRuntimeModifier()
        {
            return new EntityAttributeModifier(
                attribute,
                type,
                value,
                string.IsNullOrEmpty(label) ? "Effect Modifier" : label
            );
        }

        void IEntityEffect.Apply(Entity target, EffectContext context)
        {
            target.AddModifier(ToRuntimeModifier());
        }

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            target.AddModifier(ToRuntimeModifier());
        }
    }
}
