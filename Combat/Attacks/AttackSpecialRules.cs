namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Class for holding special attacking rules that don't fit cleanly into main logic, encapsulated in a way that will be easy to disable or enable them,
    /// For example the component targetting fallback rule where if you miss the component you were trying to hit you can still hit the chassis with a penalty.
    /// Right now the rule is hardcoded but ideally I would like to have a more structured way of adding and removing rules once there's more of them.
    /// </summary>
    public static class AttackSpecialRules
    {
        private const int ComponentTargetingPenalty = 5;

        /// <summary>Returns fallback result if applicable, null if rule doesn't apply.</summary>
        public static AttackResult TryComponentFallback(AttackSpec spec)
        {
            // not targetting a vehicle component, no fallback
            if (spec.Target is not VehicleComponent targetComponent)
                return null;

            // Find the chassis (fallback target)
            Vehicle vehicle = targetComponent.GetComponentInParent<Vehicle>();
            if (vehicle == null || vehicle.chassis == null)
                return null;

            // no need for fallback if we were already targeting the chassis
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
