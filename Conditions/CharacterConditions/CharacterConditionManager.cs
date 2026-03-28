using System.Linq;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Entities.Vehicle;
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
            => new AppliedCharacterCondition(template, applier);

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
                        skillData.skill, skillData.type, skillData.value, applied.template.effectName),
                    CharacterAttributeModifierData attrData => new CharacterAttributeModifier(
                        attrData.attribute, attrData.type, attrData.value, applied.template.effectName),
                    _ => null
                };

                if (modifier != null)
                {
                    modifier.Source = applied;
                    seat.AddCharacterModifier(modifier);
                }
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
            CombatEventBus.EmitCharacterCondition(applied, sourceEntity, seat, applied.applier?.name);
        }

        protected override void OnExpired(AppliedCharacterCondition applied)
            => CombatEventBus.EmitCharacterConditionExpired(applied, seat);

        protected override void OnRefreshed(AppliedCharacterCondition applied)
            => CombatEventBus.EmitCharacterConditionRefreshed(applied, seat);

        protected override void OnIgnored(AppliedCharacterCondition applied)
            => CombatEventBus.EmitCharacterConditionIgnored(applied, seat);

        protected override void OnStackLimitReached(CharacterCondition template)
            => CombatEventBus.EmitCharacterConditionStackLimit(template, seat, template.maxStacks);

        protected override void OnReplaced(AppliedCharacterCondition newApplied, int oldDuration)
            => CombatEventBus.EmitCharacterConditionReplaced(newApplied, seat, oldDuration);

        protected override void OnKeptStronger(AppliedCharacterCondition applied)
            => CombatEventBus.EmitCharacterConditionKeptStronger(applied, seat);

        protected override void OnRemovedByTrigger(AppliedCharacterCondition applied, RemovalTrigger trigger)
            => CombatEventBus.EmitCharacterConditionRemovedByTrigger(applied, seat, trigger);
    }
}
