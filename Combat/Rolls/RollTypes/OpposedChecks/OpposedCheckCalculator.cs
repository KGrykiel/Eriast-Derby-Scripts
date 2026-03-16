using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;

namespace Assets.Scripts.Combat.Rolls.RollTypes.OpposedChecks
{
    /// <summary>
    /// Rules engine for opposed checks — both sides roll, higher total wins.
    /// Has no vehicle/routing knowledge. Takes pre-resolved participants from CheckRouter.
    /// </summary>
    public static class OpposedCheckCalculator
    {
        public static OpposedCheckResult Compute(
            SkillCheckSpec attackerSpec, Entity attackerEntity, Character attackerCharacter,
            SkillCheckSpec defenderSpec, Entity defenderEntity, Character defenderCharacter)
        {
            var attackerBonuses = SkillCheckCalculator.GatherBonuses(attackerSpec, attackerEntity, attackerCharacter);
            var defenderBonuses = SkillCheckCalculator.GatherBonuses(defenderSpec, defenderEntity, defenderCharacter);

            var attackerGrantedSource = attackerSpec.grantedMode != RollMode.Normal
                ? new AdvantageSource(attackerSpec.DisplayName, attackerSpec.grantedMode)
                : default;
            var attackerAdvantageSources = D20RollHelpers.GatherAdvantageSources(attackerEntity, attackerSpec, attackerGrantedSource);

            var defenderGrantedSource = defenderSpec.grantedMode != RollMode.Normal
                ? new AdvantageSource(defenderSpec.DisplayName, defenderSpec.grantedMode)
                : default;
            var defenderAdvantageSources = D20RollHelpers.GatherAdvantageSources(defenderEntity, defenderSpec, defenderGrantedSource);

            // Use 0 as targetValue — opposed checks compare totals, not against a DC.
            // Success/crit/fumble on individual rolls are not used; winner is determined by total comparison.
            var attackerRoll = D20Calculator.Roll(attackerBonuses, 0, attackerAdvantageSources);
            var defenderRoll = D20Calculator.Roll(defenderBonuses, 0, defenderAdvantageSources);

            bool attackerWins = attackerRoll.Total >= defenderRoll.Total;

            return new OpposedCheckResult(
                attackerRoll, defenderRoll,
                attackerSpec, defenderSpec,
                attackerCharacter, defenderCharacter,
                attackerWins);
        }

        /// <summary>One side can't attempt — the other auto-wins without rolling.</summary>
        public static OpposedCheckResult AutoWin(
            SkillCheckSpec attackerSpec,
            SkillCheckSpec defenderSpec,
            bool attackerWins)
        {
            var dummyRoll = D20Calculator.AutoFail(0);

            return new OpposedCheckResult(
                dummyRoll, dummyRoll,
                attackerSpec, defenderSpec,
                null, null,
                attackerWins,
                isAutoResult: true);
        }
    }
}
