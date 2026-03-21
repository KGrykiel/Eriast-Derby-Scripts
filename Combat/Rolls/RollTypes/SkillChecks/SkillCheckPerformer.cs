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
        public static D20RollOutcome Execute(SkillCheckExecutionContext ctx)
        {
            // Step 1: Route (resolve who/what makes this check)
            var routing = CheckRouter.RouteSkillCheck(ctx.Vehicle, ctx.Spec, ctx.InitiatingCharacter);

            // Step 2: Gather bonuses and advantages
            D20RollOutcome roll;
            bool isAutoFail;
            if (!routing.CanAttempt)
            {
                roll = D20Calculator.AutoFail(ctx.Spec.dc);
                isAutoFail = true;
            }
            else
            {
                var gathered = RollGatherer.ForSkillCheck(ctx.Spec, routing.Component, routing.Character);
                roll = D20Calculator.Roll(gathered, ctx.Spec.dc);
                isAutoFail = false;
            }

            // Step 3: Emit event automatically (WOTR-style)
            Entity sourceEntity = routing.Component != null ? routing.Component : ctx.Vehicle.chassis;
            CombatEventBus.EmitSkillCheck(
                roll,
                sourceEntity,
                ctx.CausalSource,
                roll.Success,
                ctx.Spec.DisplayName,
                isAutoFail,
                routing.Character);

            // Step 4: Notify d20 roll trigger on roller
            if (routing.CanAttempt && routing.Component != null)
            {
                routing.Component.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }

            return roll;
        }

        /// <summary>Standalone entity overload — no vehicle routing.</summary>
        public static D20RollOutcome ExecuteForEntity(
            Entity entity,
            SkillCheckSpec spec,
            Object causalSource)
        {
            var gathered = RollGatherer.ForSkillCheck(spec, entity);
            var roll = D20Calculator.Roll(gathered, spec.dc);

            CombatEventBus.EmitSkillCheck(
                roll,
                entity,
                causalSource,
                roll.Success,
                spec.DisplayName);

            entity?.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);

            return roll;
        }
    }
}
