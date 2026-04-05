namespace Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes
{
    /// <summary>
    /// Which vehicle makes the roll — the action's source or the action's target.
    /// Set on the spec to let designers control roller direction in the inspector.
    /// Mirrors EGameplayEffectAttributeCaptureSource in GAS (Source / Target).
    /// </summary>
    public enum RollerSource
    {
        Source,
        Target
    }
}
