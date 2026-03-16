using System.Collections.Generic;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Attacks
{
    /// <summary>
    /// Calculates the total bonuses to attack power and to AC.
    /// rolls with D20Calculator and return a structured result struct.
    /// </summary>
    public static class AttackCalculator
    {
        public static AttackResult Compute(
            AttackSpec spec,
            Entity target,
            Entity attacker = null,
            Character character = null,
            bool isFallback = false)
        {
            var bonuses = GatherBonuses(attacker, character);

            if (isFallback && spec.componentTargetingPenalty != 0)
            {
                bonuses.Add(new RollBonus("Targeting Penalty", -spec.componentTargetingPenalty));
            }

            var grantedSource = spec.grantedMode != RollMode.Normal
                ? new AdvantageSource("Attack", spec.grantedMode)
                : default;
            var advantageSources = D20RollHelpers.GatherAdvantageSources(attacker, spec, grantedSource);
            int defenseValue = target.GetArmorClass();
            var roll = D20Calculator.Roll(bonuses, defenseValue, advantageSources);

            return new AttackResult(roll);
        }

        public static List<RollBonus> GatherBonuses(
            Entity attacker = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            // Weapon bonuses (base + applied modifiers)
            if (attacker is WeaponComponent weapon)
            {
                bonuses.AddRange(D20RollHelpers.GatherWeaponBonuses(weapon));
            }

            // Character bonus (via CharacterFormulas for consistency)
            if (character != null)
            {
                int charBonus = CharacterFormulas.CalculateAttackBonus(character);
                if (charBonus != 0)
                {
                    bonuses.Add(new RollBonus(character.characterName, charBonus));
                }
            }

            return bonuses;
        }
    }
}
