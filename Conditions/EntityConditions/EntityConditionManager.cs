using System.Linq;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.Conditions.EntityConditions
{
    /// <summary>
    /// Per-entity manager for status effects. Owns modifier activation, periodic effects,
    /// and CombatEventBus integration.
    /// </summary>
    public class StatusEffectManager : ConditionManagerBase<EntityCondition, AppliedEntityCondition>
    {
        private readonly Entity entity;

        public StatusEffectManager(Entity entity)
        {
            this.entity = entity;
        }

        // ==================== ABSTRACT OVERRIDES ====================

        protected override bool CanApply(EntityCondition template)
        {
            if (template.requiredFeatures != EntityFeature.None && !entity.HasFeature(template.requiredFeatures))
                return false;

            if (template.excludedFeatures != EntityFeature.None && entity.HasAnyFeature(template.excludedFeatures))
                return false;

            return true;
        }

        protected override AppliedEntityCondition CreateApplied(EntityCondition template, Object applier)
            => new(template, applier);

        protected override float GetTemplateMagnitude(EntityCondition template)
            => template.modifiers.Sum(m => Mathf.Abs(m.value));

        protected override string GetOwnerDisplayName()
            => entity != null ? entity.name : "Unknown";

        // ==================== LIFECYCLE HOOKS ====================

        protected override void OnActivate(AppliedEntityCondition applied)
        {
            foreach (var modData in applied.template.modifiers)
            {
                var modifier = new AttributeModifier(
                    modData.attribute,
                    modData.type,
                    modData.value,
                    applied.template.effectName,
                    ModifierCategory.Condition);

                applied.createdModifiers.Add(modifier);
                entity.AddModifier(modifier);
            }
        }

        protected override void OnDeactivate(AppliedEntityCondition applied)
        {
            foreach (var modifier in applied.createdModifiers)
                entity.RemoveModifier(modifier);

            applied.createdModifiers.Clear();
        }

        protected override void OnTick(AppliedEntityCondition applied)
        {
            foreach (var periodic in applied.template.periodicEffects)
            {
                switch (periodic)
                {
                    case PeriodicDamageEffect dot:
                        var resistance = entity.GetResistance(dot.damageFormula.damageType);
                        var dmgResult = DamageCalculator.Compute(dot.damageFormula, resistance);
                        DamageApplicator.Apply(dmgResult, entity, causalSource: applied.template.effectName, sourceType: DamageSource.Effect);
                        break;

                    case PeriodicRestorationEffect hot:
                        int amount = RestorationCalculator.Roll(hot.formula);
                        RestorationApplicator.Apply(hot.formula, amount, entity);
                        break;
                }
            }
        }

        // ==================== EVENT HOOKS ====================

        protected override void OnNewlyApplied(AppliedEntityCondition applied, bool wasReplacement)
        {
            Entity sourceEntity = applied.applier as Entity;
            CombatEventBus.EmitEntityCondition(applied, sourceEntity, entity, applied.applier?.name, wasReplacement);
        }

        protected override void OnExpired(AppliedEntityCondition applied)
            => CombatEventBus.EmitEntityConditionExpired(applied, entity);

        protected override void OnRefreshed(AppliedEntityCondition applied)
            => CombatEventBus.EmitEntityConditionRefreshed(applied, entity);

        protected override void OnIgnored(AppliedEntityCondition applied)
            => CombatEventBus.EmitEntityConditionIgnored(applied, entity);

        protected override void OnStackLimitReached(EntityCondition template)
            => CombatEventBus.EmitEntityConditionStackLimit(template, entity, template.maxStacks);

        protected override void OnReplaced(AppliedEntityCondition newApplied, int oldDuration)
            => CombatEventBus.EmitEntityConditionReplaced(newApplied, entity, oldDuration);

        protected override void OnKeptStronger(AppliedEntityCondition applied)
            => CombatEventBus.EmitEntityConditionKeptStronger(applied, entity);

        protected override void OnRemovedByTrigger(AppliedEntityCondition applied, RemovalTrigger trigger)
            => CombatEventBus.EmitEntityConditionRemovedByTrigger(applied, entity, trigger);
    }
}
