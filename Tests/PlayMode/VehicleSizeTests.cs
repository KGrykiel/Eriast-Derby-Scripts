using System.Linq;
using NUnit.Framework;
using Assets.Scripts.Entities.Vehicles;

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
            var modifiers = VehicleSizeModifiers.GetModifiers(size);

            if (expectedAC == 0 && expectedMobility == 0)
            {
                Assert.AreEqual(0, modifiers.Count, "Medium should have no modifiers");
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
        }

        // ==================== Modifier Properties ====================

        [Test]
        public void Size_Modifiers_AreEquipmentCategory()
        {
            var modifiers = VehicleSizeModifiers.GetModifiers(VehicleSizeCategory.Tiny);

            foreach (var mod in modifiers)
            {
                Assert.AreEqual(ModifierCategory.Equipment, mod.Category,
                    $"Size modifier for {mod.Attribute} should be Equipment category");
            }
        }

        [Test]
        public void Size_Modifiers_HaveDisplayName()
        {
            var modifiers = VehicleSizeModifiers.GetModifiers(VehicleSizeCategory.Large);

            foreach (var mod in modifiers)
            {
                Assert.IsNotNull(mod.Label, "Size modifier should have display name");
                Assert.IsTrue(mod.Label.Contains("Large"),
                    "Display name should contain size category");
            }
        }

        // ==================== Modifier Types ====================

        [Test]
        public void Size_AllModifiers_AreFlatType()
        {
            foreach (VehicleSizeCategory size in System.Enum.GetValues(typeof(VehicleSizeCategory)))
            {
                var modifiers = VehicleSizeModifiers.GetModifiers(size);
                foreach (var mod in modifiers)
                {
                    Assert.AreEqual(ModifierType.Flat, mod.Type,
                        $"Size {size} modifier for {mod.Attribute} should be Flat type");
                }
            }
        }
    }
}
