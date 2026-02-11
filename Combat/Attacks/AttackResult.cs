namespace Assets.Scripts.Combat.Attacks
{
    /// <summary>
    /// Attack result = universal roll outcome + attack-specific context.
    /// Pure data bag - no logic or factories.
    /// Access roll data through the Roll property.
    /// 
    /// Follows same pattern as SaveResult/SkillCheckResult:
    /// D20RollOutcome (mechanics) + domain context (what happened).
    /// </summary>
    [System.Serializable]
    public class AttackResult
    {
        /// <summary>Universal roll outcome (the decisive roll)</summary>
        public D20RollOutcome Roll { get; }

        /// <summary>Entity that was actually hit. Null if complete miss.</summary>
        public Entity HitTarget { get; }

        /// <summary>Whether this hit used the fallback target instead of the primary.</summary>
        public bool WasFallback { get; }

        public AttackResult(D20RollOutcome roll, Entity hitTarget = null, bool wasFallback = false)
        {
            Roll = roll;
            HitTarget = hitTarget;
            WasFallback = wasFallback;
        }
    }
}
