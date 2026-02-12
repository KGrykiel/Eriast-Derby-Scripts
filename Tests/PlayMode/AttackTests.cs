using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class AttackTests
    {
        private Vehicle vehicle;
        private readonly System.Collections.Generic.List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            if (vehicle != null)
                Object.DestroyImmediate(vehicle.gameObject);
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();
        }

        private Character CreateCharacter(string name = "Test", int level = 1, int baseAttackBonus = 0)
        {
            var c = TestCharacterFactory.Create(name: name, level: level, baseAttackBonus: baseAttackBonus);
            cleanup.Add(c);
            return c;
        }

        // ==================== Test 3A ====================

        [UnityTest]
        public IEnumerator Test3A_CharacterPlusWeaponAttack_BonusesStack()
        {
            var bob = CreateCharacter("Bob", baseAttackBonus: 2);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithWeapon(bob, attackBonus: 1)
                .Build();

            var weapon = vehicle.optionalComponents[0];

            // Create a dummy target
            var targetObj = new GameObject("Target");
            var target = targetObj.AddComponent<ChassisComponent>();
            target.health = 100;

            var bonuses = AttackCalculator.GatherBonuses(weapon, bob);
            yield return null;

            int totalMod = bonuses.Sum(b => b.Value);
            Assert.AreEqual(3, totalMod, "Total modifier should be +3 (weapon +1, character +2)");
            Assert.IsTrue(bonuses.Any(b => b.Value == 1), "Should have weapon +1 bonus");
            Assert.IsTrue(bonuses.Any(b => b.Value == 2), "Should have character +2 bonus");

            Object.DestroyImmediate(targetObj);
        }

        // ==================== Test 3B ====================

        [UnityTest]
        public IEnumerator Test3B_CharacterOnlyAttack_NoWeaponBonus()
        {
            var ada = TestCharacterFactory.CreateWithCleanup("Ada", baseAttackBonus: 2, cleanup: cleanup);

            var bonuses = AttackCalculator.GatherBonuses(attacker: null, character: ada);
            yield return null;

            int totalMod = bonuses.Sum(b => b.Value);
            Assert.AreEqual(2, totalMod, "Total modifier should be +2 (character only)");
            Assert.AreEqual(1, bonuses.Count, "Should have exactly 1 bonus source");
        }

        // ==================== Test 3C ====================

        [UnityTest]
        public IEnumerator Test3C_AttackWithStatusEffect_ModifiersApply()
        {
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", baseAttackBonus: 2, cleanup: cleanup);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithWeapon(bob, attackBonus: 0)
                .Build();

            var weapon = vehicle.optionalComponents[0];

            // Apply a +2 attack bonus modifier (simulating "Blessed" status effect)
            weapon.AddModifier(new AttributeModifier(
                Attribute.AttackBonus,
                ModifierType.Flat,
                2f,
                source: weapon,
                category: ModifierCategory.StatusEffect,
                displayNameOverride: "Blessed"
            ));

            var bonuses = AttackCalculator.GatherBonuses(weapon, bob);
            yield return null;

            int totalMod = bonuses.Sum(b => b.Value);
            Assert.AreEqual(4, totalMod, "Total modifier should be +4 (weapon 0, status effect +2, character +2)");
            Assert.IsTrue(bonuses.Any(b => b.Label == "Blessed" && b.Value == 2),
                "Should have Blessed +2 bonus from status effect");
        }
    }
}
