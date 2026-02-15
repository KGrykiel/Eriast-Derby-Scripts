namespace Assets.Scripts.Combat.Saves
{
    /// <summary>
    /// D20RollOutcome (mechanics) + save-specific context.
    /// Same layering as AttackResult/SkillCheckResult.
    /// </summary>
    [System.Serializable]
    public class SaveResult
    {
        public D20RollOutcome Roll { get; }
        public SaveSpec Spec { get; }

        /// <summary>Null for vehicle-only saves.</summary>
        public Character Character { get; }

        /// <summary>True if routing failed (missing component) — no roll occurred. Important for logging</summary>
        public bool IsAutoFail { get; }

        public SaveResult(D20RollOutcome roll, SaveSpec spec, Character character = null, bool isAutoFail = false)
        {
            Roll = roll;
            Spec = spec;
            Character = character;
            IsAutoFail = isAutoFail;
        }
    }
}

