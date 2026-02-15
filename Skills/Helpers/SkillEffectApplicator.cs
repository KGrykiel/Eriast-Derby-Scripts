using System.Collections.Generic;
using Assets.Scripts.Combat;
using Assets.Scripts.Effects;

namespace Assets.Scripts.Skills.Helpers
{
    public static class SkillEffectApplicator
    {
        /// <summary>
        /// Applies all effects of a skill based on success.
        /// uses the combat event bus to aggregate all damage in one event and ensure proper ordering of events and effects.
        /// </summary>
        public static void ApplyAllEffects(SkillContext ctx)
        {
            Vehicle targetVehicle = ctx.TargetVehicle;
            Skill skill = ctx.Skill;

            CombatEventBus.BeginAction(ctx.SourceEntity, skill, targetVehicle, ctx.SourceVehicle, ctx.SourceCharacter);
            var effectContext = EffectContext.FromSkillContext(ctx);

            try
            {
                foreach (var invocation in skill.effectInvocations)
                {
                    if (invocation.effect == null) continue;

                    List<Entity> targetEntities = ResolveTargets(ctx, invocation.target, invocation.effect);

                    foreach (var targetEntity in targetEntities)
                    {
                        invocation.effect.Apply(
                            targetEntity,
                            effectContext,
                            skill);
                    }
                }
            }
            finally
            {
                CombatEventBus.EndAction();
            }
        }

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
                    targets.Add(ctx.SourceComponent);
                    break;

                case EffectTarget.SourceVehicle:
                    targets.Add(ctx.SourceVehicle.RouteEffectTarget(effect));
                    break;

                case EffectTarget.SourceComponentSelection:
                case EffectTarget.SelectedTarget:
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
