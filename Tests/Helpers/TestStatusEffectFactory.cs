using UnityEngine;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Damage;

namespace Assets.Scripts.Tests.Helpers
{
    /// <summary>
    /// Factory for creating StatusEffect templates in tests.
    /// Handles cleanup tracking automatically.
    /// </summary>
    public static class TestStatusEffectFactory
    {
        /// <summary>
        /// Create a basic StatusEffect with a single stat modifier.
        /// </summary>
        /// <param name="name">Effect name (e.g., "Blessed", "Haste")</param>
        /// <param name="attribute">Attribute to modify</param>
        /// <param name="value">Modifier value (e.g., +2, -3)</param>
        /// <param name="duration">Duration in turns (-1 = indefinite, 0 = instant, >0 = turns)</param>
        /// <param name="cleanup">Cleanup list to track for disposal</param>
        public static StatusEffect CreateModifierEffect(
            string name,
            Attribute attribute,
            float value,
            int duration = -1,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<StatusEffect>();
            template.effectName = name;
            template.baseDuration = duration;
            template.modifiers = new System.Collections.Generic.List<ModifierData>
            {
                new ModifierData { attribute = attribute, type = ModifierType.Flat, value = value }
            };
            template.periodicEffects = new System.Collections.Generic.List<PeriodicEffectData>();
            template.behavioralEffects = new BehavioralEffectData();

            cleanup?.Add(template);
            return template;
        }

        /// <summary>
        /// Create a StatusEffect with damage-over-time (DoT).
        /// </summary>
        /// <param name="name">Effect name (e.g., "Burning", "Poison")</param>
        /// <param name="damage">Damage per turn</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="duration">Duration in turns</param>
        /// <param name="cleanup">Cleanup list to track for disposal</param>
        public static StatusEffect CreateDoTEffect(
            string name,
            int damage,
            DamageType damageType,
            int duration,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<StatusEffect>();
            template.effectName = name;
            template.baseDuration = duration;
            template.modifiers = new System.Collections.Generic.List<ModifierData>();
            template.periodicEffects = new System.Collections.Generic.List<PeriodicEffectData>
            {
                new PeriodicEffectData
                {
                    type = PeriodicEffectType.Damage,
                    diceCount = 0,
                    bonus = damage,
                    damageType = damageType
                }
            };
            template.behavioralEffects = new BehavioralEffectData();

            cleanup?.Add(template);
            return template;
        }

        /// <summary>
        /// Create a StatusEffect with behavioral effects (stun, immobilize).
        /// </summary>
        /// <param name="name">Effect name (e.g., "Stunned", "Immobilized")</param>
        /// <param name="preventsActions">If true, prevents all actions</param>
        /// <param name="preventsMovement">If true, prevents movement</param>
        /// <param name="duration">Duration in turns</param>
        /// <param name="cleanup">Cleanup list to track for disposal</param>
        public static StatusEffect CreateBehavioralEffect(
            string name,
            bool preventsActions = false,
            bool preventsMovement = false,
            int duration = 2,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<StatusEffect>();
            template.effectName = name;
            template.baseDuration = duration;
            template.modifiers = new System.Collections.Generic.List<ModifierData>();
            template.periodicEffects = new System.Collections.Generic.List<PeriodicEffectData>();
            template.behavioralEffects = new BehavioralEffectData
            {
                preventsActions = preventsActions,
                preventsMovement = preventsMovement
            };

            cleanup?.Add(template);
            return template;
        }
    }
}
