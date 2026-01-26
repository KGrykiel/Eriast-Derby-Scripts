using System.Collections.Generic;
using Assets.Scripts.Combat;
using Assets.Scripts.Effects;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Applies effects to targets with action scoping for aggregated logging.
    /// Handles target resolution and effect routing with component-aware targeting.
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
    /// Vehicles are derived from context when needed for routing/filtering.
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
        public static void ApplyAllEffects(SkillContext ctx)
        {
            Vehicle targetVehicle = ctx.TargetVehicle;
            Skill skill = ctx.Skill;
            
            // Begin action scope - all events will be aggregated
            CombatEventBus.BeginAction(ctx.SourceEntity, skill, targetVehicle);
            
            // Build effect context from SkillContext (translation point)
            var effectContext = EffectContext.FromSkillContext(ctx);
            
            try
            {
                foreach (var invocation in skill.effectInvocations)
                {
                    if (invocation.effect == null) continue;
                    
                    // Resolve ALL targets for this effect (can be multiple for AOE/Both)
                    List<Entity> targetEntities = ResolveTargets(ctx, invocation.target, invocation.effect);
                    
                    foreach (var targetEntity in targetEntities)
                    {
                        // Apply effect with combat state
                        invocation.effect.Apply(
                            ctx.SourceEntity,
                            targetEntity,
                            effectContext,
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
        /// EffectTarget determines whether to respect player selection or auto-route.
        /// </summary>
        private static List<Entity> ResolveTargets(
            SkillContext ctx,
            EffectTarget target,
            IEffect effect)
        {
            var targets = new List<Entity>();
            
            Vehicle sourceVehicle = ctx.SourceVehicle;
            Vehicle targetVehicle = ctx.TargetVehicle;
            VehicleComponent sourceComponent = ctx.SourceComponent;
            VehicleComponent targetComponent = ctx.TargetComponent;
            
            switch (target)
            {
                case EffectTarget.SourceComponent:
                    if (sourceComponent != null)
                        targets.Add(sourceComponent);
                    else if (sourceVehicle != null)
                        targets.Add(sourceVehicle.chassis); // Fallback for character skills
                    break;
                    
                case EffectTarget.SourceVehicle:
                    // Always auto-route (no player selection for source)
                    if (sourceVehicle != null)
                        targets.Add(sourceVehicle.RouteEffectTarget(effect));
                    break;
                    
                case EffectTarget.SourceComponentSelection:
                    // Player-selected component on source vehicle (for self-targeting skills)
                    if (sourceVehicle == targetVehicle && targetComponent != null)
                    {
                        // Self-targeting: targetComponent is the selected source component
                        targets.Add(targetComponent);
                    }
                    else if (sourceVehicle != null)
                    {
                        // Fallback to auto-routing
                        targets.Add(sourceVehicle.RouteEffectTarget(effect));
                    }
                    break;
                    
                case EffectTarget.SelectedTarget:
                    // Respects player-selected component (if any)
                    if (targetVehicle == null)
                    {
                        targets.Add(ctx.TargetEntity);
                    }
                    else
                    {
                        targets.Add(targetVehicle.RouteEffectTarget(effect, targetComponent));
                    }
                    break;
                    
                case EffectTarget.TargetVehicle:
                    // Always auto-routes (ignores player component selection)
                    if (targetVehicle == null)
                    {
                        targets.Add(ctx.TargetEntity);
                    }
                    else
                    {
                        targets.Add(targetVehicle.RouteEffectTarget(effect));
                    }
                    break;
                    
                case EffectTarget.Both:
                    // Both source and target
                    if (sourceVehicle != null)
                        targets.Add(sourceVehicle.RouteEffectTarget(effect));
                    if (targetVehicle != null)
                        targets.Add(targetVehicle.RouteEffectTarget(effect, targetComponent));
                    else
                        targets.Add(ctx.TargetEntity);
                    break;
                    
                case EffectTarget.AllEnemiesInStage:
                    // All enemy vehicles in stage (always auto-route)
                    if (sourceVehicle?.currentStage?.vehiclesInStage != null)
                    {
                        foreach (var vehicle in sourceVehicle.currentStage.vehiclesInStage)
                        {
                            if (vehicle != sourceVehicle && vehicle.Status == VehicleStatus.Active)
                            {
                                targets.Add(vehicle.RouteEffectTarget(effect));
                            }
                        }
                    }
                    break;
                    
                case EffectTarget.AllAlliesInStage:
                    // All allied vehicles in stage (always auto-route)
                    if (sourceVehicle?.currentStage?.vehiclesInStage != null)
                    {
                        foreach (var vehicle in sourceVehicle.currentStage.vehiclesInStage)
                        {
                            if (vehicle.Status == VehicleStatus.Active)
                            {
                                // TODO: Add faction/team check when implemented
                                targets.Add(vehicle.RouteEffectTarget(effect));
                            }
                        }
                    }
                    break;
            }
            
            return targets;
        }
    }
}
