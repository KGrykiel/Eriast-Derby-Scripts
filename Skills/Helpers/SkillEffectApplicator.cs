using System.Collections.Generic;
using Assets.Scripts.Combat;
using Assets.Scripts.Effects;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Applies effects to targets with action scoping for aggregated logging.
    /// 
    /// TARGET RESOLUTION:
    /// Most effect targets use the already-selected entity from SkillContext (ctx.TargetEntity).
    /// Routing only occurs for abstract vehicle targets (SourceVehicle, TargetVehicle) where
    /// the effect type determines which component receives it (damage→chassis, energy→powercore, etc).
    /// 
    /// ARCHITECTURE: Uses SkillContext for all execution data.
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
            CombatEventBus.BeginAction(ctx.SourceEntity, skill, targetVehicle, ctx.SourceVehicle, ctx.SourceCharacter);
            
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
        /// Returns list for potential future multi-target support.
        /// 
        /// SIMPLIFIED: Most cases just use the already-selected entity from context.
        /// Routing only happens when targeting "the vehicle" abstractly (TargetVehicle, SourceVehicle).
        /// </summary>
        private static List<Entity> ResolveTargets(
            SkillContext ctx,
            EffectTarget target,
            IEffect effect)
        {
            //Made as a list for future potential AOE or multi-target effects, even though currently most cases are single-target
            var targets = new List<Entity>();

            switch (target)
            {
                case EffectTarget.SourceComponent:
                    // The component that executed the skill
                    targets.Add(ctx.SourceComponent);
                    break;

                case EffectTarget.SourceVehicle:
                    // Route to appropriate component on source vehicle (abstract target)
                    targets.Add(ctx.SourceVehicle.RouteEffectTarget(effect));
                    break;

                case EffectTarget.SourceComponentSelection:
                case EffectTarget.SelectedTarget:
                    // Player already selected a specific entity - use it directly
                    targets.Add(ctx.TargetEntity);
                    break;

                case EffectTarget.TargetVehicle:
                    targets.Add(ctx.TargetVehicle.RouteEffectTarget(effect));
                    break;
            }

            return targets;
        }
    }
}
