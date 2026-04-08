using UnityEngine;
using Assets.Scripts.Conditions;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Modifiers;
using Assets.Scripts.Entities;

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
        /// <param name="stackBehaviour">How this effect stacks when reapplied</param>
        /// <param name="maxStacks">Maximum stacks allowed (only for Stack behaviour)</param>
        /// <param name="cleanup">Cleanup list to track for disposal</param>
        public static EntityCondition CreateModifierEffect(
            string name,
            EntityAttribute attribute,
            float value,
            int duration = -1,
            StackBehaviour stackBehaviour = StackBehaviour.Refresh,
            int maxStacks = 0,
            ConditionCategory categories = ConditionCategory.None,
            RemovalTrigger removalTriggers = RemovalTrigger.None,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<EntityCondition>();
            template.effectName = name;
            template.baseDuration = duration;
            template.stackBehaviour = stackBehaviour;
            template.maxStacks = maxStacks;
            template.categories = categories;
            template.removalTriggers = removalTriggers;
            template.modifiers = new System.Collections.Generic.List<EntityModifierData>
            {
                new() { attribute = attribute, type = ModifierType.Flat, value = value }
            };
            template.periodicEffects = new System.Collections.Generic.List<IPeriodicEffect>();
            template.behavioralEffects = new BehavioralEffectData();

            cleanup?.Add(template);
            return template;
        }

        /// <summary>
        /// Create a VehicleCondition with a single stat modifier, for testing lane status effects.
        /// </summary>
        public static VehicleCondition CreateVehicleModifierEffect(
            string name,
            EntityAttribute attribute,
            float value,
            int duration = -1,
            StackBehaviour stackBehaviour = StackBehaviour.Refresh,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<VehicleCondition>();
            template.effectName = name;
            template.baseDuration = duration;
            template.stackBehaviour = stackBehaviour;
            template.modifiers = new System.Collections.Generic.List<EntityModifierData>
            {
                new() { attribute = attribute, type = ModifierType.Flat, value = value }
            };
            template.periodicEffects = new System.Collections.Generic.List<IPeriodicEffect>();
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
        /// <param name="stackBehaviour">How this effect stacks when reapplied</param>
        /// <param name="maxStacks">Maximum stacks allowed (only for Stack behaviour)</param>
        /// <param name="cleanup">Cleanup list to track for disposal</param>
        public static EntityCondition CreateDoTEffect(
            string name,
            int damage,
            DamageType damageType,
            int duration,
            StackBehaviour stackBehaviour = StackBehaviour.Refresh,
            int maxStacks = 0,
            ConditionCategory categories = ConditionCategory.None,
            RemovalTrigger removalTriggers = RemovalTrigger.None,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<EntityCondition>();
            template.effectName = name;
            template.baseDuration = duration;
            template.stackBehaviour = stackBehaviour;
            template.maxStacks = maxStacks;
            template.categories = categories;
            template.removalTriggers = removalTriggers;
            template.modifiers = new System.Collections.Generic.List<EntityModifierData>();
            template.periodicEffects = new System.Collections.Generic.List<IPeriodicEffect>
            {
                new PeriodicDamageEffect {
                    damageFormula = new DamageFormula
                    {
                        baseDice = 0,
                        dieSize = 0,
                        bonus = damage,
                        damageType = damageType
                    }
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
        /// <param name="stackBehaviour">How this effect stacks when reapplied</param>
        /// <param name="maxStacks">Maximum stacks allowed (only for Stack behaviour)</param>
        /// <param name="cleanup">Cleanup list to track for disposal</param>
        public static EntityCondition CreateBehavioralEffect(
            string name,
            bool preventsActions = false,
            bool preventsMovement = false,
            int duration = 2,
            StackBehaviour stackBehaviour = StackBehaviour.Refresh,
            int maxStacks = 0,
            ConditionCategory categories = ConditionCategory.None,
            RemovalTrigger removalTriggers = RemovalTrigger.None,
            System.Collections.Generic.List<Object> cleanup = null)
        {
            var template = ScriptableObject.CreateInstance<EntityCondition>();
            template.effectName = name;
            template.baseDuration = duration;
            template.stackBehaviour = stackBehaviour;
            template.maxStacks = maxStacks;
            template.categories = categories;
            template.removalTriggers = removalTriggers;
            template.modifiers = new System.Collections.Generic.List<EntityModifierData>();
            template.periodicEffects = new System.Collections.Generic.List<IPeriodicEffect>();
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
