namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Optional attack rules that modify standard attack flow.
    /// Each rule can be disabled by removing its usage from AttackPerformer.
    /// 
    /// COMPONENT FALLBACK RULE:
    /// When attacking a non-chassis vehicle component and missing,
    /// automatically roll again against the chassis with a penalty.
    /// 
    /// TO DISABLE: Remove the TryComponentFallback() call from AttackPerformer.
    /// </summary>
    public static class AttackSpecialRules
    {
        private const int ComponentTargetingPenalty = 5;

        /// <summary>
        /// Component fallback rule: miss on component → try chassis with penalty.
        /// Returns fallback result if applicable, null if rule doesn't apply.
        /// </summary>
        public static AttackResult TryComponentFallback(AttackSpec spec, AttackResult missResult)
        {
            // Determine if fallback applies
            if (spec.Target is not VehicleComponent targetComponent)
                return null;

            // Find the chassis (fallback target)
            Vehicle vehicle = targetComponent.GetComponentInParent<Vehicle>();
            if (vehicle == null || vehicle.chassis == null)
                return null;

            // Already hit chassis = no fallback
            if (targetComponent == vehicle.chassis)
                return null;

            // Roll against chassis with penalty
            var fallbackResult = AttackCalculator.Compute(
                vehicle.chassis, 
                spec.Attacker, 
                spec.Character, 
                ComponentTargetingPenalty);

            Entity hitTarget = fallbackResult.Roll.Success ? vehicle.chassis : null;
            return new AttackResult(fallbackResult.Roll, hitTarget, wasFallback: true);
        }
    }
}
