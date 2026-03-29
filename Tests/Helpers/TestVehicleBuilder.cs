using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

namespace Assets.Scripts.Tests.Helpers
{
    public class TestVehicleBuilder
    {
        private readonly GameObject root;
        private readonly Vehicle vehicle;
        private readonly List<VehicleSeat> seatList = new();
        private ChassisComponent chassis;

        public TestVehicleBuilder(string vehicleName = "TestVehicle")
        {
            root = new GameObject(vehicleName);
            vehicle = root.AddComponent<Vehicle>();
            vehicle.vehicleName = vehicleName;
        }

        public TestVehicleBuilder WithChassis(Character driver = null, int maxHealth = 100, int armorClass = 10)
        {
            chassis = TestComponentFactory.CreateChassis(root, maxHealth, armorClass);
            vehicle.RegisterComponent(chassis);

            if (driver != null)
            {
                var seat = CreateSeat("Driver", driver);
                seat.controlledComponents.Add(chassis);
            }

            return this;
        }

        public TestVehicleBuilder WithWeapon(Character gunner = null, int attackBonus = 0, string weaponName = "TestWeapon")
        {
            var weapon = TestComponentFactory.CreateWeapon(root, weaponName, attackBonus);
            vehicle.RegisterComponent(weapon);

            if (gunner != null)
            {
                var seat = CreateSeat("Gunner", gunner);
                seat.controlledComponents.Add(weapon);
            }

            return this;
        }

        public TestVehicleBuilder WithUtility(Character operatorChar = null)
        {
            var utility = TestComponentFactory.CreateUtility(root);
            vehicle.RegisterComponent(utility);

            if (operatorChar != null)
            {
                var seat = CreateSeat("Technician", operatorChar);
                seat.controlledComponents.Add(utility);
            }

            return this;
        }

        public TestVehicleBuilder WithPowerCore(Character engineer = null)
        {
            var powerCore = TestComponentFactory.CreatePowerCore(root);
            vehicle.RegisterComponent(powerCore);

            if (engineer != null)
            {
                var seat = CreateSeat("Engineer", engineer);
                seat.controlledComponents.Add(powerCore);
            }

            return this;
        }

        public TestVehicleBuilder AddSeat(string seatName, Character character)
        {
            CreateSeat(seatName, character);
            return this;
        }

        public Vehicle Build()
        {
            vehicle.seats = seatList;

            // Manually initialize components since Vehicle.Awake() ran before
            // components were assigned (Awake fires on AddComponent, not Build)
            foreach (var comp in vehicle.AllComponents)
                comp.Initialize(vehicle);

            foreach (var seat in vehicle.seats)
                seat.ParentVehicle = vehicle;

            return vehicle;
        }

        private VehicleSeat CreateSeat(string seatName, Character character)
        {
            var seat = new VehicleSeat
            {
                seatName = seatName,
                controlledComponents = new List<VehicleComponent>()
            };
            seat.Assign(character);
            seatList.Add(seat);
            return seat;
        }

        public static Vehicle CreateWithChassis(Character driver = null)
        {
            return new TestVehicleBuilder()
                .WithChassis(driver)
                .Build();
        }

        public static Vehicle CreateEmpty()
        {
            return new TestVehicleBuilder().Build();
        }
    }
}
