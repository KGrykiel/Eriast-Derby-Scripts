using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class StatusEffectTests
    {
        private GameObject testObj;
        private ChassisComponent entity;
        private readonly System.Collections.Generic.List<Object> cleanup = new();

        [SetUp]
        public void SetUp()
        {
            testObj = new GameObject("TestEntity");
            entity = testObj.AddComponent<ChassisComponent>();
            entity.SetHealth(100);
        }

        [TearDown]
        public void TearDown()
        {
            if (testObj != null)
                Object.DestroyImmediate(testObj);
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();
        }

        // ==================== Application ====================

        [Test]
        public void StatusEffect_Apply_AddsModifiers()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);

            var modifiers = entity.GetModifiers();
            bool hasACBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 2f);
            Assert.IsTrue(hasACBonus, "Should have AC +2 modifier from Blessed");

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 active status effect");
        }

        // ==================== Stacking ====================

        [Test]
        public void StatusEffect_Stacking_LongerDurationReplaces()
        {
            // Apply with short duration
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, duration: 2, cleanup: cleanup);
            entity.ApplyStatusEffect(template, entity);

            // Apply SAME template again with longer duration
            template.baseDuration = 5;
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have only 1 effect (replaced)");
            Assert.AreEqual(5, activeEffects[0].turnsRemaining, "Should have the longer duration (5 turns)");

            var modifiers = entity.GetModifiers();
            bool hasBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 2f);
            Assert.IsTrue(hasBonus, "Should still have AC +2 modifier");
        }

        [Test]
        public void StatusEffect_Stacking_ShorterDurationKept()
        {
            // Apply with long duration
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, duration: 5, cleanup: cleanup);
            entity.ApplyStatusEffect(template, entity);

            // Apply SAME template again with shorter duration
            template.baseDuration = 2;
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have only 1 effect (kept existing)");
            Assert.AreEqual(5, activeEffects[0].turnsRemaining, "Should keep the longer duration (5 turns)");

            var modifiers = entity.GetModifiers();
            bool hasBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 2f);
            Assert.IsTrue(hasBonus, "Should still have AC +2 modifier");
        }

        // ==================== Duration ====================

        [Test]
        public void StatusEffect_Duration_ExpiresAfterTurns()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("ShortBuff", Attribute.ArmorClass, 3f, duration: 2, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            Assert.AreEqual(1, entity.GetActiveStatusEffects().Count, "Should be active initially");

            entity.UpdateStatusEffects(); // Turn 1 — tick + decrement (turnsRemaining: 2→1)
            Assert.AreEqual(1, entity.GetActiveStatusEffects().Count, "Should still be active after turn 1");

            entity.UpdateStatusEffects(); // Turn 2 — tick + decrement (turnsRemaining: 1→0, expired, removed)
            Assert.AreEqual(0, entity.GetActiveStatusEffects().Count, "Should expire after turn 2");

            var modifiers = entity.GetModifiers();
            bool hasBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 3f);
            Assert.IsFalse(hasBonus, "Modifiers should be cleaned up after expiration");
        }

        [Test]
        public void StatusEffect_Indefinite_NeverExpires()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("PermanentBuff", Attribute.ArmorClass, 2f, duration: -1, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);

            for (int i = 0; i < 100; i++)
            {
                entity.UpdateStatusEffects();
            }

            Assert.AreEqual(1, entity.GetActiveStatusEffects().Count, "Indefinite effect should never expire");
        }

        // ==================== Behavioral Effects ====================

        [Test]
        public void StatusEffect_PreventsActions_BlocksOperational()
        {
            var template = TestStatusEffectFactory.CreateBehavioralEffect("Stunned", preventsActions: true, duration: 2, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);

            Assert.IsFalse(entity.IsOperational, "Stunned component should not be operational");
        }

        // ==================== Removal ====================

        [Test]
        public void StatusEffect_Removal_CleansUpModifiers()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("TempBuff", Attribute.ArmorClass, 5f, cleanup: cleanup);

            var applied = entity.ApplyStatusEffect(template, entity);
            Assert.IsTrue(entity.GetModifiers().Any(m => m.Value == 5f), "Modifier should exist after apply");

            entity.RemoveStatusEffect(applied);

            Assert.AreEqual(0, entity.GetActiveStatusEffects().Count, "Effect should be removed");
            Assert.IsFalse(entity.GetModifiers().Any(m => m.Value == 5f), "Modifier should be cleaned up");
        }
    }
}
