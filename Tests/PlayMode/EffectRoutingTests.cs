using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class EffectRoutingTests
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

        // ==================== DamageEffect Routing ====================

        [UnityTest]
        public IEnumerator RouteEffect_DamageEffect_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithWeapon()
                .Build();

            var damageEffect = new DamageEffect();
            var result = vehicle.RouteEffectTarget(damageEffect);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "DamageEffect should always route to chassis");
        }

        // ==================== ResourceRestorationEffect Routing ====================

        [UnityTest]
        public IEnumerator RouteEffect_RestorationEffect_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            var restoration = new ResourceRestorationEffect();
            var result = vehicle.RouteEffectTarget(restoration);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "ResourceRestorationEffect should route to chassis");
        }

        // ==================== AttributeModifierEffect Routing ====================

        [UnityTest]
        public IEnumerator RouteEffect_ACModifier_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var modEffect = new AttributeModifierEffect
            {
                attribute = Attribute.ArmorClass,
                type = ModifierType.Flat,
                value = 2f
            };
            var result = vehicle.RouteEffectTarget(modEffect);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "AC modifier should route to chassis");
        }

        [UnityTest]
        public IEnumerator RouteEffect_EnergyModifier_GoesToPowerCore()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            var modEffect = new AttributeModifierEffect
            {
                attribute = Attribute.MaxEnergy,
                type = ModifierType.Flat,
                value = 10f
            };
            var result = vehicle.RouteEffectTarget(modEffect);
            yield return null;

            Assert.AreEqual(vehicle.powerCore, result, "MaxEnergy modifier should route to PowerCore");
        }

        // ==================== Fallback Routing ====================

        [UnityTest]
        public IEnumerator RouteEffect_NullEffect_FallsToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .Build();

            var result = vehicle.RouteEffectTarget(null);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result, "Null effect should fall back to chassis");
        }

        // ==================== Status Effect Routing ====================

        [UnityTest]
        public IEnumerator RouteEffect_StatusEffectWithACModifier_GoesToChassis()
        {
            vehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            var statusTemplate = TestStatusEffectFactory.CreateModifierEffect("TestBuff", Attribute.ArmorClass, 2f, cleanup: cleanup);

            var applyEffect = new ApplyStatusEffect { statusEffect = statusTemplate };
            var result = vehicle.RouteEffectTarget(applyEffect);
            yield return null;

            Assert.AreEqual(vehicle.chassis, result,
                "StatusEffect with AC modifier should route to chassis (first modifier's attribute)");
        }
    }
}
