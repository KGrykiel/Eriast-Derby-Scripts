using System.Collections.Generic;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Calculator for attack rolls (d20 + bonuses vs AC).
    /// Gathers bonuses, rolls d20, builds complete result in one shot.
    /// Handles critical hits (natural 20) and critical misses (natural 1).
    /// </summary>
    public static class AttackCalculator
    {
        /// <summary>
        /// Perform a complete attack roll. Gathers all bonuses, rolls, evaluates, returns complete result.
        /// </summary>
        public static AttackResult PerformAttack(
            Entity attacker,
            Entity target,
            Skill skill = null,
            Character character = null,
            int additionalPenalty = 0)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(attacker, skill, character);
            
            if (additionalPenalty != 0)
            {
                bonuses.Add(new RollBonus("Targeting Penalty", -additionalPenalty));
            }
            
            int defenseValue = target.GetArmorClass();
            int total = baseRoll + D20RollHelpers.SumBonuses(bonuses);
            
            // Crit/fumble detection
            bool isCrit = baseRoll == 20;
            bool isFumble = baseRoll == 1;
            bool success = isCrit || (!isFumble && total >= defenseValue);
            
            return new AttackResult(baseRoll, bonuses, defenseValue, success, isCrit, isFumble);
        }
        
        /// <summary>
        /// Gather all bonuses for an attack roll as RollBonus entries.
        /// </summary>
        /// <param name="attacker">Entity making the attack (for weapon bonuses and applied modifiers)</param>
        /// <param name="skill">Skill being used (reserved for future use)</param>
        /// <param name="character">Character making the attack (for base attack bonus)</param>
        public static List<RollBonus> GatherBonuses(
            Entity attacker,
            Skill skill = null,
            Character character = null)
        {
            var bonuses = new List<RollBonus>();
            
            // Weapon enhancement bonus (if attacker is a weapon)
            if (attacker is WeaponComponent weapon)
            {
                int attackBonus = weapon.GetAttackBonus();
                if (attackBonus != 0)
                {
                    bonuses.Add(new RollBonus(weapon.name ?? "Weapon", attackBonus));
                }
            }
            
            // Character attack bonus (must be provided by caller)
            if (character != null)
            {
                int charBonus = character.baseAttackBonus;
                if (charBonus != 0)
                {
                    bonuses.Add(new RollBonus(character.characterName, charBonus));
                }
            }
            
            // Applied modifiers: status effects and equipment on attacker
            if (attacker != null)
            {
                bonuses.AddRange(D20RollHelpers.GatherAppliedBonuses(attacker, Attribute.AttackBonus));
            }
            
            return bonuses;
        }
    }
}
