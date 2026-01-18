using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Combat;
using Combat.Attacks;
using Core;
using Skills.Helpers;

namespace Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use attack rolls.
    /// 
    /// Flow: User rolls d20 + attack bonus vs target's AC
    /// - Hit = effects apply
    /// - Miss = effects don't apply
    /// 
    /// Handles:
    /// - Standard attacks (vehicle targeting)
    /// - Component attacks (single roll)
    /// - Two-stage component attacks (component AC → chassis AC with penalty)
    /// </summary>
    public static class SkillAttackResolver
    {
        /// <summary>
        /// Execute an attack roll skill.
        /// Returns true if effects were applied (attack hit).
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            // Special case: Precise component targeting with damage uses two-stage attack
            if (skill.targetPrecision == TargetPrecision.Precise && 
                targetComponent != null && 
                HasDamageEffects(skill))
            {
                return ExecuteTwoStageAttack(skill, user, mainTarget, sourceComponent, targetComponent);
            }
            
            // Standard attack (may target vehicle or component)
            return ExecuteStandardAttack(skill, user, mainTarget, sourceComponent, targetComponent);
        }
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Standard attack - single roll against target's AC.
        /// </summary>
        private static bool ExecuteStandardAttack(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            Entity targetEntity = ResolveTargetEntity(mainTarget, targetComponent);
            Entity attackerEntity = sourceComponent ?? user.chassis;
            
            var attackRoll = AttackCalculator.PerformAttack(
                attacker: attackerEntity,
                target: targetEntity,
                sourceComponent: sourceComponent,
                skill: skill);
            
            // Emit event
            EmitAttackEvent(attackRoll, attackerEntity, targetEntity, targetComponent, skill, isChassisFallback: false);
            
            if (attackRoll.success != true)
            {
                return false;
            }
            
            // Hit - apply effects (pass crit flag for damage doubling)
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent, attackRoll.isCriticalHit);
            return true;
        }
        
        /// <summary>
        /// Two-stage component attack: Component AC (no penalty) → Chassis AC (with penalty).
        /// </summary>
        private static bool ExecuteTwoStageAttack(
            Skill skill,
            Vehicle user, 
            Vehicle mainTarget, 
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            Entity attackerEntity = sourceComponent ?? user.chassis;
            string targetComponentName = targetComponent.name;
            
            // ==================== STAGE 1: Component AC (NO PENALTY) ====================
            
            var componentRoll = AttackCalculator.PerformAttack(
                attacker: attackerEntity,
                target: targetComponent,
                sourceComponent: sourceComponent,
                skill: skill,
                additionalPenalty: 0);

            if (componentRoll.success == true)
            {
                // Component hit - emit event and apply effects to component (pass crit flag)
                EmitAttackEvent(componentRoll, attackerEntity, targetComponent, targetComponent, skill, isChassisFallback: false);
                SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent, componentRoll.isCriticalHit);
                return true;
            }

            // Stage 1 Miss - emit event
            EmitAttackEvent(componentRoll, attackerEntity, targetComponent, targetComponent, skill, isChassisFallback: false);

            // ==================== STAGE 2: Chassis AC (WITH PENALTY) ====================
            
            var chassisRoll = AttackCalculator.PerformAttack(
                attacker: attackerEntity,
                target: mainTarget.chassis,
                sourceComponent: sourceComponent,
                skill: skill,
                additionalPenalty: skill.componentTargetingPenalty);

            if (chassisRoll.success == true)
            {
                // Chassis hit - emit event and apply effects to chassis (pass crit flag)
                EmitAttackEvent(chassisRoll, attackerEntity, mainTarget.chassis, targetComponent, skill, isChassisFallback: true);
                SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, null, chassisRoll.isCriticalHit);
                return true;
            }

            // Stage 2 Miss - emit event
            EmitAttackEvent(chassisRoll, attackerEntity, mainTarget.chassis, targetComponent, skill, isChassisFallback: true);
            
            return false;
        }
        
        // ==================== HELPERS ====================
        
        /// <summary>
        /// Resolve which entity the attack targets.
        /// </summary>
        private static Entity ResolveTargetEntity(Vehicle mainTarget, VehicleComponent targetComponent)
        {
            if (targetComponent != null)
            {
                return targetComponent;
            }
            return mainTarget?.chassis;
        }
        
        /// <summary>
        /// Emit attack roll event.
        /// </summary>
        private static void EmitAttackEvent(
            AttackResult attackRoll,
            Entity attackerEntity,
            Entity targetEntity,
            VehicleComponent targetComponent,
            Skill skill,
            bool isChassisFallback)
        {
            string targetCompName = targetComponent?.name;
            
            CombatEventBus.EmitAttackRoll(
                attackRoll,
                attackerEntity,
                targetEntity,
                skill,
                isHit: attackRoll.success == true,
                targetCompName,
                isChassisFallback);
        }
        
        /// <summary>
        /// Check if skill has damage effects (determines two-stage behavior).
        /// </summary>
        private static bool HasDamageEffects(Skill skill)
        {
            return skill.effectInvocations.Any(e => e.effect is DamageEffect);
        }
    }
}
