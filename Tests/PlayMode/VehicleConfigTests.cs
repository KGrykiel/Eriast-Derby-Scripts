using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class VehicleConfigTests
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

        private Character CreateChar(string name = "Test")
        {
            var c = TestCharacterFactory.Create(name: name);
            cleanup.Add(c);
            return c;
        }

        // ==================== Operational Status ====================

        [UnityTest]
        public IEnumerator Vehicle_NoChassis_NotOperational()
        {
            vehicle = new TestVehicleBuilder()
                .WithPowerCore()
                .Build();

            yield return null;

            bool isOperational = vehicle.IsOperational();
            Assert.IsFalse(isOperational, "Vehicle without chassis should not be operational");

            string reason = vehicle.GetNonOperationalReason();
            Assert.IsNotNull(reason);
            Assert.IsTrue(reason.Contains("chassis"), "Should mention chassis");
        }

        [UnityTest]
        public IEnumerator Vehicle_NoPowerCore_NotOperational()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            yield return null;

            bool isOperational = vehicle.IsOperational();
            Assert.IsFalse(isOperational, "Vehicle without power core should not be operational");

            string reason = vehicle.GetNonOperationalReason();
            Assert.IsNotNull(reason);
            Assert.IsTrue(reason.Contains("power") || reason.Contains("Power"),
                "Should mention power core");
        }

        [UnityTest]
        public IEnumerator Vehicle_ChassisAndPowerCore_IsOperational()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            yield return null;

            Assert.IsTrue(vehicle.IsOperational(), "Vehicle with chassis + power core should be operational");
        }

        [UnityTest]
        public IEnumerator Vehicle_DestroyedChassis_NotOperational()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            vehicle.chassis.TakeDamage(vehicle.chassis.GetCurrentHealth());

            yield return null;

            Assert.IsFalse(vehicle.IsOperational(), "Vehicle with destroyed chassis should not be operational");
        }

        // ==================== Seat Access ====================

        [UnityTest]
        public IEnumerator Vehicle_GetSeatForComponent_ReturnsCorrectSeat()
        {
            var driver = CreateChar("Driver");
            vehicle = TestVehicleBuilder.CreateWithChassis(driver);

            var seat = vehicle.GetSeatForComponent(vehicle.chassis);
            yield return null;

            Assert.IsNotNull(seat, "Should find seat controlling chassis");
            Assert.AreEqual(driver, seat.assignedCharacter, "Seat should have driver assigned");
        }

        [UnityTest]
        public IEnumerator Vehicle_GetSeatForComponent_NoSeat_ReturnsNull()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithUtility() // No character assigned
                .Build();

            var utility = vehicle.optionalComponents[0];
            var seat = vehicle.GetSeatForComponent(utility);
            yield return null;

            // Utility has a seat but with no character — let's check the seat exists
            // GetSeatForComponent returns the seat object (which may have null character)
            // but if WithUtility() creates no seat when no character, it returns null
            Assert.IsNull(seat, "Should return null when no seat controls component");
        }
    }
}
