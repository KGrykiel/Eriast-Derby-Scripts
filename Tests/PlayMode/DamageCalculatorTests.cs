using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Combat.Damage;

namespace Assets.Scripts.Tests.PlayMode
{
    public class DamageCalculatorTests
    {
        // ==================== Basic Damage Calculation ====================

        [Test]
        public void Damage_FlatBonus_CorrectResult()
        {
            var formula = new DamageFormula
            {
                baseDice = 0,
                dieSize = 0,
                bonus = 10,
                damageType = DamageType.Physical
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Normal);

            Assert.AreEqual(10, result.FinalDamage, "Flat 10 damage should deal 10");
            Assert.AreEqual(DamageType.Physical, result.DamageType);
        }

        [Test]
        public void Damage_DiceRoll_InExpectedRange()
        {
            var formula = new DamageFormula
            {
                baseDice = 2,
                dieSize = 6,
                bonus = 3,
                damageType = DamageType.Fire
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Normal);

            int minExpected = 2 + 3; // 2d6 min (2) + bonus (3)
            int maxExpected = 12 + 3; // 2d6 max (12) + bonus (3)
            Assert.GreaterOrEqual(result.FinalDamage, minExpected, "Damage should be at least min roll + bonus");
            Assert.LessOrEqual(result.FinalDamage, maxExpected, "Damage should be at most max roll + bonus");
        }

        // ==================== Critical Hits ====================

        [Test]
        public void Damage_CriticalHit_DoublesDiceNotBonus()
        {
            var formula = new DamageFormula
            {
                baseDice = 1,
                dieSize = 6,
                bonus = 5,
                damageType = DamageType.Physical
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Normal, isCriticalHit: true);

            Assert.IsTrue(result.IsCritical, "Should be marked as critical");
            Assert.AreEqual(2, result.DiceCount, "Crit should double dice count");

            int minExpected = 2 + 5; // 2d6 min (2) + bonus (5) - bonus NOT doubled
            int maxExpected = 12 + 5; // 2d6 max (12) + bonus (5)
            Assert.GreaterOrEqual(result.FinalDamage, minExpected);
            Assert.LessOrEqual(result.FinalDamage, maxExpected);
        }

        [Test]
        public void Damage_CriticalHit_ZeroDice_NoCrash()
        {
            var formula = new DamageFormula
            {
                baseDice = 0,
                dieSize = 0,
                bonus = 5,
                damageType = DamageType.Physical
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Normal, isCriticalHit: true);

            Assert.AreEqual(5, result.FinalDamage, "Flat damage should be unaffected by crit");
            Assert.IsFalse(result.IsCritical, "Should not mark as critical when no dice");
        }

        // ==================== Resistances ====================

        [Test]
        public void Damage_Resistant_HalvesDamage()
        {
            var formula = new DamageFormula
            {
                baseDice = 0,
                dieSize = 0,
                bonus = 10,
                damageType = DamageType.Fire
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Resistant);

            Assert.AreEqual(5, result.FinalDamage, "Resistant should halve damage (10/2=5)");
        }

        [Test]
        public void Damage_Vulnerable_DoublesDamage()
        {
            var formula = new DamageFormula
            {
                baseDice = 0,
                dieSize = 0,
                bonus = 10,
                damageType = DamageType.Fire
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Vulnerable);

            Assert.AreEqual(20, result.FinalDamage, "Vulnerable should double damage (10*2=20)");
        }

        [Test]
        public void Damage_Immune_ZeroDamage()
        {
            var formula = new DamageFormula
            {
                baseDice = 0,
                dieSize = 0,
                bonus = 10,
                damageType = DamageType.Fire
            };

            var result = DamageCalculator.Compute(formula, ResistanceLevel.Immune);

            Assert.AreEqual(0, result.FinalDamage, "Immune should negate all damage");
        }

        // ==================== Damage Application ====================

        [UnityTest]
        public IEnumerator Damage_Applied_ReducesHealth()
        {
            var targetObj = new GameObject("Target");
            var target = targetObj.AddComponent<ChassisComponent>();
            target.health = 100;

            var formula = new DamageFormula { baseDice = 0, dieSize = 0, bonus = 25, damageType = DamageType.Physical };
            var result = DamageCalculator.Compute(formula, ResistanceLevel.Normal);

            DamageApplicator.Apply(result, target);
            yield return null;

            Assert.AreEqual(75, target.GetCurrentHealth(), "Health should be reduced by 25");
            Assert.IsFalse(target.isDestroyed, "Should not be destroyed at 75 HP");

            Object.DestroyImmediate(targetObj);
        }

        [UnityTest]
        public IEnumerator Damage_Applied_DestroysAtZeroHP()
        {
            var targetObj = new GameObject("Target");
            var target = targetObj.AddComponent<ChassisComponent>();
            target.health = 20;

            var formula = new DamageFormula { baseDice = 0, dieSize = 0, bonus = 30, damageType = DamageType.Physical };
            var result = DamageCalculator.Compute(formula, ResistanceLevel.Normal);

            DamageApplicator.Apply(result, target);
            yield return null;

            Assert.AreEqual(0, target.GetCurrentHealth(), "Health should be 0");
            Assert.IsTrue(target.isDestroyed, "Should be destroyed");

            Object.DestroyImmediate(targetObj);
        }
    }
}
