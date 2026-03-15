using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using UnityEngine;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Saves
{
    /// <summary>
    /// Entry point for saving throws from all sources — skills, DoT, hazards, event cards, maybe more in the future.
    /// different methods for vehicles that need routing vs standalone entities that don't (objects, monsters etc).
    /// </summary>
    public static class SavePerformer
    {
        public static SaveResult Execute(SaveExecutionContext ctx)
        {
            // Step 1: Route (resolve who/what makes this save)
            var routing = CheckRouter.RouteSave(ctx.Vehicle, ctx.Spec, ctx.TargetComponent);

            // Step 2: Compute
            SaveResult result;
            if (!routing.CanAttempt)
            {
                result = SaveCalculator.AutoFail(ctx.Spec, ctx.DC);
            }
            else
            {
                result = SaveCalculator.Compute(ctx.Spec, ctx.DC, routing.Component, routing.Character);
            }

            // Step 3: Emit event automatically
            Entity defenderEntity = routing.Component != null ? routing.Component : ctx.Vehicle.chassis;
            string targetName = ctx.TargetComponent != null ? ctx.TargetComponent.name : "Vehicle";
            CombatEventBus.EmitSavingThrow(
                result,
                ctx.AttackerEntity,
                defenderEntity,
                ctx.CausalSource,
                result.Roll.Success,
                targetName,
                result.Character);

            return result;
        }

        /// <summary>Standalone entity overload — no vehicle routing.</summary>
        public static SaveResult ExecuteForEntity(
            Entity entity,
            SaveSpec spec,
            int dc,
            Object causalSource,
            Entity attackerEntity = null)
        {
            var result = SaveCalculator.Compute(spec, dc, entity);

            string targetName = entity != null ? entity.name : "Target";
            CombatEventBus.EmitSavingThrow(
                result,
                attackerEntity,
                entity,
                causalSource,
                result.Roll.Success,
                targetName);

            return result;
        }
    }
}
