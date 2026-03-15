using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;

namespace Assets.Scripts.Combat.Rolls.RollTypes.OpposedChecks
{
    /// <summary>
    /// Both D20 roll outcomes + contest-specific context.
    /// Same layering as AttackResult/SaveResult/SkillCheckResult.
    /// </summary>
    [System.Serializable]
    public class OpposedCheckResult
    {
        public D20RollOutcome AttackerRoll { get; }
        public D20RollOutcome DefenderRoll { get; }
        public SkillCheckSpec AttackerSpec { get; }
        public SkillCheckSpec DefenderSpec { get; }

        /// <summary>Null for vehicle-only checks.</summary>
        public Character AttackerCharacter { get; }

        /// <summary>Null for vehicle-only checks.</summary>
        public Character DefenderCharacter { get; }

        public bool AttackerWins { get; }

        /// <summary>True if one side couldn't attempt — no rolls occurred.</summary>
        public bool IsAutoResult { get; }

        public OpposedCheckResult(
            D20RollOutcome attackerRoll,
            D20RollOutcome defenderRoll,
            SkillCheckSpec attackerSpec,
            SkillCheckSpec defenderSpec,
            Character attackerCharacter,
            Character defenderCharacter,
            bool attackerWins,
            bool isAutoResult = false)
        {
            AttackerRoll = attackerRoll;
            DefenderRoll = defenderRoll;
            AttackerSpec = attackerSpec;
            DefenderSpec = defenderSpec;
            AttackerCharacter = attackerCharacter;
            DefenderCharacter = defenderCharacter;
            AttackerWins = attackerWins;
            IsAutoResult = isAutoResult;
        }
    }
}
