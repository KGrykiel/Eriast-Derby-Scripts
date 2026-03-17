using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.StatusEffects;

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

        // ==================== Stacking: Refresh ====================

        [Test]
        public void StatusEffect_Stacking_Refresh_ResetsDuration()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, duration: 3, stackBehaviour: StackBehaviour.Refresh, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect after first apply");
            Assert.AreEqual(3, activeEffects[0].turnsRemaining, "Should have 3 turns initially");

            entity.UpdateStatusEffects();
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Should have 2 turns after first update");

            entity.ApplyStatusEffect(template, entity);
            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should still have 1 effect (refreshed, not stacked)");
            Assert.AreEqual(3, activeEffects[0].turnsRemaining, "Duration should be refreshed back to 3 turns");
        }

        [Test]
        public void StatusEffect_Stacking_Refresh_KeepsSingleModifier()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, duration: 2, stackBehaviour: StackBehaviour.Refresh, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);

            var modifiers = entity.GetModifiers();
            int acModifierCount = modifiers.Count(m => m.Attribute == Attribute.ArmorClass && m.Value == 2f);
            Assert.AreEqual(1, acModifierCount, "Should have exactly 1 AC modifier despite multiple applications");

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have only 1 active effect");
        }

        // ==================== Stacking: Stack ====================

        [Test]
        public void StatusEffect_Stacking_Stack_AllowsMultipleInstances()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Slowed", Attribute.Mobility, -1f, duration: 3, stackBehaviour: StackBehaviour.Stack, maxStacks: 3, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(3, activeEffects.Count, "Should have 3 independent instances");

            var modifiers = entity.GetModifiers();
            int mobilityModifierCount = modifiers.Count(m => m.Attribute == Attribute.Mobility && m.Value == -1f);
            Assert.AreEqual(3, mobilityModifierCount, "Should have 3 separate mobility modifiers");
        }

        [Test]
        public void StatusEffect_Stacking_Stack_RespectsMaxStacks()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Slowed", Attribute.Mobility, -1f, duration: 3, stackBehaviour: StackBehaviour.Stack, maxStacks: 2, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(2, activeEffects.Count, "Should have maximum 2 stacks");

            var modifiers = entity.GetModifiers();
            int mobilityModifierCount = modifiers.Count(m => m.Attribute == Attribute.Mobility && m.Value == -1f);
            Assert.AreEqual(2, mobilityModifierCount, "Should have exactly 2 mobility modifiers");
        }

        [Test]
        public void StatusEffect_Stacking_Stack_UnlimitedWhenMaxStacksZero()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Vulnerable", Attribute.ArmorClass, -1f, duration: 2, stackBehaviour: StackBehaviour.Stack, maxStacks: 0, cleanup: cleanup);

            for (int i = 0; i < 10; i++)
            {
                entity.ApplyStatusEffect(template, entity);
            }

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(10, activeEffects.Count, "Should allow unlimited stacks when maxStacks = 0");
        }

        [Test]
        public void StatusEffect_Stacking_Stack_IndependentDurations()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Slowed", Attribute.Mobility, -1f, duration: 3, stackBehaviour: StackBehaviour.Stack, maxStacks: 0, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            entity.UpdateStatusEffects();
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(2, activeEffects.Count, "Should have 2 stacks");
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "First stack should have 2 turns (decremented)");
            Assert.AreEqual(3, activeEffects[1].turnsRemaining, "Second stack should have 3 turns (fresh)");

            entity.UpdateStatusEffects();
            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(2, activeEffects.Count, "Both stacks should still exist");
            Assert.AreEqual(1, activeEffects[0].turnsRemaining, "First stack down to 1 turn");
            Assert.AreEqual(2, activeEffects[1].turnsRemaining, "Second stack down to 2 turns");

            entity.UpdateStatusEffects();
            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "First stack should expire");
            Assert.AreEqual(1, activeEffects[0].turnsRemaining, "Second stack should remain with 1 turn");
        }

        // ==================== Stacking: Ignore ====================

        [Test]
        public void StatusEffect_Stacking_Ignore_DoesNothing()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Stunned", Attribute.ArmorClass, -2f, duration: 3, stackBehaviour: StackBehaviour.Ignore, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect after first apply");
            Assert.AreEqual(3, activeEffects[0].turnsRemaining, "Should have 3 turns initially");

            entity.UpdateStatusEffects();
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Should tick down to 2 turns");

            entity.ApplyStatusEffect(template, entity);
            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should still have only 1 effect");
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Duration should NOT refresh (still 2 turns)");
        }

        [Test]
        public void StatusEffect_Stacking_Ignore_KeepsSingleModifier()
        {
            var template = TestStatusEffectFactory.CreateBehavioralEffect("Stunned", preventsActions: true, duration: 2, stackBehaviour: StackBehaviour.Ignore, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should ignore all reapplications");
            Assert.IsFalse(entity.IsOperational, "Should remain stunned");
        }

        // ==================== Stacking: Replace ====================

        [Test]
        public void StatusEffect_Stacking_Replace_LongerDurationReplaces()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Fortified", Attribute.ArmorClass, 3f, duration: 2, stackBehaviour: StackBehaviour.Replace, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect initially");
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Should have 2 turns");

            template.baseDuration = 5;
            entity.ApplyStatusEffect(template, entity);
            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should still have 1 effect (replaced)");
            Assert.AreEqual(5, activeEffects[0].turnsRemaining, "Should have the longer duration (5 turns)");

            var modifiers = entity.GetModifiers();
            bool hasBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 3f);
            Assert.IsTrue(hasBonus, "Should still have AC +3 modifier");
        }

        [Test]
        public void StatusEffect_Stacking_Replace_ShorterDurationIgnored()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Fortified", Attribute.ArmorClass, 3f, duration: 5, stackBehaviour: StackBehaviour.Replace, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);
            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect initially");
            Assert.AreEqual(5, activeEffects[0].turnsRemaining, "Should have 5 turns");

            template.baseDuration = 2;
            entity.ApplyStatusEffect(template, entity);
            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should still have 1 effect (kept stronger)");
            Assert.AreEqual(5, activeEffects[0].turnsRemaining, "Should keep the longer duration (5 turns)");

            var modifiers = entity.GetModifiers();
            bool hasBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 3f);
            Assert.IsTrue(hasBonus, "Should still have AC +3 modifier");
        }

        [Test]
        public void StatusEffect_Stacking_Replace_IndefiniteIsStrongest()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Fortified", Attribute.ArmorClass, 2f, duration: 99, stackBehaviour: StackBehaviour.Replace, cleanup: cleanup);

            entity.ApplyStatusEffect(template, entity);

            template.baseDuration = -1;
            entity.ApplyStatusEffect(template, entity);

            var activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect");
            Assert.AreEqual(-1, activeEffects[0].turnsRemaining, "Should replace with indefinite duration");

            entity.UpdateStatusEffects();
            entity.UpdateStatusEffects();
            entity.UpdateStatusEffects();

            activeEffects = entity.GetActiveStatusEffects();
            Assert.AreEqual(1, activeEffects.Count, "Indefinite effect should never expire");
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
