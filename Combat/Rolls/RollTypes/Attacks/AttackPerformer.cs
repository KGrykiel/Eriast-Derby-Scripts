namespace Assets.Scripts.Combat.Rolls.RollTypes.Attacks
{
    /// <summary>
    /// Main entry for executing an attack roll. Created to handle any special rules in the future without polluting AttackCalculator.
    /// The component fallback is hardcoded for now, but I'll need to think of a more structured way to handle special rules once there are more.
    /// </summary>
    public static class AttackPerformer
    {
        public static AttackResult Execute(AttackExecutionContext ctx)
        {
            // Roll against primary target, conclude if successful
            var primaryResult = AttackCalculator.Compute(ctx.Spec, ctx.Target, ctx.Attacker, ctx.Character);

            if (primaryResult.Roll.Success)
            {
                var result = new AttackResult(primaryResult.Roll, hitTarget: ctx.Target);
                EmitEvent(result, ctx, isFallback: false);
                return result;
            }

            // Primary miss - emit miss event
            var missResult = new AttackResult(primaryResult.Roll, hitTarget: null);
            EmitEvent(missResult, ctx, isFallback: false);

            // OPTIONAL RULE: Component fallback
            // TO DISABLE THIS RULE: Delete the lines below
            var fallbackResult = AttackSpecialRules.TryComponentFallback(ctx);
            if (fallbackResult != null)
            {
                EmitEvent(fallbackResult, ctx, isFallback: true);
                return fallbackResult;
            }

            return missResult;
        }

        // ==================== EVENT EMISSION ====================

        private static void EmitEvent(AttackResult result, AttackExecutionContext ctx, bool isFallback)
        {
            string targetCompName = ctx.Target != null ? ctx.Target.name : null;
            Entity eventTarget = isFallback ? result.HitTarget : ctx.Target;

            CombatEventBus.EmitAttackRoll(
                result,
                ctx.Attacker,
                eventTarget,
                ctx.CausalSource,
                result.Roll.Success,
                targetCompName,
                isFallback,
                ctx.Character);
        }
    }
}
