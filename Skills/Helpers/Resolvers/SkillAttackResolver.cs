using System.Linq;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Attacks;

namespace Assets.Scripts.Skills.Helpers.Resolvers
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
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
    /// </summary>
    public static class SkillAttackResolver
    {
        /// <summary>
        /// Execute an attack roll skill.
        /// Returns true if effects were applied (attack hit).
        /// </summary>
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;
            VehicleComponent targetComponent = ctx.TargetComponent;
            
            // For non-vehicle targets (Props, NPCs), use standard attack
            if (targetComponent == null)
            {
                return ExecuteStandardAttack(ctx);
            }
            
            // Derive chassis once (single derivation point)
            VehicleComponent targetChassis = ctx.TargetVehicle.chassis;
            
            // Special case: Precise component targeting with damage uses two-stage attack
            // (only if NOT targeting chassis - chassis is just standard attack)
            bool isChassisTarget = targetComponent == targetChassis;
            
            if (skill.targetingMode == TargetingMode.EnemyComponent && 
                !isChassisTarget && 
                HasDamageEffects(skill))
            {
                return ExecuteTwoStageAttack(ctx, targetChassis);
            }
            
            // Standard attack
            return ExecuteStandardAttack(ctx);
        }
        
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Standard attack - single roll against target's AC.
        /// </summary>
        private static bool ExecuteStandardAttack(SkillContext ctx)
        {
            VehicleComponent sourceComponent = ctx.SourceComponent;
            Entity targetEntity = ctx.TargetEntity;
            Skill skill = ctx.Skill;
            
            var attackRoll = AttackCalculator.PerformAttack(
                attacker: sourceComponent,
                target: targetEntity,
                skill: skill,
                character: ctx.SourceCharacter);
            
            // Emit event
            EmitAttackEvent(attackRoll, sourceComponent, targetEntity, ctx.TargetComponent, skill, isChassisFallback: false, ctx.SourceCharacter);
            
            if (attackRoll.Success != true)
            {
                return false;
            }
            
            // Hit - apply effects with crit state
            SkillEffectApplicator.ApplyAllEffects(ctx.WithCriticalHit(attackRoll.IsCriticalHit));
            return true;
        }
        
        /// <summary>
        /// Two-stage component attack: Component AC (no penalty) → Chassis AC (with penalty).
        /// Chassis is passed in to avoid redundant derivation.
        /// </summary>
        private static bool ExecuteTwoStageAttack(SkillContext ctx, VehicleComponent chassisFallback)
        {
            VehicleComponent sourceComponent = ctx.SourceComponent;
            VehicleComponent targetComponent = ctx.TargetComponent;
            Skill skill = ctx.Skill;
            Character character = ctx.SourceCharacter;
            
            // ==================== STAGE 1: Component AC (NO PENALTY) ====================
            
            var componentRoll = AttackCalculator.PerformAttack(
                attacker: sourceComponent,
                target: targetComponent,
                skill: skill,
                character: character,
                additionalPenalty: 0);

            if (componentRoll.Success == true)
            {
                // Component hit - emit event and apply effects to component
                EmitAttackEvent(componentRoll, sourceComponent, targetComponent, targetComponent, skill, isChassisFallback: false, character);
                SkillEffectApplicator.ApplyAllEffects(ctx.WithCriticalHit(componentRoll.IsCriticalHit));
                return true;
            }

            // Stage 1 Miss - emit event
            EmitAttackEvent(componentRoll, sourceComponent, targetComponent, targetComponent, skill, isChassisFallback: false, character);

            // ==================== STAGE 2: Chassis AC (WITH PENALTY) ====================
            
            var chassisRoll = AttackCalculator.PerformAttack(
                attacker: sourceComponent,
                target: chassisFallback,
                skill: skill,
                character: character,
                additionalPenalty: skill.componentTargetingPenalty);

            if (chassisRoll.Success == true)
            {
                // Chassis hit - retarget context to chassis
                EmitAttackEvent(chassisRoll, sourceComponent, chassisFallback, targetComponent, skill, isChassisFallback: true, character);
                SkillEffectApplicator.ApplyAllEffects(ctx.WithTarget(chassisFallback).WithCriticalHit(chassisRoll.IsCriticalHit));
                return true;
            }

            // Stage 2 Miss - emit event
            EmitAttackEvent(chassisRoll, sourceComponent, chassisFallback, targetComponent, skill, isChassisFallback: true, character);
            
            return false;
        }
        
        // ==================== HELPERS ====================
        
        /// <summary>
        /// Emit attack roll event.
        /// Character is passed directly from the caller (not stored on AttackResult)
        /// because the caller already has it — unlike saves/checks where it's resolved internally.
        /// </summary>
        private static void EmitAttackEvent(
            AttackResult attackRoll,
            Entity attackerEntity,
            Entity targetEntity,
            VehicleComponent originalTargetComponent,
            Skill skill,
            bool isChassisFallback,
            Character character)
        {
            string targetCompName = originalTargetComponent != null ? originalTargetComponent.name : null;
            
            CombatEventBus.EmitAttackRoll(
                attackRoll,
                attackerEntity,
                targetEntity,
                skill,
                isHit: attackRoll.Success == true,
                targetCompName,
                isChassisFallback,
                character: character);
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
