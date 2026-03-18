using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.StatusEffects;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks
{
    /// <summary>
    /// Entry point for skill checks from all sources — skills, event cards, maybe more in the future.
    /// different methods for vehicles that need routing vs standalone entities that don't (objects, monsters etc).
    /// </summary>
    public static class SkillCheckPerformer
    {
        public static SkillCheckResult Execute(SkillCheckExecutionContext ctx)
        {
            // Step 1: Route (resolve who/what makes this check)
            var routing = CheckRouter.RouteSkillCheck(ctx.Vehicle, ctx.Spec, ctx.InitiatingCharacter);

            // Step 2: Compute
            SkillCheckResult result;
            if (!routing.CanAttempt)
            {
                result = SkillCheckCalculator.AutoFail(ctx.Spec);
            }
            else
            {
                result = SkillCheckCalculator.Compute(ctx.Spec, routing.Component, routing.Character);
            }

            // Step 3: Emit event automatically (WOTR-style)
            Entity sourceEntity = routing.Component != null ? routing.Component : ctx.Vehicle.chassis;
            CombatEventBus.EmitSkillCheck(
                result,
                sourceEntity,
                ctx.CausalSource,
                result.Roll.Success,
                result.Character);

            // Step 4: Notify d20 roll trigger on roller
            if (routing.CanAttempt && routing.Component != null)
            {
                routing.Component.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }

            return result;
        }

        /// <summary>Standalone entity overload — no vehicle routing.</summary>
        public static SkillCheckResult ExecuteForEntity(
            Entity entity,
            SkillCheckSpec spec,
            Object causalSource)
        {
            var result = SkillCheckCalculator.Compute(spec, entity);

            CombatEventBus.EmitSkillCheck(
                result,
                entity,
                causalSource,
                result.Roll.Success);

            entity?.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);

            return result;
        }
    }
}
