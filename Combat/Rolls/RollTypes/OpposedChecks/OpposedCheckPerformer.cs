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
                    ctx.Spec.defenderSpec, defenderRouting.Component, defenderRouting.Character);
                defenderRoll = D20Calculator.Roll(defenderGathered, 0);

                var attackerGathered = RollGatherer.ForSkillCheck(
                    ctx.Spec.attackerSpec, attackerRouting.Component, attackerRouting.Character);
                result = D20Calculator.Roll(attackerGathered, defenderRoll.Total);
            }

            Entity attackerEntity = attackerRouting.Component ?? ctx.AttackerVehicle?.chassis;
            Entity defenderEntity = defenderRouting.Component ?? ctx.DefenderVehicle?.chassis;

            CombatEventBus.EmitOpposedCheck(
                result, defenderRoll, attackerEntity, defenderEntity, ctx.CausalSource,
                ctx.Spec.attackerSpec.DisplayName, ctx.Spec.defenderSpec.DisplayName);

            if (attackerRouting.CanAttempt && attackerRouting.Component != null)
            {
                attackerRouting.Component.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }
            if (defenderRouting.CanAttempt && defenderRouting.Component != null)
            {
                defenderRouting.Component.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);
            }

            return result;
        }
    }
}
