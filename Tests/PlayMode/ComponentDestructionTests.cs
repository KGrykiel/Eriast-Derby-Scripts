using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Combat.Rolls.RollTypes.SkillChecks;

namespace Assets.Scripts.Tests.PlayMode
{
    public class ComponentDestructionTests
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

        // ==================== Test 7A ====================

        [UnityTest]
        public IEnumerator Test7A_RequiredComponentDestroyed_AutoFail()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var utility = vehicle.optionalComponents[0];

            // First attempt - should work
            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility, dc: 10);
            var resultBefore = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, InitiatingCharacter = character });
            yield return null;
            Assert.AreNotEqual(0, resultBefore.BaseRoll, "Should succeed before destruction");

            // Destroy the utility component via TakeDamage
            utility.TakeDamage(utility.GetCurrentHealth());
            Assert.IsTrue(utility.isDestroyed, "Utility should be destroyed");

            // Second attempt - should auto-fail
            var resultAfter = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, InitiatingCharacter = character });
            yield return null;
            Assert.AreEqual(0, resultAfter.BaseRoll, "Should auto-fail after component destroyed");
        }

        // ==================== Test 7B ====================

        [UnityTest]
        public IEnumerator Test7B_ComponentRestored_WorksAgain()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var utility = vehicle.optionalComponents[0];
            var spec = TestSkillFactory.CharacterSkillCheck(CharacterSkill.Mechanics, requiredComponent: ComponentType.Utility, dc: 10);

            // Destroy
            utility.TakeDamage(utility.GetCurrentHealth());
            Assert.IsTrue(utility.isDestroyed, "Should be destroyed");

            var resultDestroyed = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, InitiatingCharacter = character });
            yield return null;
            Assert.AreEqual(0, resultDestroyed.BaseRoll, "Should auto-fail when destroyed");

            // Restore - reset destroyed state and heal
            utility.isDestroyed = false;
            utility.SetHealth(utility.GetMaxHealth());

            bool isOperational = utility.IsOperational;
            Assert.IsTrue(isOperational, "Should be operational after restoration");

            var resultRestored = SkillCheckPerformer.Execute(new SkillCheckExecutionContext { Vehicle = vehicle, Spec = spec, CausalSource = null, InitiatingCharacter = character });
            yield return null;
            Assert.AreNotEqual(0, resultRestored.BaseRoll, "Should work after component restored");
        }
    }
}
