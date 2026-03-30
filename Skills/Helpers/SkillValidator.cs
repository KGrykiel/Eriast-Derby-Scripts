using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
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
                Vehicle sourceVehicle = GetSourceVehicle(ctx);
                string vehicleName = sourceVehicle != null ? sourceVehicle.vehicleName : "<unknown>";
                Debug.LogError($"[SkillValidator] {vehicleName} attempted to use {skill.name} but it has no roll node configured. Fix the skill asset!");
                return false;
            }

            return true;
        }

        private static bool ValidateTarget(RollContext ctx, Skill skill)
        {
            Entity targetEntity = ctx.Target as Entity;
            if (targetEntity == null) return true;

            Vehicle sourceVehicle = GetSourceVehicle(ctx);
            string sourceName = sourceVehicle != null ? sourceVehicle.vehicleName : "<unknown>";

            if (targetEntity.IsDestroyed())
            {
                Debug.LogWarning($"[SkillValidator] {sourceName} tried to target destroyed entity {targetEntity.name} with {skill.name}");
                return false;
            }

            VehicleComponent targetComponent = targetEntity as VehicleComponent;
            if (targetComponent != null)
            {
                Vehicle targetVehicle = targetComponent.ParentVehicle;
                bool isSelfTargeting = sourceVehicle == targetVehicle;
                if (!isSelfTargeting && !targetVehicle.IsComponentAccessible(targetComponent))
                {
                    string reason = targetVehicle.GetInaccessibilityReason(targetComponent);
                    Debug.LogWarning($"[SkillValidator] {sourceName} cannot target inaccessible component {targetComponent.name}: {reason}");
                    return false;
                }
            }

            return true;
        }

        private static Vehicle GetSourceVehicle(RollContext ctx)
        {
            if (ctx.SourceActor != null)
            {
                Vehicle vehicle = ctx.SourceActor.GetVehicle();
                if (vehicle != null) return vehicle;
            }
            return ctx.Target as Vehicle;
        }
    }
}
