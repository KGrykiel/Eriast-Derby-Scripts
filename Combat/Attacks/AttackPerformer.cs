namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Universal entry point for attack rolls.
    /// Orchestrates: calculator (computation) + event emission + optional special rules.
    /// Inspired by WOTR's Rulebook system - execution automatically logs events.
    /// </summary>
    public static class AttackPerformer
    {
        /// <summary>
        /// Execute an attack from a spec.
        /// Delegates computation to AttackCalculator, applies special rules, emits events.
        /// </summary>
        public static AttackResult Execute(AttackSpec spec)
        {
            // Roll against primary target
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
            var fallbackResult = AttackSpecialRules.TryComponentFallback(spec, missResult);
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
