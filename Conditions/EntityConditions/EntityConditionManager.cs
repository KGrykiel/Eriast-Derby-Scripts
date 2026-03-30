using System.Linq;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Entities;
using Assets.Scripts.Modifiers;
using UnityEngine;

namespace Assets.Scripts.Conditions.EntityConditions
{
    /// <summary>
    /// Per-entity manager for status effects. Owns modifier activation, periodic effects,
    /// and CombatEventBus integration.
    /// </summary>
    public class EntityConditionManager : ConditionManagerBase<EntityCondition, AppliedEntityCondition>
    {
        private readonly Entity entity;

        public EntityConditionManager(Entity entity)
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
                var modifier = new EntityAttributeModifier(modData.attribute, modData.type, modData.value) { Source = applied };
                entity.AddModifier(modifier);
            }

            foreach (var grant in applied.template.advantageGrants)
            {
                var runtimeGrant = new AdvantageGrant
                {
                    label = !string.IsNullOrEmpty(grant.label) ? grant.label : applied.template.effectName,
                    type = grant.type,
                    targets = grant.targets,
                    Source = applied
                };
                entity.AddAdvantageGrant(runtimeGrant);
            }
        }

        protected override void OnDeactivate(AppliedEntityCondition applied)
        {
            entity.RemoveModifiersFromSource(applied);
            entity.RemoveAdvantageGrantsFromSource(applied);
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
                        DamageApplicator.Apply(dmgResult, entity, causalSource: applied.template.effectName);
                        break;

                    case PeriodicRestorationEffect hot:
                        int amount = RestorationCalculator.Roll(hot.formula);
                        RestorationApplicator.Apply(hot.formula, amount, entity, causalSource: applied.template.effectName);
                        break;
                }
            }
        }

        // ==================== EVENT HOOKS ====================

        protected override void OnNewlyApplied(AppliedEntityCondition applied, bool wasReplacement)
        {
            Entity sourceEntity = applied.applier as Entity;
            string applierName = applied.applier != null ? applied.applier.name : null;
            CombatEventBus.Emit(new EntityConditionEvent(applied, sourceEntity, entity, applierName));
        }

        protected override void OnExpired(AppliedEntityCondition applied)
            => CombatEventBus.Emit(new EntityConditionExpiredEvent(applied, entity));

        protected override void OnRefreshed(AppliedEntityCondition applied)
            => CombatEventBus.Emit(new EntityConditionRefreshedEvent(applied, entity));

        protected override void OnIgnored(AppliedEntityCondition applied)
            => CombatEventBus.Emit(new EntityConditionIgnoredEvent(applied, entity));

        protected override void OnStackLimitReached(EntityCondition template)
            => CombatEventBus.Emit(new EntityConditionStackLimitEvent(template, entity, template.maxStacks));

        protected override void OnReplaced(AppliedEntityCondition newApplied, int oldDuration)
            => CombatEventBus.Emit(new EntityConditionReplacedEvent(newApplied, entity, oldDuration));

        protected override void OnKeptStronger(AppliedEntityCondition applied)
            => CombatEventBus.Emit(new EntityConditionKeptStrongerEvent(applied, entity));

        protected override void OnRemovedByTrigger(AppliedEntityCondition applied, RemovalTrigger trigger)
            => CombatEventBus.Emit(new EntityConditionRemovedByTriggerEvent(applied, entity, trigger));
    }
}
