using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Applies effects to targets and tracks damage breakdowns.
    /// Handles target resolution and effect routing with component-aware targeting.
    /// </summary>
    public static class SkillEffectApplicator
    {
        /// <summary>
        /// Applies all effects to their targets and tracks damage breakdowns.
        /// Returns dictionary of damage breakdowns by target entity.
        /// </summary>
        public static Dictionary<Entity, List<DamageBreakdown>> ApplyAllEffects(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponentOverride = null)
        {
            var damageByTarget = new Dictionary<Entity, List<DamageBreakdown>>();
            
            foreach (var invocation in skill.effectInvocations)
            {
                if (invocation.effect == null) continue;
                
                // Resolve ALL targets for this effect (can be multiple for AOE/Both)
                List<Entity> targetEntities = ResolveTargets(
                    invocation.target,
                    user,
                    mainTarget,
                    sourceComponent,
                    targetComponentOverride,
                    invocation.effect);  // Pass effect for routing
                
                foreach (var targetEntity in targetEntities)
                {
                    // DEBUG: Log which target we're applying to
                    string targetName = EntityHelpers.GetEntityDisplayName(targetEntity);
                    Debug.Log($"[Skill] Applying {invocation.effect.GetType().Name} to {targetName} (target: {invocation.target})");
                    
                    // Apply effect - source component, routed target, weapon for context, skill for metadata
                    invocation.effect.Apply(
                        sourceComponent ?? user.chassis,
                        targetEntity,
                        sourceComponent as WeaponComponent,
                        skill);
                    
                    // Track damage breakdowns for DamageEffect
                    if (invocation.effect is DamageEffect damageEffect && damageEffect.LastBreakdown != null)
                    {
                        if (!damageByTarget.ContainsKey(targetEntity))
                        {
                            damageByTarget[targetEntity] = new List<DamageBreakdown>();
                        }
                        damageByTarget[targetEntity].Add(damageEffect.LastBreakdown);
                        
                        // DEBUG: Log damage tracking
                        Debug.Log($"[Skill] Tracked {damageEffect.LastBreakdown.finalDamage} damage to {targetName}");
                    }
                }
            }
            
            return damageByTarget;
        }
        
        /// <summary>
        /// Resolves effect target(s) based on EffectTarget enum.
        /// Returns list because some targets (Both, AllEnemies) resolve to multiple entities.
        /// Uses Vehicle.RouteEffectTarget() to automatically route effects to correct components.
        /// </summary>
        private static List<Entity> ResolveTargets(
            EffectTarget target,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponentOverride,
            IEffect effect)
        {
            var targets = new List<Entity>();
            
            switch (target)
            {
                case EffectTarget.SourceComponent:
                    if (sourceComponent != null)
                        targets.Add(sourceComponent);
                    break;
                    
                case EffectTarget.SourceVehicle:
                    // Route based on effect type (damage→chassis, speed→drive, etc.)
                    if (user.chassis != null)
                        targets.Add(user.RouteEffectTarget(effect, user.chassis));
                    break;
                    
                case EffectTarget.SelectedTarget:
                    // Respects component targeting if specified, otherwise routes based on effect type
                    if (targetComponentOverride != null)
                        targets.Add(targetComponentOverride);
                    else if (mainTarget.chassis != null)
                        targets.Add(mainTarget.RouteEffectTarget(effect, mainTarget.chassis));
                    break;
                    
                case EffectTarget.TargetVehicle:
                    // Always routes based on effect type (ignores player component selection)
                    if (mainTarget.chassis != null)
                        targets.Add(mainTarget.RouteEffectTarget(effect, mainTarget.chassis));
                    break;
                    
                case EffectTarget.Both:
                    // Both source and target, each routed based on effect type
                    if (user.chassis != null)
                        targets.Add(user.RouteEffectTarget(effect, user.chassis));
                    if (mainTarget.chassis != null)
                        targets.Add(mainTarget.RouteEffectTarget(effect, mainTarget.chassis));
                    break;
                    
                case EffectTarget.AllEnemiesInStage:
                    // All enemy vehicles in stage, each routed based on effect type
                    if (user.currentStage != null && user.currentStage.vehiclesInStage != null)
                    {
                        foreach (var vehicle in user.currentStage.vehiclesInStage)
                        {
                            if (vehicle != user && vehicle.Status == VehicleStatus.Active && vehicle.chassis != null)
                            {
                                targets.Add(vehicle.RouteEffectTarget(effect, vehicle.chassis));
                            }
                        }
                    }
                    break;
                    
                case EffectTarget.AllAlliesInStage:
                    // All allied vehicles in stage (including self), each routed based on effect type
                    if (user.currentStage != null && user.currentStage.vehiclesInStage != null)
                    {
                        foreach (var vehicle in user.currentStage.vehiclesInStage)
                        {
                            if (vehicle.Status == VehicleStatus.Active && vehicle.chassis != null)
                            {
                                // TODO: Add faction/team check when implemented
                                targets.Add(vehicle.RouteEffectTarget(effect, vehicle.chassis));
                            }
                        }
                    }
                    break;
            }
            
            return targets;
        }
    }
}
