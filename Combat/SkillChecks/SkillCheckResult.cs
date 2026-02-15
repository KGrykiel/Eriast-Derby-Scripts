namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// D20RollOutcome (mechanics) + check-specific context.
    /// Same layering as AttackResult/SaveResult.
    /// </summary>
    [System.Serializable]
    public class SkillCheckResult
    {
        public D20RollOutcome Roll { get; }
        public SkillCheckSpec Spec { get; }

        /// <summary>Null for vehicle-only checks.</summary>
        public Character Character { get; }

        /// <summary>True if routing failed (missing component) — no roll occurred, useful for logging.</summary>
        public bool IsAutoFail { get; }

        public SkillCheckResult(D20RollOutcome roll, SkillCheckSpec spec, Character character = null, bool isAutoFail = false)
        {
            Roll = roll;
            Spec = spec;
            Character = character;
            IsAutoFail = isAutoFail;
        }
    }
}