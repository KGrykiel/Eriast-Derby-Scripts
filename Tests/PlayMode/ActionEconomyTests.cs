using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Skills;
using Assets.Scripts.Skills.Helpers;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Rolls.RollSpecs;

namespace Assets.Scripts.Tests.PlayMode
{
    internal class ActionEconomyTests
    {
        private readonly List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();
        }

        // ==================== POOL UNIT TESTS ====================

        [Test]
        public void ActionPool_BeforeReset_DefaultsToAvailable()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };

            Assert.IsTrue(seat.CanSpendAction(ActionType.Action), "Uninitialised Action pool defaults to available");
            Assert.IsTrue(seat.CanSpendAction(ActionType.BonusAction), "Uninitialised BonusAction pool defaults to available");
            Assert.IsTrue(seat.CanSpendAction(ActionType.Free), "Free is always available");
        }

        [Test]
        public void ActionPool_AfterReset_BothSlotsHaveOneCharge()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            Assert.AreEqual(1, seat.GetActionCount(ActionType.Action), "Action slot should start at 1");
            Assert.AreEqual(1, seat.GetActionCount(ActionType.BonusAction), "BonusAction slot should start at 1");
            Assert.IsTrue(seat.CanSpendAction(ActionType.Action));
            Assert.IsTrue(seat.CanSpendAction(ActionType.BonusAction));
        }

        [Test]
        public void ActionPool_SpendAction_DrainActionSlot_LeavesBonus()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.SpendAction(ActionType.Action);

            Assert.IsFalse(seat.CanSpendAction(ActionType.Action), "Action should be spent");
            Assert.AreEqual(0, seat.GetActionCount(ActionType.Action));
            Assert.IsTrue(seat.CanSpendAction(ActionType.BonusAction), "BonusAction should be untouched");
            Assert.IsTrue(seat.HasAnyActionsRemaining(), "BonusAction still available");
        }

        [Test]
        public void ActionPool_SpendBonusAction_DrainsBonusSlot_LeavesAction()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.SpendAction(ActionType.BonusAction);

            Assert.IsFalse(seat.CanSpendAction(ActionType.BonusAction), "BonusAction should be spent");
            Assert.AreEqual(0, seat.GetActionCount(ActionType.BonusAction));
            Assert.IsTrue(seat.CanSpendAction(ActionType.Action), "Action should be untouched");
            Assert.IsTrue(seat.HasAnyActionsRemaining(), "Action still available");
        }

        [Test]
        public void ActionPool_FreeCost_NeverDrainsPool()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.SpendAction(ActionType.Free);
            seat.SpendAction(ActionType.Free);
            seat.SpendAction(ActionType.Free);

            Assert.AreEqual(1, seat.GetActionCount(ActionType.Action), "Action pool unchanged after Free spends");
            Assert.AreEqual(1, seat.GetActionCount(ActionType.BonusAction), "BonusAction pool unchanged after Free spends");
            Assert.IsTrue(seat.CanSpendAction(ActionType.Free), "Free is always available");
        }

        [Test]
        public void ActionPool_FullAction_RequiresBothSlots_BlockedAfterOneSpent()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            Assert.IsTrue(seat.CanSpendAction(ActionType.FullAction), "FullAction available when both slots full");

            seat.SpendAction(ActionType.Action);
            Assert.IsFalse(seat.CanSpendAction(ActionType.FullAction), "FullAction blocked when Action already spent");

            seat.ResetTurnState();
            seat.SpendAction(ActionType.BonusAction);
            Assert.IsFalse(seat.CanSpendAction(ActionType.FullAction), "FullAction blocked when BonusAction already spent");
        }

        [Test]
        public void ActionPool_SpendFullAction_DrainsBothSlots()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.SpendAction(ActionType.FullAction);

            Assert.IsFalse(seat.CanSpendAction(ActionType.Action), "Action drained by FullAction");
            Assert.IsFalse(seat.CanSpendAction(ActionType.BonusAction), "BonusAction drained by FullAction");
            Assert.IsFalse(seat.CanSpendAction(ActionType.FullAction), "FullAction not repeatable");
            Assert.IsFalse(seat.HasAnyActionsRemaining(), "No actions remaining after FullAction");
            Assert.AreEqual(0, seat.GetActionCount(ActionType.Action));
            Assert.AreEqual(0, seat.GetActionCount(ActionType.BonusAction));
        }

        [Test]
        public void ActionPool_HasAnyActionsRemaining_FalseWhenBothDrained()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.SpendAction(ActionType.Action);
            seat.SpendAction(ActionType.BonusAction);

            Assert.IsFalse(seat.HasAnyActionsRemaining(), "No actions remaining when both slots drained");
        }

        [Test]
        public void ActionPool_GrantExtraAction_PermitsDoubleUse()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.GrantExtraAction(ActionType.Action, 1);

            Assert.AreEqual(2, seat.GetActionCount(ActionType.Action), "Extra grant should give 2 action charges");

            seat.SpendAction(ActionType.Action);
            Assert.IsTrue(seat.CanSpendAction(ActionType.Action), "Second action charge still available");
            Assert.AreEqual(1, seat.GetActionCount(ActionType.Action));

            seat.SpendAction(ActionType.Action);
            Assert.IsFalse(seat.CanSpendAction(ActionType.Action), "Both charges spent");
        }

        [Test]
        public void ActionPool_GrantExtraAction_NoopForFullActionAndFree()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();

            seat.GrantExtraAction(ActionType.FullAction, 5);
            seat.GrantExtraAction(ActionType.Free, 5);

            Assert.AreEqual(1, seat.GetActionCount(ActionType.Action), "Action pool unaffected by FullAction/Free grant");
            Assert.AreEqual(1, seat.GetActionCount(ActionType.BonusAction), "BonusAction pool unaffected by FullAction/Free grant");
        }

        [Test]
        public void ActionPool_ResetTurnState_RefillsAfterDrain()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();
            seat.SpendAction(ActionType.Action);
            seat.SpendAction(ActionType.BonusAction);

            Assert.IsFalse(seat.HasAnyActionsRemaining(), "Both drained before reset");

            seat.ResetTurnState();

            Assert.AreEqual(1, seat.GetActionCount(ActionType.Action), "Action refilled after reset");
            Assert.AreEqual(1, seat.GetActionCount(ActionType.BonusAction), "BonusAction refilled after reset");
            Assert.IsTrue(seat.HasAnyActionsRemaining(), "Actions available after reset");
        }

        // ==================== VALIDATOR INTEGRATION TESTS ====================

        [Test]
        public void SkillValidator_ActionSkill_BlockedAfterActionSpent()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();
            seat.SpendAction(ActionType.Action);

            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.rollNode = new RollNode();
            skill.actionCost = ActionType.Action;
            cleanup.Add(skill);

            var ctx = new RollContext { SourceActor = new CharacterActor(seat), Target = null };

            Assert.IsFalse(SkillValidator.Validate(ctx, skill), "Action skill should be blocked when action already spent");
        }

        [Test]
        public void SkillValidator_BonusActionSkill_AllowedAfterActionSpent()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();
            seat.SpendAction(ActionType.Action);

            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.rollNode = new RollNode();
            skill.actionCost = ActionType.BonusAction;
            cleanup.Add(skill);

            var ctx = new RollContext { SourceActor = new CharacterActor(seat), Target = null };

            Assert.IsTrue(SkillValidator.Validate(ctx, skill), "BonusAction skill should still be allowed after action spent");
        }

        [Test]
        public void SkillValidator_FreeSkill_AlwaysAllowed()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();
            seat.SpendAction(ActionType.Action);
            seat.SpendAction(ActionType.BonusAction);

            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.rollNode = new RollNode();
            skill.actionCost = ActionType.Free;
            cleanup.Add(skill);

            var ctx = new RollContext { SourceActor = new CharacterActor(seat), Target = null };

            Assert.IsTrue(SkillValidator.Validate(ctx, skill), "Free skills always allowed regardless of pool state");
        }

        [Test]
        public void SkillValidator_FullActionSkill_BlockedWhenActionAlreadySpent()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();
            seat.SpendAction(ActionType.Action);

            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.rollNode = new RollNode();
            skill.actionCost = ActionType.FullAction;
            cleanup.Add(skill);

            var ctx = new RollContext { SourceActor = new CharacterActor(seat), Target = null };

            Assert.IsFalse(SkillValidator.Validate(ctx, skill), "FullAction skill blocked when Action already spent");
        }

        [Test]
        public void SkillValidator_FullActionSkill_BlockedWhenBonusActionAlreadySpent()
        {
            var seat = new VehicleSeat { seatName = "TestSeat" };
            seat.ResetTurnState();
            seat.SpendAction(ActionType.BonusAction);

            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.rollNode = new RollNode();
            skill.actionCost = ActionType.FullAction;
            cleanup.Add(skill);

            var ctx = new RollContext { SourceActor = new CharacterActor(seat), Target = null };

            Assert.IsFalse(SkillValidator.Validate(ctx, skill), "FullAction skill blocked when BonusAction already spent");
        }

        [Test]
        public void SkillValidator_NoSeat_PoolCheckSkipped()
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.rollNode = new RollNode();
            skill.actionCost = ActionType.Action;
            cleanup.Add(skill);

            var ctx = new RollContext { SourceActor = null, Target = null };

            Assert.IsTrue(SkillValidator.Validate(ctx, skill), "Validator should skip pool check when no seat is present");
        }
    }
}
