using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Conditions;
using Assets.Scripts.Entities;

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
            // Step 1: Gather and compute
            D20RollOutcome roll;
            if (!ctx.Routing.CanAttempt)
                roll = D20Calculator.AutoFail(ctx.Spec.dc);
            else
            {
                var gathered = RollGatherer.ForSave(ctx.Spec, ctx.Routing.Actor);
                roll = D20Calculator.Roll(gathered, ctx.Spec.dc);
            }

            // Step 2: Emit event automatically
            RollActor defender = ctx.Routing.Actor ?? new ComponentActor(ctx.Vehicle.chassis);
            CombatEventBus.Emit(new SavingThrowEvent(
                roll,
                ctx.AttackerEntity,
                defender,
                ctx.CausalSource,
                ctx.Spec.DisplayName));

            // Step 3: Notify d20 roll trigger
            if (ctx.Routing.CanAttempt)
            {
                Entity actorEntity = defender.GetEntity();
                if (actorEntity != null)
                    actorEntity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

                var defenderSeat = defender.GetSeat();
                if (defenderSeat != null)
                    defenderSeat.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);
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

            CombatEventBus.Emit(new SavingThrowEvent(
                roll,
                attackerEntity,
                defender,
                causalSource,
                spec.DisplayName));

            if (entity != null)
                entity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

            return roll;
        }
    }
}
