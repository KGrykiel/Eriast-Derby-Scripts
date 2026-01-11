using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Attacks;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// High-level orchestrator for skill-based attacks.
    /// 
    /// Uses AttackCalculator for:
    /// - Rolling attacks
    /// - Gathering modifiers
    /// - Evaluating hits
    /// 
    /// Handles skill-specific logic:
    /// - Two-stage component targeting
    /// - Emitting combat events
    /// - Coordinating with SkillEffectApplicator
    /// </summary>
    public static class SkillAttackResolver
    {
        /// <summary>
        /// Performs a skill roll if required. Returns null if no roll is needed.
        /// Delegates to AttackCalculator for actual rolling and modifier gathering.
        /// </summary>
        public static AttackResult PerformSkillRoll(
            Skill skill,
            Vehicle user,
            Entity targetEntity,
            VehicleComponent sourceComponent)
        {
            if (skill.skillRollType == SkillRollType.None)
                return null;
            
            // Get the attacking entity (weapon or chassis)
            Entity attackerEntity = sourceComponent ?? user.chassis;
            
            // Perform the attack using AttackCalculator
            var result = AttackCalculator.PerformAttack(
                attacker: attackerEntity,
                target: targetEntity,
                sourceComponent: sourceComponent,
                skill: skill);
            
            return result;
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
            Entity attackerEntity = sourceComponent ?? user.chassis;
            
            // ==================== STAGE 1: Component AC (NO PENALTY) ====================
            
            var componentRoll = AttackCalculator.RollAttack(AttackCategory.Attack);
            
            // Gather modifiers (no penalty for direct component targeting)
            var componentModifiers = AttackCalculator.GatherAttackModifiers(attackerEntity, sourceComponent, skill);
            AttackCalculator.AddModifiers(componentRoll, componentModifiers);
            
            // Evaluate against component AC
            int componentAC = AttackCalculator.GatherDefenseValue(targetComponent);
            AttackCalculator.EvaluateAgainst(componentRoll, componentAC, "Component AC");

            if (componentRoll.success == true)
            {
                // Component hit - emit event and apply effects
                CombatEventBus.EmitAttackRoll(
                    componentRoll, 
                    attackerEntity, 
                    targetComponent, 
                    skill, 
                    isHit: true, 
                    targetComponentName);
                
                SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
                return true;
            }

            // Stage 1 Miss - emit event
            CombatEventBus.EmitAttackRoll(
                componentRoll, 
                attackerEntity, 
                targetComponent, 
                skill, 
                isHit: false, 
                targetComponentName);

            // ==================== STAGE 2: Chassis AC (WITH PENALTY) ====================
            
            var chassisRoll = AttackCalculator.RollAttack(AttackCategory.Attack);
            
            // Gather modifiers WITH component targeting penalty
            var chassisModifiers = AttackCalculator.GatherAttackModifiers(attackerEntity, sourceComponent, skill);
            AttackCalculator.AddModifiers(chassisRoll, chassisModifiers);
            AttackCalculator.AddModifier(chassisRoll, "Component Targeting Penalty", -skill.componentTargetingPenalty, skill.name);
            
            // Evaluate against chassis AC
            int chassisAC = AttackCalculator.GatherDefenseValue(mainTarget.chassis);
            AttackCalculator.EvaluateAgainst(chassisRoll, chassisAC, "Chassis AC");

            if (chassisRoll.success == true)
            {
                // Chassis hit - emit event and apply effects
                CombatEventBus.EmitAttackRoll(
                    chassisRoll, 
                    attackerEntity, 
                    mainTarget.chassis, 
                    skill, 
                    isHit: true, 
                    targetComponentName,
                    isChassisFallback: true);
                
                SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, null);
                return true;
            }

            // Stage 2 Miss - emit event
            CombatEventBus.EmitAttackRoll(
                chassisRoll, 
                attackerEntity, 
                mainTarget.chassis, 
                skill, 
                isHit: false, 
                targetComponentName,
                isChassisFallback: true);
            
            return false;
        }
    }
}
