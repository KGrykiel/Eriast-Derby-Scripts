using UnityEngine;
using Combat;
using Combat.SkillChecks;

namespace Skills.Helpers
{
    /// <summary>
    /// Resolver for skills that use skill checks.
    /// 
    /// Flow: User rolls d20 + skill bonus vs DC
    /// - Success = effects apply
    /// - Failure = effects don't apply
    /// 
    /// Handles skill checks for vehicle maneuvering, perception, etc.
    /// </summary>
    public static class SkillCheckResolver
    {
        /// <summary>
        /// Execute a skill check skill.
        /// Returns true if effects were applied (check succeeded).
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            // Determine who makes the check (usually the user)
            Entity checkEntity = ResolveCheckEntity(user, sourceComponent);
            if (checkEntity == null)
            {
                Debug.LogWarning($"[SkillCheckResolver] {skill.name}: Could not resolve check entity!");
                return false;
            }
            
            // Perform the skill check
            int dc = skill.checkDC;
            SkillCheckResult checkResult = SkillCheckCalculator.PerformSkillCheck(checkEntity, skill.checkType, dc);

            // Emit event
            EmitCheckEvent(checkResult, user, sourceComponent, skill);
            
            // If check failed, don't apply effects
            if (!checkResult.Succeeded)
            {
                return false;
            }
            
            // Check succeeded - apply effects
            SkillEffectApplicator.ApplyAllEffects(skill, user, mainTarget, sourceComponent, targetComponent);
            return true;
        }
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Resolve which entity makes the skill check.
        /// Typically the source component or user chassis.
        /// </summary>
        private static Entity ResolveCheckEntity(Vehicle user, VehicleComponent sourceComponent)
        {
            if (sourceComponent != null)
            {
                return sourceComponent;
            }
            return user?.chassis;
        }
        
        /// <summary>
        /// Emit the appropriate combat event.
        /// </summary>
        private static void EmitCheckEvent(
            SkillCheckResult checkResult,
            Vehicle user,
            VehicleComponent sourceComponent,
            Skill skill)
        {
            Entity sourceEntity = sourceComponent != null ? sourceComponent : user.chassis;
            
            CombatEventBus.EmitSkillCheck(
                checkResult,
                sourceEntity,
                skill,
                succeeded: checkResult.Succeeded);
        }
    }
}

