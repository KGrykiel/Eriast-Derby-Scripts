using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Restoration;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Core;
using Assets.Scripts.Conditions;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Combat.Logging
{
    /// <summary>
    /// Logging orchestration for combat events.
    /// Takes combat events, formats them via CombatFormatter, and emits to RaceHistory.
    /// </summary>
    public static class CombatLogManager
    {
        // ==================== MAIN LOGGING ENTRY POINTS ====================

        public static void LogAction(CombatAction action)
        {
            if (action == null || !action.HasEvents) return;

            LogAttackRolls(action);
            LogSavingThrows(action);
            LogSkillChecks(action);
            LogOpposedChecks(action);
            LogDamageByTarget(action);
            LogEntityConditions(action);
            LogVehicleConditions(action);
            LogRestorations(action);
            LogCharacterConditions(action);
            LogConsumables(action);
        }

        // ==================== ATTACK ROLL LOGGING ====================

        private static void LogAttackRolls(CombatAction action)
        {
            foreach (var evt in action.Get<AttackRollEvent>())
                LogSingleAttackRoll(evt);
        }

        private static void LogSingleAttackRoll(AttackRollEvent evt)
        {
            Vehicle attackerVehicle = evt.Actor?.GetVehicle();
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string sourceName = CombatDisplayHelpers.FormatRollActor(evt.Actor);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);
            string resultText = evt.Roll.Success
                ? $"<color={CombatFormatter.Colors.Success}>Hit</color>"
                : $"<color={CombatFormatter.Colors.Failure}>Miss</color>";

            string message = $"{sourceName} attacks {targetName} with {evt.CausalSource}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.High : EventImportance.Medium;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                targetVehicle?.CurrentStage,
                attackerVehicle, targetVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatAttackDetailed(evt.Roll));

            if (evt.Target != null)
            {
                var (totalAC, baseAC, acModifiers) = StatCalculator.GatherDefenseValueWithBreakdown(evt.Target);
                logEvt.WithMetadata("defenseBreakdown", CombatFormatter.FormatDefenseDetailed(totalAC, baseAC, acModifiers, "AC"));
            }
        }

        // ==================== SAVING THROW LOGGING ====================

        private static void LogSavingThrows(CombatAction action)
        {
            foreach (var evt in action.Get<SavingThrowEvent>())
                LogSingleSavingThrow(evt);
        }

        private static void LogSingleSavingThrow(SavingThrowEvent evt)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = evt.Defender?.GetVehicle();

            string targetName = CombatDisplayHelpers.FormatRollActor(evt.Defender);
            string skillName = evt.CausalSource;
            string saveTypeName = evt.CheckName;

            string resultText;
            if (evt.Roll.Success)
                resultText = $"<color={CombatFormatter.Colors.Success}>Saved</color>";
            else if (evt.Roll.IsAutoFail)
                resultText = $"<color={CombatFormatter.Colors.Failure}>Auto-Failed</color>";
            else
                resultText = $"<color={CombatFormatter.Colors.Failure}>Failed</color>";

            string message = $"{targetName} attempts {saveTypeName} save vs {skillName}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.Medium : EventImportance.High;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                targetVehicle?.CurrentStage,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatSaveDetailed(evt.Roll, saveTypeName));

            if (evt.CausalSource != null)
                logEvt.WithMetadata("dcBreakdown", CombatFormatter.FormatDCDetailed(evt.Roll.TargetValue, evt.CausalSource, saveTypeName));
        }

        // ==================== SKILL CHECK LOGGING ====================

        private static void LogSkillChecks(CombatAction action)
        {
            foreach (var evt in action.Get<SkillCheckEvent>())
                LogSingleSkillCheck(evt);
        }

        private static void LogSingleSkillCheck(SkillCheckEvent evt)
        {
            Vehicle sourceVehicle = evt.Actor?.GetVehicle();

            string sourceName = CombatDisplayHelpers.FormatRollActor(evt.Actor);
            string skillName = evt.CausalSource;
            string checkTypeName = evt.CheckName;

            string resultText;
            if (evt.Roll.Success)
                resultText = $"<color={CombatFormatter.Colors.Success}>Success</color>";
            else if (evt.Roll.IsAutoFail)
                resultText = $"<color={CombatFormatter.Colors.Failure}>Auto-Failed</color>";
            else
                resultText = $"<color={CombatFormatter.Colors.Failure}>Failure</color>";

            string message = $"{sourceName} attempts {checkTypeName} check for {skillName}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.Medium : EventImportance.High;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                sourceVehicle?.CurrentStage,
                sourceVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatSkillCheckDetailed(evt.Roll, checkTypeName));

            if (evt.CausalSource != null)
                logEvt.WithMetadata("dcBreakdown", CombatFormatter.FormatDCDetailed(evt.Roll.TargetValue, evt.CausalSource, checkTypeName, "Check"));
        }

        // ==================== OPPOSED CHECK LOGGING ====================

        private static void LogOpposedChecks(CombatAction action)
        {
            foreach (var evt in action.Get<OpposedCheckEvent>())
                LogSingleOpposedCheck(evt);
        }

        private static void LogSingleOpposedCheck(OpposedCheckEvent evt)
        {
            Vehicle attackerVehicle = evt.AttackerActor?.GetVehicle();
            Vehicle defenderVehicle = evt.DefenderActor?.GetVehicle();

            string winnerName = evt.Roll.Success
                ? (attackerVehicle?.vehicleName ?? "Attacker")
                : (defenderVehicle?.vehicleName ?? "Defender");
            string loserName = evt.Roll.Success
                ? (defenderVehicle?.vehicleName ?? "Defender")
                : (attackerVehicle?.vehicleName ?? "Attacker");
            string skillName = evt.CausalSource;
            int attackerTotal = evt.Roll.Total;
            int defenderTotal = evt.Roll.TargetValue;

            string message = $"{winnerName} wins {skillName} against {loserName} ({attackerTotal} vs {defenderTotal}).";

            var logEvt = RaceHistory.Log(
                EventType.Combat, EventImportance.High, message,
                attackerVehicle?.CurrentStage,
                attackerVehicle, defenderVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatOpposedCheckDetailed(evt.Roll, evt.DefenderRoll, evt.AttackerCheckName, evt.DefenderCheckName));
        }

        // ==================== DAMAGE LOGGING ====================

        private static void LogDamageByTarget(CombatAction action)
        {
            foreach (var (target, sourceActor, causalSource, events) in action.GetDamageByTarget())
            {
                if (events.Count > 0)
                    LogCombinedDamage(events, target, sourceActor, causalSource);
            }
        }

        private static void LogCombinedDamage(List<DamageEvent> damages, Entity target, RollActor sourceActor, string causalSource)
        {
            Vehicle attackerVehicle = sourceActor?.GetVehicle();
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

            string sourceName = CombatDisplayHelpers.FormatRollActor(sourceActor);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(target);
            bool isSelf = CombatDisplayHelpers.IsSelfTarget(sourceActor?.GetEntity(), target, attackerVehicle, targetVehicle);

            string damageText = BuildCombinedDamageText(damages);

            string message;
            if (sourceActor == null)
                message = $"{targetName} takes {damageText} from {causalSource}";
            else if (isSelf)
                message = $"{targetName} takes {damageText}";
            else
                message = $"{sourceName} deals {damageText} to {targetName}";

            var logEvt = RaceHistory.Log(
                EventType.Combat, EventImportance.High, message,
                targetVehicle?.CurrentStage ?? attackerVehicle?.CurrentStage,
                attackerVehicle, targetVehicle);

            var results = damages.Select(d => d.Result).ToList();
            logEvt.WithMetadata("damageBreakdown", CombatFormatter.FormatCombinedDamageDetailed(results, causalSource));
        }

        // ==================== STATUS EFFECT LOGGING ====================

        private static void LogEntityConditions(CombatAction action)
        {
            foreach (var evt in action.Get<EntityConditionEvent>().Where(e => e.Applied != null))
                LogSingleEntityCondition(evt);

            foreach (var evt in action.Get<EntityConditionRefreshedEvent>())
                LogConditionRefreshed(EntityContext(evt.Target), evt.Refreshed, CombatFormatter.FormatEntityConditionTooltip(evt.Refreshed));

            foreach (var evt in action.Get<EntityConditionIgnoredEvent>())
                LogConditionIgnored(EntityContext(evt.Target), evt.Existing, CombatFormatter.FormatEntityConditionTooltip(evt.Existing));

            foreach (var evt in action.Get<EntityConditionReplacedEvent>())
                LogConditionReplaced(EntityContext(evt.Target), evt.NewEffect, evt.OldDuration, CombatFormatter.FormatEntityConditionTooltip(evt.NewEffect));

            foreach (var evt in action.Get<EntityConditionKeptStrongerEvent>())
                LogConditionKeptStronger(EntityContext(evt.Target), evt.Kept, CombatFormatter.FormatEntityConditionTooltip(evt.Kept));

            foreach (var evt in action.Get<EntityConditionStackLimitEvent>())
                LogConditionStackLimit(EntityContext(evt.Target), evt.Template.effectName, evt.MaxStacks);

            foreach (var evt in action.Get<EntityConditionExpiredEvent>())
                LogConditionExpired(EntityContext(evt.Target), evt.Expired, CombatFormatter.FormatEntityConditionTooltip(evt.Expired));

            foreach (var evt in action.Get<EntityConditionRemovedByTriggerEvent>())
                LogConditionRemovedByTrigger(EntityContext(evt.Target), evt.Removed, evt.Trigger.ToString(), CombatFormatter.FormatEntityConditionTooltip(evt.Removed));
        }

        private static void LogSingleEntityCondition(EntityConditionEvent evt)
        {
            if (evt.Applied == null) return;

            var applied = evt.Applied;
            var effect = applied.template;

            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            bool isBuff = CombatDisplayHelpers.DetermineIfBuff(effect);
            string color = isBuff ? CombatFormatter.Colors.Success : CombatFormatter.Colors.Failure;
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            bool isSelf = CombatDisplayHelpers.IsSelfTarget(evt.Source, evt.Target, sourceVehicle, targetVehicle);
            string message;

            if (evt.Source == null)
            {
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> from {evt.CausalSource} ({durationText})";
            }
            else if (isSelf)
            {
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> ({durationText})";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source);
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceName} {actionVerb} <color={color}>{effect.effectName}</color> on {targetName} ({durationText})";
            }

            bool playerInvolved = (sourceVehicle != null && sourceVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);
            EventImportance importance;
            if (playerInvolved && !isBuff)
                importance = EventImportance.High;
            else if (playerInvolved)
                importance = EventImportance.Medium;
            else
                importance = EventImportance.Low;

            var logEvt = RaceHistory.Log(
                EventType.Condition, importance, message,
                targetVehicle?.CurrentStage,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(applied));
        }

        // ==================== RESTORATION LOGGING ====================

        private static void LogRestorations(CombatAction action)
        {
            foreach (var (target, sourceActor, causalSource, events) in action.GetRestorationByTarget())
                LogCombinedRestoration(events, target, sourceActor, causalSource);
        }

        private static void LogCombinedRestoration(List<RestorationEvent> restorations, Entity target, RollActor sourceActor, string causalSource)
        {
            Vehicle sourceVehicle = sourceActor?.GetVehicle();
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

            string sourceName = CombatDisplayHelpers.FormatRollActor(sourceActor);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(target);
            bool isSelf = CombatDisplayHelpers.IsSelfTarget(sourceActor?.GetEntity(), target, sourceVehicle, targetVehicle);

            var parts = new List<string>();
            BuildRestorationPart(parts, restorations, ResourceType.Health, "HP", CombatFormatter.Colors.Health);
            BuildRestorationPart(parts, restorations, ResourceType.Energy, "energy", CombatFormatter.Colors.Energy);

            if (parts.Count == 0) return;

            string restorationText = string.Join(" and ", parts);

            string message = (sourceActor == null || isSelf)
                ? $"{targetName} {restorationText}"
                : $"{sourceName} {restorationText} to {targetName}";

            bool playerInvolved = (sourceVehicle != null && sourceVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);

            var logEvt = RaceHistory.Log(
                EventType.Resource,
                playerInvolved ? EventImportance.Medium : EventImportance.Low,
                message,
                targetVehicle?.CurrentStage,
                sourceVehicle, targetVehicle);

            var results = restorations.Select(r => r.Result).ToList();
            logEvt.WithMetadata("restorationBreakdown", CombatFormatter.FormatCombinedRestorationDetailed(results, causalSource));
        }

        // ==================== PRIVATE HELPERS ====================

        private static string BuildCombinedDamageText(List<DamageEvent> damages)
        {
            if (damages.Count == 0) return "0 damage";

            if (damages.Count == 1)
            {
                var d = damages[0].Result;
                return $"<color={CombatFormatter.Colors.Damage}>{d.FinalDamage}</color> {d.DamageType} damage";
            }

            var parts = damages.Select(d =>
                $"<color={CombatFormatter.Colors.Damage}>{d.Result.FinalDamage}</color> {d.Result.DamageType}");
            int total = damages.Sum(d => d.Result.FinalDamage);

            return $"{string.Join(" + ", parts)} (<color={CombatFormatter.Colors.Damage}>{total} total</color>) damage";
        }

        private static void BuildRestorationPart(
            List<string> parts,
            List<RestorationEvent> restorations,
            ResourceType resourceType,
            string resourceName,
            string color)
        {
            var filtered = restorations.Where(r => r.Result.ResourceType == resourceType).ToList();
            if (filtered.Count == 0) return;

            int total = filtered.Sum(r => r.Result.ActualChange);
            if (total == 0) return;

            string verb = total > 0 ? "restores" : "drains";
            parts.Add($"{verb} <color={color}>{Math.Abs(total)}</color> {resourceName}");
        }

        // ==================== CHARACTER CONDITION LOGGING ====================

        private static void LogCharacterConditions(CombatAction action)
        {
            foreach (var evt in action.Get<CharacterConditionEvent>())
                LogSingleCharacterCondition(evt);

            foreach (var evt in action.Get<CharacterConditionRefreshedEvent>())
                LogConditionRefreshed(CharacterContext(evt.TargetSeat), evt.Refreshed, CombatFormatter.FormatCharacterConditionTooltip(evt.Refreshed));

            foreach (var evt in action.Get<CharacterConditionIgnoredEvent>())
                LogConditionIgnored(CharacterContext(evt.TargetSeat), evt.Existing, CombatFormatter.FormatCharacterConditionTooltip(evt.Existing));

            foreach (var evt in action.Get<CharacterConditionReplacedEvent>())
                LogConditionReplaced(CharacterContext(evt.TargetSeat), evt.NewCondition, evt.OldDuration, CombatFormatter.FormatCharacterConditionTooltip(evt.NewCondition));

            foreach (var evt in action.Get<CharacterConditionKeptStrongerEvent>())
                LogConditionKeptStronger(CharacterContext(evt.TargetSeat), evt.Kept, CombatFormatter.FormatCharacterConditionTooltip(evt.Kept));

            foreach (var evt in action.Get<CharacterConditionStackLimitEvent>())
                LogConditionStackLimit(CharacterContext(evt.TargetSeat), evt.Template.effectName, evt.MaxStacks);

            foreach (var evt in action.Get<CharacterConditionExpiredEvent>())
                LogConditionExpired(CharacterContext(evt.TargetSeat), evt.Expired, CombatFormatter.FormatCharacterConditionTooltip(evt.Expired));

            foreach (var evt in action.Get<CharacterConditionRemovedByTriggerEvent>())
                LogConditionRemovedByTrigger(CharacterContext(evt.TargetSeat), evt.Removed, evt.Trigger.ToString(), CombatFormatter.FormatCharacterConditionTooltip(evt.Removed));
        }

        private static void LogSingleCharacterCondition(CharacterConditionEvent evt)
        {
            if (evt.Applied == null) return;

            var applied = evt.Applied;
            var condition = applied.template;

            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);
            bool isBuff = CombatDisplayHelpers.DetermineIfBuff(condition);
            string color = isBuff ? CombatFormatter.Colors.Success : CombatFormatter.Colors.Failure;
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            string message;
            if (evt.Source == null)
            {
                message = $"{targetName} gains <color={color}>{condition.effectName}</color> from {evt.CausalSource} ({durationText})";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source);
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceName} {actionVerb} <color={color}>{condition.effectName}</color> on {targetName} ({durationText})";
            }

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Medium, message);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(applied));
        }

        // ==================== VEHICLE CONDITION LOGGING ====================

        private static void LogVehicleConditions(CombatAction action)
        {
            foreach (var evt in action.Get<VehicleConditionEvent>().Where(e => e.Applied != null))
                LogSingleVehicleCondition(evt);

            foreach (var evt in action.Get<VehicleConditionRefreshedEvent>())
                LogConditionRefreshed(VehicleContext(evt.Target), evt.Refreshed, CombatFormatter.FormatVehicleConditionTooltip(evt.Refreshed));

            foreach (var evt in action.Get<VehicleConditionIgnoredEvent>())
                LogConditionIgnored(VehicleContext(evt.Target), evt.Existing, CombatFormatter.FormatVehicleConditionTooltip(evt.Existing));

            foreach (var evt in action.Get<VehicleConditionReplacedEvent>())
                LogConditionReplaced(VehicleContext(evt.Target), evt.NewCondition, evt.OldDuration, CombatFormatter.FormatVehicleConditionTooltip(evt.NewCondition));

            foreach (var evt in action.Get<VehicleConditionKeptStrongerEvent>())
                LogConditionKeptStronger(VehicleContext(evt.Target), evt.Kept, CombatFormatter.FormatVehicleConditionTooltip(evt.Kept));

            foreach (var evt in action.Get<VehicleConditionStackLimitEvent>())
                LogConditionStackLimit(VehicleContext(evt.Target), evt.Template.effectName, evt.MaxStacks);

            foreach (var evt in action.Get<VehicleConditionExpiredEvent>())
                LogConditionExpired(VehicleContext(evt.Target), evt.Expired, CombatFormatter.FormatVehicleConditionTooltip(evt.Expired));

            foreach (var evt in action.Get<VehicleConditionRemovedByTriggerEvent>())
                LogConditionRemovedByTrigger(VehicleContext(evt.Target), evt.Removed, evt.Trigger.ToString(), CombatFormatter.FormatVehicleConditionTooltip(evt.Removed));
        }

        private static void LogSingleVehicleCondition(VehicleConditionEvent evt)
        {
            if (evt.Applied == null) return;

            var applied = evt.Applied;
            var condition = applied.template;

            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = evt.Target;
            string targetName = targetVehicle != null ? targetVehicle.vehicleName : "Unknown";
            bool isBuff = CombatDisplayHelpers.DetermineIfBuff(condition);
            string color = isBuff ? CombatFormatter.Colors.Success : CombatFormatter.Colors.Failure;
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            string message;
            if (evt.Source == null)
            {
                message = $"{targetName} gains <color={color}>{condition.effectName}</color> from {evt.CausalSource} ({durationText})";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source);
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceName} {actionVerb} <color={color}>{condition.effectName}</color> on {targetName} ({durationText})";
            }

            bool playerInvolved = (sourceVehicle != null && sourceVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);
            EventImportance importance;
            if (playerInvolved && !isBuff)
                importance = EventImportance.High;
            else if (playerInvolved)
                importance = EventImportance.Medium;
            else
                importance = EventImportance.Low;

            var logEvt = RaceHistory.Log(
                EventType.Condition, importance, message,
                targetVehicle != null ? targetVehicle.CurrentStage : null,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatVehicleConditionTooltip(applied));
        }

        // ==================== CONDITION LOG HELPERS ====================

        private readonly struct ConditionLogContext
        {
            public readonly string TargetName;
            public readonly Vehicle TargetVehicle;

            public ConditionLogContext(string targetName, Vehicle targetVehicle)
            {
                TargetName = targetName;
                TargetVehicle = targetVehicle;
            }
        }

        private static ConditionLogContext EntityContext(Entity target)
        {
            Vehicle vehicle = EntityHelpers.GetParentVehicle(target);
            return new ConditionLogContext(CombatDisplayHelpers.FormatEntityWithVehicle(target), vehicle);
        }

        private static ConditionLogContext VehicleContext(Vehicle target)
        {
            string name = target != null ? target.vehicleName : "Unknown";
            return new ConditionLogContext(name, target);
        }

        private static ConditionLogContext CharacterContext(VehicleSeat seat)
            => new(CombatDisplayHelpers.FormatSeatName(seat), null);

        private static string DurationText(AppliedConditionBase applied)
            => applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

        private static string DurationText(int turns)
            => turns == -1 ? "indefinite" : $"{turns} turns";

        private static RaceEvent LogConditionEvent(EventImportance importance, string message, ConditionLogContext ctx)
        {
            if (ctx.TargetVehicle != null)
                return RaceHistory.Log(EventType.Condition, importance, message, ctx.TargetVehicle.CurrentStage, ctx.TargetVehicle);
            return RaceHistory.Log(EventType.Condition, importance, message);
        }

        private static void LogConditionExpired(ConditionLogContext ctx, AppliedConditionBase applied, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {applied.Template.effectName} has expired", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionRefreshed(ConditionLogContext ctx, AppliedConditionBase applied, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {applied.Template.effectName} refreshed ({DurationText(applied)})", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionIgnored(ConditionLogContext ctx, AppliedConditionBase existing, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {existing.Template.effectName} reapplication ignored (already active)", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionReplaced(ConditionLogContext ctx, AppliedConditionBase newCondition, int oldDuration, string tooltip)
        {
            string msg = $"{ctx.TargetName}'s {newCondition.Template.effectName} replaced ({DurationText(oldDuration)} → {DurationText(newCondition)})";
            var logEvt = LogConditionEvent(EventImportance.Low, msg, ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionKeptStronger(ConditionLogContext ctx, AppliedConditionBase kept, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {kept.Template.effectName} kept stronger version ({DurationText(kept)})", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionStackLimit(ConditionLogContext ctx, string effectName, int maxStacks)
        {
            LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {effectName} stack limit reached ({maxStacks})", ctx);
        }

        private static void LogConditionRemovedByTrigger(ConditionLogContext ctx, AppliedConditionBase removed, string trigger, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {removed.Template.effectName} removed by trigger ({trigger})", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        // ==================== CONSUMABLE LOGGING ====================

        private static void LogConsumables(CombatAction action)
        {
            foreach (var evt in action.Get<ConsumableSpentEvent>())
                LogConsumableSpent(evt);

            foreach (var evt in action.Get<ConsumableRestoredEvent>())
                LogConsumableRestored(evt);

            foreach (var evt in action.Get<ConsumableUnavailableEvent>())
                LogConsumableUnavailable(evt);
        }

        private static void LogConsumableSpent(ConsumableSpentEvent evt)
        {
            bool isPlayer = evt.Vehicle != null && evt.Vehicle.controlType == ControlType.Player;
            string vehicleName = evt.Vehicle != null ? evt.Vehicle.vehicleName : "Unknown";
            string suffix = evt.ChargesRemaining == 0
                ? " (last charge)"
                : $" ({evt.ChargesRemaining} remaining)";

            RaceHistory.Log(
                EventType.Resource,
                isPlayer ? EventImportance.Medium : EventImportance.Low,
                $"{vehicleName} used {evt.Template.name}{suffix}",
                evt.Vehicle != null ? evt.Vehicle.CurrentStage : null,
                evt.Vehicle);
        }

        private static void LogConsumableRestored(ConsumableRestoredEvent evt)
        {
            bool isPlayer = evt.Vehicle != null && evt.Vehicle.controlType == ControlType.Player;
            string vehicleName = evt.Vehicle != null ? evt.Vehicle.vehicleName : "Unknown";

            RaceHistory.Log(
                EventType.Resource,
                isPlayer ? EventImportance.Medium : EventImportance.Low,
                $"{vehicleName} restored {evt.Amount}x {evt.Template.name} ({evt.ChargesAfter} total)",
                evt.Vehicle != null ? evt.Vehicle.CurrentStage : null,
                evt.Vehicle);
        }

        private static void LogConsumableUnavailable(ConsumableUnavailableEvent evt)
        {
            string vehicleName = evt.Vehicle != null ? evt.Vehicle.vehicleName : "Unknown";

            RaceHistory.Log(
                EventType.Resource,
                EventImportance.High,
                $"{vehicleName} tried to use {evt.Template.name} but had no charges",
                evt.Vehicle != null ? evt.Vehicle.CurrentStage : null,
                evt.Vehicle);
        }
    }
}
