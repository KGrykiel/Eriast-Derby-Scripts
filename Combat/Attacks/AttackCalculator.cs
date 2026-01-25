using System.Collections.Generic;
using Assets.Scripts.Core;

namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Calculator for attack rolls (d20 + modifiers vs AC).
    /// 
    /// Responsibilities:
    /// - Rolling d20 attacks
    /// - Gathering attack modifiers from all sources
    /// - Evaluating hit/miss against AC
    /// - Crit/fumble detection
    /// 
    /// DESIGN: Entities store raw base values. This calculator gathers modifiers
    /// from all sources and computes final values. Provides breakdown data for tooltips.
    /// 
    /// NOTE: Defense values (AC) delegated to StatCalculator (single source of truth).
    /// </summary>
    public static class AttackCalculator
    {
        // ==================== ATTACK ROLLING ====================
        
        /// <summary>
        /// Perform a complete attack roll with modifiers and evaluation.
        /// This is the primary method for making attacks.
        /// Handles critical hits (natural 20) and critical misses (natural 1).
        /// </summary>
        /// <param name="attacker">Entity making the attack (for status effect modifiers)</param>
        /// <param name="target">Entity being attacked (for AC calculation)</param>
        /// <param name="sourceComponent">Component used for attack (weapon bonus)</param>
        /// <param name="skill">Skill being used (for skill-specific modifiers)</param>
        /// <param name="character">Character providing attack bonus (explicit, or derived from seat if null)</param>
        /// <param name="additionalPenalty">Extra penalty (e.g., component targeting)</param>
        public static AttackResult PerformAttack(
            Entity attacker,
            Entity target,
            VehicleComponent sourceComponent = null,
            Skill skill = null,
            PlayerCharacter character = null,
            int additionalPenalty = 0)
        {
            // Roll the d20
            var result = RollAttack();
            
            // Gather and add attack modifiers
            var modifiers = GatherAttackModifiers(attacker, sourceComponent, skill, character);
            D20RollHelpers.AddModifiers(result, modifiers);
            
            // Add any additional penalty (e.g., component targeting)
            if (additionalPenalty != 0)
            {
                AddPenalty(result, additionalPenalty, "Targeting Penalty");
            }
            
            // Get target's defense value
            int defenseValue = StatCalculator.GatherDefenseValue(target);
            result.targetValue = defenseValue;
            
            // Evaluate: Critical hit (natural 20) auto-hits, critical miss (natural 1) auto-misses
            if (IsNatural20(result))
            {
                result.success = true;
                result.isCriticalHit = true;
            }
            else if (IsNatural1(result))
            {
                result.success = false;
                result.isCriticalMiss = true;
            }
            else
            {
                // Normal evaluation
                D20RollHelpers.EvaluateAgainstTarget(result, defenseValue);
            }
            
            return result;
        }
        
        /// <summary>
        /// Roll a d20 attack and create an attack result.
        /// </summary>
        public static AttackResult RollAttack()
        {
            int roll = RollUtility.RollD20();
            return AttackResult.FromD20(roll);
        }
        
        // ==================== ATTACK MODIFIER GATHERING ====================
        
        /// <summary>
        /// Gather attack modifiers from all sources.
        /// 
        /// Sources:
        /// - Intrinsic (weapon, character): Raw values converted to modifiers here
        /// - Applied (buffs, debuffs): Pre-existing modifiers on entity
        /// - Skill-specific: Skill grants bonus/penalty (future)
        /// </summary>
        /// <param name="attacker">Entity making attack (for status effect modifiers)</param>
        /// <param name="sourceComponent">Component used (weapon bonus)</param>
        /// <param name="skill">Skill being used</param>
        /// <param name="character">Character providing bonus (explicit, or derived from seat if null)</param>
        public static List<AttributeModifier> GatherAttackModifiers(
            Entity attacker,
            VehicleComponent sourceComponent = null,
            Skill skill = null,
            PlayerCharacter character = null)
        {
            var modifiers = new List<AttributeModifier>();
            
            // 1. Intrinsic: Weapon enhancement bonus
            GatherWeaponBonus(sourceComponent, modifiers);
            
            // 2. Intrinsic: Character attack bonus (from explicit param or seat lookup)
            GatherCharacterBonus(sourceComponent, character, modifiers);
            
            // 3. Applied: Status effects and equipment (shared helper)
            if (attacker != null)
            {
                D20RollHelpers.GatherAppliedModifiers(attacker, Attribute.AttackBonus, modifiers);
            }
            
            // 4. Skill-specific modifiers (future)
            if (skill != null)
            {
                GatherSkillModifiers(skill, modifiers);
            }
            
            return modifiers;
        }
        
        // ==================== MODIFIER SOURCES ====================
        
        /// <summary>
        /// Intrinsic: Weapon's inherent accuracy bonus.
        /// </summary>
        private static void GatherWeaponBonus(VehicleComponent sourceComponent, List<AttributeModifier> modifiers)
        {
            if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
            {
                modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    weapon.attackBonus,
                    weapon));
            }
        }
        
        /// <summary>
        /// Intrinsic: Character's base attack bonus.
        /// Uses explicit character if provided, otherwise queries through VehicleSeat system.
        /// For character personal skills, only character bonus applies (no component lookup).
        /// </summary>
        /// <param name="sourceComponent">Component for seat lookup (can be null for personal skills)</param>
        /// <param name="explicitCharacter">Character passed explicitly (takes priority)</param>
        /// <param name="modifiers">Modifier list to add to</param>
        private static void GatherCharacterBonus(VehicleComponent sourceComponent, PlayerCharacter explicitCharacter, List<AttributeModifier> modifiers)
        {
            // Use explicit character if provided, otherwise look up from seat
            PlayerCharacter character = explicitCharacter;
            
            if (character == null && sourceComponent?.ParentVehicle != null)
            {
                // Fallback: Get character from seat that controls this component
                var seat = sourceComponent.ParentVehicle.GetSeatForComponent(sourceComponent);
                character = seat?.assignedCharacter;
            }
            
            if (character != null)
            {
                int charBonus = character.baseAttackBonus;
                if (charBonus != 0)
                {
                    modifiers.Add(new AttributeModifier(
                        Attribute.AttackBonus,
                        ModifierType.Flat,
                        charBonus,
                        character));
                }
            }
        }
        
        /// <summary>
        /// Skill-specific: Skill grants attack bonus/penalty (e.g., "Power Attack").
        /// </summary>
        private static void GatherSkillModifiers(Skill skill, List<AttributeModifier> modifiers)
        {
            // Future: Skill-specific attack bonuses
        }
        
        // ==================== RESULT HELPERS ====================
        
        /// <summary>
        /// Check if an attack roll is within the critical threat range.
        /// Currently only natural 20, but expandable to 19-20, 18-20, etc.
        /// </summary>
        public static bool IsCriticalThreat(AttackResult result, int criticalRange = 20)
        {
            return result.baseRoll >= criticalRange;
        }
        
        private static void AddPenalty(AttackResult result, int penalty, string reason)
        {
            if (penalty != 0)
            {
                result.modifiers.Add(new AttributeModifier(
                    Attribute.AttackBonus,
                    ModifierType.Flat,
                    -penalty,
                    null));
            }
        }
        
        // ==================== CRIT/FUMBLE ====================
        
        /// <summary>Check if this is a natural 20 (critical hit potential).</summary>
        public static bool IsNatural20(AttackResult result) => D20RollHelpers.IsNatural20(result);
        
        /// <summary>Check if this is a natural 1 (automatic miss).</summary>
        public static bool IsNatural1(AttackResult result) => D20RollHelpers.IsNatural1(result);
    }
}
