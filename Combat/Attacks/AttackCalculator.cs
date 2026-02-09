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
            VehicleComponent sourceComponent = null,
            Skill skill = null,
            PlayerCharacter character = null,
            int additionalPenalty = 0)
        {
            int baseRoll = RollUtility.RollD20();
            var bonuses = GatherBonuses(attacker, sourceComponent, skill, character);
            
            if (additionalPenalty != 0)
            {
                bonuses.Add(new RollBonus("Targeting Penalty", -additionalPenalty));
            }
            
            int defenseValue = target.GetArmorClass();
            int total = baseRoll + SumBonuses(bonuses);
            
            // Crit/fumble detection
            bool isCrit = baseRoll == 20;
            bool isFumble = baseRoll == 1;
            bool success = isCrit || (!isFumble && total >= defenseValue);
            
            return new AttackResult(baseRoll, bonuses, defenseValue, success, isCrit, isFumble);
        }
        
        /// <summary>
        /// Gather all bonuses for an attack roll as RollBonus entries.
        /// </summary>
        public static List<RollBonus> GatherBonuses(
            Entity attacker,
            VehicleComponent sourceComponent = null,
            Skill skill = null,
            PlayerCharacter character = null)
        {
            var bonuses = new List<RollBonus>();
            
            // Weapon enhancement bonus
            if (sourceComponent is WeaponComponent weapon)
            {
                int attackBonus = weapon.GetAttackBonus();
                if (attackBonus != 0)
                {
                    bonuses.Add(new RollBonus(weapon.name ?? "Weapon", attackBonus));
                }
            }
            
            // Character attack bonus (explicit or derived from seat)
            PlayerCharacter resolvedCharacter = character;
            if (resolvedCharacter == null && sourceComponent != null && sourceComponent.ParentVehicle != null)
            {
                var seat = sourceComponent.ParentVehicle.GetSeatForComponent(sourceComponent);
                resolvedCharacter = seat?.assignedCharacter;
            }
            if (resolvedCharacter != null)
            {
                int charBonus = resolvedCharacter.baseAttackBonus;
                if (charBonus != 0)
                {
                    bonuses.Add(new RollBonus(resolvedCharacter.characterName, charBonus));
                }
            }
            
            // Applied: status effects and equipment on attacker
            if (attacker != null)
            {
                bonuses.AddRange(D20RollHelpers.GatherAppliedBonuses(attacker, Attribute.AttackBonus));
            }
            
            return bonuses;
        }
        
        private static int SumBonuses(List<RollBonus> bonuses)
        {
            int sum = 0;
            foreach (var b in bonuses) sum += b.Value;
            return sum;
        }
    }
}
