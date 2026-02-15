using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    public static class SkillValidator
    {
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
        
        public static bool ValidateTarget(SkillContext ctx)
        {
            Vehicle user = ctx.SourceVehicle;
            Skill skill = ctx.Skill;
            Entity targetEntity = ctx.TargetEntity;

            VehicleComponent targetComponent = ctx.TargetComponent;
            if (targetComponent == null)
            {
                if (targetEntity.isDestroyed)
                {
                    Debug.LogWarning($"[SkillValidator] {user.vehicleName} tried to target {targetEntity.name} with {skill.name}, but it's destroyed");
                    return false;
                }
                return true;
            }
            
            Vehicle mainTarget = ctx.TargetVehicle;
            string attackerName = user.vehicleName;
            string targetComponentName = targetComponent.name;

            if (targetComponent.isDestroyed)
            {
                Debug.LogWarning($"[SkillValidator] {attackerName} tried to target {mainTarget.vehicleName}'s {targetComponentName} with {skill.name}, but it's destroyed");
                return false;
            }

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
