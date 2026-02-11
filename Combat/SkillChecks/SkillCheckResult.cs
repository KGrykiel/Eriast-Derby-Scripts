namespace Assets.Scripts.Combat.SkillChecks
{
    /// <summary>
    /// Skill check result = universal roll outcome + check-specific context.
    /// Pure data bag - no logic or factories.
    /// Access roll data through the Roll property.
    /// </summary>
    [System.Serializable]
    public class SkillCheckResult
    {
        /// <summary>Universal roll outcome (mechanics)</summary>
        public D20RollOutcome Roll { get; }

        /// <summary>What was tested (vehicle attribute or character skill)</summary>
        public SkillCheckSpec Spec { get; }

        /// <summary>Character who made the check (null for vehicle-only checks)</summary>
        public Character Character { get; }

        /// <summary>True if required component was missing (no roll occurred)</summary>
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