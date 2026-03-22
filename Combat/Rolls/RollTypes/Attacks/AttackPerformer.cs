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
            var gathered = RollGatherer.ForAttack(ctx.Spec, ctx.Attacker);
            int defenseValue = ctx.Target.GetArmorClass();
            var result = D20Calculator.Roll(gathered, defenseValue);

            Entity attackerEntity = ctx.Attacker?.GetEntity();

            if (attackerEntity != null)
                attackerEntity.NotifyStatusEffectTrigger(RemovalTrigger.OnD20Roll);

            CombatEventBus.EmitAttackRoll(
                result,
                ctx.Attacker,
                ctx.Target,
                ctx.CausalSource);

            if (attackerEntity != null)
                attackerEntity.NotifyStatusEffectTrigger(RemovalTrigger.OnAttackMade);
            return result;
        }
    }
}
