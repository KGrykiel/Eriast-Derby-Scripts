using Assets.Scripts.Conditions;
using Assets.Scripts.Entities;

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
            var data = D20Calculator.Roll(gathered);
            bool success = data.IsCrit || (!data.IsFumble && data.Total >= defenseValue);
            var result = new D20RollOutcome(
                data.KeptRoll, data.Bonuses, data.TotalModifier,
                data.Total, defenseValue, success,
                data.IsCrit, data.IsFumble, data.Advantage);

            Entity attackerEntity = ctx.Attacker.GetEntity();

            if (attackerEntity != null)
                attackerEntity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);
            
            if (ctx.Attacker.GetSeat() != null)
                ctx.Attacker.GetSeat().NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

            CombatEventBus.Emit(new AttackRollEvent(
                result,
                ctx.Attacker,
                ctx.Target,
                ctx.CausalSource));

            if (attackerEntity != null)
                attackerEntity.NotifyConditionTrigger(RemovalTrigger.OnAttackMade);
            return result;
        }
    }
}
