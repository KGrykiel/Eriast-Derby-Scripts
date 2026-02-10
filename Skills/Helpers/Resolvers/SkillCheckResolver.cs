using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat;
using UnityEngine;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use skill checks.
    /// 
    /// Flow: User rolls d20 + skill bonus vs DC
    /// - Success = effects apply
    /// - Failure = effects don't apply
    /// </summary>
    public static class SkillCheckResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            Skill skill = ctx.Skill;

            var checkResult = SkillCheckCalculator.PerformSkillCheck(
                ctx.SourceVehicle,
                skill.checkSpec,
                skill.checkDC,
                ctx.SourceCharacter);  // Pass initiating character

            if (checkResult == null)
            {
                Debug.LogWarning($"[SkillCheckResolver] {skill.name}: Check cannot be attempted");
                return false;
            }

            EmitCheckEvent(checkResult, ctx.SourceComponent, skill);
            
            if (!checkResult.Succeeded)
                return false;
            
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
                succeeded: checkResult.Succeeded,
                character: checkResult.Character);  // Pass character from result
        }
    }
}

