using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Combat;
using Assets.Scripts.Consumables;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes;
using Assets.Scripts.Skills;
using Assets.Scripts.Skills.Costs;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects.EffectTypes.VehicleEffects;

namespace Assets.Scripts.Tests.PlayMode
{
    internal class ConsumableTests
    {
        private readonly List<Object> cleanup = new();
        private Vehicle vehicle;

        [TearDown]
        public void TearDown()
        {
            if (vehicle != null) Object.DestroyImmediate(vehicle.gameObject);
            vehicle = null;

            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();

            CombatEventBus.Clear();
        }

        // ==================== HELPERS ====================

        private CombatConsumable CreateCombatConsumable(string name = "TestGrenade", int bulkPerCharge = 1)
        {
            var c = ScriptableObject.CreateInstance<CombatConsumable>();
            c.name = name;
            c.bulkPerCharge = bulkPerCharge;
            cleanup.Add(c);
            return c;
        }

        private UtilityConsumable CreateUtilityConsumable(string name = "TestPotion", int bulkPerCharge = 1)
        {
            var c = ScriptableObject.CreateInstance<UtilityConsumable>();
            c.name = name;
            c.bulkPerCharge = bulkPerCharge;
            cleanup.Add(c);
            return c;
        }

        private AmmunitionType CreateAmmo(string name = "TestAmmo", int bulkPerCharge = 1)
        {
            var a = ScriptableObject.CreateInstance<AmmunitionType>();
            a.name = name;
            a.bulkPerCharge = bulkPerCharge;
            cleanup.Add(a);
            return a;
        }

        private Skill CreateConsumableGatedSkill(ConsumableBase required)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = "GatedSkill";
            skill.costs.Add(new ConsumableCost { template = required });
            skill.rollNode = new RollNode();
            cleanup.Add(skill);
            return skill;
        }

        private void AttachSkill(Consumable consumable, ActionType actionCost)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = consumable.name;
            skill.actionCost = actionCost;
            skill.rollNode = new RollNode();
            skill.costs.Add(new ConsumableCost { template = consumable });
            cleanup.Add(skill);
            consumable.skill = skill;
        }

        private Vehicle BuildVehicleWithInventory(
            int cargoCapacity = 100,
            params ConsumableStack[] stacks)
        {
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("InvVehicle")
                .WithChassis(driver)
                .Build();

            vehicle.Chassis.baseCargoCapacity = cargoCapacity;

            foreach (var stack in stacks)
                vehicle.inventory.Add(stack);

            return vehicle;
        }

        // ==================== HasChargesFor ====================

        [Test]
        public void HasChargesFor_MatchingTemplateWithCharges_ReturnsTrue()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 3 });

            bool result = vehicle.HasChargesFor(grenade);

            Assert.IsTrue(result);
        }

        [Test]
        public void HasChargesFor_NoMatchingTemplate_ReturnsFalse()
        {
            var grenade = CreateCombatConsumable("Grenade");
            var potion = CreateUtilityConsumable("Potion");
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 2 });

            bool result = vehicle.HasChargesFor(potion);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasChargesFor_EmptyInventory_ReturnsFalse()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100);

            bool result = vehicle.HasChargesFor(grenade);

            Assert.IsFalse(result);
        }

        // ==================== TrySpendConsumable ====================

        [Test]
        public void TrySpend_DecreasesChargesByOne()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 3 });

            bool spent = vehicle.TrySpendConsumable(grenade);

            Assert.IsTrue(spent);
            Assert.AreEqual(2, vehicle.inventory[0].charges);
        }

        [Test]
        public void TrySpend_LastCharge_RemovesStackFromInventory()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 1 });

            bool spent = vehicle.TrySpendConsumable(grenade);

            Assert.IsTrue(spent);
            Assert.AreEqual(0, vehicle.inventory.Count, "Stack should be removed when charges hit 0");
        }

        [Test]
        public void TrySpend_NoMatchingStack_ReturnsFalse()
        {
            var grenade = CreateCombatConsumable("Grenade");
            var potion = CreateUtilityConsumable("Potion");
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 5 });

            bool spent = vehicle.TrySpendConsumable(potion);

            Assert.IsFalse(spent);
        }

        [Test]
        public void TrySpend_NullTemplate_ReturnsFalse()
        {
            BuildVehicleWithInventory(100);

            bool spent = vehicle.TrySpendConsumable(null);

            Assert.IsFalse(spent);
        }

        [Test]
        public void TrySpend_MultipleStacks_OnlyDecrementsFirstMatch()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100,
                new ConsumableStack { template = grenade, charges = 1 },
                new ConsumableStack { template = grenade, charges = 3 });

            vehicle.TrySpendConsumable(grenade);

            Assert.AreEqual(1, vehicle.inventory.Count, "First stack removed, second remains");
            Assert.AreEqual(3, vehicle.inventory[0].charges, "Second stack untouched");
        }

        // ==================== RestoreConsumable ====================

        [Test]
        public void Restore_ExistingStack_IncreasesCharges()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 2 });

            vehicle.RestoreConsumable(grenade, 3);

            Assert.AreEqual(5, vehicle.inventory[0].charges);
        }

        [Test]
        public void Restore_NoExistingStack_CreatesNewStack()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100);

            vehicle.RestoreConsumable(grenade, 4);

            Assert.AreEqual(1, vehicle.inventory.Count);
            Assert.AreEqual(grenade, vehicle.inventory[0].template);
            Assert.AreEqual(4, vehicle.inventory[0].charges);
        }

        [Test]
        public void Restore_NullTemplate_DoesNothing()
        {
            BuildVehicleWithInventory(100);

            vehicle.RestoreConsumable(null, 5);

            Assert.AreEqual(0, vehicle.inventory.Count);
        }

        [Test]
        public void Restore_ZeroAmount_DoesNothing()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 2 });

            vehicle.RestoreConsumable(grenade, 0);

            Assert.AreEqual(2, vehicle.inventory[0].charges, "Charges unchanged for zero amount");
        }

        [Test]
        public void Restore_NegativeAmount_DoesNothing()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 2 });

            vehicle.RestoreConsumable(grenade, -3);

            Assert.AreEqual(2, vehicle.inventory[0].charges, "Charges unchanged for negative amount");
        }

        // ==================== CARGO CAPACITY CONSTRAINTS ====================

        [Test]
        public void Restore_CappedByAvailableCapacity()
        {
            var grenade = CreateCombatConsumable(bulkPerCharge: 2);
            BuildVehicleWithInventory(10, new ConsumableStack { template = grenade, charges = 3 });
            // Used: 3*2 = 6, Free: 10-6 = 4, Can add: 4/2 = 2

            vehicle.RestoreConsumable(grenade, 5);

            Assert.AreEqual(5, vehicle.inventory[0].charges, "3 + 2 capped by cargo");
        }

        [Test]
        public void Restore_NoFreeCapacity_DoesNothing()
        {
            var grenade = CreateCombatConsumable(bulkPerCharge: 5);
            BuildVehicleWithInventory(10, new ConsumableStack { template = grenade, charges = 2 });
            // Used: 2*5 = 10, Free: 0

            vehicle.RestoreConsumable(grenade, 3);

            Assert.AreEqual(2, vehicle.inventory[0].charges, "No free capacity means no restoration");
        }

        [Test]
        public void Restore_ZeroBulkPerCharge_IgnoresCapacity()
        {
            var freeItem = CreateCombatConsumable(bulkPerCharge: 0);
            BuildVehicleWithInventory(0);

            vehicle.RestoreConsumable(freeItem, 10);

            Assert.AreEqual(1, vehicle.inventory.Count);
            Assert.AreEqual(10, vehicle.inventory[0].charges, "Zero bulk ignores capacity");
        }

        [Test]
        public void Restore_NewStack_CappedByCapacity()
        {
            var grenade = CreateCombatConsumable(bulkPerCharge: 3);
            BuildVehicleWithInventory(9);
            // Free: 9, Can add: 9/3 = 3

            vehicle.RestoreConsumable(grenade, 5);

            Assert.AreEqual(3, vehicle.inventory[0].charges, "New stack capped at 3 by cargo capacity");
        }

        // ==================== TrimInventoryToCapacity ====================

        [Test]
        public void TrimInventory_OverCapacity_RemovesLastStacks()
        {
            var grenade = CreateCombatConsumable(bulkPerCharge: 5);
            var potion = CreateUtilityConsumable(bulkPerCharge: 3);
            BuildVehicleWithInventory(10,
                new ConsumableStack { template = grenade, charges = 2 },
                new ConsumableStack { template = potion, charges = 4 });
            // Total: 2*5 + 4*3 = 22, Cap: 10

            vehicle.TrimInventoryToCapacity();

            // Should remove stacks from the end until under capacity
            Assert.IsTrue(vehicle.inventory.Sum(s => s.charges * s.template.bulkPerCharge) <= 10,
                "Bulk should be at or under capacity after trim");
        }

        [Test]
        public void TrimInventory_UnderCapacity_DoesNothing()
        {
            var grenade = CreateCombatConsumable(bulkPerCharge: 1);
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 3 });

            int countBefore = vehicle.inventory.Count;

            vehicle.TrimInventoryToCapacity();

            Assert.AreEqual(countBefore, vehicle.inventory.Count);
        }

        // ==================== ConsumableValidator / Access Flags ====================

        [Test]
        public void Validator_CombatSeat_SeesCombatConsumable()
        {
            var grenade = CreateCombatConsumable();
            var driver = TestCharacterFactory.Create("Gunner");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("AccessVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat;
            vehicle.inventory.Add(new ConsumableStack { template = grenade, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(1, available.Count, "Combat seat should see combat consumable");
        }

        [Test]
        public void Validator_CombatSeat_CannotSeeUtilityConsumable()
        {
            var potion = CreateUtilityConsumable();
            var driver = TestCharacterFactory.Create("Gunner");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("AccessVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat;
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(0, available.Count, "Combat seat should not see utility consumable");
        }

        [Test]
        public void Validator_UtilitySeat_SeesUtilityConsumable()
        {
            var potion = CreateUtilityConsumable();
            var driver = TestCharacterFactory.Create("Medic");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("AccessVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Utility;
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(1, available.Count, "Utility seat should see utility consumable");
        }

        [Test]
        public void Validator_DualAccessSeat_SeesBothTypes()
        {
            var grenade = CreateCombatConsumable();
            var potion = CreateUtilityConsumable();
            var driver = TestCharacterFactory.Create("Commander");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("AccessVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat | ConsumableAccess.Utility;
            vehicle.inventory.Add(new ConsumableStack { template = grenade, charges = 1 });
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(2, available.Count, "Dual-access seat should see both types");
        }

        [Test]
        public void Validator_NoAccessSeat_SeesNothing()
        {
            var grenade = CreateCombatConsumable();
            var potion = CreateUtilityConsumable();
            var driver = TestCharacterFactory.Create("Passenger");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("AccessVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.None;
            vehicle.inventory.Add(new ConsumableStack { template = grenade, charges = 1 });
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(0, available.Count, "No-access seat should see nothing");
        }

        [Test]
        public void Validator_Validate_WrongAccessFlag_ReturnsFalse()
        {
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);
            vehicle = new TestVehicleBuilder("ValidatorVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Utility;
            seat.ResetTurnState();

            var grenade = CreateCombatConsumable();
            AttachSkill(grenade, ActionType.Action);

            var ctx = new RollContext
            {
                SourceActor = new CharacterActor(seat),
                Target = vehicle.Chassis
            };

            bool valid = ConsumableValidator.Validate(ctx, grenade);

            Assert.IsFalse(valid, "Utility seat should fail validation for combat consumable");
        }

        [Test]
        public void Validator_Validate_ValidContext_ReturnsTrue()
        {
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);
            vehicle = new TestVehicleBuilder("ValidatorVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat;
            seat.ResetTurnState();

            var grenade = CreateCombatConsumable();
            AttachSkill(grenade, ActionType.Action);

            var ctx = new RollContext
            {
                SourceActor = new CharacterActor(seat),
                Target = vehicle.Chassis
            };

            bool valid = ConsumableValidator.Validate(ctx, grenade);

            Assert.IsTrue(valid);
        }

        // ==================== GetAvailableConsumables ====================

        [Test]
        public void GetAvailable_FiltersOutUtilityForCombatOnlySeat()
        {
            var grenade = CreateCombatConsumable();
            var potion = CreateUtilityConsumable();
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("FilterVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat;

            vehicle.inventory.Add(new ConsumableStack { template = grenade, charges = 2 });
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 3 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(1, available.Count);
            Assert.AreEqual(grenade, available[0].template);
        }

        [Test]
        public void GetAvailable_DualAccess_ReturnsBothTypes()
        {
            var grenade = CreateCombatConsumable();
            var potion = CreateUtilityConsumable();
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("DualVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat | ConsumableAccess.Utility;

            vehicle.inventory.Add(new ConsumableStack { template = grenade, charges = 1 });
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(2, available.Count);
        }

        [Test]
        public void GetAvailable_ExcludesAmmunition()
        {
            var ammo = CreateAmmo();
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("AmmoVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat | ConsumableAccess.Utility;

            vehicle.inventory.Add(new ConsumableStack { template = ammo, charges = 5 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(0, available.Count, "Ammo should not appear in available consumables");
        }

        [Test]
        public void GetAvailable_NullTemplateSafe()
        {
            var driver = TestCharacterFactory.Create("Driver");
            cleanup.Add(driver);

            vehicle = new TestVehicleBuilder("NullTemplateVehicle")
                .WithChassis(driver)
                .Build();

            VehicleSeat seat = vehicle.seats[0];
            seat.consumableAccess = ConsumableAccess.Combat;

            vehicle.inventory.Add(new ConsumableStack { template = null, charges = 1 });

            var available = vehicle.GetAvailableConsumables(seat);

            Assert.AreEqual(0, available.Count, "Null templates should be silently skipped");
        }

        // ==================== ConsumableGatedSkill ====================

        [Test]
        public void ConsumableGatedSkill_HasRequiredConsumableField()
        {
            var ammo = CreateAmmo("SpecialAmmo");
            var gatedSkill = CreateConsumableGatedSkill(ammo);

            var consumableCost = gatedSkill.costs.OfType<ConsumableCost>().FirstOrDefault();
            Assert.IsNotNull(consumableCost, "Should have a ConsumableCost in the costs list");
            Assert.AreEqual(ammo, consumableCost.template);
        }

        [Test]
        public void ConsumableGatedSkill_VehicleHasCharges_SpendSucceeds()
        {
            var ammo = CreateAmmo("SpecialAmmo");
            BuildVehicleWithInventory(100, new ConsumableStack { template = ammo, charges = 2 });

            bool hasCharges = vehicle.HasChargesFor(ammo);
            bool spent = vehicle.TrySpendConsumable(ammo);

            Assert.IsTrue(hasCharges, "Should have charges before spend");
            Assert.IsTrue(spent, "Spend should succeed");
            Assert.AreEqual(1, vehicle.inventory[0].charges);
        }

        [Test]
        public void ConsumableGatedSkill_NoCharges_SpendFails()
        {
            var ammo = CreateAmmo("SpecialAmmo");
            BuildVehicleWithInventory(100);

            bool hasCharges = vehicle.HasChargesFor(ammo);
            bool spent = vehicle.TrySpendConsumable(ammo);

            Assert.IsFalse(hasCharges, "Should not have charges");
            Assert.IsFalse(spent, "Spend should fail");
        }

        // ==================== EVENT EMISSION ====================

        [Test]
        public void TrySpend_Success_EmitsConsumableSpentEvent()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 3 });

            var scope = CombatEventBus.BeginAction();
            vehicle.TrySpendConsumable(grenade, "TestSource");
            CombatEventBus.EndAction();

            var spentEvents = scope.Get<ConsumableSpentEvent>().ToList();
            Assert.AreEqual(1, spentEvents.Count);
            Assert.AreEqual(grenade, spentEvents[0].Template);
            Assert.AreEqual(vehicle, spentEvents[0].Vehicle);
            Assert.AreEqual("TestSource", spentEvents[0].CausalSource);
            Assert.AreEqual(2, spentEvents[0].ChargesRemaining);
        }

        [Test]
        public void TrySpend_LastCharge_EmitsSpentEventWithZeroRemaining()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 1 });

            var scope = CombatEventBus.BeginAction();
            vehicle.TrySpendConsumable(grenade, "TestSource");
            CombatEventBus.EndAction();

            var spentEvents = scope.Get<ConsumableSpentEvent>().ToList();
            Assert.AreEqual(1, spentEvents.Count);
            Assert.AreEqual(0, spentEvents[0].ChargesRemaining);
        }

        [Test]
        public void TrySpend_Failure_EmitsUnavailableEvent()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100);

            var scope = CombatEventBus.BeginAction();
            vehicle.TrySpendConsumable(grenade, "TestSource");
            CombatEventBus.EndAction();

            var unavailable = scope.Get<ConsumableUnavailableEvent>().ToList();
            Assert.AreEqual(1, unavailable.Count);
            Assert.AreEqual(grenade, unavailable[0].Template);
            Assert.AreEqual(vehicle, unavailable[0].Vehicle);
        }

        [Test]
        public void Restore_EmitsConsumableRestoredEvent()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 2 });

            var scope = CombatEventBus.BeginAction();
            vehicle.RestoreConsumable(grenade, 3, "HealSource");
            CombatEventBus.EndAction();

            var restored = scope.Get<ConsumableRestoredEvent>().ToList();
            Assert.AreEqual(1, restored.Count);
            Assert.AreEqual(grenade, restored[0].Template);
            Assert.AreEqual(3, restored[0].Amount);
            Assert.AreEqual(5, restored[0].ChargesAfter);
            Assert.AreEqual("HealSource", restored[0].CausalSource);
        }

        [Test]
        public void Restore_NewStack_EmitsRestoredEvent()
        {
            var potion = CreateUtilityConsumable();
            BuildVehicleWithInventory(100);

            var scope = CombatEventBus.BeginAction();
            vehicle.RestoreConsumable(potion, 2, "Resupply");
            CombatEventBus.EndAction();

            var restored = scope.Get<ConsumableRestoredEvent>().ToList();
            Assert.AreEqual(1, restored.Count);
            Assert.AreEqual(2, restored[0].Amount);
            Assert.AreEqual(2, restored[0].ChargesAfter);
        }

        [Test]
        public void Restore_ZeroAmount_NoEventEmitted()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 2 });

            var scope = CombatEventBus.BeginAction();
            vehicle.RestoreConsumable(grenade, 0);
            CombatEventBus.EndAction();

            var restored = scope.Get<ConsumableRestoredEvent>().ToList();
            Assert.AreEqual(0, restored.Count, "Zero amount should not emit event");
        }

        [Test]
        public void TrySpend_NullTemplate_NoEventEmitted()
        {
            BuildVehicleWithInventory(100);

            var scope = CombatEventBus.BeginAction();
            vehicle.TrySpendConsumable(null);
            CombatEventBus.EndAction();

            Assert.AreEqual(0, scope.Events.Count, "Null template should emit no events");
        }

        // ==================== RestoreConsumableEffect ====================

        [Test]
        public void RestoreEffect_RestoresChargesToVehicle()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 1 });

            var effect = new RestoreConsumableEffect();
            SetPrivateField(effect, "targetConsumable", grenade);
            SetPrivateField(effect, "amount", 3);

            var context = new EffectContext { CausalSource = "ResupplyEffect" };
            ((IVehicleEffect)effect).Apply(vehicle, context);

            Assert.AreEqual(4, vehicle.inventory[0].charges, "1 + 3 restored");
        }

        // ==================== INTEGRATION: SPEND-RESTORE CYCLE ====================

        [Test]
        public void SpendAndRestore_FullCycle_ChargesCorrect()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 5 });

            vehicle.TrySpendConsumable(grenade);
            vehicle.TrySpendConsumable(grenade);

            Assert.AreEqual(3, vehicle.inventory[0].charges);

            vehicle.RestoreConsumable(grenade, 2);

            Assert.AreEqual(5, vehicle.inventory[0].charges);
        }

        [Test]
        public void SpendAllThenRestore_RecreatesStack()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 1 });

            vehicle.TrySpendConsumable(grenade);
            Assert.AreEqual(0, vehicle.inventory.Count, "Stack removed after last charge");

            vehicle.RestoreConsumable(grenade, 2);
            Assert.AreEqual(1, vehicle.inventory.Count, "New stack created");
            Assert.AreEqual(2, vehicle.inventory[0].charges);
        }

        [Test]
        public void MultipleConsumables_SpendCorrectOne()
        {
            var grenade = CreateCombatConsumable("Grenade");
            var potion = CreateUtilityConsumable("Potion");
            BuildVehicleWithInventory(100,
                new ConsumableStack { template = grenade, charges = 3 },
                new ConsumableStack { template = potion, charges = 2 });

            vehicle.TrySpendConsumable(potion);

            Assert.AreEqual(3, vehicle.inventory[0].charges, "Grenade untouched");
            Assert.AreEqual(1, vehicle.inventory[1].charges, "Potion decremented");
        }

        [Test]
        public void Ammo_SpendAndCheck_WorksIdenticallyToConsumable()
        {
            var ammo = CreateAmmo("Bullets");
            BuildVehicleWithInventory(100, new ConsumableStack { template = ammo, charges = 4 });

            Assert.IsTrue(vehicle.HasChargesFor(ammo));
            vehicle.TrySpendConsumable(ammo);
            Assert.AreEqual(3, vehicle.inventory[0].charges);
        }

        // ==================== INTEGRATION: DUAL-SEAT ACCESS ====================

        [Test]
        public void TwoSeats_DifferentAccess_FilteredCorrectly()
        {
            var grenade = CreateCombatConsumable("Grenade");
            var potion = CreateUtilityConsumable("Potion");
            var driver = TestCharacterFactory.Create("Driver");
            var medic = TestCharacterFactory.Create("Medic");
            cleanup.Add(driver);
            cleanup.Add(medic);

            vehicle = new TestVehicleBuilder("DualSeatVehicle")
                .WithChassis(driver)
                .AddSeat("Medic", medic)
                .Build();

            vehicle.Chassis.baseCargoCapacity = 100;

            VehicleSeat driverSeat = vehicle.seats[0];
            VehicleSeat medicSeat = vehicle.seats[1];
            driverSeat.consumableAccess = ConsumableAccess.Combat;
            medicSeat.consumableAccess = ConsumableAccess.Utility;

            vehicle.inventory.Add(new ConsumableStack { template = grenade, charges = 2 });
            vehicle.inventory.Add(new ConsumableStack { template = potion, charges = 3 });

            var driverAvailable = vehicle.GetAvailableConsumables(driverSeat);
            var medicAvailable = vehicle.GetAvailableConsumables(medicSeat);

            Assert.AreEqual(1, driverAvailable.Count, "Driver sees only combat");
            Assert.AreEqual(grenade, driverAvailable[0].template);
            Assert.AreEqual(1, medicAvailable.Count, "Medic sees only utility");
            Assert.AreEqual(potion, medicAvailable[0].template);
        }

        // ==================== EDGE CASES ====================

        [Test]
        public void SpendSameConsumableMultipleTimes_DrainsFully()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 3 });

            vehicle.TrySpendConsumable(grenade);
            vehicle.TrySpendConsumable(grenade);
            vehicle.TrySpendConsumable(grenade);
            bool fourthSpend = vehicle.TrySpendConsumable(grenade);

            Assert.IsFalse(fourthSpend, "Fourth spend should fail");
            Assert.AreEqual(0, vehicle.inventory.Count, "All stacks removed");
        }

        [Test]
        public void Restore_CapacityExactlyFilled_NoExtraCharges()
        {
            var grenade = CreateCombatConsumable(bulkPerCharge: 5);
            BuildVehicleWithInventory(10, new ConsumableStack { template = grenade, charges = 1 });
            // Used: 5, Free: 5, Can add: 5/5 = 1

            vehicle.RestoreConsumable(grenade, 100);

            Assert.AreEqual(2, vehicle.inventory[0].charges, "1 existing + 1 cap-limited = 2");
        }

        [Test]
        public void HasChargesFor_ZeroCharges_ReturnsFalse()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 0 });

            bool result = vehicle.HasChargesFor(grenade);

            Assert.IsFalse(result, "Zero-charge stack should report no charges");
        }

        [Test]
        public void SpendFromZeroChargeStack_ReturnsFalse()
        {
            var grenade = CreateCombatConsumable();
            BuildVehicleWithInventory(100, new ConsumableStack { template = grenade, charges = 0 });

            bool spent = vehicle.TrySpendConsumable(grenade);

            Assert.IsFalse(spent, "Should not spend from zero-charge stack");
        }

        [Test]
        public void Restore_MixedInventory_CapacityAccountsForAll()
        {
            var grenade = CreateCombatConsumable("Grenade", bulkPerCharge: 3);
            var potion = CreateUtilityConsumable("Potion", bulkPerCharge: 2);
            BuildVehicleWithInventory(15,
                new ConsumableStack { template = grenade, charges = 2 },
                new ConsumableStack { template = potion, charges = 2 });
            // Used: 2*3 + 2*2 = 10, Free: 15-10 = 5, Can add potion: 5/2 = 2

            vehicle.RestoreConsumable(potion, 10);

            Assert.AreEqual(4, vehicle.inventory[1].charges, "2 existing + 2 capped = 4");
        }

        [Test]
        public void ConsumableAccess_FlagsCombine_Correctly()
        {
            ConsumableAccess combined = ConsumableAccess.Combat | ConsumableAccess.Utility;
            bool hasCombat = (combined & ConsumableAccess.Combat) != ConsumableAccess.None;
            bool hasUtility = (combined & ConsumableAccess.Utility) != ConsumableAccess.None;

            Assert.IsTrue(hasCombat);
            Assert.IsTrue(hasUtility);
            Assert.AreEqual((ConsumableAccess)3, combined, "Combat=1 | Utility=2 = 3");
        }

        // ==================== REFLECTION HELPER ====================

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
