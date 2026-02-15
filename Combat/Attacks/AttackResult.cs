namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Result of a D20 roll for an attack with additional attack-related info.
    /// Same layering as SaveResult/SkillCheckResult.
    /// </summary>
    [System.Serializable]
    public class AttackResult
    {
        /// <summary>Universal D20 outcome struct</summary>
        public D20RollOutcome Roll { get; }

        /// <summary>Entity that was hit</summary>
        public Entity HitTarget { get; }

        /// <summary>Flag to signify if the hit entity wasn't the primary target via the fallback mechanic. Used for logging</summary>
        public bool WasFallback { get; }

        public AttackResult(D20RollOutcome roll, Entity hitTarget = null, bool wasFallback = false)
        {
            Roll = roll;
            HitTarget = hitTarget;
            WasFallback = wasFallback;
        }
    }
}
