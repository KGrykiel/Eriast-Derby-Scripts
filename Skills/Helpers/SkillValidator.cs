using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Validates skill configuration and target accessibility.
    /// Returns false and logs validation failures.
    /// 
    /// Works with SkillContext - all data bundled together.
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
                EventImportance importance = user.controlType == ControlType.Player
                    ? EventImportance.Medium
                    : EventImportance.Low;

                RaceHistory.Log(
                    EventType.SkillUse,
                    importance,
                    $"{user.vehicleName} attempted to use {skill.name} but it has no effects configured",
                    user.currentStage,
                    user
                ).WithMetadata("skillName", skill.name)
                    .WithMetadata("failed", true)
                    .WithMetadata("reason", "NoEffects");

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
                    RaceHistory.Log(
                        EventType.SkillUse,
                        EventImportance.Medium,
                        $"{user.vehicleName} tried to target {targetEntity.name}, but it's destroyed",
                        user.currentStage,
                        user
                    ).WithMetadata("skillName", skill.name)
                     .WithMetadata("targetEntity", targetEntity.name)
                     .WithMetadata("failed", true)
                     .WithMetadata("reason", "EntityDestroyed");
                    
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
                RaceHistory.Log(
                    EventType.SkillUse,
                    EventImportance.Medium,
                    $"{attackerName} tried to target {mainTarget.vehicleName}'s {targetComponentName}, but it's destroyed",
                    user.currentStage,
                    user, mainTarget
                ).WithMetadata("skillName", skill.name)
                 .WithMetadata("targetComponent", targetComponentName)
                 .WithMetadata("failed", true)
                 .WithMetadata("reason", "ComponentDestroyed");
                
                return false;
            }

            // Self-targeting: Skip accessibility checks (can always access own components)
            bool isSelfTargeting = user == mainTarget;
            if (!isSelfTargeting && !mainTarget.IsComponentAccessible(targetComponent))
            {
                string reason = mainTarget.GetInaccessibilityReason(targetComponent);
                RaceHistory.Log(
                    EventType.SkillUse,
                    EventImportance.Medium,
                    $"{attackerName} cannot target {mainTarget.vehicleName}'s {targetComponentName}: {reason}",
                    user.currentStage,
                    user, mainTarget
                ).WithMetadata("skillName", skill.name)
                 .WithMetadata("targetComponent", targetComponentName)
                 .WithMetadata("failed", true)
                 .WithMetadata("reason", "ComponentInaccessible")
                 .WithMetadata("accessibilityReason", reason);
                 
                return false;
            }
            
            return true;
        }
    }
}
