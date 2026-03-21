using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.StatusEffects;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Saves
{
    /// <summary>
    /// Entry point for saving throws from all sources — skills, DoT, hazards, event cards, maybe more in the future.
    /// different methods for vehicles that need routing vs standalone entities that don't (objects, monsters etc).
    /// </summary>
    public static class SavePerformer
    {
        public static D20RollOutcome Execute(SaveExecutionContext ctx)
        {
            // Step 1: Route (resolve who/what makes this save)
            var routing = CheckRouter.RouteSave(ctx.Vehicle, ctx.Spec, ctx.TargetComponent);

            // Step 2: Gather and compute
            D20RollOutcome roll;
            bool isAutoFail;
            if (!routing.CanAttempt)
            {
                roll = D20Calculator.AutoFail(ctx.Spec.dc);
                isAutoFail = true;
            }
            else
            {
                var gathered = RollGatherer.ForSave(ctx.Spec, routing.Component, routing.Character);
                roll = D20Calculator.Roll(gathered, ctx.Spec.dc);
                isAutoFail = false;
            }

            // Step 3: Emit event automatically
            Entity defenderEntity = routing.Component != null ? routing.Component : ctx.Vehicle.chassis;
            string targetName = ctx.TargetComponent != null ? ctx.TargetComponent.name : "Vehicle";
            CombatEventBus.EmitSavingThrow(
                roll,
                ctx.AttackerEntity,
                defenderEntity,
                ctx.CausalSource,
                roll.Success,
                ctx.Spec.DisplayName,
                isAutoFail,
                targetName,
                routing.Character);

            // Step 4: Notify d20 roll trigger
            if (routing.CanAttempt && routing.Component != null)
            {
                routing.Component.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }

            return roll;
        }

        /// <summary>Standalone entity overload — no vehicle routing.</summary>
        public static D20RollOutcome ExecuteForEntity(
            Entity entity,
            SaveSpec spec,
            Object causalSource,
            Entity attackerEntity = null)
        {
            var gathered = RollGatherer.ForSave(spec, entity);
            var roll = D20Calculator.Roll(gathered, spec.dc);

            string targetName = entity != null ? entity.name : "Target";
            CombatEventBus.EmitSavingThrow(
                roll,
                attackerEntity,
                entity,
                causalSource,
                roll.Success,
                spec.DisplayName,
                isAutoFail: false,
                targetName);

            entity?.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);

            return roll;
        }
    }
}
