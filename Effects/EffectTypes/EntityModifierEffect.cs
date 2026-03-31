using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Modifiers;
using SerializeReferenceEditor;
using System;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Permanent invisible stat modifier — no duration, no status icon.
    /// For temporary buffs/debuffs, use ApplyStatusEffect.
    /// Per ModifierSystem.md: skills should generally use StatusEffects, not this.
    /// Probably deprecated in favor of the status effect system but I'm keeping it just in case.
    /// </summary>
    [Serializable]
    [SRName("Modifier")]
    public class EntityModifierEffect : EffectBase
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

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            Entity entity = ResolveEntity(target);
            if (entity == null) return;

            entity.AddModifier(ToRuntimeModifier());
        }

        protected override Entity ResolveEntity(IEffectTarget target)
        {
            switch (target)
            {
                case Entity e:
                    return e;
                case Vehicle vehicle:
                    return VehicleComponentResolver.ResolveForAttribute(vehicle, attribute) ?? vehicle.chassis;
                case VehicleSeat:
                    Debug.LogWarning($"[{GetType().Name}] VehicleSeat is not a valid target for this effect.");
                    return null;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {(target != null ? target.GetType().Name : "null")}");
                    return null;
            }
        }
    }
}
