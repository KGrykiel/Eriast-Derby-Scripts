using Assets.Scripts.Combat.RollSpecs;
using Assets.Scripts.Combat.SkillChecks;

namespace Assets.Scripts.Combat.OpposedChecks
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

            int attackerBase = RollUtility.RollD20();
            int attackerMod = D20RollHelpers.SumBonuses(attackerBonuses);
            int attackerTotal = attackerBase + attackerMod;

            int defenderBase = RollUtility.RollD20();
            int defenderMod = D20RollHelpers.SumBonuses(defenderBonuses);
            int defenderTotal = defenderBase + defenderMod;

            bool attackerWins = attackerTotal >= defenderTotal;

            var attackerRoll = new D20RollOutcome(
                attackerBase, attackerBonuses, attackerMod, attackerTotal,
                defenderTotal, attackerWins, attackerBase == 20, attackerBase == 1);

            var defenderRoll = new D20RollOutcome(
                defenderBase, defenderBonuses, defenderMod, defenderTotal,
                attackerTotal, !attackerWins, defenderBase == 20, defenderBase == 1);

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
