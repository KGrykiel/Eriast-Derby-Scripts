using System.Linq;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Modifiers;
using UnityEngine;

namespace Assets.Scripts.Conditions.CharacterConditions
{
    /// <summary>
    /// Per-seat manager for character conditions. Simple stacking and expiry.
    /// Activates modifier objects on the seat for eager roll gathering.
    /// No periodic effects — characters have no HP/Energy resources.
    /// </summary>
    public class CharacterConditionManager : ConditionManagerBase<CharacterCondition, AppliedCharacterCondition>
    {
        private readonly VehicleSeat seat;

        public CharacterConditionManager(VehicleSeat seat)
        {
            this.seat = seat;
        }

        protected override bool CanApply(CharacterCondition template) => seat.IsAssigned;

        protected override AppliedCharacterCondition CreateApplied(CharacterCondition template, Object applier)
            => new(template, applier);

        protected override float GetTemplateMagnitude(CharacterCondition template)
            => template.modifiers.Sum(m => Mathf.Abs(m.value));

        protected override string GetOwnerDisplayName() => seat.seatName;

        protected override void OnActivate(AppliedCharacterCondition applied)
        {
            foreach (var modData in applied.template.modifiers)
            {
                CharacterModifier modifier = modData switch
                {
                    CharacterSkillModifierData skillData => new CharacterSkillModifier(
                        skillData.skill, skillData.type, skillData.value) { Source = applied },
                    CharacterAttributeModifierData attrData => new CharacterAttributeModifier(
                        attrData.attribute, attrData.type, attrData.value) { Source = applied },
                    _ => null
                };

                if (modifier != null)
                    seat.AddCharacterModifier(modifier);
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
                seat.AddAdvantageGrant(runtimeGrant);
            }
        }

        protected override void OnDeactivate(AppliedCharacterCondition applied)
        {
            seat.RemoveCharacterModifiersFromSource(applied);
            seat.RemoveAdvantageGrantsFromSource(applied);
        }

        // ==================== EVENT HOOKS ====================

        protected override void OnNewlyApplied(AppliedCharacterCondition applied, bool wasReplacement)
        {
            Entity sourceEntity = applied.applier as Entity;
            string applierName = applied.applier != null ? applied.applier.name : null;
            CombatEventBus.Emit(new CharacterConditionEvent(applied, sourceEntity, seat, applierName));
        }

        protected override void OnExpired(AppliedCharacterCondition applied)
            => CombatEventBus.Emit(new CharacterConditionExpiredEvent(applied, seat));

        protected override void OnRefreshed(AppliedCharacterCondition applied)
            => CombatEventBus.Emit(new CharacterConditionRefreshedEvent(applied, seat));

        protected override void OnIgnored(AppliedCharacterCondition applied)
            => CombatEventBus.Emit(new CharacterConditionIgnoredEvent(applied, seat));

        protected override void OnStackLimitReached(CharacterCondition template, int maxStacks)
            => CombatEventBus.Emit(new CharacterConditionStackLimitEvent(template, seat, maxStacks));

        protected override void OnReplaced(AppliedCharacterCondition newApplied, int oldDuration)
            => CombatEventBus.Emit(new CharacterConditionReplacedEvent(newApplied, seat, oldDuration));

        protected override void OnKeptStronger(AppliedCharacterCondition applied)
            => CombatEventBus.Emit(new CharacterConditionKeptStrongerEvent(applied, seat));

        protected override void OnRemovedByTrigger(AppliedCharacterCondition applied, RemovalTrigger trigger)
            => CombatEventBus.Emit(new CharacterConditionRemovedByTriggerEvent(applied, seat, trigger));
    }
}
