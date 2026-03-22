using Assets.Scripts.StatusEffects;

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
            var attackerRouting = CheckRouter.RouteSkillCheck(
                ctx.AttackerVehicle, ctx.Spec.attackerSpec, ctx.AttackerCharacter);

            var defenderRouting = CheckRouter.RouteSkillCheck(
                ctx.DefenderVehicle, ctx.Spec.defenderSpec);

            D20RollOutcome result;
            D20RollOutcome defenderRoll = null;

            if (!attackerRouting.CanAttempt)
            {
                result = D20Calculator.AutoFail(0);
            }
            else if (!defenderRouting.CanAttempt)
            {
                result = D20Calculator.AutoSuccess(0);
            }
            else
            {
                var defenderGathered = RollGatherer.ForSkillCheck(
                    ctx.Spec.defenderSpec, defenderRouting.Actor);
                defenderRoll = D20Calculator.Roll(defenderGathered, 0);

                var attackerGathered = RollGatherer.ForSkillCheck(
                    ctx.Spec.attackerSpec, attackerRouting.Actor);
                result = D20Calculator.Roll(attackerGathered, defenderRoll.Total);
            }

            RollActor attackerActor = attackerRouting.Actor
                ?? (ctx.AttackerVehicle != null ? new ComponentActor(ctx.AttackerVehicle.chassis) : null);
            RollActor defenderActor = defenderRouting.Actor
                ?? (ctx.DefenderVehicle != null ? new ComponentActor(ctx.DefenderVehicle.chassis) : null);

            CombatEventBus.EmitOpposedCheck(
                result, defenderRoll, attackerActor, defenderActor, ctx.CausalSource,
                ctx.Spec.attackerSpec.DisplayName, ctx.Spec.defenderSpec.DisplayName);

            Entity attackerEntity = attackerActor?.GetEntity();
            Entity defenderEntity = defenderActor?.GetEntity();
            if (attackerRouting.CanAttempt && attackerEntity != null)
            {
                attackerEntity.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }
            if (defenderRouting.CanAttempt && defenderEntity != null)
            {
                defenderEntity.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }

            return result;
        }
    }
}
