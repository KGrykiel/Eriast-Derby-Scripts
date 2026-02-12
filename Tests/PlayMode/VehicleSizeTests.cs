using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Tests.PlayMode
{
    public class VehicleSizeTests
    {
        // ==================== Modifier Values ====================

        [Test]
        [TestCase(VehicleSizeCategory.Tiny, 3, 3)]
        [TestCase(VehicleSizeCategory.Small, 1, 1)]
        [TestCase(VehicleSizeCategory.Medium, 0, 0)]
        [TestCase(VehicleSizeCategory.Large, -2, -3)]
        [TestCase(VehicleSizeCategory.Huge, -4, -5)]
        public void Size_CorrectModifiers(VehicleSizeCategory size, int expectedAC, int expectedMobility)
        {
            var dummySource = ScriptableObject.CreateInstance<ScriptableObject>();
            var modifiers = VehicleSizeModifiers.GetModifiers(size, dummySource);

            if (expectedAC == 0 && expectedMobility == 0)
            {
                Assert.AreEqual(0, modifiers.Count, "Medium should have no modifiers");
                Object.DestroyImmediate(dummySource);
                return;
            }

            if (expectedAC != 0)
            {
                var acMod = modifiers.FirstOrDefault(m => m.Attribute == Attribute.ArmorClass);
                Assert.IsNotNull(acMod, $"Size {size} should have AC modifier");
                Assert.AreEqual(expectedAC, (int)acMod.Value, $"Size {size} AC modifier");
            }

            if (expectedMobility != 0)
            {
                var mobMod = modifiers.FirstOrDefault(m => m.Attribute == Attribute.Mobility);
                Assert.IsNotNull(mobMod, $"Size {size} should have Mobility modifier");
                Assert.AreEqual(expectedMobility, (int)mobMod.Value, $"Size {size} Mobility modifier");
            }

            Object.DestroyImmediate(dummySource);
        }

        // ==================== Modifier Properties ====================

        [Test]
        public void Size_Modifiers_AreEquipmentCategory()
        {
            var dummySource = ScriptableObject.CreateInstance<ScriptableObject>();
            var modifiers = VehicleSizeModifiers.GetModifiers(VehicleSizeCategory.Tiny, dummySource);

            foreach (var mod in modifiers)
            {
                Assert.AreEqual(ModifierCategory.Equipment, mod.Category,
                    $"Size modifier for {mod.Attribute} should be Equipment category");
            }

            Object.DestroyImmediate(dummySource);
        }

        [Test]
        public void Size_Modifiers_HaveDisplayName()
        {
            var dummySource = ScriptableObject.CreateInstance<ScriptableObject>();
            var modifiers = VehicleSizeModifiers.GetModifiers(VehicleSizeCategory.Large, dummySource);

            foreach (var mod in modifiers)
            {
                Assert.IsNotNull(mod.DisplayNameOverride, "Size modifier should have display name");
                Assert.IsTrue(mod.DisplayNameOverride.Contains("Large"),
                    "Display name should contain size category");
            }

            Object.DestroyImmediate(dummySource);
        }

        // ==================== Modifier Types ====================

        [Test]
        public void Size_AllModifiers_AreFlatType()
        {
            var dummySource = ScriptableObject.CreateInstance<ScriptableObject>();

            foreach (VehicleSizeCategory size in System.Enum.GetValues(typeof(VehicleSizeCategory)))
            {
                var modifiers = VehicleSizeModifiers.GetModifiers(size, dummySource);
                foreach (var mod in modifiers)
                {
                    Assert.AreEqual(ModifierType.Flat, mod.Type,
                        $"Size {size} modifier for {mod.Attribute} should be Flat type");
                }
            }

            Object.DestroyImmediate(dummySource);
        }
    }
}
