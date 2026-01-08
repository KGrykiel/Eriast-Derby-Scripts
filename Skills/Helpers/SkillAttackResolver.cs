using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Handles attack rolls, modifier building, and two-stage component targeting.
    /// </summary>
    public static class SkillAttackResolver
    {
        /// <summary>
        /// Performs a skill roll (attack roll or saving throw) if required. Returns null if no roll is needed.
        /// </summary>
        public static RollBreakdown PerformSkillRoll(Skill skill, Vehicle user, Entity targetEntity, VehicleComponent sourceComponent)
        {
            if (!skill.requiresAttackRoll)
                return null;
            
            var modifiers = BuildSkillRollModifiers(skill, user, sourceComponent);
            return RollUtility.RollToHitWithBreakdown(user, targetEntity, skill.rollType, modifiers, skill.name);
        }
        
        /// <summary>
        /// Build the list of modifiers for a skill roll with proper source tracking.
        /// Works for attack rolls, saving throws, etc.
        /// </summary>
        public static List<RollModifier> BuildSkillRollModifiers(Skill skill, Vehicle user, VehicleComponent sourceComponent)
        {
            var builder = RollUtility.BuildModifiers();
            
            // Add caster/vehicle bonus (future: from character stats)
            int casterBonus = GetCasterToHitBonus(user, skill.rollType);
            builder.AddIf(casterBonus != 0, "Caster Bonus", casterBonus, user.vehicleName);
            
            // Add source component bonus (weapon attack bonus, power core bonus, etc.)
            if (sourceComponent != null)
            {
                // Weapons have attackBonus
                if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
                {
                    builder.Add("Weapon Attack Bonus", weapon.attackBonus, weapon.name);
                }
                // Future: Other components could add their own bonuses here
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Utility: Get caster's to-hit bonus based on roll type.
        /// </summary>
        private static int GetCasterToHitBonus(Vehicle caster, RollType rollType)
        {
            if (rollType == RollType.None)
                return 0;

            // Future: Pull from vehicle/character attributes
            return 0;
        }
        
        /// <summary>
        /// Build modifiers for component targeting (no penalty).
        /// </summary>
        public static List<RollModifier> BuildComponentModifiers(VehicleComponent sourceComponent)
        {
            var builder = RollUtility.BuildModifiers();
            
            if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
            {
                builder.Add("Weapon Attack Bonus", weapon.attackBonus, weapon.name);
            }
            
            return builder.Build();
        }
        
        /// <summary>
        /// Build modifiers for chassis fallback (with penalty).
        /// </summary>
        public static List<RollModifier> BuildChassisModifiers(VehicleComponent sourceComponent, int componentTargetingPenalty, string skillName)
        {
            var builder = RollUtility.BuildModifiers();
            
            if (sourceComponent is WeaponComponent weapon && weapon.attackBonus != 0)
            {
                builder.Add("Weapon Attack Bonus", weapon.attackBonus, weapon.name);
            }
            
            builder.Add("Component Targeting Penalty", -componentTargetingPenalty, skillName);
            
            return builder.Build();
        }
        
        /// <summary>
        /// Attempts two-stage component attack: Component AC (no penalty) → Chassis AC (with penalty).
        /// </summary>
        public static bool AttemptTwoStageComponentAttack(
            Skill skill,
            Vehicle user, 
            Vehicle mainTarget, 
            VehicleComponent targetComponent,
            string targetComponentName,
            VehicleComponent sourceComponent,
            Dictionary<Entity, List<DamageBreakdown>> damageByTarget)
        {
            // Build modifiers
            var componentModifiers = BuildComponentModifiers(sourceComponent);
            var chassisModifiers = BuildChassisModifiers(sourceComponent, skill.componentTargetingPenalty, skill.name);

            // Stage 1: Roll vs Component AC (NO PENALTY)
            int componentAC = mainTarget.GetComponentAC(targetComponent);
            var componentRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
            foreach (var mod in componentModifiers) componentRoll.WithModifier(mod.name, mod.value, mod.source);
            componentRoll.Against(componentAC, "Component AC");

            if (componentRoll.success == true)
            {
                SkillCombatLogger.LogComponentHit(skill.name, user, mainTarget, targetComponentName, sourceComponent, componentRoll, damageByTarget);
                return true;
            }

            // Stage 1 Miss - log it
            SkillCombatLogger.LogComponentMiss(skill.name, user, mainTarget, targetComponentName, sourceComponent, componentRoll);

            // Stage 2: Try chassis (WITH PENALTY)
            int chassisAC = mainTarget.GetArmorClass();
            var chassisRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
            foreach (var mod in chassisModifiers) chassisRoll.WithModifier(mod.name, mod.value, mod.source);
            chassisRoll.Against(chassisAC, "Chassis AC");

            if (chassisRoll.success == true)
            {
                SkillCombatLogger.LogChassisHit(skill.name, user, mainTarget, targetComponentName, sourceComponent, chassisRoll, damageByTarget);
                return true;
            }

            // Stage 2 Miss - log it
            SkillCombatLogger.LogChassisMiss(skill.name, user, mainTarget, sourceComponent, chassisRoll);
            return false;
        }
        
        /// <summary>
        /// DEPRECATED: Old method name - use PerformSkillRoll instead.
        /// </summary>
        [System.Obsolete("Use PerformSkillRoll - more generic than 'AttackRoll'")]
        public static RollBreakdown PerformAttackRoll(Skill skill, Vehicle user, Entity targetEntity, WeaponComponent weapon)
        {
            return PerformSkillRoll(skill, user, targetEntity, weapon);
        }
        
        /// <summary>
        /// DEPRECATED: Old method name - use BuildSkillRollModifiers instead.
        /// </summary>
        [System.Obsolete("Use BuildSkillRollModifiers - works for all skill types")]
        public static List<RollModifier> BuildAttackModifiers(Skill skill, Vehicle user, WeaponComponent weapon)
        {
            return BuildSkillRollModifiers(skill, user, weapon);
        }
    }
}
