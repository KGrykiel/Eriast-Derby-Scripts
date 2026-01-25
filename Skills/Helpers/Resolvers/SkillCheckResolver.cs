using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use skill checks.
    /// 
    /// Flow: User rolls d20 + skill bonus vs DC
    /// - Success = effects apply
    /// - Failure = effects don't apply
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
    /// </summary>
    public static class SkillCheckResolver
    {
        /// <summary>
        /// Execute a skill check skill.
        /// Returns true if effects were applied (check succeeded).
        /// </summary>
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;
            
            // Use source component or source vehicle's chassis for the check
            Entity checkingEntity = ctx.SourceEntity != null ? ctx.SourceEntity : ctx.SourceVehicle?.chassis;
            
            // Perform the skill check
            int dc = skill.checkDC;
            SkillCheckResult checkResult = SkillCheckCalculator.PerformSkillCheck(checkingEntity, skill.checkType, dc);

            // Emit event
            EmitCheckEvent(checkResult, ctx.SourceComponent, skill);
            
            // If check failed, don't apply effects
            if (!checkResult.Succeeded)
            {
                return false;
            }
            
            // Check succeeded - apply effects
            SkillEffectApplicator.ApplyAllEffects(ctx);
            return true;
        }
        
        // ==================== INTERNAL METHODS ====================
        
        /// <summary>
        /// Emit the appropriate combat event.
        /// </summary>
        private static void EmitCheckEvent(
            SkillCheckResult checkResult,
            VehicleComponent sourceComponent,
            Skill skill)
        {
            CombatEventBus.EmitSkillCheck(
                checkResult,
                sourceComponent,
                skill,
                succeeded: checkResult.Succeeded);
        }
    }
}

