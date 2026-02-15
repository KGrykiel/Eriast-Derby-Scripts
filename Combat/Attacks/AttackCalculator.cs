using System.Collections.Generic;
using Assets.Scripts.Characters;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Calculates the total bonuses to attack power and to AC.
    /// rolls with D20Calculator and return a structured result struct.
    /// </summary>
    public static class AttackCalculator
    {
        public static AttackResult Compute(
            Entity target,
            Entity attacker = null,
            Character character = null,
            int additionalPenalty = 0)
        {
            var bonuses = GatherBonuses(attacker, character);

            if (additionalPenalty != 0)
            {
                bonuses.Add(new RollBonus("Targeting Penalty", -additionalPenalty));
            }

            int defenseValue = target.GetArmorClass();
            var roll = D20Calculator.Roll(bonuses, defenseValue);

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
                D20RollHelpers.GatherWeaponBonuses(weapon, bonuses);
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
