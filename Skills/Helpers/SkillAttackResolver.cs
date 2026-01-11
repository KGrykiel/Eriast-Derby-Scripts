using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Handles attack rolls, modifier building, and two-stage component targeting.
    /// Emits AttackRollEvent for hit/miss logging via CombatEventBus.
    /// </summary>
    public static class SkillAttackResolver
    {
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
        /// Performs a skill roll (attack roll or saving throw) if required. Returns null if no roll is needed.
        /// </summary>
        public static RollBreakdown PerformSkillRoll(Skill skill, Vehicle user, Entity targetEntity, VehicleComponent sourceComponent)
        {
            if (skill.skillRollType != SkillRollType.AttackRoll)
                return null;
            
            // Infer target number type from SkillRollType
            TargetNumberType targetNumberType = skill.skillRollType switch
            {
                SkillRollType.AttackRoll => TargetNumberType.ArmorClass,
                SkillRollType.SavingThrow => TargetNumberType.DifficultyClass,
                SkillRollType.SkillCheck => TargetNumberType.DifficultyClass,
                _ => TargetNumberType.ArmorClass
            };
            
            var modifiers = BuildSkillRollModifiers(skill, user, sourceComponent);
            return RollUtility.RollToHitWithBreakdown(user, targetEntity, targetNumberType, modifiers, skill.name);
        }
        
        /// <summary>
        /// Build the list of modifiers for a skill roll with proper source tracking.
        /// Works for attack rolls, saving throws, etc.
        /// </summary>
        public static List<RollModifier> BuildSkillRollModifiers(Skill skill, Vehicle user, VehicleComponent sourceComponent)
        {
            var builder = RollUtility.BuildModifiers();
            
            // Infer target number type for caster bonus calculation
            TargetNumberType targetNumberType = skill.skillRollType switch
            {
                SkillRollType.AttackRoll => TargetNumberType.ArmorClass,
                _ => TargetNumberType.DifficultyClass
            };
            
            // Add caster/vehicle bonus (future: from character stats)
            int casterBonus = GetCasterToHitBonus(user, targetNumberType);
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
        /// Utility: Get caster's to-hit bonus based on target number type.
        /// </summary>
        private static int GetCasterToHitBonus(Vehicle caster, TargetNumberType targetNumberType)
        {
            // Future: Pull from vehicle/character attributes
            return 0;
        }
        
        /// <summary>
        /// Attempts two-stage component attack: Component AC (no penalty) → Chassis AC (with penalty).
        /// Effects are applied after successful hit via SkillEffectApplicator.
        /// </summary>
        public static bool AttemptTwoStageComponentAttack(
            Skill skill,
            Vehicle user, 
            Vehicle mainTarget, 
            VehicleComponent targetComponent,
            string targetComponentName,
            VehicleComponent sourceComponent)
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
                // Component hit - emit event and apply effects
                CombatEventBus.EmitAttackRoll(
                    componentRoll, 
                    sourceComponent ?? user.chassis, 
                    targetComponent, 
                    skill, 
                    isHit: true, 
                    targetComponentName);
                
                // Apply effects to component
                SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
                return true;
            }

            // Stage 1 Miss - emit event
            CombatEventBus.EmitAttackRoll(
                componentRoll, 
                sourceComponent ?? user.chassis, 
                targetComponent, 
                skill, 
                isHit: false, 
                targetComponentName);

            // Stage 2: Try chassis (WITH PENALTY)
            int chassisAC = mainTarget.armorClass;
            var chassisRoll = RollBreakdown.D20(Random.Range(1, 21), RollCategory.Attack);
            foreach (var mod in chassisModifiers) chassisRoll.WithModifier(mod.name, mod.value, mod.source);
            chassisRoll.Against(chassisAC, "Chassis AC");

            if (chassisRoll.success == true)
            {
                // Chassis hit - emit event and apply effects
                CombatEventBus.EmitAttackRoll(
                    chassisRoll, 
                    sourceComponent ?? user.chassis, 
                    mainTarget.chassis, 
                    skill, 
                    isHit: true, 
                    targetComponentName,
                    isChassisFallback: true);
                
                // Apply effects to chassis (null = use routing)
                SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, null);
                return true;
            }

            // Stage 2 Miss - emit event
            CombatEventBus.EmitAttackRoll(
                chassisRoll, 
                sourceComponent ?? user.chassis, 
                mainTarget.chassis, 
                skill, 
                isHit: false, 
                targetComponentName,
                isChassisFallback: true);
            
            return false;
        }
    }
}
