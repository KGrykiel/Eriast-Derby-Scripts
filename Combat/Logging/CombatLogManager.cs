using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Combat.Restoration;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Core;

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
            LogStatusEffects(action);
            LogRestorations(action);
        }

        public static void LogImmediate(CombatEvent evt)
        {
            switch (evt)
            {
                case DamageEvent damage:        LogSingleDamage(damage); break;
                case StatusEffectEvent status:   LogSingleStatusEffect(status); break;
                case StatusEffectExpiredEvent expired: LogStatusExpired(expired); break;
                case StatusEffectRefreshedEvent refreshed: LogStatusRefreshed(refreshed); break;
                case StatusEffectIgnoredEvent ignored: LogStatusIgnored(ignored); break;
                case StatusEffectReplacedEvent replaced: LogStatusReplaced(replaced); break;
                case StatusEffectKeptStrongerEvent kept: LogStatusKeptStronger(kept); break;
                case StatusEffectStackLimitEvent stackLimit: LogStatusStackLimit(stackLimit); break;
                case RestorationEvent restoration:     LogSingleRestoration(restoration); break;
                case AttackRollEvent attack:     LogSingleAttackRoll(attack); break;
                case SavingThrowEvent save:      LogSingleSavingThrow(save); break;
                case SkillCheckEvent check:      LogSingleSkillCheck(check); break;
                case OpposedCheckEvent opposed:  LogSingleOpposedCheck(opposed); break;
            }
        }

        // ==================== ATTACK ROLL LOGGING ====================

        private static void LogAttackRolls(CombatAction action)
        {
            foreach (var evt in action.GetAttackRollEvents())
                LogSingleAttackRoll(evt, action);
        }

        private static void LogSingleAttackRoll(AttackRollEvent evt, CombatAction action = null)
        {
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source) ?? action?.ActorVehicle;
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string sourceName = CombatDisplayHelpers.FormatRollActor(evt.Actor, attackerVehicle);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);
            string skillName = action?.SourceName ?? evt.CausalSource ?? "attack";

            string resultText = evt.Roll.Success
                ? $"<color={CombatFormatter.Colors.Success}>Hit</color>"
                : $"<color={CombatFormatter.Colors.Failure}>Miss</color>";

            string message = $"{sourceName} attacks {targetName}. {resultText}";
            var importance = evt.Roll.Success ? EventImportance.High : EventImportance.Medium;

            var logEvt = RaceHistory.Log(
                EventType.Combat, importance, message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                attackerVehicle, targetVehicle);

            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.Roll.Success ? "hit" : "miss")
                  .WithMetadata("rollBreakdown", evt.Roll != null ? CombatFormatter.FormatAttackDetailed(evt.Roll) : "");

            if (evt.Target != null)
            {
                var (totalAC, baseAC, acModifiers) = StatCalculator.GatherDefenseValueWithBreakdown(evt.Target);
                logEvt.WithMetadata("defenseBreakdown", CombatFormatter.FormatDefenseDetailed(totalAC, baseAC, acModifiers, "AC"));
            }

            if (evt.Source is VehicleComponent sourceComp)
                logEvt.WithMetadata("sourceComponent", sourceComp.name);
            if (evt.Target is VehicleComponent targetComp)
                logEvt.WithMetadata("targetComponent", targetComp.name);
        }

        // ==================== SAVING THROW LOGGING ====================

        private static void LogSavingThrows(CombatAction action)
        {
            foreach (var evt in action.GetSavingThrowEvents())
                LogSingleSavingThrow(evt, action);
        }

        private static void LogSingleSavingThrow(SavingThrowEvent evt, CombatAction action = null)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string targetName = CombatDisplayHelpers.FormatRollActorDefensive(evt.Defender, targetVehicle);
            string skillName = action?.SourceName ?? evt.CausalSource ?? "effect";
            string saveTypeName = evt.CheckName ?? "Mobility";

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
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.Roll.Success ? "saved" : "failed")
                  .WithMetadata("saveType", saveTypeName)
                  .WithMetadata("rollBreakdown", evt.Roll != null ? CombatFormatter.FormatSaveDetailed(evt.Roll, saveTypeName) : "");

            if (evt.Roll != null && evt.CausalSource != null)
                logEvt.WithMetadata("dcBreakdown", CombatFormatter.FormatDCDetailed(evt.Roll.TargetValue, evt.CausalSource, saveTypeName));
        }

        // ==================== SKILL CHECK LOGGING ====================

        private static void LogSkillChecks(CombatAction action)
        {
            foreach (var evt in action.GetSkillCheckEvents())
                LogSingleSkillCheck(evt, action);
        }

        private static void LogSingleSkillCheck(SkillCheckEvent evt, CombatAction action = null)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);

            string sourceName = CombatDisplayHelpers.FormatRollActor(evt.Actor, sourceVehicle);
            string skillName = action?.SourceName ?? evt.CausalSource ?? "task";
            string checkTypeName = evt.CheckName ?? "Mobility";

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
                sourceVehicle != null ? sourceVehicle.currentStage : null,
                sourceVehicle, null);

            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.Roll.Success ? "success" : "failure")
                  .WithMetadata("checkType", checkTypeName)
                  .WithMetadata("rollBreakdown", evt.Roll != null ? CombatFormatter.FormatSkillCheckDetailed(evt.Roll, checkTypeName) : "");

            if (evt.Roll != null && evt.CausalSource != null)
                logEvt.WithMetadata("dcBreakdown", $"{checkTypeName} Check DC: {evt.Roll.TargetValue} ({evt.CausalSource})");
        }

        // ==================== OPPOSED CHECK LOGGING ====================

        private static void LogOpposedChecks(CombatAction action)
        {
            foreach (var evt in action.GetOpposedCheckEvents())
                LogSingleOpposedCheck(evt, action);
        }

        private static void LogSingleOpposedCheck(OpposedCheckEvent evt, CombatAction action = null)
        {
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle defenderVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string winnerName = evt.Roll.Success
                ? (attackerVehicle?.vehicleName ?? "Attacker")
                : (defenderVehicle?.vehicleName ?? "Defender");
            string loserName = evt.Roll.Success
                ? (defenderVehicle?.vehicleName ?? "Defender")
                : (attackerVehicle?.vehicleName ?? "Attacker");
            string skillName = action?.SourceName ?? evt.CausalSource ?? "contest";
            int attackerTotal = evt.Roll?.Total ?? 0;
            int defenderTotal = evt.Roll?.TargetValue ?? 0;

            string message = $"{winnerName} wins {skillName} against {loserName} ({attackerTotal} vs {defenderTotal}).";

            var logEvt = RaceHistory.Log(
                EventType.Combat, EventImportance.High, message,
                attackerVehicle != null ? attackerVehicle.currentStage : null,
                attackerVehicle, defenderVehicle);

            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.Roll.Success ? "attacker_wins" : "defender_wins")
                  .WithMetadata("rollBreakdown", evt.Roll != null ? CombatFormatter.FormatOpposedCheckDetailed(evt.Roll, evt.DefenderRoll, evt.AttackerCheckName, evt.DefenderCheckName) : "");
        }

        // ==================== DAMAGE LOGGING ====================

        private static void LogDamageByTarget(CombatAction action)
        {
            foreach (var kvp in action.GetDamageByTarget())
            {
                if (kvp.Value.Count > 0)
                    LogCombinedDamage(kvp.Value, action, kvp.Key);
            }
        }

        private static void LogCombinedDamage(List<DamageEvent> damages, CombatAction action, Entity target)
        {
            Vehicle attackerVehicle = action.ActorVehicle;
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

            string sourceName = CombatDisplayHelpers.FormatActionSource(action);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(target, targetVehicle);
            bool isSelf = CombatDisplayHelpers.IsSelfTarget(action.Actor, target, attackerVehicle, targetVehicle);

            string damageText = BuildCombinedDamageText(damages);
            int totalDamage = damages.Sum(d => d.Result.FinalDamage);

            string message = isSelf
                ? $"{targetName} takes {damageText}"
                : $"{sourceName} deals {damageText} to {targetName}";

            var logEvt = RaceHistory.Log(
                EventType.Combat, EventImportance.High, message,
                targetVehicle != null ? targetVehicle.currentStage : (attackerVehicle != null ? attackerVehicle.currentStage : null),
                attackerVehicle, targetVehicle);

            var results = damages.Select(d => d.Result).ToList();
            logEvt.WithMetadata("skillName", action.SourceName)
                  .WithMetadata("totalDamage", totalDamage)
                  .WithMetadata("isSelfDamage", isSelf)
                  .WithMetadata("damageBreakdown", CombatFormatter.FormatCombinedDamageDetailed(results, action.SourceName));

            if (action.Actor is VehicleComponent sourceComp)
                logEvt.WithMetadata("sourceComponent", sourceComp.name);
            if (target is VehicleComponent targetComp)
                logEvt.WithMetadata("targetComponent", targetComp.name);
        }

        private static void LogSingleDamage(DamageEvent evt)
        {
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);
            string causalSourceName = evt.CausalSource ?? "Unknown";

            int damage = evt.Result.FinalDamage;
            string damageType = evt.Result.DamageType.ToString();

            string message;
            if (evt.Source == null)
            {
                message = $"{targetName} takes <color={CombatFormatter.Colors.Damage}>{damage}</color> {damageType} damage from {causalSourceName}";
            }
            else
            {
                string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source, attackerVehicle);
                bool isSelf = CombatDisplayHelpers.IsSelfTarget(evt.Source, evt.Target, attackerVehicle, targetVehicle);

                message = isSelf
                    ? $"{targetName} takes <color={CombatFormatter.Colors.Damage}>{damage}</color> {damageType} damage"
                    : $"{sourceName} deals <color={CombatFormatter.Colors.Damage}>{damage}</color> {damageType} damage to {targetName}";
            }

            bool playerInvolved = (attackerVehicle != null && attackerVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);

            var logEvt = RaceHistory.Log(
                EventType.Combat,
                playerInvolved ? EventImportance.High : EventImportance.Medium,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                attackerVehicle, targetVehicle);

            logEvt.WithMetadata("damage", damage)
                  .WithMetadata("damageType", damageType)
                  .WithMetadata("source", causalSourceName)
                  .WithMetadata("damageBreakdown", CombatFormatter.FormatDamageDetailed(evt.Result));
        }

        // ==================== STATUS EFFECT LOGGING ====================

        private static void LogStatusEffects(CombatAction action)
        {
            foreach (var evt in action.GetStatusEffectEvents().Where(e => !e.WasBlocked))
                LogSingleStatusEffect(evt, action);

            foreach (var evt in action.GetStatusRefreshedEvents())
                LogStatusRefreshed(evt);

            foreach (var evt in action.GetStatusIgnoredEvents())
                LogStatusIgnored(evt);

            foreach (var evt in action.GetStatusReplacedEvents())
                LogStatusReplaced(evt);

            foreach (var evt in action.GetStatusKeptStrongerEvents())
                LogStatusKeptStronger(evt);

            foreach (var evt in action.GetStatusStackLimitEvents())
                LogStatusStackLimit(evt);

            foreach (var evt in action.GetStatusExpiredEvents())
                LogStatusExpired(evt);
        }

        private static void LogSingleStatusEffect(StatusEffectEvent evt, CombatAction action = null)
        {
            if (evt.WasBlocked || evt.Applied == null) return;

            var applied = evt.Applied;
            var effect = applied.template;

            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string sourceName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Source, sourceVehicle);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            bool isBuff = CombatDisplayHelpers.DetermineIfBuff(effect);
            string color = isBuff ? CombatFormatter.Colors.Success : CombatFormatter.Colors.Failure;
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";

            bool isSelf = CombatDisplayHelpers.IsSelfTarget(evt.Source, evt.Target, sourceVehicle, targetVehicle);
            string message;

            if (evt.Source == null)
            {
                string causalName = evt.CausalSource ?? action?.SourceName ?? "Unknown";
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> from {causalName} ({durationText})";
            }
            else if (isSelf)
            {
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> ({durationText})";
            }
            else
            {
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
                EventType.StatusEffect, importance, message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("statusEffectName", effect.effectName)
                  .WithMetadata("duration", applied.turnsRemaining)
                  .WithMetadata("isIndefinite", applied.IsIndefinite)
                  .WithMetadata("isBuff", isBuff)
                  .WithMetadata("isSelfTarget", isSelf)
                  .WithMetadata("effectBreakdown", CombatFormatter.FormatStatusEffectTooltip(applied));
        }

        private static void LogStatusExpired(StatusEffectExpiredEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            var logEvt = RaceHistory.Log(
                EventType.StatusEffect, EventImportance.Low,
                $"{targetName}'s {evt.Expired.template.effectName} has expired",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle);

            logEvt.WithMetadata("statusEffectName", evt.Expired.template.effectName)
                  .WithMetadata("expired", true)
                  .WithMetadata("effectBreakdown", CombatFormatter.FormatStatusEffectTooltip(evt.Expired));
        }

        private static void LogStatusRefreshed(StatusEffectRefreshedEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            string durationText = evt.Refreshed.IsIndefinite
                ? "indefinite"
                : $"{evt.Refreshed.turnsRemaining} turns";

            var logEvt = RaceHistory.Log(
                EventType.StatusEffect, EventImportance.Low,
                $"{targetName}'s {evt.Refreshed.template.effectName} refreshed ({durationText})",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle);

            logEvt.WithMetadata("statusEffectName", evt.Refreshed.template.effectName)
                  .WithMetadata("refreshed", true)
                  .WithMetadata("duration", evt.Refreshed.turnsRemaining)
                  .WithMetadata("effectBreakdown", CombatFormatter.FormatStatusEffectTooltip(evt.Refreshed));
        }

        private static void LogStatusIgnored(StatusEffectIgnoredEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            var logEvt = RaceHistory.Log(
                EventType.StatusEffect, EventImportance.Low,
                $"{targetName}'s {evt.Existing.template.effectName} reapplication ignored (already active)",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle);

            logEvt.WithMetadata("statusEffectName", evt.Existing.template.effectName)
                  .WithMetadata("ignored", true)
                  .WithMetadata("stackBehaviour", "Ignore")
                  .WithMetadata("effectBreakdown", CombatFormatter.FormatStatusEffectTooltip(evt.Existing));
        }

        private static void LogStatusReplaced(StatusEffectReplacedEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            string newDurationText = evt.NewEffect.IsIndefinite
                ? "indefinite"
                : $"{evt.NewEffect.turnsRemaining} turns";

            string oldDurationText = evt.OldDuration == -1
                ? "indefinite"
                : $"{evt.OldDuration} turns";

            var logEvt = RaceHistory.Log(
                EventType.StatusEffect, EventImportance.Low,
                $"{targetName}'s {evt.NewEffect.template.effectName} replaced ({oldDurationText} → {newDurationText})",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle);

            logEvt.WithMetadata("statusEffectName", evt.NewEffect.template.effectName)
                  .WithMetadata("replaced", true)
                  .WithMetadata("oldDuration", evt.OldDuration)
                  .WithMetadata("newDuration", evt.NewEffect.turnsRemaining)
                  .WithMetadata("stackBehaviour", "Replace")
                  .WithMetadata("effectBreakdown", CombatFormatter.FormatStatusEffectTooltip(evt.NewEffect));
        }

        private static void LogStatusKeptStronger(StatusEffectKeptStrongerEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            string durationText = evt.Kept.IsIndefinite
                ? "indefinite"
                : $"{evt.Kept.turnsRemaining} turns";

            var logEvt = RaceHistory.Log(
                EventType.StatusEffect, EventImportance.Low,
                $"{targetName}'s {evt.Kept.template.effectName} kept stronger version ({durationText})",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle);

            logEvt.WithMetadata("statusEffectName", evt.Kept.template.effectName)
                  .WithMetadata("keptStronger", true)
                  .WithMetadata("duration", evt.Kept.turnsRemaining)
                  .WithMetadata("stackBehaviour", "Replace")
                  .WithMetadata("effectBreakdown", CombatFormatter.FormatStatusEffectTooltip(evt.Kept));
        }

        private static void LogStatusStackLimit(StatusEffectStackLimitEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            var logEvt = RaceHistory.Log(
                EventType.StatusEffect, EventImportance.Low,
                $"{targetName}'s {evt.Template.effectName} stack limit reached ({evt.MaxStacks})",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle);

            logEvt.WithMetadata("statusEffectName", evt.Template.effectName)
                  .WithMetadata("stackLimitReached", true)
                  .WithMetadata("maxStacks", evt.MaxStacks)
                  .WithMetadata("stackBehaviour", "Stack");
        }

        // ==================== RESTORATION LOGGING ====================

        private static void LogRestorations(CombatAction action)
        {
            foreach (var kvp in action.GetRestorationByTarget())
                LogCombinedRestoration(kvp.Value, action, kvp.Key);
        }

        private static void LogCombinedRestoration(List<RestorationEvent> restorations, CombatAction action, Entity target)
        {
            Vehicle sourceVehicle = action.ActorVehicle;
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);

            string sourceName = CombatDisplayHelpers.FormatActionSource(action);
            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(target, targetVehicle);
            bool isSelf = CombatDisplayHelpers.IsSelfTarget(action.Actor, target, sourceVehicle, targetVehicle);

            var parts = new List<string>();
            BuildRestorationPart(parts, restorations, ResourceType.Health, "HP", CombatFormatter.Colors.Health);
            BuildRestorationPart(parts, restorations, ResourceType.Energy, "energy", CombatFormatter.Colors.Energy);

            if (parts.Count == 0) return;

            string restorationText = string.Join(" and ", parts);

            string message = (isSelf || action.Actor == null)
                ? $"{targetName} {restorationText}"
                : $"{sourceName} {restorationText} to {targetName}";

            bool playerInvolved = (sourceVehicle != null && sourceVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);

            var logEvt = RaceHistory.Log(
                EventType.Resource,
                playerInvolved ? EventImportance.Medium : EventImportance.Low,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle);

            var results = restorations.Select(r => r.Result).ToList();
            logEvt.WithMetadata("skillName", action.SourceName)
                  .WithMetadata("isSelfTarget", isSelf)
                  .WithMetadata("restorationBreakdown", CombatFormatter.FormatCombinedRestorationDetailed(results, action.SourceName));
        }

        private static void LogSingleRestoration(RestorationEvent evt)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);

            string targetName = CombatDisplayHelpers.FormatEntityWithVehicle(evt.Target, targetVehicle);

            bool isHealth = evt.Result.ResourceType == ResourceType.Health;
            string resourceName = isHealth ? "HP" : "energy";
            string color = isHealth ? CombatFormatter.Colors.Health : CombatFormatter.Colors.Energy;
            string verb = evt.Result.ActualChange > 0 ? "restores" : "drains";
            int absChange = Math.Abs(evt.Result.ActualChange);

            string message = $"{targetName} {verb} <color={color}>{absChange}</color> {resourceName}";

            bool playerInvolved = targetVehicle != null && targetVehicle.controlType == ControlType.Player;

            var logEvt = RaceHistory.Log(
                EventType.Resource,
                playerInvolved ? EventImportance.Medium : EventImportance.Low,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle);

            logEvt.WithMetadata("resourceType", evt.Result.ResourceType.ToString())
                  .WithMetadata("actualChange", evt.Result.ActualChange)
                  .WithMetadata("restorationBreakdown", CombatFormatter.FormatRestorationDetailed(evt.Result));
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
    }
}
