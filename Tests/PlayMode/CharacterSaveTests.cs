using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Core;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Rolls.RollTypes.Saves;

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

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Dexterity, dc: 15);
            var result = SavePerformer.Execute(new SaveExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSave(vehicle, spec, vehicle.chassis) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreNotEqual(0, result.BaseRoll, "Should not auto-fail");

            int expectedDexMod = CharacterStatCalculator.CalculateAttributeModifier(16);
            int expectedHalfLevel = CharacterStatCalculator.CalculateHalfLevelBonus(3);
            Assert.AreEqual(expectedDexMod + expectedHalfLevel, result.TotalModifier,
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

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Wisdom, dc: 13);
            var result = SavePerformer.Execute(new SaveExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSave(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);

            int expectedWisMod = CharacterStatCalculator.CalculateAttributeModifier(16);
            int expectedHalfLevel = CharacterStatCalculator.CalculateHalfLevelBonus(3);
            Assert.AreEqual(expectedWisMod + expectedHalfLevel, result.TotalModifier,
                $"Expected WIS({expectedWisMod}) + HalfLevel({expectedHalfLevel})");
        }

        // ==================== Test 2C ====================

        [UnityTest]
        public IEnumerator Test2C_SaveWithRequiredComponent_AutoFail()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = TestSkillFactory.CharacterSave(CharacterAttribute.Intelligence, requiredComponent: ComponentType.PowerCore, dc: 15);
            var result = SavePerformer.Execute(new SaveExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSave(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.BaseRoll, "Should auto-fail when required component missing");
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
            var result = SavePerformer.Execute(new SaveExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSave(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreNotEqual(0, result.BaseRoll, "Should not auto-fail");

            int expectedIntMod = CharacterStatCalculator.CalculateAttributeModifier(14);
            int expectedHalfLevel = CharacterStatCalculator.CalculateHalfLevelBonus(4);
            Assert.AreEqual(expectedIntMod + expectedHalfLevel, result.TotalModifier,
                $"Expected INT({expectedIntMod}) + HalfLevel({expectedHalfLevel})");
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
            int actual = CharacterStatCalculator.CalculateHalfLevelBonus(level);
            Assert.AreEqual(expectedHalfLevel, actual, $"Level {level} should have half-level {expectedHalfLevel}");
        }
    }
}
