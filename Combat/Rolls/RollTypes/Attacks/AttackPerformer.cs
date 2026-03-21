using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Combat.Rolls.RollTypes.Attacks
{
    /// <summary>
    /// Main entry for executing an attack roll.
    /// </summary>
    public static class AttackPerformer
    {
        public static D20RollOutcome Execute(AttackExecutionContext ctx)
        {
            var gathered = RollGatherer.ForAttack(ctx.Spec, ctx.Attacker, ctx.Character);
            int defenseValue = ctx.Target.GetArmorClass();
            var result = D20Calculator.Roll(gathered, defenseValue);

            ctx.Attacker.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);

            string targetCompName = ctx.Target != null ? ctx.Target.name : null;

            CombatEventBus.EmitAttackRoll(
                result,
                ctx.Attacker,
                ctx.Target,
                ctx.CausalSource,
                result.Success,
                targetCompName,
                ctx.Character);

            ctx.Attacker.NotifyStatusEffectTrigger(RemovalTrigger.OnAttackMade);
            return result;
        }
    }
}
