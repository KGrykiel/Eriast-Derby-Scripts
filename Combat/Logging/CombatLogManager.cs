using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Combat.Restoration;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Core;
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
            LogRestorations(action);
            LogCharacterConditions(action);
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
                targetVehicle?.currentStage,
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
                targetVehicle?.currentStage,
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
                sourceVehicle?.currentStage,
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
                attackerVehicle?.currentStage,
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
                targetVehicle?.currentStage ?? attackerVehicle?.currentStage,
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
                LogEntityConditionRefreshed(evt);

            foreach (var evt in action.Get<EntityConditionIgnoredEvent>())
                LogEntityConditionIgnored(evt);

            foreach (var evt in action.Get<EntityConditionReplacedEvent>())
                LogEntityConditionReplaced(evt);

            foreach (var evt in action.Get<EntityConditionKeptStrongerEvent>())
                LogEntityConditionKeptStronger(evt);

            foreach (var evt in action.Get<EntityConditionStackLimitEvent>())
                LogEntityConditionStackLimit(evt);

            foreach (var evt in action.Get<EntityConditionExpiredEvent>())
                LogEntityConditionExpired(evt);

            foreach (var evt in action.Get<EntityConditionRemovedByTriggerEvent>())
                LogEntityConditionRemovedByTrigger(evt);
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
                targetVehicle?.currentStage,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(applied));
        }

        private static void LogEntityConditionExpired(EntityConditionExpiredEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Expired.template.effectName} has expired",
                targetVehicle?.currentStage,
                targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(evt.Expired));
        }

        private static void LogEntityConditionRefreshed(EntityConditionRefreshedEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            string durationText = evt.Refreshed.IsIndefinite
                ? "indefinite"
                : $"{evt.Refreshed.turnsRemaining} turns";

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Refreshed.template.effectName} refreshed ({durationText})",
                targetVehicle?.currentStage,
                targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(evt.Refreshed));
        }

        private static void LogEntityConditionIgnored(EntityConditionIgnoredEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Existing.template.effectName} reapplication ignored (already active)",
                targetVehicle?.currentStage,
                targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(evt.Existing));
        }

        private static void LogEntityConditionReplaced(EntityConditionReplacedEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            string newDurationText = evt.NewEffect.IsIndefinite
                ? "indefinite"
                : $"{evt.NewEffect.turnsRemaining} turns";

            string oldDurationText = evt.OldDuration == -1
                ? "indefinite"
                : $"{evt.OldDuration} turns";

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.NewEffect.template.effectName} replaced ({oldDurationText} → {newDurationText})",
                targetVehicle?.currentStage,
                targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(evt.NewEffect));
        }

        private static void LogEntityConditionKeptStronger(EntityConditionKeptStrongerEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            string durationText = evt.Kept.IsIndefinite
                ? "indefinite"
                : $"{evt.Kept.turnsRemaining} turns";

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Kept.template.effectName} kept stronger version ({durationText})",
                targetVehicle?.currentStage,
                targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(evt.Kept));
        }

        private static void LogEntityConditionStackLimit(EntityConditionStackLimitEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Template.effectName} stack limit reached ({evt.MaxStacks})",
                targetVehicle?.currentStage,
                targetVehicle);
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
                targetVehicle?.currentStage,
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
                LogCharacterConditionRefreshed(evt);

            foreach (var evt in action.Get<CharacterConditionIgnoredEvent>())
                LogCharacterConditionIgnored(evt);

            foreach (var evt in action.Get<CharacterConditionReplacedEvent>())
                LogCharacterConditionReplaced(evt);

            foreach (var evt in action.Get<CharacterConditionKeptStrongerEvent>())
                LogCharacterConditionKeptStronger(evt);

            foreach (var evt in action.Get<CharacterConditionStackLimitEvent>())
                LogCharacterConditionStackLimit(evt);

            foreach (var evt in action.Get<CharacterConditionExpiredEvent>())
                LogCharacterConditionExpired(evt);

            foreach (var evt in action.Get<CharacterConditionRemovedByTriggerEvent>())
                LogCharacterConditionRemovedByTrigger(evt);
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

        private static void LogCharacterConditionExpired(CharacterConditionExpiredEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Expired.template.effectName} has expired");

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(evt.Expired));
        }

        private static void LogCharacterConditionRefreshed(CharacterConditionRefreshedEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);
            string durationText = evt.Refreshed.IsIndefinite ? "indefinite" : $"{evt.Refreshed.turnsRemaining} turns";

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Refreshed.template.effectName} refreshed ({durationText})");

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(evt.Refreshed));
        }

        private static void LogCharacterConditionIgnored(CharacterConditionIgnoredEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Existing.template.effectName} reapplication ignored (already active)");

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(evt.Existing));
        }

        private static void LogCharacterConditionReplaced(CharacterConditionReplacedEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);
            string newDurationText = evt.NewCondition.IsIndefinite ? "indefinite" : $"{evt.NewCondition.turnsRemaining} turns";
            string oldDurationText = evt.OldDuration == -1 ? "indefinite" : $"{evt.OldDuration} turns";

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.NewCondition.template.effectName} replaced ({oldDurationText} → {newDurationText})");

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(evt.NewCondition));
        }

        private static void LogCharacterConditionKeptStronger(CharacterConditionKeptStrongerEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);
            string durationText = evt.Kept.IsIndefinite ? "indefinite" : $"{evt.Kept.turnsRemaining} turns";

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Kept.template.effectName} kept stronger version ({durationText})");

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(evt.Kept));
        }

        private static void LogCharacterConditionStackLimit(CharacterConditionStackLimitEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);

            RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Template.effectName} stack limit reached ({evt.MaxStacks})");
        }

        private static void LogEntityConditionRemovedByTrigger(EntityConditionRemovedByTriggerEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target);

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Removed.template.effectName} removed by trigger ({evt.Trigger})",
                targetVehicle?.currentStage,
                targetVehicle);

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatEntityConditionTooltip(evt.Removed));
        }

        private static void LogCharacterConditionRemovedByTrigger(CharacterConditionRemovedByTriggerEvent evt)
        {
            string targetName = CombatDisplayHelpers.FormatSeatName(evt.TargetSeat);

            var logEvt = RaceHistory.Log(
                EventType.Condition, EventImportance.Low,
                $"{targetName}'s {evt.Removed.template.effectName} removed by trigger ({evt.Trigger})");

            logEvt.WithMetadata("effectBreakdown", CombatFormatter.FormatCharacterConditionTooltip(evt.Removed));
        }
    }
}
