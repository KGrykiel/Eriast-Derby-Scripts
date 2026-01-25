using UnityEngine;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use opposed checks.
    /// 
    /// Flow: User rolls skill check vs target's skill check
    /// - User wins = effects apply
    /// - Target wins = effects don't apply
    /// 
    /// TODO: Design opposed check system.
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
    /// </summary>
    public static class SkillOpposedCheckResolver
    {
        /// <summary>
        /// Execute an opposed check skill.
        /// Currently not implemented - returns false and logs warning.
        /// </summary>
        public static bool Execute(SkillContext ctx)
        {
            Debug.LogWarning($"[SkillOpposedCheckResolver] {ctx.Skill.name}: Opposed checks not yet implemented!");
            return false;
        }
    }
}
