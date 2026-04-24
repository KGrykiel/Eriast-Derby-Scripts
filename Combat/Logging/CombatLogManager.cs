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
using Assets.Scripts.Managers.Race;

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

            ForEach<AttackRollEvent>(action, LogSingleAttackRoll);
            ForEach<SavingThrowEvent>(action, LogSingleSavingThrow);
            ForEach<SkillCheckEvent>(action, LogSingleSkillCheck);
            ForEach<OpposedCheckEvent>(action, LogSingleOpposedCheck);
            LogDamageByTarget(action);
            LogEntityConditions(action);
            LogVehicleConditions(action);
            LogRestorations(action);
            LogCharacterConditions(action);
            LogConsumables(action);
        }

        // ==================== ATTACK ROLL LOGGING ====================

        private static void LogSingleAttackRoll(AttackRollEvent evt)
        {
            Vehicle attackerVehicle = evt.Actor?.GetVehicle();
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string sourceName = CombatDisplayHelpers.FormatRollActor(evt.Actor);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);
            string resultText = evt.Roll.Success
                ? LogColors.Success("Hit")
                : LogColors.Failure("Miss");

            string message = $"{sourceName} attacks {targetName} with {LogColors.Ability(evt.CausalSource)}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.High : EventImportance.Medium;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                RacePositionTracker.GetStage(targetVehicle),
                attackerVehicle, targetVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatAttackDetailed(evt.Roll));

            if (evt.Target != null)
            {
                var (totalAC, baseAC, acModifiers) = StatCalculator.GatherDefenseValueWithBreakdown(evt.Target);
                logEvt.WithMetadata("defenseBreakdown", CombatFormatter.FormatDefenseDetailed(totalAC, baseAC, acModifiers, "AC"));
            }
        }

        // ==================== SAVING THROW LOGGING ====================

        private static void LogSingleSavingThrow(SavingThrowEvent evt)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = evt.Defender?.GetVehicle();

            string targetName = CombatDisplayHelpers.FormatRollActor(evt.Defender);
            string skillName = evt.CausalSource;
            string saveTypeName = evt.CheckName;

            string resultText;
            if (evt.Roll.Success)
                resultText = LogColors.Success("Saved");
            else if (evt.Roll.IsAutoFail)
                resultText = LogColors.Failure("Auto-Failed");
            else
                resultText = LogColors.Failure("Failed");

            string message = $"{targetName} attempts {LogColors.Skill(saveTypeName)} save vs {LogColors.Ability(skillName)}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.Medium : EventImportance.High;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                RacePositionTracker.GetStage(targetVehicle),
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatSaveDetailed(evt.Roll, saveTypeName));

            if (evt.CausalSource != null)
                logEvt.WithMetadata("dcBreakdown", CombatFormatter.FormatDCDetailed(evt.Roll.TargetValue, evt.CausalSource, saveTypeName));
        }

        // ==================== SKILL CHECK LOGGING ====================

        private static void LogSingleSkillCheck(SkillCheckEvent evt)
        {
            Vehicle sourceVehicle = evt.Actor?.GetVehicle();

            string sourceName = CombatDisplayHelpers.FormatRollActor(evt.Actor);
            string skillName = evt.CausalSource;
            string checkTypeName = evt.CheckName;

            string resultText;
            if (evt.Roll.Success)
                resultText = LogColors.Success("Success");
            else if (evt.Roll.IsAutoFail)
                resultText = LogColors.Failure("Auto-Failed");
            else
                resultText = LogColors.Failure("Failure");

            string message = $"{sourceName} attempts {LogColors.Skill(checkTypeName)} check for {LogColors.Ability(skillName)}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.Medium : EventImportance.High;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                RacePositionTracker.GetStage(sourceVehicle),
                sourceVehicle);

            logEvt.WithMetadata("rollBreakdown", CombatFormatter.FormatSkillCheckDetailed(evt.Roll, checkTypeName));

            if (evt.CausalSource != null)
                logEvt.WithMetadata("dcBreakdown", CombatFormatter.FormatDCDetailed(evt.Roll.TargetValue, evt.CausalSource, checkTypeName, "Check"));
        }

        // ==================== OPPOSED CHECK LOGGING ====================

        private static void LogSingleOpposedCheck(OpposedCheckEvent evt)
        {
            Vehicle attackerVehicle = evt.AttackerActor?.GetVehicle();
            Vehicle defenderVehicle = evt.DefenderActor?.GetVehicle();

            string winnerName = evt.Roll.Success
                ? (attackerVehicle != null ? CombatDisplayHelpers.FormatVehicleName(attackerVehicle.vehicleName) : "Attacker")
                : (defenderVehicle != null ? CombatDisplayHelpers.FormatVehicleName(defenderVehicle.vehicleName) : "Defender");
            string loserName = evt.Roll.Success
                ? (defenderVehicle != null ? CombatDisplayHelpers.FormatVehicleName(defenderVehicle.vehicleName) : "Defender")
                : (attackerVehicle != null ? CombatDisplayHelpers.FormatVehicleName(attackerVehicle.vehicleName) : "Attacker");
            string skillName = evt.CausalSource;
            int attackerTotal = evt.Roll.Total;
            int defenderTotal = evt.Roll.TargetValue;

            string message = $"{winnerName} wins {LogColors.Ability(skillName)} against {loserName} ({LogColors.Number(attackerTotal.ToString())} vs {LogColors.Number(defenderTotal.ToString())}).";

            var logEvt = RaceHistory.Log(
                EventType.Combat, EventImportance.High, message,
                RacePositionTracker.GetStage(attackerVehicle),
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

            var logStage = RacePositionTracker.GetStage(targetVehicle);
            if (logStage == null) logStage = RacePositionTracker.GetStage(attackerVehicle);
            var logEvt = RaceHistory.Log(
                EventType.Combat, EventImportance.High, message,
                logStage,
                attackerVehicle, targetVehicle);

            var results = damages.Select(d => d.Result).ToList();
            logEvt.WithMetadata("damageBreakdown", CombatFormatter.FormatCombinedDamageDetailed(results, causalSource));
        }

        // ==================== STATUS EFFECT LOGGING ====================

        private static void LogEntityConditions(CombatAction action)
        {
            ForEach<EntityConditionEvent>(action, evt => { if (evt.Applied != null) LogSingleEntityCondition(evt); });
            ForEach<EntityConditionRefreshedEvent>(action, evt => LogConditionRefreshed(EntityContext(evt.Target), evt.Refreshed, CombatFormatter.FormatEntityConditionTooltip(evt.Refreshed)));
            ForEach<EntityConditionIgnoredEvent>(action, evt => LogConditionIgnored(EntityContext(evt.Target), evt.Existing, CombatFormatter.FormatEntityConditionTooltip(evt.Existing)));
            ForEach<EntityConditionReplacedEvent>(action, evt => LogConditionReplaced(EntityContext(evt.Target), evt.NewEffect, evt.OldDuration, CombatFormatter.FormatEntityConditionTooltip(evt.NewEffect)));
            ForEach<EntityConditionKeptStrongerEvent>(action, evt => LogConditionKeptStronger(EntityContext(evt.Target), evt.Kept, CombatFormatter.FormatEntityConditionTooltip(evt.Kept)));
            ForEach<EntityConditionStackLimitEvent>(action, evt => LogConditionStackLimit(EntityContext(evt.Target), evt.Template.effectName, evt.MaxStacks));
            ForEach<EntityConditionExpiredEvent>(action, evt => LogConditionExpired(EntityContext(evt.Target), evt.Expired, CombatFormatter.FormatEntityConditionTooltip(evt.Expired)));
            ForEach<EntityConditionRemovedByTriggerEvent>(action, evt => LogConditionRemovedByTrigger(EntityContext(evt.Target), evt.Removed, evt.Trigger.ToString(), CombatFormatter.FormatEntityConditionTooltip(evt.Removed)));
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
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            bool isSelf = CombatDisplayHelpers.IsSelfTarget(evt.Source, evt.Target, sourceVehicle, targetVehicle);
            string message;

            if (evt.Source == null)
            {
                message = $"{targetName} gains {LogColors.Condition(effect.effectName, isBuff)} from {evt.CausalSource} ({LogColors.Duration(durationText)})";
            }
            else if (isSelf)
            {
                message = $"{targetName} gains {LogColors.Condition(effect.effectName, isBuff)} ({LogColors.Duration(durationText)})";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source);
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceName} {actionVerb} {LogColors.Condition(effect.effectName, isBuff)} on {targetName} ({LogColors.Duration(durationText)})";
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
                RacePositionTracker.GetStage(targetVehicle),
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
            BuildRestorationPart(parts, restorations, ResourceType.Health, "HP",     LogColors.Health);
            BuildRestorationPart(parts, restorations, ResourceType.Energy, "energy", LogColors.Energy);

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
                RacePositionTracker.GetStage(targetVehicle),
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
                return LogColors.DamageType(d.DamageType, $"{d.FinalDamage} {d.DamageType} damage");
            }

            var parts = damages.Select(d =>
                LogColors.DamageType(d.Result.DamageType, $"{d.Result.FinalDamage} {d.Result.DamageType}")
            );
            int total = damages.Sum(d => d.Result.FinalDamage);

            return $"{string.Join(" + ", parts)} ({LogColors.Damage($"{total} total")}) damage";
        }

        private static void ForEach<TEvent>(CombatAction action, Action<TEvent> handler) where TEvent : CombatEvent
        {
            foreach (var evt in action.Get<TEvent>())
                handler(evt);
        }

        private static void BuildRestorationPart(
            List<string> parts,
            List<RestorationEvent> restorations,
            ResourceType resourceType,
            string resourceName,
            Func<string, string> format)
        {
            var filtered = restorations.Where(r => r.Result.ResourceType == resourceType).ToList();
            if (filtered.Count == 0) return;

            int total = filtered.Sum(r => r.Result.ActualChange);
            if (total == 0) return;

            string verb = total > 0 ? "restores" : "drains";
            parts.Add($"{verb} {format($"{Math.Abs(total)} {resourceName}")}");
        }

        // ==================== CHARACTER CONDITION LOGGING ====================

        private static void LogCharacterConditions(CombatAction action)
        {
            ForEach<CharacterConditionEvent>(action, LogSingleCharacterCondition);
            ForEach<CharacterConditionRefreshedEvent>(action, evt => LogConditionRefreshed(CharacterContext(evt.TargetSeat), evt.Refreshed, CombatFormatter.FormatCharacterConditionTooltip(evt.Refreshed)));
            ForEach<CharacterConditionIgnoredEvent>(action, evt => LogConditionIgnored(CharacterContext(evt.TargetSeat), evt.Existing, CombatFormatter.FormatCharacterConditionTooltip(evt.Existing)));
            ForEach<CharacterConditionReplacedEvent>(action, evt => LogConditionReplaced(CharacterContext(evt.TargetSeat), evt.NewCondition, evt.OldDuration, CombatFormatter.FormatCharacterConditionTooltip(evt.NewCondition)));
            ForEach<CharacterConditionKeptStrongerEvent>(action, evt => LogConditionKeptStronger(CharacterContext(evt.TargetSeat), evt.Kept, CombatFormatter.FormatCharacterConditionTooltip(evt.Kept)));
            ForEach<CharacterConditionStackLimitEvent>(action, evt => LogConditionStackLimit(CharacterContext(evt.TargetSeat), evt.Template.effectName, evt.MaxStacks));
            ForEach<CharacterConditionExpiredEvent>(action, evt => LogConditionExpired(CharacterContext(evt.TargetSeat), evt.Expired, CombatFormatter.FormatCharacterConditionTooltip(evt.Expired)));
            ForEach<CharacterConditionRemovedByTriggerEvent>(action, evt => LogConditionRemovedByTrigger(CharacterContext(evt.TargetSeat), evt.Removed, evt.Trigger.ToString(), CombatFormatter.FormatCharacterConditionTooltip(evt.Removed)));
        }

        private static void LogSingleCharacterCondition(CharacterConditionEvent evt)
        {
            if (evt.Applied == null) return;

            var applied = evt.Applied;
            var condition = applied.template;

            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);
            bool isBuff = CombatDisplayHelpers.DetermineIfBuff(condition);
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            string message;
            if (evt.Source == null)
            {
                message = $"{targetName} gains {LogColors.Condition(condition.effectName, isBuff)} from {evt.CausalSource} ({LogColors.Duration(durationText)})";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source);
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceName} {actionVerb} {LogColors.Condition(condition.effectName, isBuff)} on {targetName} ({LogColors.Duration(durationText)})";
            }

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Medium, message);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(applied));
        }

        // ==================== VEHICLE CONDITION LOGGING ====================

        private static void LogVehicleConditions(CombatAction action)
        {
            ForEach<VehicleConditionEvent>(action, evt => { if (evt.Applied != null) LogSingleVehicleCondition(evt); });
            ForEach<VehicleConditionRefreshedEvent>(action, evt => LogConditionRefreshed(VehicleContext(evt.Target), evt.Refreshed, CombatFormatter.FormatVehicleConditionTooltip(evt.Refreshed)));
            ForEach<VehicleConditionIgnoredEvent>(action, evt => LogConditionIgnored(VehicleContext(evt.Target), evt.Existing, CombatFormatter.FormatVehicleConditionTooltip(evt.Existing)));
            ForEach<VehicleConditionReplacedEvent>(action, evt => LogConditionReplaced(VehicleContext(evt.Target), evt.NewCondition, evt.OldDuration, CombatFormatter.FormatVehicleConditionTooltip(evt.NewCondition)));
            ForEach<VehicleConditionKeptStrongerEvent>(action, evt => LogConditionKeptStronger(VehicleContext(evt.Target), evt.Kept, CombatFormatter.FormatVehicleConditionTooltip(evt.Kept)));
            ForEach<VehicleConditionStackLimitEvent>(action, evt => LogConditionStackLimit(VehicleContext(evt.Target), evt.Template.effectName, evt.MaxStacks));
            ForEach<VehicleConditionExpiredEvent>(action, evt => LogConditionExpired(VehicleContext(evt.Target), evt.Expired, CombatFormatter.FormatVehicleConditionTooltip(evt.Expired)));
            ForEach<VehicleConditionRemovedByTriggerEvent>(action, evt => LogConditionRemovedByTrigger(VehicleContext(evt.Target), evt.Removed, evt.Trigger.ToString(), CombatFormatter.FormatVehicleConditionTooltip(evt.Removed)));
        }

        private static void LogSingleVehicleCondition(VehicleConditionEvent evt)
        {
            if (evt.Applied == null) return;

            var applied = evt.Applied;
            var condition = applied.template;

            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = evt.Target;
            string targetName = targetVehicle != null ? CombatDisplayHelpers.FormatVehicleName(targetVehicle.vehicleName) : "Unknown";
            bool isBuff = CombatDisplayHelpers.DetermineIfBuff(condition);
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            string message;
            if (evt.Source == null)
            {
                message = $"{targetName} gains {LogColors.Condition(condition.effectName, isBuff)} from {evt.CausalSource} ({LogColors.Duration(durationText)})";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source);
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceName} {actionVerb} {LogColors.Condition(condition.effectName, isBuff)} on {targetName} ({LogColors.Duration(durationText)})";
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
                RacePositionTracker.GetStage(targetVehicle),
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
            string name = target != null ? CombatDisplayHelpers.FormatVehicleName(target.vehicleName) : "Unknown";
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
                return RaceHistory.Log(EventType.Condition, importance, message, RacePositionTracker.GetStage(ctx.TargetVehicle), ctx.TargetVehicle);
            return RaceHistory.Log(EventType.Condition, importance, message);
        }

        private static void LogConditionExpired(ConditionLogContext ctx, AppliedConditionBase applied, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {applied.Template.effectName} has expired", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionRefreshed(ConditionLogContext ctx, AppliedConditionBase applied, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {applied.Template.effectName} refreshed ({LogColors.Duration(DurationText(applied))})", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionIgnored(ConditionLogContext ctx, AppliedConditionBase existing, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {existing.Template.effectName} reapplication ignored (already active)", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionReplaced(ConditionLogContext ctx, AppliedConditionBase newCondition, int oldDuration, string tooltip)
        {
            string msg = $"{ctx.TargetName}'s {newCondition.Template.effectName} replaced ({LogColors.Duration(DurationText(oldDuration))} -> {LogColors.Duration(DurationText(newCondition))})";
            var logEvt = LogConditionEvent(EventImportance.Low, msg, ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionKeptStronger(ConditionLogContext ctx, AppliedConditionBase kept, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {kept.Template.effectName} kept stronger version ({LogColors.Duration(DurationText(kept))})", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        private static void LogConditionStackLimit(ConditionLogContext ctx, string effectName, int maxStacks)
        {
            LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {effectName} stack limit reached ({LogColors.Number(maxStacks.ToString())})", ctx);
        }

        private static void LogConditionRemovedByTrigger(ConditionLogContext ctx, AppliedConditionBase removed, string trigger, string tooltip)
        {
            var logEvt = LogConditionEvent(EventImportance.Low, $"{ctx.TargetName}'s {removed.Template.effectName} removed by trigger ({trigger})", ctx);
            logEvt.WithMetadata("effectBreakdown", tooltip);
        }

        // ==================== CONSUMABLE LOGGING ====================

        private static void LogConsumables(CombatAction action)
        {
            ForEach<ConsumableSpentEvent>(action, LogConsumableSpent);
            ForEach<ConsumableRestoredEvent>(action, LogConsumableRestored);
            ForEach<ConsumableUnavailableEvent>(action, LogConsumableUnavailable);
        }

        private static void LogConsumableSpent(ConsumableSpentEvent evt)
        {
            bool isPlayer = evt.Vehicle != null && evt.Vehicle.controlType == ControlType.Player;
            string vehicleName = evt.Vehicle != null ? CombatDisplayHelpers.FormatVehicleName(evt.Vehicle.vehicleName) : "Unknown";
            string suffix = evt.ChargesRemaining == 0
                ? " (last charge)"
                : $" ({LogColors.Number($"{evt.ChargesRemaining} remaining")})";

            RaceHistory.Log(
                EventType.Resource,
                isPlayer ? EventImportance.Medium : EventImportance.Low,
                $"{vehicleName} used {evt.Template.name}{suffix}",
                RacePositionTracker.GetStage(evt.Vehicle),
                evt.Vehicle);
        }

        private static void LogConsumableRestored(ConsumableRestoredEvent evt)
        {
            bool isPlayer = evt.Vehicle != null && evt.Vehicle.controlType == ControlType.Player;
            string vehicleName = evt.Vehicle != null ? CombatDisplayHelpers.FormatVehicleName(evt.Vehicle.vehicleName) : "Unknown";

            RaceHistory.Log(
                EventType.Resource,
                isPlayer ? EventImportance.Medium : EventImportance.Low,
                $"{vehicleName} restored {LogColors.Number($"{evt.Amount}x")} {evt.Template.name} ({LogColors.Number($"{evt.ChargesAfter} total")})",
                RacePositionTracker.GetStage(evt.Vehicle),
                evt.Vehicle);
        }

        private static void LogConsumableUnavailable(ConsumableUnavailableEvent evt)
        {
            string vehicleName = evt.Vehicle != null ? CombatDisplayHelpers.FormatVehicleName(evt.Vehicle.vehicleName) : "Unknown";

            RaceHistory.Log(
                EventType.Resource,
                EventImportance.High,
                $"{vehicleName} tried to use {evt.Template.name} but had no charges",
                RacePositionTracker.GetStage(evt.Vehicle),
                evt.Vehicle);
        }
    }
}
