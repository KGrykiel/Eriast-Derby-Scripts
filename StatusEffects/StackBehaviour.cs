namespace Assets.Scripts.StatusEffects
{
    /// <summary>
    /// Determines how a status effect behaves when reapplied to a target that already has it active.
    /// </summary>
    public enum StackBehaviour
    {
        /// <summary>Reset duration to base, keep single instance.</summary>
        Refresh,

        /// <summary>Create new instance, accumulate effects (each stack independent).</summary>
        Stack,

        /// <summary>Do nothing if already active (duration does not refresh).</summary>
        Ignore,

        /// <summary>Replace existing effect if new application is stronger.</summary>
        Replace
    }
}
