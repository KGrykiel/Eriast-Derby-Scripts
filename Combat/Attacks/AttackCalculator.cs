using System.Collections.Generic;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Rules engine for attack rolls.
    /// Gathers bonuses according to game rules, rolls via D20Calculator, wraps in result.
    /// 
    /// UNIVERSAL: No attack flow knowledge (single vs two-stage).
    /// AttackPerformer handles flow orchestration and event emission.
    /// </summary>
    public static class AttackCalculator
    {
        /// <summary>
        /// Compute an attack roll against a target.
        /// Gathers bonuses from attacker/character, rolls vs target AC, returns result.
        /// </summary>
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

        /// <summary>
        /// Gather all bonuses for an attack roll based on game rules.
        /// Weapon provides: attack bonus + applied modifiers.
        /// Character provides: base attack bonus.
        /// </summary>
        public static List<RollBonus> GatherBonuses(
            Entity attacker = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();

            if (attacker is WeaponComponent weapon)
            {
                int attackBonus = weapon.GetAttackBonus();
                if (attackBonus != 0)
                {
                    bonuses.Add(new RollBonus(weapon.name ?? "Weapon", attackBonus));
                }
            }

            if (character != null)
            {
                int charBonus = character.baseAttackBonus;
                if (charBonus != 0)
                {
                    bonuses.Add(new RollBonus(character.characterName, charBonus));
                }
            }

            if (attacker != null)
            {
                bonuses.AddRange(D20RollHelpers.GatherAppliedBonuses(attacker, Attribute.AttackBonus));
            }

            return bonuses;
        }
    }
}
