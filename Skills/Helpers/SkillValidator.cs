using Assets.Scripts.Combat.Rolls.RollSpecs;
using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Validates skill configuration and targeting rules.
    /// For player actions, UI already enforces targeting rules — this catches AI/test errors.
    /// </summary>
    public static class SkillValidator
    {
        public static bool Validate(RollContext ctx, Skill skill)
        {
            return ValidateConfiguration(ctx, skill) && ValidateTarget(ctx, skill);
        }

        private static bool ValidateConfiguration(RollContext ctx, Skill skill)
        {
            if (skill.rollNode == null)
            {
                Debug.LogError($"[SkillValidator] {ctx.SourceVehicle.vehicleName} attempted to use {skill.name} but it has no roll node configured. Fix the skill asset!");
                return false;
            }

            return true;
        }

        private static bool ValidateTarget(RollContext ctx, Skill skill)
        {
            Entity targetEntity = ctx.TargetEntity;
            if (targetEntity == null) return true;

            if (targetEntity.isDestroyed)
            {
                Debug.LogWarning($"[SkillValidator] {ctx.SourceVehicle.vehicleName} tried to target destroyed entity {targetEntity.name} with {skill.name}");
                return false;
            }

            VehicleComponent targetComponent = targetEntity as VehicleComponent;
            if (targetComponent != null)
            {
                Vehicle targetVehicle = targetComponent.ParentVehicle;
                bool isSelfTargeting = ctx.SourceVehicle == targetVehicle;
                if (!isSelfTargeting && !targetVehicle.IsComponentAccessible(targetComponent))
                {
                    string reason = targetVehicle.GetInaccessibilityReason(targetComponent);
                    Debug.LogWarning($"[SkillValidator] {ctx.SourceVehicle.vehicleName} cannot target inaccessible component {targetComponent.name}: {reason}");
                    return false;
                }
            }

            return true;
        }
    }
}
