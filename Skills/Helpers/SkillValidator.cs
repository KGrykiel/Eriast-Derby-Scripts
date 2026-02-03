using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Validates skill configuration and target accessibility.
    /// Returns false and logs validation failures using Unity's Debug system.
    /// 
    /// These are VALIDATION failures (developer/edge case feedback), not game events.
    /// Game events are logged by SkillExecutor when skills actually execute.
    /// </summary>
    public static class SkillValidator
    {
        /// <summary>
        /// Validates that the skill has effects configured.
        /// </summary>
        public static bool ValidateSkillConfiguration(SkillContext ctx)
        {
            Vehicle user = ctx.SourceVehicle;
            Skill skill = ctx.Skill;
            
            if (skill.effectInvocations == null || skill.effectInvocations.Count == 0)
            {
                Debug.LogError($"[SkillValidator] {user.vehicleName} attempted to use {skill.name} but it has no effects configured. Fix the skill asset!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validates target is valid and accessible.
        /// Skips accessibility checks when self-targeting (user can always access own components).
        /// Handles both VehicleComponent targets and other Entity types.
        /// </summary>
        public static bool ValidateTarget(SkillContext ctx)
        {
            Vehicle user = ctx.SourceVehicle;
            Skill skill = ctx.Skill;
            Entity targetEntity = ctx.TargetEntity;
            
            // If target is not a VehicleComponent, skip vehicle-specific checks
            VehicleComponent targetComponent = ctx.TargetComponent;
            if (targetComponent == null)
            {
                // Non-component entity (Prop, NPC, etc.) - just check destroyed
                if (targetEntity.isDestroyed)
                {
                    Debug.LogWarning($"[SkillValidator] {user.vehicleName} tried to target {targetEntity.name} with {skill.name}, but it's destroyed");
                    return false;
                }
                return true;
            }
            
            // VehicleComponent target - full validation
            Vehicle mainTarget = ctx.TargetVehicle;
            string attackerName = user.vehicleName;
            string targetComponentName = targetComponent.name;

            // Check if component is destroyed
            if (targetComponent.isDestroyed)
            {
                Debug.LogWarning($"[SkillValidator] {attackerName} tried to target {mainTarget.vehicleName}'s {targetComponentName} with {skill.name}, but it's destroyed");
                return false;
            }

            // Self-targeting: Skip accessibility checks (can always access own components)
            bool isSelfTargeting = user == mainTarget;
            if (!isSelfTargeting && !mainTarget.IsComponentAccessible(targetComponent))
            {
                string reason = mainTarget.GetInaccessibilityReason(targetComponent);
                Debug.LogWarning($"[SkillValidator] {attackerName} cannot target {mainTarget.vehicleName}'s {targetComponentName} with {skill.name}: {reason}");
                return false;
            }
            
            return true;
        }
    }
}
