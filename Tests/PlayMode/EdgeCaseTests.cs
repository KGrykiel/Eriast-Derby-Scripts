using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Core;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Tests.PlayMode
{
    public class EdgeCaseTests
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

        // ==================== Test 8A ====================

        [Test]
        public void Test8A_NegativeModifiers_CalculateCorrectly()
        {
            // C# integer division truncates toward zero: (3-10)/2 = -7/2 = -3
            int modifier = CharacterStatCalculator.CalculateAttributeModifier(3);
            Assert.AreEqual(-3, modifier, "Attribute 3 should give -3 modifier");

            int modifier8 = CharacterStatCalculator.CalculateAttributeModifier(8);
            Assert.AreEqual(-1, modifier8, "Attribute 8 should give -1 modifier");
        }

        // ==================== Test 8B ====================

        [UnityTest]
        public IEnumerator Test8B_MultipleComponentsSameType_DeterministicSelection()
        {
            var gunner1 = TestCharacterFactory.CreateWithCleanup("Gunner1", cleanup: cleanup);
            var gunner2 = TestCharacterFactory.CreateWithCleanup("Gunner2", cleanup: cleanup);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithWeapon(gunner1, weaponName: "Weapon1")
                .WithWeapon(gunner2, weaponName: "Weapon2")
                .Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Weapon, dc: 10);

            var result1 = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;
            var result2 = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.AreEqual(result1.TotalModifier, result2.TotalModifier, "Should deterministically select same character");
        }

        // ==================== Test 8C ====================

        [UnityTest]
        public IEnumerator Test8C_ComponentNoControllingSeat_AutoFail()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithUtility() // No character assigned
                .Build();

            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility, dc: 10);
            var result = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, Routing = CheckRouter.RouteSkillCheck(vehicle, spec) });
            yield return null;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.BaseRoll, "Should auto-fail when no character controls the component");
        }

        // ==================== Test 8D ====================

        [Test]
        public void Test8D_HighLevel_Level20_ProficiencyCorrect()
        {
            int proficiency = CharacterStatCalculator.CalculateProficiencyBonus(20);
            Assert.AreEqual(6, proficiency, "Level 20 should have +6 proficiency");
        }

        // ==================== Test 8E ====================

        [Test]
        public void Test8E_LowLevel_Level1_BoundaryCorrect()
        {
            int proficiency = CharacterStatCalculator.CalculateProficiencyBonus(1);
            Assert.AreEqual(2, proficiency, "Level 1 should have +2 proficiency");

            int halfLevel = CharacterStatCalculator.CalculateHalfLevelBonus(1);
            Assert.AreEqual(0, halfLevel, "Level 1 should have 0 half-level bonus");
        }
    }
}
