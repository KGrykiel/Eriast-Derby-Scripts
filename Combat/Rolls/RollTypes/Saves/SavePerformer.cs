using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.StatusEffects;

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
                var gathered = RollGatherer.ForSave(ctx.Spec, routing.Actor);
                roll = D20Calculator.Roll(gathered, ctx.Spec.dc);
                isAutoFail = false;
            }

            // Step 3: Emit event automatically
            RollActor defender = routing.Actor ?? new ComponentActor(ctx.Vehicle.chassis);
            CombatEventBus.EmitSavingThrow(
                roll,
                ctx.AttackerEntity,
                defender,
                ctx.CausalSource,
                ctx.Spec.DisplayName,
                isAutoFail);

            // Step 4: Notify d20 roll trigger
            Entity actorEntity = defender.GetEntity();
            if (routing.CanAttempt && actorEntity != null)
            {
                actorEntity.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }

            return roll;
        }

        /// <summary>Standalone entity overload — no vehicle routing.</summary>
        public static D20RollOutcome ExecuteForEntity(
            Entity entity,
            SaveSpec spec,
            string causalSource,
            Entity attackerEntity = null)
        {
            var defender = new ComponentActor(entity);
            var gathered = RollGatherer.ForSave(spec, defender);
            var roll = D20Calculator.Roll(gathered, spec.dc);

            CombatEventBus.EmitSavingThrow(
                roll,
                attackerEntity,
                defender,
                causalSource,
                spec.DisplayName);

            entity?.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);

            return roll;
        }
    }
}
