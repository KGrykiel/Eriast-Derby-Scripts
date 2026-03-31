using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Tests.PlayMode
{
    /// <summary>
    /// Tests VehicleComponentResolver — the single source of truth for attribute→component mapping.
    /// Replaces old VehicleEffectRouter tests; each effect's Apply now calls the resolver internally.
    /// </summary>
    public class EffectRoutingTests
    {
        private Vehicle vehicle;

        [TearDown]
        public void TearDown()
        {
            if (vehicle != null)
                Object.DestroyImmediate(vehicle.gameObject);
        }

        // ==================== Chassis-routed Attributes ====================

        [UnityTest]
        public IEnumerator Resolver_ArmorClass_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var result = VehicleComponentResolver.ResolveForAttribute(vehicle, EntityAttribute.ArmorClass);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "ArmorClass should resolve to chassis");
        }

        [UnityTest]
        public IEnumerator Resolver_MaxHealth_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var result = VehicleComponentResolver.ResolveForAttribute(vehicle, EntityAttribute.MaxHealth);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "MaxHealth should resolve to chassis");
        }

        [UnityTest]
        public IEnumerator Resolver_Mobility_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var result = VehicleComponentResolver.ResolveForAttribute(vehicle, EntityAttribute.Mobility);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "Mobility should resolve to chassis");
        }

        // ==================== Power Core-routed Attributes ====================

        [UnityTest]
        public IEnumerator Resolver_MaxEnergy_GoesToPowerCore()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            var result = VehicleComponentResolver.ResolveForAttribute(vehicle, EntityAttribute.MaxEnergy);
            yield return null;

            Assert.AreEqual(vehicle.powerCore, result, "MaxEnergy should resolve to PowerCore");
        }

        [UnityTest]
        public IEnumerator Resolver_EnergyRegen_GoesToPowerCore()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            var result = VehicleComponentResolver.ResolveForAttribute(vehicle, EntityAttribute.EnergyRegen);
            yield return null;

            Assert.AreEqual(vehicle.powerCore, result, "EnergyRegen should resolve to PowerCore");
        }

        // ==================== Fallback ====================

        [UnityTest]
        public IEnumerator Resolver_UnknownAttribute_FallsToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var result = VehicleComponentResolver.ResolveForAttribute(vehicle, EntityAttribute.ArmorClass);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "Should fall back to chassis");
        }
    }
}
