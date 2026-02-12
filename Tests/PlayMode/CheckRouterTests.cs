using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class CheckRouterTests
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

        // ==================== Skill Check Routing ====================

        [UnityTest]
        public IEnumerator RouteSkillCheck_CharacterRequired_ValidComponent_ReturnsOperator()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", dexterity: 16, cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(driver);

            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting, ComponentType.Chassis);
            var result = CheckRouter.RouteSkillCheck(vehicle, spec);
            yield return null;

            Assert.IsTrue(result.CanAttempt, "Should be able to attempt");
            Assert.AreEqual(driver, result.Character, "Should route to Driver");
            Assert.IsNotNull(result.Component, "Should return chassis component");
        }

        [UnityTest]
        public IEnumerator RouteSkillCheck_CharacterRequired_NoComponent_Fails()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(character);

            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);
            var result = CheckRouter.RouteSkillCheck(vehicle, spec);
            yield return null;

            Assert.IsFalse(result.CanAttempt, "Should fail when component missing");
            Assert.IsNotNull(result.FailureReason, "Should have failure reason");
            Assert.IsTrue(result.FailureReason.Contains("Utility"), "Reason should mention Utility");
        }

        [UnityTest]
        public IEnumerator RouteSkillCheck_CharacterRequired_NoSeat_Fails()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithUtility() // No character assigned
                .Build();

            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);
            var result = CheckRouter.RouteSkillCheck(vehicle, spec);
            yield return null;

            Assert.IsFalse(result.CanAttempt, "Should fail when no character in seat");
        }

        [UnityTest]
        public IEnumerator RouteSkillCheck_CharacterRequired_DestroyedComponent_Fails()
        {
            var character = TestCharacterFactory.CreateWithCleanup(cleanup: cleanup);
            vehicle = new TestVehicleBuilder()
                .WithChassis(character)
                .WithUtility(character)
                .Build();

            var utility = vehicle.optionalComponents[0];
            utility.TakeDamage(utility.GetCurrentHealth());

            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);
            var result = CheckRouter.RouteSkillCheck(vehicle, spec);
            yield return null;

            Assert.IsFalse(result.CanAttempt, "Should fail when component destroyed");
        }

        [UnityTest]
        public IEnumerator RouteSkillCheck_InitiatingCharacter_UsedDirectly()
        {
            var alice = TestCharacterFactory.CreateWithCleanup("Alice", intelligence: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(alice, CharacterSkill.Mechanics);
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", intelligence: 8, cleanup: cleanup);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .AddSeat("Seat1", alice)
                .AddSeat("Seat2", bob)
                .Build();

            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics);
            var result = CheckRouter.RouteSkillCheck(vehicle, spec, initiatingCharacter: bob);
            yield return null;

            Assert.IsTrue(result.CanAttempt);
            Assert.AreEqual(bob, result.Character, "Should use Bob (initiating), not Alice (better modifier)");
        }

        [UnityTest]
        public IEnumerator RouteSkillCheck_NullVehicle_Fails()
        {
            var result = CheckRouter.RouteSkillCheck(null, SkillCheckSpec.ForCharacter(CharacterSkill.Piloting));
            yield return null;

            Assert.IsFalse(result.CanAttempt);
            Assert.AreEqual("No vehicle", result.FailureReason);
        }

        // ==================== Save Routing ====================

        [UnityTest]
        public IEnumerator RouteSave_TargetComponent_RoutesToOperator()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", level: 3, dexterity: 16, cleanup: cleanup);
            vehicle = TestVehicleBuilder.CreateWithChassis(driver);

            var spec = SaveSpec.ForCharacter(CharacterAttribute.Dexterity);
            var result = CheckRouter.RouteSave(vehicle, spec, targetComponent: vehicle.chassis);
            yield return null;

            Assert.IsTrue(result.CanAttempt);
            Assert.AreEqual(driver, result.Character, "Should route to Driver (operates chassis)");
        }

        [UnityTest]
        public IEnumerator RouteSave_NoTarget_RoutesToBestModifier()
        {
            var ada = TestCharacterFactory.CreateWithCleanup("Ada", level: 3, wisdom: 10, cleanup: cleanup);
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", level: 3, wisdom: 16, cleanup: cleanup);

            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .AddSeat("Seat1", ada)
                .AddSeat("Seat2", bob)
                .Build();

            var spec = SaveSpec.ForCharacter(CharacterAttribute.Wisdom);
            var result = CheckRouter.RouteSave(vehicle, spec);
            yield return null;

            Assert.IsTrue(result.CanAttempt);
            Assert.AreEqual(bob, result.Character, "Should route to Bob (best WIS)");
        }
    }
}
