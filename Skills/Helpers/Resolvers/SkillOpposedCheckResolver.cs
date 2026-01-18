using UnityEngine;

namespace Skills.Helpers.Resolvers
{
    /// <summary>
    /// Resolver for skills that use opposed checks.
    /// 
    /// Flow: User rolls skill check vs target's skill check
    /// - User wins = effects apply
    /// - Target wins = effects don't apply
    /// 
    /// TODO: Design opposed check system.
    /// Considerations:
    /// - Which skills oppose which? (e.g., user Mobility vs target Mobility)
    /// - Ties? (tie goes to defender, or re-roll?)
    /// - Critical success/failure on opposed checks?
    /// </summary>
    public static class SkillOpposedCheckResolver
    {
        /// <summary>
        /// Execute an opposed check skill.
        /// Currently not implemented - returns false and logs warning.
        /// </summary>
        public static bool Execute(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            Debug.LogWarning($"[SkillOpposedCheckResolver] {skill.name}: Opposed checks not yet implemented!");
            return false;
        }
    }
}
