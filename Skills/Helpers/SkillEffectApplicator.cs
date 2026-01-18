using System.Collections.Generic;
using UnityEngine;
using StatusEffects;
using Combat;
using Effects;

namespace Skills.Helpers
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
            VehicleComponent targetComponentOverride = null,
            bool isCriticalHit = false)
        {
            // Begin action scope - all events will be aggregated
            CombatEventBus.BeginAction(sourceComponent ?? user.chassis, skill, mainTarget);
            
            // Build effect context for situational/combat state
            var context = new EffectContext
            {
                isCriticalHit = isCriticalHit
            };
            
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
                        invocation.effect,
                        skill);
                    
                    foreach (var targetEntity in targetEntities)
                    {
                        // Apply effect - pass EffectContext for situational data (crits, etc.)
                        // Note: Weapon is NOT in context - effects extract it from user parameter
                        invocation.effect.Apply(
                            sourceComponent ?? user.chassis,
                            targetEntity,
                            context,
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
        /// Uses Vehicle.RouteEffectTarget() with skill's precision mode to route effects.
        /// </summary>
        private static List<Entity> ResolveTargets(
            EffectTarget target,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponentOverride,
            IEffect effect,
            Skill skill)
        {
            var targets = new List<Entity>();
            TargetPrecision precision = skill?.targetPrecision ?? TargetPrecision.Auto;
            
            switch (target)
            {
                case EffectTarget.SourceComponent:
                    if (sourceComponent != null)
                        targets.Add(sourceComponent);
                    break;
                    
                case EffectTarget.SourceVehicle:
                    // Route based on precision and effect type
                    if (user.chassis != null)
                        targets.Add(user.RouteEffectTarget(effect, precision, null));
                    break;
                    
                case EffectTarget.SourceComponentSelection:
                    // Player-selected component on source vehicle (for self-targeting skills)
                    // When user == mainTarget, targetComponentOverride is the selected source component
                    if (user.chassis != null)
                    {
                        if (user == mainTarget && targetComponentOverride != null)
                        {
                            // Self-targeting: targetComponentOverride is the selected source component
                            targets.Add(targetComponentOverride);
                        }
                        else
                        {
                            // Fallback to routing if not self-targeting
                            targets.Add(user.RouteEffectTarget(effect, precision, null));
                        }
                    }
                    break;
                    
                case EffectTarget.SelectedTarget:
                    // Respects player-selected component for Precise targeting
                    if (mainTarget.chassis != null)
                        targets.Add(mainTarget.RouteEffectTarget(effect, precision, targetComponentOverride));
                    break;
                    
                case EffectTarget.TargetVehicle:
                    // Always routes based on precision mode (ignores player component selection for VehicleOnly)
                    if (mainTarget.chassis != null)
                        targets.Add(mainTarget.RouteEffectTarget(effect, precision, null));
                    break;
                    
                case EffectTarget.Both:
                    // Both source and target, each routed based on precision and effect type
                    if (user.chassis != null)
                        targets.Add(user.RouteEffectTarget(effect, precision, null));
                    if (mainTarget.chassis != null)
                        targets.Add(mainTarget.RouteEffectTarget(effect, precision, targetComponentOverride));
                    break;
                    
                case EffectTarget.AllEnemiesInStage:
                    // All enemy vehicles in stage, each routed based on precision and effect type
                    if (user.currentStage != null && user.currentStage.vehiclesInStage != null)
                    {
                        foreach (var vehicle in user.currentStage.vehiclesInStage)
                        {
                            if (vehicle != user && vehicle.Status == VehicleStatus.Active && vehicle.chassis != null)
                            {
                                targets.Add(vehicle.RouteEffectTarget(effect, precision, null));
                            }
                        }
                    }
                    break;
                    
                case EffectTarget.AllAlliesInStage:
                    // All allied vehicles in stage (including self), each routed based on precision and effect type
                    if (user.currentStage != null && user.currentStage.vehiclesInStage != null)
                    {
                        foreach (var vehicle in user.currentStage.vehiclesInStage)
                        {
                            if (vehicle.Status == VehicleStatus.Active && vehicle.chassis != null)
                            {
                                // TODO: Add faction/team check when implemented
                                targets.Add(vehicle.RouteEffectTarget(effect, precision, null));
                            }
                        }
                    }
                    break;
            }
            
            return targets;
        }
    }
}
