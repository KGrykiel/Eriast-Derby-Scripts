using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Core;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Tests.PlayMode
{
    public class CharacterCheckTests
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

        // ==================== Test 1A ====================

        [UnityTest]
        public IEnumerator Test1A_ComponentRequiredCheck_RoutesToOperator()
        {
            var ada = TestCharacterFactory.CreateWithCleanup("Ada", level: 3, dexterity: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(ada, CharacterSkill.Piloting);

            vehicle = new TestVehicleBuilder().WithChassis().WithDrive(ada).Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredRole: RoleType.Driver, dc: 12);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreNotEqual(0, result.BaseRoll, "Should not auto-fail");

            int expectedDexMod = CharacterStatCalculator.CalculateAttributeModifier(16);
            int expectedProf = CharacterStatCalculator.CalculateProficiencyBonus(3);
            Assert.AreEqual(expectedDexMod + expectedProf, result.TotalModifier,
                $"Expected DEX({expectedDexMod}) + Prof({expectedProf})");
            Assert.AreEqual(12, result.TargetValue);
        }

        // ==================== Test 1B ====================

        [UnityTest]
        public IEnumerator Test1B_ComponentRequiredCheck_AutoFail_MissingComponent()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredRole: RoleType.Technician, dc: 15);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.BaseRoll, "Base roll should be 0 for auto-fail");
            Assert.IsFalse(result.Success);
        }

        // ==================== Test 1C ====================

        [UnityTest]
        public IEnumerator Test1C_ComponentRequiredCheck_DestroyedComponent()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            // Destroy the utility component
            var utility = vehicle.GetComponentOfRole(RoleType.Technician);
            utility.TakeDamage(utility.GetCurrentHealth());

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredRole: RoleType.Technician, dc: 15);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.BaseRoll, "Should auto-fail when component destroyed");
        }

        // ==================== Test 1D ====================

        [UnityTest]
        public IEnumerator Test1D_ComponentOptionalCheck_RoutesToBestCharacter()
        {
            var ada = TestCharacterFactory.CreateWithCleanup("Ada", level: 3, wisdom: 10, cleanup: cleanup);
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", level: 3, wisdom: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(bob, CharacterSkill.Perception);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .AddSeat("Seat1", ada)
                .AddSeat("Seat2", bob)
                .Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Perception, dc: 14);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreNotEqual(0, result.BaseRoll, "Should not auto-fail");

            int expectedWisMod = CharacterStatCalculator.CalculateAttributeModifier(16);
            int expectedProf = CharacterStatCalculator.CalculateProficiencyBonus(3);
            Assert.AreEqual(expectedWisMod + expectedProf, result.TotalModifier,
                $"Expected WIS({expectedWisMod}) + Prof({expectedProf})");
        }

        // ==================== Test 1E ====================

        [UnityTest]
        public IEnumerator Test1E_ComponentOptionalCheck_NoCharacters_AutoFail()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Perception, dc: 14);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.BaseRoll, "Should auto-fail with no characters");
        }

        // ==================== Test 1F ====================

        [UnityTest]
        public IEnumerator Test1F_CharacterInitiatedSkill_UsesCorrectCharacter()
        {
            var alice = TestCharacterFactory.CreateWithCleanup("Alice", level: 5, intelligence: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(alice, CharacterSkill.Mechanics);

            var bob = TestCharacterFactory.CreateWithCleanup("Bob", level: 1, intelligence: 10, cleanup: cleanup);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithWeapon(alice)
                .WithWeapon(bob)
                .Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, dc: 12);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec, new CharacterActor(vehicle.GetSeatForCharacter(bob))) });
            yield return null;

            Assert.IsNotNull(result);
            int expectedIntMod = CharacterStatCalculator.CalculateAttributeModifier(10);
            Assert.AreEqual(expectedIntMod, result.TotalModifier,
                "Should use Bob's modifier (initiating character), NOT Alice's (best modifier)");
        }

        // ==================== Test 1G ====================

        [Test]
        [TestCase(1, 2)]
        [TestCase(5, 3)]
        [TestCase(9, 4)]
        [TestCase(13, 5)]
        [TestCase(17, 6)]
        [TestCase(20, 6)]
        public void Test1G_ProficiencyBonus_AllLevels(int level, int expectedProf)
        {
            int actual = CharacterStatCalculator.CalculateProficiencyBonus(level);
            Assert.AreEqual(expectedProf, actual, $"Level {level} should have proficiency +{expectedProf}");
        }
    }
}
