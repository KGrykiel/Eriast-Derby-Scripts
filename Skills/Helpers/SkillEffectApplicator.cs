using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Applies effects to targets with action scoping for aggregated logging.
    /// Handles target resolution and effect routing with component-aware targeting.
    /// 
    /// LOGGING: Effects emit events to CombatEventBus. This class manages the action scope
    /// so all events from a skill are aggregated and logged together.
    /// </summary>
    public static class SkillEffectApplicator
    {
        /// <summary>
        /// Applies all effects to their targets within a combat action scope.
        /// Events are collected and logged together when the scope ends.
        /// </summary>
        public static void ApplyAllEffects(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponentOverride = null)
        {
            // Begin action scope - all events will be aggregated
            CombatEventBus.BeginAction(sourceComponent ?? user.chassis, skill, mainTarget);
            
            try
            {
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
                        invocation.effect);
                    
                    foreach (var targetEntity in targetEntities)
                    {
                        // Apply effect - effects emit events to CombatEventBus
                        invocation.effect.Apply(
                            sourceComponent ?? user.chassis,
                            targetEntity,
                            sourceComponent as WeaponComponent,
                            skill);
                    }
                }
            }
            finally
            {
                // End action scope - triggers aggregated logging
                CombatEventBus.EndAction();
            }
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
