namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Main entry for executing an attack roll. Created to handle any special rules in the future without polluting AttackCalculator.
    /// The component fallback is hardcoded for now, but I'll need to think of a more structured way to handle special rules once there are more.
    /// </summary>
    public static class AttackPerformer
    {
        public static AttackResult Execute(AttackSpec spec)
        {
            // Roll against primary target, conclude if successful
            var primaryResult = AttackCalculator.Compute(spec.Target, spec.Attacker, spec.Character);

            if (primaryResult.Roll.Success)
            {
                var result = new AttackResult(primaryResult.Roll, hitTarget: spec.Target);
                EmitEvent(result, spec, isFallback: false);
                return result;
            }

            // Primary miss - emit miss event
            var missResult = new AttackResult(primaryResult.Roll, hitTarget: null);
            EmitEvent(missResult, spec, isFallback: false);

            // OPTIONAL RULE: Component fallback
            // TO DISABLE THIS RULE: Delete the lines below
            var fallbackResult = AttackSpecialRules.TryComponentFallback(spec);
            if (fallbackResult != null)
            {
                EmitEvent(fallbackResult, spec, isFallback: true);
                return fallbackResult;
            }

            return missResult;
        }

        // ==================== EVENT EMISSION ====================

        private static void EmitEvent(AttackResult result, AttackSpec spec, bool isFallback)
        {
            string targetCompName = spec.Target != null ? spec.Target.name : null;
            Entity eventTarget = isFallback ? result.HitTarget : spec.Target;

            CombatEventBus.EmitAttackRoll(
                result,
                spec.Attacker,
                eventTarget,
                spec.CausalSource,
                result.Roll.Success,
                targetCompName,
                isFallback,
                spec.Character);
        }
    }
}
