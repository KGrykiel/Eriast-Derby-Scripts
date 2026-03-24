using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Conditions;

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

            entity.ApplyCondition(template, entity);

            var modifiers = entity.GetModifiers();
            bool hasACBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 2f);
            Assert.IsTrue(hasACBonus, "Should have AC +2 modifier from Blessed");

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 active status effect");
        }

        // ==================== Stacking: Refresh ====================

        [Test]
        public void StatusEffect_Stacking_Refresh_ResetsDuration()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, duration: 3, stackBehaviour: StackBehaviour.Refresh, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect after first apply");
            Assert.AreEqual(3, activeEffects[0].turnsRemaining, "Should have 3 turns initially");

            entity.UpdateConditions();
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Should have 2 turns after first update");

            entity.ApplyCondition(template, entity);
            activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should still have 1 effect (refreshed, not stacked)");
            Assert.AreEqual(3, activeEffects[0].turnsRemaining, "Duration should be refreshed back to 3 turns");
        }

        [Test]
        public void StatusEffect_Stacking_Refresh_KeepsSingleModifier()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Blessed", Attribute.ArmorClass, 2f, duration: 2, stackBehaviour: StackBehaviour.Refresh, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);

            var modifiers = entity.GetModifiers();
            int acModifierCount = modifiers.Count(m => m.Attribute == Attribute.ArmorClass && m.Value == 2f);
            Assert.AreEqual(1, acModifierCount, "Should have exactly 1 AC modifier despite multiple applications");

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have only 1 active effect");
        }

        // ==================== Stacking: Stack ====================

        [Test]
        public void StatusEffect_Stacking_Stack_AllowsMultipleInstances()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Slowed", Attribute.Mobility, -1f, duration: 3, stackBehaviour: StackBehaviour.Stack, maxStacks: 3, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(3, activeEffects.Count, "Should have 3 independent instances");

            var modifiers = entity.GetModifiers();
            int mobilityModifierCount = modifiers.Count(m => m.Attribute == Attribute.Mobility && m.Value == -1f);
            Assert.AreEqual(3, mobilityModifierCount, "Should have 3 separate mobility modifiers");
        }

        [Test]
        public void StatusEffect_Stacking_Stack_RespectsMaxStacks()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Slowed", Attribute.Mobility, -1f, duration: 3, stackBehaviour: StackBehaviour.Stack, maxStacks: 2, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);

            var activeEffects = entity.GetActiveConditions();
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
                entity.ApplyCondition(template, entity);
            }

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(10, activeEffects.Count, "Should allow unlimited stacks when maxStacks = 0");
        }

        [Test]
        public void StatusEffect_Stacking_Stack_IndependentDurations()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Slowed", Attribute.Mobility, -1f, duration: 3, stackBehaviour: StackBehaviour.Stack, maxStacks: 0, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.UpdateConditions();
            entity.ApplyCondition(template, entity);

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(2, activeEffects.Count, "Should have 2 stacks");
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "First stack should have 2 turns (decremented)");
            Assert.AreEqual(3, activeEffects[1].turnsRemaining, "Second stack should have 3 turns (fresh)");

            entity.UpdateConditions();
            activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(2, activeEffects.Count, "Both stacks should still exist");
            Assert.AreEqual(1, activeEffects[0].turnsRemaining, "First stack down to 1 turn");
            Assert.AreEqual(2, activeEffects[1].turnsRemaining, "Second stack down to 2 turns");

            entity.UpdateConditions();
            activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "First stack should expire");
            Assert.AreEqual(1, activeEffects[0].turnsRemaining, "Second stack should remain with 1 turn");
        }

        // ==================== Stacking: Ignore ====================

        [Test]
        public void StatusEffect_Stacking_Ignore_DoesNothing()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Stunned", Attribute.ArmorClass, -2f, duration: 3, stackBehaviour: StackBehaviour.Ignore, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect after first apply");
            Assert.AreEqual(3, activeEffects[0].turnsRemaining, "Should have 3 turns initially");

            entity.UpdateConditions();
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Should tick down to 2 turns");

            entity.ApplyCondition(template, entity);
            activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should still have only 1 effect");
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Duration should NOT refresh (still 2 turns)");
        }

        [Test]
        public void StatusEffect_Stacking_Ignore_KeepsSingleModifier()
        {
            var template = TestStatusEffectFactory.CreateBehavioralEffect("Stunned", preventsActions: true, duration: 2, stackBehaviour: StackBehaviour.Ignore, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should ignore all reapplications");
            Assert.IsFalse(entity.IsOperational, "Should remain stunned");
        }

        // ==================== Stacking: Replace ====================

        [Test]
        public void StatusEffect_Stacking_Replace_LongerDurationReplaces()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("Fortified", Attribute.ArmorClass, 3f, duration: 2, stackBehaviour: StackBehaviour.Replace, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect initially");
            Assert.AreEqual(2, activeEffects[0].turnsRemaining, "Should have 2 turns");

            template.baseDuration = 5;
            entity.ApplyCondition(template, entity);
            activeEffects = entity.GetActiveConditions();
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

            entity.ApplyCondition(template, entity);
            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect initially");
            Assert.AreEqual(5, activeEffects[0].turnsRemaining, "Should have 5 turns");

            template.baseDuration = 2;
            entity.ApplyCondition(template, entity);
            activeEffects = entity.GetActiveConditions();
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

            entity.ApplyCondition(template, entity);

            template.baseDuration = -1;
            entity.ApplyCondition(template, entity);

            var activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Should have 1 effect");
            Assert.AreEqual(-1, activeEffects[0].turnsRemaining, "Should replace with indefinite duration");

            entity.UpdateConditions();
            entity.UpdateConditions();
            entity.UpdateConditions();

            activeEffects = entity.GetActiveConditions();
            Assert.AreEqual(1, activeEffects.Count, "Indefinite effect should never expire");
        }

        // ==================== Duration ====================

        [Test]
        public void StatusEffect_Duration_ExpiresAfterTurns()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("ShortBuff", Attribute.ArmorClass, 3f, duration: 2, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should be active initially");

            entity.UpdateConditions(); // Turn 1 — tick + decrement (turnsRemaining: 2→1)
            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should still be active after turn 1");

            entity.UpdateConditions(); // Turn 2 — tick + decrement (turnsRemaining: 1→0, expired, removed)
            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should expire after turn 2");

            var modifiers = entity.GetModifiers();
            bool hasBonus = modifiers.Any(m => m.Attribute == Attribute.ArmorClass && m.Value == 3f);
            Assert.IsFalse(hasBonus, "Modifiers should be cleaned up after expiration");
        }

        [Test]
        public void StatusEffect_Indefinite_NeverExpires()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("PermanentBuff", Attribute.ArmorClass, 2f, duration: -1, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            for (int i = 0; i < 100; i++)
            {
                entity.UpdateConditions();
            }

            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Indefinite effect should never expire");
        }

        // ==================== Behavioral Effects ====================

        [Test]
        public void StatusEffect_PreventsActions_BlocksOperational()
        {
            var template = TestStatusEffectFactory.CreateBehavioralEffect("Stunned", preventsActions: true, duration: 2, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            Assert.IsFalse(entity.IsOperational, "Stunned component should not be operational");
        }

        // ==================== Removal ====================

        [Test]
        public void StatusEffect_Removal_CleansUpModifiers()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect("TempBuff", Attribute.ArmorClass, 5f, cleanup: cleanup);

            var applied = entity.ApplyCondition(template, entity);
            Assert.IsTrue(entity.GetModifiers().Any(m => m.Value == 5f), "Modifier should exist after apply");

            entity.RemoveCondition(applied);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Effect should be removed");
            Assert.IsFalse(entity.GetModifiers().Any(m => m.Value == 5f), "Modifier should be cleaned up");
        }

        // ==================== Removal Triggers ====================

        [Test]
        public void StatusEffect_RemovalTrigger_OnDamageTaken_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Concentration", Attribute.ArmorClass, 2f,
                removalTriggers: RemovalTrigger.OnDamageTaken, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should have 1 effect before damage");

            entity.TakeDamage(5);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Effect should be removed on damage");
            Assert.IsFalse(entity.GetModifiers().Any(m => m.Value == 2f), "Modifiers should be cleaned up");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnDamageTaken_ZeroDamageDoesNotRemove()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Concentration", Attribute.ArmorClass, 2f,
                removalTriggers: RemovalTrigger.OnDamageTaken, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            entity.TakeDamage(0);

            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Zero damage should not trigger removal");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnD20Roll_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Inspired", Attribute.ArmorClass, 1f,
                removalTriggers: RemovalTrigger.OnD20Roll, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed after d20 roll");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnAttackMade_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Ambush", Attribute.Mobility, 3f,
                removalTriggers: RemovalTrigger.OnAttackMade, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnAttackMade);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed after attack");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnSkillUsed_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Overcharge", Attribute.ArmorClass, 4f,
                removalTriggers: RemovalTrigger.OnSkillUsed, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnSkillUsed);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed after skill use");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnMovement_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Entrenched", Attribute.ArmorClass, 3f,
                removalTriggers: RemovalTrigger.OnMovement, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnMovement);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed after movement");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnTurnEnd_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "QuickBuff", Attribute.Mobility, 2f,
                removalTriggers: RemovalTrigger.OnTurnEnd, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnTurnEnd);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed at turn end");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnRoundEnd_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "RoundBuff", Attribute.ArmorClass, 1f,
                removalTriggers: RemovalTrigger.OnRoundEnd, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnRoundEnd);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed at round end");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnTurnStart_RemovesBeforeTick()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "FadingBuff", Attribute.ArmorClass, 2f, duration: 5,
                removalTriggers: RemovalTrigger.OnTurnStart, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            // UpdateStatusEffects calls OnTurnStart which processes triggers BEFORE tick/decrement
            entity.UpdateConditions();

            Assert.AreEqual(0, entity.GetActiveConditions().Count,
                "OnTurnStart trigger should remove effect before tick, regardless of remaining duration");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_OnStageExit_RemovesEffect()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "EnvironmentalBuff", Attribute.ArmorClass, 3f,
                removalTriggers: RemovalTrigger.OnStageExit, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnStageExit);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should be removed on stage exit");
        }

        // ==================== Trigger Selectivity ====================

        [Test]
        public void StatusEffect_RemovalTrigger_DoesNotRemoveUnrelated()
        {
            var onDamage = TestStatusEffectFactory.CreateModifierEffect(
                "Concentration", Attribute.ArmorClass, 2f,
                removalTriggers: RemovalTrigger.OnDamageTaken, cleanup: cleanup);
            var onMovement = TestStatusEffectFactory.CreateModifierEffect(
                "Entrenched", Attribute.Mobility, 3f,
                removalTriggers: RemovalTrigger.OnMovement, cleanup: cleanup);

            entity.ApplyCondition(onDamage, entity);
            entity.ApplyCondition(onMovement, entity);
            Assert.AreEqual(2, entity.GetActiveConditions().Count);

            entity.NotifyConditionTrigger(RemovalTrigger.OnMovement);

            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Only OnMovement effect should be removed");
            Assert.AreEqual("Concentration", entity.GetActiveConditions()[0].template.effectName,
                "OnDamageTaken effect should survive OnMovement trigger");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_NoTriggers_SurvivesAll()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "SteadyBuff", Attribute.ArmorClass, 2f,
                removalTriggers: RemovalTrigger.None, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            entity.NotifyConditionTrigger(RemovalTrigger.OnDamageTaken);
            entity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);
            entity.NotifyConditionTrigger(RemovalTrigger.OnAttackMade);
            entity.NotifyConditionTrigger(RemovalTrigger.OnMovement);
            entity.NotifyConditionTrigger(RemovalTrigger.OnTurnEnd);
            entity.NotifyConditionTrigger(RemovalTrigger.OnRoundEnd);

            Assert.AreEqual(1, entity.GetActiveConditions().Count,
                "Effect with no removal triggers should survive all trigger types");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_MultipleTriggers_FirstOneRemoves()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Fragile", Attribute.ArmorClass, 1f,
                removalTriggers: RemovalTrigger.OnDamageTaken | RemovalTrigger.OnMovement,
                cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.AreEqual(1, entity.GetActiveConditions().Count);

            // Whichever fires first should remove the effect
            entity.NotifyConditionTrigger(RemovalTrigger.OnMovement);

            Assert.AreEqual(0, entity.GetActiveConditions().Count,
                "Effect with multiple triggers should be removed by whichever fires first");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_MultipleTriggers_SecondAlsoWorks()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Fragile", Attribute.ArmorClass, 1f,
                removalTriggers: RemovalTrigger.OnDamageTaken | RemovalTrigger.OnMovement,
                cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            entity.TakeDamage(1);

            Assert.AreEqual(0, entity.GetActiveConditions().Count,
                "Either trigger should be sufficient for removal");
        }

        // ==================== Category Removal ====================

        [Test]
        public void StatusEffect_RemoveByCategory_RemovesMatchingEffects()
        {
            var buff = TestStatusEffectFactory.CreateModifierEffect(
                "Fortified", Attribute.ArmorClass, 2f,
                categories: ConditionCategory.Buff, cleanup: cleanup);
            var debuff = TestStatusEffectFactory.CreateModifierEffect(
                "Weakened", Attribute.ArmorClass, -2f,
                categories: ConditionCategory.Debuff, cleanup: cleanup);

            entity.ApplyCondition(buff, entity);
            entity.ApplyCondition(debuff, entity);
            Assert.AreEqual(2, entity.GetActiveConditions().Count);

            entity.RemoveConditionsByCategory(ConditionCategory.Buff);

            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should remove only buffs");
            Assert.AreEqual("Weakened", entity.GetActiveConditions()[0].template.effectName,
                "Debuff should remain");
        }

        [Test]
        public void StatusEffect_RemoveByCategory_RemovesMultiCategoryEffects()
        {
            var buffMod = TestStatusEffectFactory.CreateModifierEffect(
                "Blessed", Attribute.ArmorClass, 3f,
                categories: ConditionCategory.Buff | ConditionCategory.AttributeModifier, cleanup: cleanup);
            var pureBuff = TestStatusEffectFactory.CreateModifierEffect(
                "Lucky", Attribute.Mobility, 1f,
                categories: ConditionCategory.Buff, cleanup: cleanup);

            entity.ApplyCondition(buffMod, entity);
            entity.ApplyCondition(pureBuff, entity);

            entity.RemoveConditionsByCategory(ConditionCategory.AttributeModifier);

            Assert.AreEqual(1, entity.GetActiveConditions().Count,
                "Should remove effect that has AttributeModifier flag");
            Assert.AreEqual("Lucky", entity.GetActiveConditions()[0].template.effectName,
                "Effect without AttributeModifier flag should survive");
        }

        [Test]
        public void StatusEffect_RemoveByCategory_NoCategoryIsImmune()
        {
            var uncategorised = TestStatusEffectFactory.CreateModifierEffect(
                "Environmental", Attribute.ArmorClass, 1f,
                categories: ConditionCategory.None, cleanup: cleanup);
            var buff = TestStatusEffectFactory.CreateModifierEffect(
                "Shield", Attribute.ArmorClass, 2f,
                categories: ConditionCategory.Buff, cleanup: cleanup);

            entity.ApplyCondition(uncategorised, entity);
            entity.ApplyCondition(buff, entity);

            entity.RemoveConditionsByCategory(ConditionCategory.Buff);

            Assert.AreEqual(1, entity.GetActiveConditions().Count);
            Assert.AreEqual("Environmental", entity.GetActiveConditions()[0].template.effectName,
                "Uncategorised effects should not be removed by category-based dispel");
        }

        [Test]
        public void StatusEffect_RemoveByCategory_DebuffFlagRemovesAllDebuffs()
        {
            var dot = TestStatusEffectFactory.CreateModifierEffect(
                "Burning", Attribute.ArmorClass, -1f,
                categories: ConditionCategory.Debuff | ConditionCategory.DoT, cleanup: cleanup);
            var cc = TestStatusEffectFactory.CreateBehavioralEffect(
                "Stunned", preventsActions: true,
                categories: ConditionCategory.Debuff | ConditionCategory.CrowdControl, cleanup: cleanup);
            var buff = TestStatusEffectFactory.CreateModifierEffect(
                "Shield", Attribute.ArmorClass, 2f,
                categories: ConditionCategory.Buff, cleanup: cleanup);

            entity.ApplyCondition(dot, entity);
            entity.ApplyCondition(cc, entity);
            entity.ApplyCondition(buff, entity);
            Assert.AreEqual(3, entity.GetActiveConditions().Count);

            entity.RemoveConditionsByCategory(ConditionCategory.Debuff);

            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should remove both debuffs");
            Assert.AreEqual("Shield", entity.GetActiveConditions()[0].template.effectName,
                "Buff should survive debuff purge");
        }

        // ==================== Template Removal ====================

        [Test]
        public void StatusEffect_RemoveByTemplate_RemovesSpecificEffect()
        {
            var burning = TestStatusEffectFactory.CreateModifierEffect(
                "Burning", Attribute.ArmorClass, -1f, cleanup: cleanup);
            var bleeding = TestStatusEffectFactory.CreateModifierEffect(
                "Bleeding", Attribute.Mobility, -1f, cleanup: cleanup);

            entity.ApplyCondition(burning, entity);
            entity.ApplyCondition(bleeding, entity);
            Assert.AreEqual(2, entity.GetActiveConditions().Count);

            entity.RemoveConditionsByTemplate(burning);

            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should remove only Burning");
            Assert.AreEqual("Bleeding", entity.GetActiveConditions()[0].template.effectName,
                "Bleeding should remain");
        }

        [Test]
        public void StatusEffect_RemoveByTemplate_RemovesAllStacks()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Slowed", Attribute.Mobility, -1f, duration: 3,
                stackBehaviour: StackBehaviour.Stack, maxStacks: 5, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            Assert.AreEqual(3, entity.GetActiveConditions().Count, "Should have 3 stacks");

            entity.RemoveConditionsByTemplate(template);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Should remove all stacks of the template");
            Assert.IsFalse(entity.GetModifiers().Any(m => m.Attribute == Attribute.Mobility),
                "All modifiers from stacks should be cleaned up");
        }

        // ==================== Edge Cases ====================

        [Test]
        public void StatusEffect_RemovalTrigger_DuringStack_RemovesAllMatchingStacks()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "FragileStack", Attribute.ArmorClass, -1f, duration: 5,
                stackBehaviour: StackBehaviour.Stack, maxStacks: 5,
                removalTriggers: RemovalTrigger.OnDamageTaken, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            entity.ApplyCondition(template, entity);
            Assert.AreEqual(3, entity.GetActiveConditions().Count, "Should have 3 stacks");

            entity.TakeDamage(1);

            Assert.AreEqual(0, entity.GetActiveConditions().Count,
                "All stacks with the trigger should be removed");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_WithDuration_TriggerRemovesBeforeExpiry()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "TimedFragile", Attribute.ArmorClass, 2f, duration: 10,
                removalTriggers: RemovalTrigger.OnAttackMade, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            entity.UpdateConditions(); // Turn 1
            entity.UpdateConditions(); // Turn 2
            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Should still be active mid-duration");
            Assert.AreEqual(8, entity.GetActiveConditions()[0].turnsRemaining, "Should have 8 turns left");

            entity.NotifyConditionTrigger(RemovalTrigger.OnAttackMade);

            Assert.AreEqual(0, entity.GetActiveConditions().Count,
                "Trigger should remove effect despite having turns remaining");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_IndefiniteEffectCanBeTriggered()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Sentinel", Attribute.ArmorClass, 5f, duration: -1,
                removalTriggers: RemovalTrigger.OnD20Roll, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            for (int i = 0; i < 10; i++)
            {
                entity.UpdateConditions();
            }
            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Indefinite effect should survive ticks");

            entity.NotifyConditionTrigger(RemovalTrigger.OnD20Roll);

            Assert.AreEqual(0, entity.GetActiveConditions().Count,
                "Trigger should remove even indefinite effects");
        }

        [Test]
        public void StatusEffect_RemovalTrigger_AlreadyRemovedIsIdempotent()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "OneShot", Attribute.ArmorClass, 1f,
                removalTriggers: RemovalTrigger.OnDamageTaken, cleanup: cleanup);

            entity.ApplyCondition(template, entity);

            entity.TakeDamage(1);
            Assert.AreEqual(0, entity.GetActiveConditions().Count);

            // Firing the same trigger again when no effects remain should not throw
            entity.TakeDamage(1);
            entity.NotifyConditionTrigger(RemovalTrigger.OnDamageTaken);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "No errors on redundant trigger firing");
        }

        [Test]
        public void StatusEffect_RemoveByCategory_CleansUpModifiers()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Cursed", Attribute.ArmorClass, -3f,
                categories: ConditionCategory.Debuff, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.IsTrue(entity.GetModifiers().Any(m => m.Value == -3f), "Modifier should exist after apply");

            entity.RemoveConditionsByCategory(ConditionCategory.Debuff);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Effect should be removed");
            Assert.IsFalse(entity.GetModifiers().Any(m => m.Value == -3f),
                "Category removal should clean up modifiers");
        }

        [Test]
        public void StatusEffect_RemoveByTemplate_CleansUpModifiers()
        {
            var template = TestStatusEffectFactory.CreateModifierEffect(
                "Hex", Attribute.Mobility, -4f, cleanup: cleanup);

            entity.ApplyCondition(template, entity);
            Assert.IsTrue(entity.GetModifiers().Any(m => m.Value == -4f), "Modifier should exist after apply");

            entity.RemoveConditionsByTemplate(template);

            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Effect should be removed");
            Assert.IsFalse(entity.GetModifiers().Any(m => m.Value == -4f),
                "Template removal should clean up modifiers");
        }

        [Test]
        public void StatusEffect_MixedTriggerAndCategory_BothPathsWork()
        {
            var triggerable = TestStatusEffectFactory.CreateModifierEffect(
                "Fragile", Attribute.ArmorClass, 1f,
                categories: ConditionCategory.Buff,
                removalTriggers: RemovalTrigger.OnDamageTaken, cleanup: cleanup);
            var persistent = TestStatusEffectFactory.CreateModifierEffect(
                "Resilient", Attribute.ArmorClass, 2f,
                categories: ConditionCategory.Buff,
                removalTriggers: RemovalTrigger.None, cleanup: cleanup);

            entity.ApplyCondition(triggerable, entity);
            entity.ApplyCondition(persistent, entity);
            Assert.AreEqual(2, entity.GetActiveConditions().Count);

            // Trigger removes only the triggerable one
            entity.TakeDamage(1);
            Assert.AreEqual(1, entity.GetActiveConditions().Count, "Only triggerable effect should be removed");
            Assert.AreEqual("Resilient", entity.GetActiveConditions()[0].template.effectName);

            // Category removal still works for the remaining one
            entity.RemoveConditionsByCategory(ConditionCategory.Buff);
            Assert.AreEqual(0, entity.GetActiveConditions().Count, "Category removal should get the rest");
        }
    }
}
