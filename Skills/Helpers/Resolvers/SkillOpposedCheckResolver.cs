using UnityEngine;

namespace Assets.Scripts.Skills.Helpers.Resolvers
{
    /// <summary>Handler for opposed checks between two entities, not yet implemented.</summary>
    public static class SkillOpposedCheckResolver
    {
        public static bool Execute(SkillContext ctx)
        {
            Debug.LogWarning($"[SkillOpposedCheckResolver] {ctx.Skill.name}: Opposed checks not yet implemented!");
            return false;
        }
    }
}
