using Assets.Scripts.Conditions;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Combat.Rolls.RollTypes.OpposedChecks
{
    /// <summary>
    /// Entry point for opposed checks. Routes both sides, computes, and emits events.
    /// Same pattern as AttackPerformer/SavePerformer/SkillCheckPerformer.
    /// Defender rolls first; their total becomes the DC for the attacker's roll.
    /// </summary>
    public static class OpposedCheckPerformer
    {
        public static D20RollOutcome Execute(OpposedCheckExecutionContext ctx)
        {
            D20RollOutcome result;
            D20RollOutcome defenderRoll = null;

            if (!ctx.AttackerRouting.CanAttempt)
            {
                result = D20Calculator.AutoFail(0);
            }
            else if (!ctx.DefenderRouting.CanAttempt)
            {
                result = D20Calculator.AutoSuccess(0);
            }
            else
            {
                var defenderGathered = RollGatherer.ForSkillCheck(
                    ctx.Spec.defenderSpec, ctx.DefenderRouting.Actor);
                defenderRoll = D20Calculator.Roll(defenderGathered, 0);

                var attackerGathered = RollGatherer.ForSkillCheck(
                    ctx.Spec.attackerSpec, ctx.AttackerRouting.Actor);
                result = D20Calculator.Roll(attackerGathered, defenderRoll.Total);
            }

            RollActor attackerActor = ctx.AttackerRouting.Actor;
            RollActor defenderActor = ctx.DefenderRouting.Actor;

            CombatEventBus.Emit(new OpposedCheckEvent(
                result, defenderRoll, attackerActor, defenderActor, ctx.CausalSource,
                ctx.Spec.attackerSpec.DisplayName, ctx.Spec.defenderSpec.DisplayName));

            if (ctx.AttackerRouting.CanAttempt)
            {
                Entity attackerEntity = attackerActor.GetEntity();
                if (attackerEntity != null)
                    attackerEntity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

                var attackerSeat = attackerActor.GetSeat();
                if (attackerSeat != null)
                    attackerSeat.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);
            }
            if (ctx.DefenderRouting.CanAttempt)
            {
                Entity defenderEntity = defenderActor.GetEntity();
                if (defenderEntity != null)
                    defenderEntity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

                var defenderSeat = defenderActor.GetSeat();
                if (defenderSeat != null)
                    defenderSeat.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);
            }

            return result;
        }
    }
}
