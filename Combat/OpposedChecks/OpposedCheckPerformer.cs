using Assets.Scripts.Combat.RollSpecs;
using UnityEngine;

namespace Assets.Scripts.Combat.OpposedChecks
{
    /// <summary>
    /// Entry point for opposed checks. Routes both sides, computes, and emits events.
    /// Same pattern as AttackPerformer/SavePerformer/SkillCheckPerformer.
    /// </summary>
    public static class OpposedCheckPerformer
    {
        public static OpposedCheckResult Execute(OpposedCheckExecutionContext ctx)
        {
            var attackerRouting = CheckRouter.RouteSkillCheck(
                ctx.AttackerVehicle, ctx.Spec.attackerSpec, ctx.AttackerCharacter);

            var defenderRouting = CheckRouter.RouteSkillCheck(
                ctx.DefenderVehicle, ctx.Spec.defenderSpec);

            OpposedCheckResult result;

            if (!attackerRouting.CanAttempt && !defenderRouting.CanAttempt)
            {
                result = OpposedCheckCalculator.AutoWin(
                    ctx.Spec.attackerSpec, ctx.Spec.defenderSpec, attackerWins: false);
            }
            else if (!attackerRouting.CanAttempt)
            {
                result = OpposedCheckCalculator.AutoWin(
                    ctx.Spec.attackerSpec, ctx.Spec.defenderSpec, attackerWins: false);
            }
            else if (!defenderRouting.CanAttempt)
            {
                result = OpposedCheckCalculator.AutoWin(
                    ctx.Spec.attackerSpec, ctx.Spec.defenderSpec, attackerWins: true);
            }
            else
            {
                result = OpposedCheckCalculator.Compute(
                    ctx.Spec.attackerSpec, attackerRouting.Component, attackerRouting.Character,
                    ctx.Spec.defenderSpec, defenderRouting.Component, defenderRouting.Character);
            }

            Entity attackerEntity = attackerRouting.Component ?? ctx.AttackerVehicle?.chassis;
            Entity defenderEntity = defenderRouting.Component ?? ctx.DefenderVehicle?.chassis;

            CombatEventBus.EmitOpposedCheck(
                result, attackerEntity, defenderEntity, ctx.CausalSource);

            return result;
        }
    }
}
