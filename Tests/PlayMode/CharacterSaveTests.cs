using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class CharacterSaveTests
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

        // ==================== Test 2A ====================

        [UnityTest]
        public IEnumerator Test2A_SaveWithTargetComponent_RoutesToOperator()
        {
            var ada = TestCharacterFactory.CreateWithCleanup("Ada", level: 3, dexterity: 16, cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(ada);

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Dexterity);
            var result = SavePerformer.Execute(vehicle, spec, dc: 15, causalSource: null, targetComponent: vehicle.chassis);
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(ada, result.Character, "Should route to Ada (controls targeted chassis)");
            Assert.IsFalse(result.IsAutoFail);

            int expectedDexMod = CharacterFormulas.CalculateAttributeModifier(16);
            int expectedHalfLevel = CharacterFormulas.CalculateHalfLevelBonus(3);
            Assert.AreEqual(expectedDexMod + expectedHalfLevel, result.Roll.TotalModifier,
                $"Expected DEX({expectedDexMod}) + HalfLevel({expectedHalfLevel})");
        }

        // ==================== Test 2B ====================

        [UnityTest]
        public IEnumerator Test2B_SaveWithoutTarget_RoutesToBestModifier()
        {
            var ada = TestCharacterFactory.CreateWithCleanup("Ada", level: 3, wisdom: 10, cleanup: cleanup);
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", level: 3, wisdom: 16, cleanup: cleanup);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .AddSeat("Seat1", ada)
                .AddSeat("Seat2", bob)
                .Build();

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Wisdom);
            var result = SavePerformer.Execute(vehicle, spec, dc: 13, causalSource: null);
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(bob, result.Character, "Should route to Bob (best WIS save)");

            int bobSaveMod = CharacterFormulas.CalculateSaveModifier(bob, CharacterAttribute.Wisdom);
            Assert.AreEqual(bobSaveMod, result.Roll.TotalModifier, $"Bob's save modifier should be {bobSaveMod}");
        }

        // ==================== Test 2C ====================

        [UnityTest]
        public IEnumerator Test2C_SaveWithRequiredComponent_AutoFail()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Intelligence, requiredComponent: ComponentType.PowerCore);
            var result = SavePerformer.Execute(vehicle, spec, dc: 15, causalSource: null);
            yield return null;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAutoFail, "Should auto-fail when required component missing");
        }

        // ==================== Test 2D ====================

        [UnityTest]
        public IEnumerator Test2D_SaveWithRequiredComponent_RoutesToOperator()
        {
            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", level: 4, intelligence: 14, cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore(engineer)
                .Build();

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Intelligence, requiredComponent: ComponentType.PowerCore);
            var result = SavePerformer.Execute(vehicle, spec, dc: 15, causalSource: null);
            yield return null;

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAutoFail);
            Assert.AreEqual(engineer, result.Character, "Should route to Engineer (controls PowerCore)");

            int expectedMod = CharacterFormulas.CalculateSaveModifier(engineer, CharacterAttribute.Intelligence);
            Assert.AreEqual(expectedMod, result.Roll.TotalModifier, $"Engineer's save modifier should be {expectedMod}");
        }

        // ==================== Test 2E ====================

        [Test]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(5, 2)]
        [TestCase(10, 5)]
        [TestCase(20, 10)]
        public void Test2E_HalfLevelBonus_IntegerDivision(int level, int expectedHalfLevel)
        {
            int actual = CharacterFormulas.CalculateHalfLevelBonus(level);
            Assert.AreEqual(expectedHalfLevel, actual, $"Level {level} should have half-level {expectedHalfLevel}");
        }
    }
}
