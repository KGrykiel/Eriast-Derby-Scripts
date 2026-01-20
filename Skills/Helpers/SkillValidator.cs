using System;
using System.Collections.Generic;
using System.Text;
using EventType = Assets.Scripts.Logging.EventType;
using UnityEngine;
using Assets.Scripts.Logging;

namespace Skills.Helpers
{
    /// <summary>
    /// Validates skill targets and component accessibility.
    /// Returns false and logs validation failures.
    /// </summary>
    public static class SkillValidator
    {
        /// <summary>
        /// Validates that the skill has a valid target and effects configured.
        /// Returns false and logs if validation fails.
        /// </summary>
        public static bool ValidateTarget(Skill skill, Vehicle user, Vehicle mainTarget)
        {
            // Check for null target
            if (mainTarget == null)
            {
                EventImportance importance = user.controlType == ControlType.Player
                    ? EventImportance.Medium
                    : EventImportance.Low;

                RaceHistory.Log(
                    EventType.SkillUse,
                    importance,
                    $"{user.vehicleName} attempted to use {skill.name} but there was no valid target",
                    user.currentStage,
                    user
                ).WithMetadata("skillName", skill.name)
                    .WithMetadata("failed", true)
                    .WithMetadata("reason", "NoTarget");

                return false;
            }

            // Check for no effects configured
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
        /// Validates component target accessibility.
        /// Skips accessibility checks when self-targeting (user can always access own components).
        /// </summary>
        public static bool ValidateComponentTarget(Skill skill, Vehicle user, Vehicle mainTarget, VehicleComponent targetComponent, string targetComponentName)
        {
            string attackerName = user.vehicleName;

            if (targetComponent == null || targetComponent.isDestroyed)
            {
                RaceHistory.Log(
                    EventType.SkillUse,
                    EventImportance.Medium,
                    $"{attackerName} tried to target {mainTarget.vehicleName}'s {targetComponentName}, but it's unavailable",
                    user.currentStage,
                    user, mainTarget
                ).WithMetadata("skillName", skill.name)
                 .WithMetadata("targetComponent", targetComponentName)
                 .WithMetadata("failed", true)
                 .WithMetadata("reason", "ComponentUnavailable");
                
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
