using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes.EntityEffects;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Effects.Targeting;
using Assets.Scripts.Effects.Targeting.EntityTarget;
using Assets.Scripts.Effects.Targeting.VehicleTarget;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Tests.PlayMode
{
    /// <summary>
    /// Comprehensive tests for the targeting refactor:
    /// - ITargetResolver implementations (unit)
    /// - EffectTarget resolution via RollNodeExecutor (integration)
    /// - Fan-out execution via RollNode.targetResolver (integration)
    /// </summary>
    public class TargetingTests
    {
        private readonly List<GameObject> gameObjects = new();
        private readonly List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in gameObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            gameObjects.Clear();
            cleanup.Clear();
        }

        // ==================== HELPERS ====================

        private Vehicle BuildVehicle(string name = "Vehicle", int chassisHealth = 100)
        {
            var vehicle = new TestVehicleBuilder(name)
                .WithChassis(maxHealth: chassisHealth)
                .Build();
            gameObjects.Add(vehicle.gameObject);
            return vehicle;
        }

        private (Stage stage, StageLane lane) BuildStageWithLane(string stageName = "Stage", string laneName = "Lane")
        {
            var stage = TestStageFactory.CreateStage(stageName, out GameObject stageObj);
            gameObjects.Add(stageObj);
            var lane = TestStageFactory.CreateLane(laneName, stage, stageObj);
            return (stage, lane);
        }

        private void PlaceVehicleInLane(Vehicle vehicle, Stage stage, StageLane lane)
        {
            RacePositionTracker.SetStage(vehicle, stage);
            RacePositionTracker.SetLane(vehicle, lane);
        }

        private static DamageEffect CreateFlatDamageEffect(int amount)
        {
            return new DamageEffect
            {
                formulaProvider = new StaticFormulaProvider
                {
                    formula = new DamageFormula
                    {
                        baseDice = 0,
                        dieSize = 6,
                        bonus = amount,
                        damageType = DamageType.Physical
                    }
                }
            };
        }

        private static RollNode CreateVehicleDamageNode(int damage, IVehicleEffectResolver targetResolver)
        {
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(damage), targetResolver = targetResolver }
                }
            };
        }

        private static RollNode CreateEntityDamageNode(int damage, IEntityEffectResolver targetResolver)
        {
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new EntityEffectInvocation { effect = CreateFlatDamageEffect(damage), targetResolver = targetResolver }
                }
            };
        }

        // ================================================================
        //                    RESOLVER UNIT TESTS
        // ================================================================

        // ==================== CurrentTargetResolver ====================

        [UnityTest]
        public IEnumerator CurrentTargetResolver_ReturnsVehicle_WhenTargetIsVehicle()
        {
            var vehicle = BuildVehicle();
            var resolver = new CurrentTargetResolver();
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(vehicle, results[0]);
        }

        [UnityTest]
        public IEnumerator CurrentTargetResolver_ReturnsEntity_WhenTargetIsEntity()
        {
            var vehicle = BuildVehicle();
            Entity chassis = vehicle.Chassis;
            var resolver = new CurrentTargetResolver();
            var ctx = new RollContext { Target = chassis };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(chassis, results[0]);
        }

        [UnityTest]
        public IEnumerator CurrentTargetResolver_ReturnsEmpty_WhenTargetNull()
        {
            var resolver = new CurrentTargetResolver();
            var ctx = new RollContext { Target = null };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(0, results.Count);
        }

        // ==================== AllVehiclesInLaneResolver ====================

        [UnityTest]
        public IEnumerator AllVehiclesInLaneResolver_ReturnsAllVehiclesInLane()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicleA = BuildVehicle("A");
            var vehicleB = BuildVehicle("B");
            var vehicleC = BuildVehicle("C");
            PlaceVehicleInLane(vehicleA, stage, lane);
            PlaceVehicleInLane(vehicleB, stage, lane);
            PlaceVehicleInLane(vehicleC, stage, lane);

            var resolver = new AllVehiclesInLaneResolver();
            var ctx = new RollContext { Target = vehicleA };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Contains(vehicleA));
            Assert.IsTrue(results.Contains(vehicleB));
            Assert.IsTrue(results.Contains(vehicleC));
        }

        [UnityTest]
        public IEnumerator AllVehiclesInLaneResolver_ReturnsEmpty_WhenNoLane()
        {
            var vehicle = BuildVehicle();
            var resolver = new AllVehiclesInLaneResolver();
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(0, results.Count);
        }

        [UnityTest]
        public IEnumerator AllVehiclesInLaneResolver_HandlesStageLaneTarget()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicleA = BuildVehicle("A");
            var vehicleB = BuildVehicle("B");
            PlaceVehicleInLane(vehicleA, stage, lane);
            PlaceVehicleInLane(vehicleB, stage, lane);

            var resolver = new AllVehiclesInLaneResolver();
            var ctx = new RollContext { Target = lane };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(vehicleA));
            Assert.IsTrue(results.Contains(vehicleB));
        }

        [UnityTest]
        public IEnumerator AllVehiclesInLaneResolver_HandlesEntityTarget()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicle = BuildVehicle("A");
            PlaceVehicleInLane(vehicle, stage, lane);

            var resolver = new AllVehiclesInLaneResolver();
            var ctx = new RollContext { Target = vehicle.Chassis };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(vehicle, results[0]);
        }

        // ==================== AllVehiclesInStageResolver ====================

        [UnityTest]
        public IEnumerator AllVehiclesInStageResolver_ReturnsAllVehiclesInStage()
        {
            var (stage, laneA) = BuildStageWithLane("Stage", "LaneA");
            var laneB = TestStageFactory.CreateLane("LaneB", stage, stage.gameObject);

            var vehicleA = BuildVehicle("A");
            var vehicleB = BuildVehicle("B");
            var vehicleC = BuildVehicle("C");
            PlaceVehicleInLane(vehicleA, stage, laneA);
            PlaceVehicleInLane(vehicleB, stage, laneB);
            PlaceVehicleInLane(vehicleC, stage, laneA);

            var resolver = new AllVehiclesInStageResolver();
            var ctx = new RollContext { Target = vehicleA };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Contains(vehicleA));
            Assert.IsTrue(results.Contains(vehicleB));
            Assert.IsTrue(results.Contains(vehicleC));
        }

        [UnityTest]
        public IEnumerator AllVehiclesInStageResolver_ReturnsEmpty_WhenNoStage()
        {
            var vehicle = BuildVehicle();
            var resolver = new AllVehiclesInStageResolver();
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(0, results.Count);
        }

        [UnityTest]
        public IEnumerator AllVehiclesInStageResolver_HandlesEntityTarget()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicle = BuildVehicle("A");
            PlaceVehicleInLane(vehicle, stage, lane);

            var resolver = new AllVehiclesInStageResolver();
            var ctx = new RollContext { SourceActor = null, Target = vehicle.Chassis };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(vehicle, results[0]);
        }

        [UnityTest]
        public IEnumerator AllVehiclesInStageResolver_HandlesVehicleSeatTarget()
        {
            var character = TestCharacterFactory.CreateWithCleanup("Gunner", cleanup: cleanup);
            var vehicle = new TestVehicleBuilder("V")
                .WithChassis()
                .WithWeapon(character)
                .Build();
            gameObjects.Add(vehicle.gameObject);

            var (stage, lane) = BuildStageWithLane();
            PlaceVehicleInLane(vehicle, stage, lane);

            VehicleSeat seat = vehicle.seats.First(s => s.seatName == "Gunner");
            var resolver = new AllVehiclesInStageResolver();
            var ctx = new RollContext { SourceActor = null, Target = seat };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(vehicle, results[0]);
        }

        // ==================== SpecificLaneResolver ====================

        [UnityTest]
        public IEnumerator SpecificLaneResolver_ReturnsConfiguredLane()
        {
            var (_, lane) = BuildStageWithLane();
            var resolver = new SpecificLaneResolver(lane);
            var ctx = new RollContext();

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(lane, results[0]);
        }

        [UnityTest]
        public IEnumerator SpecificLaneResolver_ReturnsEmpty_WhenLaneNull()
        {
            var resolver = new SpecificLaneResolver(null);
            var ctx = new RollContext();

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(0, results.Count);
        }

        // ==================== SeatByRoleResolver ====================

        [UnityTest]
        public IEnumerator SeatByRoleResolver_ReturnsGunnerSeat()
        {
            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", cleanup: cleanup);
            var vehicle = new TestVehicleBuilder("V")
                .WithChassis()
                .WithWeapon(gunner)
                .Build();
            gameObjects.Add(vehicle.gameObject);

            var resolver = new SeatByRoleResolver(RoleType.Gunner, SeatSource.TargetVehicle);
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count);
            VehicleSeat gunnerSeat = vehicle.seats.First(s => s.seatName == "Gunner");
            Assert.AreEqual(gunnerSeat, results[0]);
        }

        [UnityTest]
        public IEnumerator SeatByRoleResolver_ReturnsEmpty_WhenNoMatchingRole()
        {
            var vehicle = BuildVehicle();

            var resolver = new SeatByRoleResolver(RoleType.Gunner, SeatSource.TargetVehicle);
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(0, results.Count, "No gunner seat should exist on a chassis-only vehicle");
        }

        [UnityTest]
        public IEnumerator SeatByRoleResolver_SourceVehicle_FallsBackToTarget_WhenSourceActorNull()
        {
            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", cleanup: cleanup);
            var vehicle = new TestVehicleBuilder("V")
                .WithChassis()
                .WithWeapon(gunner)
                .Build();
            gameObjects.Add(vehicle.gameObject);

            var resolver = new SeatByRoleResolver(RoleType.Gunner, SeatSource.SourceVehicle);
            var ctx = new RollContext { SourceActor = null, Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count, "Should fall back to Target when SourceActor is null");
        }

        // ================================================================
        //           EFFECT TARGET RESOLUTION (integration)
        // ================================================================

        [UnityTest]
        public IEnumerator EffectTarget_SelectedTarget_DamagesTargetVehicle()
        {
            var vehicle = BuildVehicle("Target", chassisHealth: 100);
            var node = CreateVehicleDamageNode(10, new TargetVehicleResolver());
            var ctx = new RollContext { Target = vehicle, CausalSource = "Test" };

            int hpBefore = vehicle.Chassis.GetCurrentHealth();
            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(hpBefore - 10, vehicle.Chassis.GetCurrentHealth());
        }

        [UnityTest]
        public IEnumerator EffectTarget_SourceVehicle_DamagesSourceVehicle()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", cleanup: cleanup);
            var source = new TestVehicleBuilder("Source")
                .WithChassis(driver, maxHealth: 100)
                .Build();
            gameObjects.Add(source.gameObject);
            var target = BuildVehicle("Target", chassisHealth: 100);

            var seat = source.seats[0];
            var actor = new CharacterActor(seat);
            var node = CreateVehicleDamageNode(15, new SourceVehicleResolver());
            var ctx = new RollContext { SourceActor = actor, Target = target, CausalSource = "Test" };

            int sourceHpBefore = source.Chassis.GetCurrentHealth();
            int targetHpBefore = target.Chassis.GetCurrentHealth();
            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(sourceHpBefore - 15, source.Chassis.GetCurrentHealth(), "Source should take damage");
            Assert.AreEqual(targetHpBefore, target.Chassis.GetCurrentHealth(), "Target should be unaffected");
        }

        [UnityTest]
        public IEnumerator EffectTarget_TargetVehicle_DamagesTargetVehicle()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", cleanup: cleanup);
            var source = new TestVehicleBuilder("Source")
                .WithChassis(driver, maxHealth: 100)
                .Build();
            gameObjects.Add(source.gameObject);
            var target = BuildVehicle("Target", chassisHealth: 100);

            var seat = source.seats[0];
            var actor = new CharacterActor(seat);
            var node = CreateVehicleDamageNode(20, new TargetVehicleResolver());
            var ctx = new RollContext { SourceActor = actor, Target = target, CausalSource = "Test" };

            int targetHpBefore = target.Chassis.GetCurrentHealth();
            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(targetHpBefore - 20, target.Chassis.GetCurrentHealth());
        }

        [UnityTest]
        public IEnumerator EffectTarget_AllVehiclesInTargetLane_DamagesAllInLane()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicleA = BuildVehicle("A", chassisHealth: 100);
            var vehicleB = BuildVehicle("B", chassisHealth: 100);
            var vehicleC = BuildVehicle("C", chassisHealth: 100);
            PlaceVehicleInLane(vehicleA, stage, lane);
            PlaceVehicleInLane(vehicleB, stage, lane);
            PlaceVehicleInLane(vehicleC, stage, lane);

            var node = CreateVehicleDamageNode(10, new AllVehiclesInLaneEffectResolver());
            var ctx = new RollContext { Target = vehicleA, CausalSource = "Test" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(90, vehicleA.Chassis.GetCurrentHealth(), "Vehicle A should take 10 damage");
            Assert.AreEqual(90, vehicleB.Chassis.GetCurrentHealth(), "Vehicle B should take 10 damage");
            Assert.AreEqual(90, vehicleC.Chassis.GetCurrentHealth(), "Vehicle C should take 10 damage");
        }

        [UnityTest]
        public IEnumerator EffectTarget_AllOtherVehiclesInTargetLane_ExcludesSelf()
        {
            var (stage, lane) = BuildStageWithLane();
            var self = BuildVehicle("Self", chassisHealth: 100);
            var other = BuildVehicle("Other", chassisHealth: 100);
            PlaceVehicleInLane(self, stage, lane);
            PlaceVehicleInLane(other, stage, lane);

            var node = CreateVehicleDamageNode(10, new AllVehiclesInLaneEffectResolver { ExcludeSelf = true });
            var ctx = new RollContext { Target = self, CausalSource = "Test" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(100, self.Chassis.GetCurrentHealth(), "Self should NOT take damage");
            Assert.AreEqual(90, other.Chassis.GetCurrentHealth(), "Other should take 10 damage");
        }

        [UnityTest]
        public IEnumerator EffectTarget_AllOtherVehiclesInStage_ExcludesSelf()
        {
            var (stage, laneA) = BuildStageWithLane("Stage", "LaneA");
            var laneB = TestStageFactory.CreateLane("LaneB", stage, stage.gameObject);

            var self = BuildVehicle("Self", chassisHealth: 100);
            var otherSameLane = BuildVehicle("OtherSameLane", chassisHealth: 100);
            var otherDiffLane = BuildVehicle("OtherDiffLane", chassisHealth: 100);
            PlaceVehicleInLane(self, stage, laneA);
            PlaceVehicleInLane(otherSameLane, stage, laneA);
            PlaceVehicleInLane(otherDiffLane, stage, laneB);

            var node = CreateVehicleDamageNode(10, new AllVehiclesInStageEffectResolver { ExcludeSelf = true });
            var ctx = new RollContext { Target = self, CausalSource = "Test" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(100, self.Chassis.GetCurrentHealth(), "Self should NOT take damage");
            Assert.AreEqual(90, otherSameLane.Chassis.GetCurrentHealth(), "Other in same lane should take damage");
            Assert.AreEqual(90, otherDiffLane.Chassis.GetCurrentHealth(), "Other in different lane should take damage");
        }

        [UnityTest]
        public IEnumerator EffectTarget_AllComponentsOnTarget_DamagesAllComponents()
        {
            var vehicle = new TestVehicleBuilder("V")
                .WithChassis(maxHealth: 100)
                .WithWeapon(weaponName: "Gun")
                .WithPowerCore()
                .Build();
            gameObjects.Add(vehicle.gameObject);

            var node = CreateEntityDamageNode(5, new ComponentsOnVehicleResolver());
            var ctx = new RollContext { Target = vehicle, CausalSource = "Test" };

            int chassisHp = vehicle.Chassis.GetCurrentHealth();
            int weaponHp = vehicle.AllComponents.First(c => c is WeaponComponent).GetCurrentHealth();
            int powerCoreHp = vehicle.PowerCore.GetCurrentHealth();

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(chassisHp - 5, vehicle.Chassis.GetCurrentHealth(), "Chassis should take 5 damage");
            Assert.AreEqual(weaponHp - 5,
                vehicle.AllComponents.First(c => c is WeaponComponent).GetCurrentHealth(),
                "Weapon should take 5 damage");
            Assert.AreEqual(powerCoreHp - 5, vehicle.PowerCore.GetCurrentHealth(), "Power core should take 5 damage");
        }

        [UnityTest]
        public IEnumerator EffectTarget_SourceComponent_DamagesOnlySourceComponent()
        {
            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", cleanup: cleanup);
            var vehicle = new TestVehicleBuilder("V")
                .WithChassis(maxHealth: 100)
                .WithWeapon(gunner, weaponName: "Gun")
                .Build();
            gameObjects.Add(vehicle.gameObject);

            VehicleComponent weapon = vehicle.AllComponents.First(c => c is WeaponComponent);
            var actor = new ComponentActor(weapon);
            var node = CreateEntityDamageNode(8, new SourceComponentResolver());
            var ctx = new RollContext { SourceActor = actor, Target = vehicle, CausalSource = "Test" };

            int weaponHp = weapon.GetCurrentHealth();
            int chassisHp = vehicle.Chassis.GetCurrentHealth();

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(weaponHp - 8, weapon.GetCurrentHealth(), "Weapon should take damage");
            Assert.AreEqual(chassisHp, vehicle.Chassis.GetCurrentHealth(), "Chassis should be unaffected");
        }

        // ================================================================
        //          MIXED EFFECT TARGETS IN SINGLE NODE
        // ================================================================

        [UnityTest]
        public IEnumerator MixedTargets_SelfDamageAndLaneDamage_YouExplodeScenario()
        {
            var (stage, lane) = BuildStageWithLane();
            var exploder = BuildVehicle("Exploder", chassisHealth: 100);
            var bystander = BuildVehicle("Bystander", chassisHealth: 100);
            PlaceVehicleInLane(exploder, stage, lane);
            PlaceVehicleInLane(bystander, stage, lane);

            var node = new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(10), targetResolver = new AllVehiclesInLaneEffectResolver { ExcludeSelf = true } },
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(20), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { Target = exploder, CausalSource = "You Explode!" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(80, exploder.Chassis.GetCurrentHealth(),
                "Exploder takes 20 self-damage only (excluded from AllOther)");
            Assert.AreEqual(90, bystander.Chassis.GetCurrentHealth(),
                "Bystander takes 10 lane damage only");
        }

        [UnityTest]
        public IEnumerator MixedTargets_SourceVehicleHealAndTargetDamage()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", cleanup: cleanup);
            var source = new TestVehicleBuilder("Source")
                .WithChassis(driver, maxHealth: 100)
                .Build();
            gameObjects.Add(source.gameObject);
            var target = BuildVehicle("Target", chassisHealth: 100);

            var seat = source.seats[0];
            var actor = new CharacterActor(seat);

            var node = new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(25), targetResolver = new TargetVehicleResolver() },
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(5), targetResolver = new SourceVehicleResolver() }
                }
            };
            var ctx = new RollContext { SourceActor = actor, Target = target, CausalSource = "Test" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(75, target.Chassis.GetCurrentHealth(), "Target should take 25 damage");
            Assert.AreEqual(95, source.Chassis.GetCurrentHealth(), "Source should take 5 recoil damage");
        }

        // ================================================================
        //             FAN-OUT INTEGRATION TESTS
        // ================================================================

        [UnityTest]
        public IEnumerator FanOut_AllVehiclesInLane_UnconditionalDamage_DamagesAll()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicleA = BuildVehicle("A", chassisHealth: 100);
            var vehicleB = BuildVehicle("B", chassisHealth: 100);
            PlaceVehicleInLane(vehicleA, stage, lane);
            PlaceVehicleInLane(vehicleB, stage, lane);

            var node = new RollNode
            {
                targetResolver = new AllVehiclesInLaneResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(10), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { Target = vehicleA, CausalSource = "LaneHazard" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(90, vehicleA.Chassis.GetCurrentHealth(), "Vehicle A should take 10 damage");
            Assert.AreEqual(90, vehicleB.Chassis.GetCurrentHealth(), "Vehicle B should take 10 damage");
        }

        [UnityTest]
        public IEnumerator FanOut_AllVehiclesInStage_UnconditionalDamage_DamagesAll()
        {
            var (stage, laneA) = BuildStageWithLane("Stage", "LaneA");
            var laneB = TestStageFactory.CreateLane("LaneB", stage, stage.gameObject);

            var vehicleA = BuildVehicle("A", chassisHealth: 100);
            var vehicleB = BuildVehicle("B", chassisHealth: 100);
            PlaceVehicleInLane(vehicleA, stage, laneA);
            PlaceVehicleInLane(vehicleB, stage, laneB);

            var node = new RollNode
            {
                targetResolver = new AllVehiclesInStageResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(15), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { Target = vehicleA, CausalSource = "StageQuake" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(85, vehicleA.Chassis.GetCurrentHealth(), "Vehicle A should take 15 damage");
            Assert.AreEqual(85, vehicleB.Chassis.GetCurrentHealth(), "Vehicle B should take 15 damage");
        }

        [UnityTest]
        public IEnumerator FanOut_NullSourceActor_StillResolvesFromTarget()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicleA = BuildVehicle("A", chassisHealth: 100);
            var vehicleB = BuildVehicle("B", chassisHealth: 100);
            PlaceVehicleInLane(vehicleA, stage, lane);
            PlaceVehicleInLane(vehicleB, stage, lane);

            var node = new RollNode
            {
                targetResolver = new AllVehiclesInLaneResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(7), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { SourceActor = null, Target = vehicleA, CausalSource = "EventCard" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(93, vehicleA.Chassis.GetCurrentHealth(), "A should take damage with null SourceActor");
            Assert.AreEqual(93, vehicleB.Chassis.GetCurrentHealth(), "B should take damage with null SourceActor");
        }

        [UnityTest]
        public IEnumerator FanOut_EmptyLane_ExecutesNothing()
        {
            var (stage, lane) = BuildStageWithLane();

            var triggerVehicle = BuildVehicle("Trigger", chassisHealth: 100);
            RacePositionTracker.SetStage(triggerVehicle, stage);
            RacePositionTracker.SetLane(triggerVehicle, lane);

            var node = new RollNode
            {
                targetResolver = new AllVehiclesInLaneResolver(excludeTarget: true),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(99), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { Target = triggerVehicle, CausalSource = "EmptyLane" };

            bool result = RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.IsFalse(result, "Should return false when no targets resolved (anySuccess stays false)");
            Assert.AreEqual(100, triggerVehicle.Chassis.GetCurrentHealth(),
                "Trigger vehicle should be unaffected (excluded as the initiator)");
        }

        [UnityTest]
        public IEnumerator FanOut_SeatByRole_ResolvesPerSeat()
        {
            var gunnerA = TestCharacterFactory.CreateWithCleanup("GunnerA", cleanup: cleanup);
            var gunnerB = TestCharacterFactory.CreateWithCleanup("GunnerB", cleanup: cleanup);
            var vehicle = new TestVehicleBuilder("V")
                .WithChassis()
                .WithWeapon(gunnerA, weaponName: "WeaponA")
                .WithWeapon(gunnerB, weaponName: "WeaponB")
                .Build();
            gameObjects.Add(vehicle.gameObject);

            var resolver = new SeatByRoleResolver(RoleType.Gunner, SeatSource.TargetVehicle);
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(2, results.Count, "Should resolve both gunner seats");
            Assert.IsTrue(results.All(r => r is VehicleSeat), "All results should be VehicleSeat");
        }

        [UnityTest]
        public IEnumerator FanOut_WithComponentActor_PreservesSourceAcrossFanOut()
        {
            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", cleanup: cleanup);
            var sourceVehicle = new TestVehicleBuilder("Source")
                .WithChassis(maxHealth: 100)
                .WithWeapon(gunner, weaponName: "Cannon")
                .Build();
            gameObjects.Add(sourceVehicle.gameObject);

            var (stage, lane) = BuildStageWithLane();
            var targetA = BuildVehicle("TargetA", chassisHealth: 100);
            var targetB = BuildVehicle("TargetB", chassisHealth: 100);
            PlaceVehicleInLane(targetA, stage, lane);
            PlaceVehicleInLane(targetB, stage, lane);

            VehicleComponent weapon = sourceVehicle.AllComponents.First(c => c is WeaponComponent);
            var actor = new ComponentActor(weapon);

            var node = new RollNode
            {
                targetResolver = new AllVehiclesInLaneResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(10), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { SourceActor = actor, Target = targetA, CausalSource = "ScatterShot" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(90, targetA.Chassis.GetCurrentHealth(), "Target A should take damage");
            Assert.AreEqual(90, targetB.Chassis.GetCurrentHealth(), "Target B should take damage");
            Assert.AreEqual(100, sourceVehicle.Chassis.GetCurrentHealth(), "Source should be unaffected");
        }

        // ================================================================
        //            NULL RESOLVER = SINGLE EXECUTION
        // ================================================================

        [UnityTest]
        public IEnumerator NullResolver_ReturnsFalseAndSkipsExecution()
        {
            var vehicle = BuildVehicle("Target", chassisHealth: 100);
            var node = new RollNode
            {
                targetResolver = null,
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(10), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { Target = vehicle, CausalSource = "Test" };

            bool result = RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.IsFalse(result, "Null resolver should return false");
            Assert.AreEqual(100, vehicle.Chassis.GetCurrentHealth(), "No effects should apply when resolver is null");
        }

        // ================================================================
        //           EVENT CARD CONTEXT (null SourceActor)
        // ================================================================

        [UnityTest]
        public IEnumerator EventCardContext_NullSourceActor_SelectedTarget_Works()
        {
            var vehicle = BuildVehicle("EventTarget", chassisHealth: 100);
            var node = CreateVehicleDamageNode(12, new TargetVehicleResolver());
            var ctx = new RollContext { SourceActor = null, Target = vehicle, CausalSource = "EventCard" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(88, vehicle.Chassis.GetCurrentHealth());
        }

        [UnityTest]
        public IEnumerator EventCardContext_NullSourceActor_AllOtherVehiclesInLane_Works()
        {
            var (stage, lane) = BuildStageWithLane();
            var self = BuildVehicle("Self", chassisHealth: 100);
            var other = BuildVehicle("Other", chassisHealth: 100);
            PlaceVehicleInLane(self, stage, lane);
            PlaceVehicleInLane(other, stage, lane);

            var node = CreateVehicleDamageNode(10, new AllVehiclesInLaneEffectResolver { ExcludeSelf = true });
            var ctx = new RollContext { SourceActor = null, Target = self, CausalSource = "EventCard" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(100, self.Chassis.GetCurrentHealth(), "Self excluded with null SourceActor");
            Assert.AreEqual(90, other.Chassis.GetCurrentHealth(), "Other takes damage with null SourceActor");
        }

        // ================================================================
        //       EDGE CASES: DESTROYED VEHICLES, EMPTY COLLECTIONS
        // ================================================================

        [UnityTest]
        public IEnumerator AllVehiclesInLane_SkipsNullEntries()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicle = BuildVehicle("A", chassisHealth: 100);
            PlaceVehicleInLane(vehicle, stage, lane);

            var resolver = new AllVehiclesInLaneResolver();
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count, "Should skip null entries in vehiclesInLane");
        }

        [UnityTest]
        public IEnumerator AllVehiclesInStage_SkipsNullEntries()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicle = BuildVehicle("A", chassisHealth: 100);
            PlaceVehicleInLane(vehicle, stage, lane);

            var resolver = new AllVehiclesInStageResolver();
            var ctx = new RollContext { Target = vehicle };

            var results = resolver.ResolveFrom(ctx);
            yield return null;

            Assert.AreEqual(1, results.Count, "Should skip null entries in vehiclesInStage");
        }

        [UnityTest]
        public IEnumerator FanOut_AllVehiclesInLane_StageLaneTarget_ResolvesVehicles()
        {
            var (stage, lane) = BuildStageWithLane();
            var vehicleA = BuildVehicle("A", chassisHealth: 100);
            var vehicleB = BuildVehicle("B", chassisHealth: 100);
            PlaceVehicleInLane(vehicleA, stage, lane);
            PlaceVehicleInLane(vehicleB, stage, lane);

            var node = new RollNode
            {
                targetResolver = new AllVehiclesInLaneResolver(),
                successEffects = new List<IEffectInvocation>
                {
                    new VehicleEffectInvocation { effect = CreateFlatDamageEffect(10), targetResolver = new TargetVehicleResolver() }
                }
            };
            var ctx = new RollContext { Target = lane, CausalSource = "Bombardment" };

            RollNodeExecutor.Execute(node, ctx);
            yield return null;

            Assert.AreEqual(90, vehicleA.Chassis.GetCurrentHealth(), "A should take damage from StageLane target");
            Assert.AreEqual(90, vehicleB.Chassis.GetCurrentHealth(), "B should take damage from StageLane target");
        }

        [UnityTest]
        public IEnumerator NullNode_ReturnsTrue()
        {
            var ctx = new RollContext { CausalSource = "NullNode" };

            bool result = RollNodeExecutor.Execute(null, ctx);
            yield return null;

            Assert.IsTrue(result, "Null node should be treated as unconditional success");
        }
    }
}
