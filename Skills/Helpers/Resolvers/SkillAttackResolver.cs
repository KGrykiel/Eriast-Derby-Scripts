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
    /// Builds an AttackSpec from skill context, delegates to AttackPerformer.
    /// Component fallback is handled automatically by AttackPerformer (not skill-specific).
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
            // Build attack spec from skill context
            var spec = new AttackSpec
            {
                Target = ctx.TargetEntity,
                CausalSource = ctx.Skill,
                Attacker = ctx.SourceComponent,
                Character = ctx.SourceCharacter
            };

            // Execute attack (performer handles roll + optional fallback + events)
            var result = AttackPerformer.Execute(spec);

            if (result.HitTarget == null)
                return false;

            // Retarget effects if we hit fallback instead of primary
            var effectCtx = result.WasFallback
                ? ctx.WithTarget(result.HitTarget)
                : ctx;

            SkillEffectApplicator.ApplyAllEffects(effectCtx.WithCriticalHit(result.Roll.IsCriticalHit));
            return true;
        }
    }
}
