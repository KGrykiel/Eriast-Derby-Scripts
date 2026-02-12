using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Core;

namespace Assets.Scripts.Tests.PlayMode
{
    public class StatCalculatorTests
    {
        private GameObject testObj;
        private ChassisComponent entity;

        [SetUp]
        public void SetUp()
        {
            testObj = new GameObject("TestEntity");
            entity = testObj.AddComponent<ChassisComponent>();
            entity.health = 100;
        }

        [TearDown]
        public void TearDown()
        {
            if (testObj != null)
                Object.DestroyImmediate(testObj);
        }

        // ==================== Base Values ====================

        [Test]
        public void Stat_NoModifiers_ReturnsBase()
        {
            int result = StatCalculator.GatherAttributeValue(entity, Attribute.ArmorClass, 15);
            Assert.AreEqual(15, result, "Should return base value when no modifiers");
        }

        [Test]
        public void Stat_NullEntity_ReturnsBase()
        {
            int result = StatCalculator.GatherAttributeValue(null, Attribute.ArmorClass, 15);
            Assert.AreEqual(15, result, "Should return base value for null entity");
        }

        // ==================== Flat Modifiers ====================

        [Test]
        public void Stat_FlatModifier_AddsToBase()
        {
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 5f, source: entity));

            int result = StatCalculator.GatherAttributeValue(entity, Attribute.ArmorClass, 10);
            Assert.AreEqual(15, result, "Base 10 + Flat 5 = 15");
        }

        [Test]
        public void Stat_MultipleFlatModifiers_AllStack()
        {
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 2f, source: entity));
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 3f, source: entity));
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, -1f, source: entity));

            int result = StatCalculator.GatherAttributeValue(entity, Attribute.ArmorClass, 10);
            Assert.AreEqual(14, result, "Base 10 + 2 + 3 + (-1) = 14");
        }

        [Test]
        public void Stat_FlatModifier_WrongAttribute_Ignored()
        {
            entity.AddModifier(new AttributeModifier(Attribute.MaxHealth, ModifierType.Flat, 99f, source: entity));

            int result = StatCalculator.GatherAttributeValue(entity, Attribute.ArmorClass, 10);
            Assert.AreEqual(10, result, "MaxHealth modifier should not affect ArmorClass");
        }

        // ==================== Multiplier Modifiers ====================

        [Test]
        public void Stat_MultiplierModifier_MultipliesTotal()
        {
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Multiplier, 1.5f, source: entity));

            int result = StatCalculator.GatherAttributeValue(entity, Attribute.ArmorClass, 10);
            Assert.AreEqual(15, result, "Base 10 * 1.5 = 15");
        }

        // ==================== Application Order ====================

        [Test]
        public void Stat_FlatThenMultiplier_CorrectOrder()
        {
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 10f, source: entity));
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Multiplier, 1.5f, source: entity));

            int result = StatCalculator.GatherAttributeValue(entity, Attribute.ArmorClass, 10);
            // Order: base(10) + flat(10) = 20, then * 1.5 = 30
            Assert.AreEqual(30, result, "Base 10 + Flat 10 = 20, then * 1.5 = 30");
        }

        // ==================== Breakdown ====================

        [Test]
        public void Stat_Breakdown_ContainsAllSources()
        {
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 3f, source: entity,
                displayNameOverride: "Shield"));
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 2f, source: entity,
                displayNameOverride: "Blessing"));

            var (total, baseValue, modifiers) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, Attribute.ArmorClass, 10);

            Assert.AreEqual(10, baseValue, "Base value should be 10");
            Assert.AreEqual(15, total, "Total should be 15");
            Assert.IsTrue(modifiers.Any(m => m.DisplayNameOverride == "Shield" && m.Value == 3f),
                "Should contain Shield +3");
            Assert.IsTrue(modifiers.Any(m => m.DisplayNameOverride == "Blessing" && m.Value == 2f),
                "Should contain Blessing +2");
        }

        [Test]
        public void Stat_ZeroValueModifier_ExcludedFromBreakdown()
        {
            entity.AddModifier(new AttributeModifier(Attribute.ArmorClass, ModifierType.Flat, 0f, source: entity));

            var (total, _, modifiers) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, Attribute.ArmorClass, 10);

            Assert.AreEqual(10, total, "Zero modifier should not change total");
            Assert.AreEqual(0, modifiers.Count, "Zero-value modifier should be excluded from breakdown");
        }
    }
}
