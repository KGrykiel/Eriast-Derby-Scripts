using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Tests.Helpers;

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

            vehicle = TestVehicleBuilder.CreateWithChassis(ada);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Piloting, requiredComponent: ComponentType.Chassis);
            var result = SkillCheckPerformer.Execute(vehicle, spec, dc: 12, causalSource: null, initiatingCharacter: ada);
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(ada, result.Character, "Should route to Ada");
            Assert.IsFalse(result.IsAutoFail, "Should not auto-fail");

            int expectedDexMod = CharacterFormulas.CalculateAttributeModifier(16);
            int expectedProf = CharacterFormulas.CalculateProficiencyBonus(3);
            Assert.AreEqual(expectedDexMod + expectedProf, result.Roll.TotalModifier,
                $"Expected DEX({expectedDexMod}) + Prof({expectedProf})");
            Assert.AreEqual(12, result.Roll.TargetValue);
        }

        // ==================== Test 1B ====================

        [UnityTest]
        public IEnumerator Test1B_ComponentRequiredCheck_AutoFail_MissingComponent()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility);
            var result = SkillCheckPerformer.Execute(vehicle, spec, dc: 15, causalSource: null, initiatingCharacter: character);
            yield return null;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAutoFail, "Should auto-fail when component missing");
            Assert.IsFalse(result.Roll.Success);
            Assert.AreEqual(0, result.Roll.BaseRoll, "Base roll should be 0 for auto-fail");
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
            var utility = vehicle.optionalComponents[0];
            utility.TakeDamage(utility.GetCurrentHealth());

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility);
            var result = SkillCheckPerformer.Execute(vehicle, spec, dc: 15, causalSource: null, initiatingCharacter: character);
            yield return null;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAutoFail, "Should auto-fail when component destroyed");
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

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Perception);
            var result = SkillCheckPerformer.Execute(vehicle, spec, dc: 14, causalSource: null, initiatingCharacter: null);
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(bob, result.Character, "Should route to Bob (best Perception)");
            Assert.IsFalse(result.IsAutoFail);

            int bobMod = CharacterFormulas.CalculateSkillCheckModifier(bob, CharacterSkill.Perception);
            Assert.AreEqual(bobMod, result.Roll.TotalModifier, $"Bob's modifier should be {bobMod}");
        }

        // ==================== Test 1E ====================

        [UnityTest]
        public IEnumerator Test1E_ComponentOptionalCheck_NoCharacters_AutoFail()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Perception);
            var result = SkillCheckPerformer.Execute(vehicle, spec, dc: 14, causalSource: null, initiatingCharacter: null);
            yield return null;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsAutoFail, "Should auto-fail with no characters");
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

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics);
            var result = SkillCheckPerformer.Execute(vehicle, spec, dc: 12, causalSource: null, initiatingCharacter: bob);
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(bob, result.Character, "Should use Bob (initiating character), NOT Alice (best modifier)");
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
            int actual = CharacterFormulas.CalculateProficiencyBonus(level);
            Assert.AreEqual(expectedProf, actual, $"Level {level} should have proficiency +{expectedProf}");
        }
    }
}
