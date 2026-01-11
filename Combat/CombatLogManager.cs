using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RacingGame.Events;
using Assets.Scripts.StatusEffects;
using EventType = RacingGame.Events.EventType;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Formats and logs combat events to RaceHistory.
    /// Aggregates multiple events within an action into combined log entries.
    /// 
    /// Example output for a skill with multiple damage types:
    /// "Speed Racer deals 5 Physical + 8 Fire (13 total) damage to Enemy"
    /// </summary>
    public static class CombatLogManager
    {
        // ==================== MAIN ENTRY POINTS ====================
        
        /// <summary>
        /// Log a completed combat action with all its events.
        /// Aggregates damage by target, status effects, etc.
        /// </summary>
        public static void LogAction(CombatAction action)
        {
            if (action == null || !action.HasEvents) return;
            
            // Log attack rolls (hit/miss)
            LogAttackRolls(action);
            
            // Log aggregated damage by target
            LogDamageByTarget(action);
            
            // Log status effects
            LogStatusEffects(action);
            
            // Log restorations
            LogRestorations(action);
        }
        
        /// <summary>
        /// Log an immediate event (no action scope - DoT, environmental, etc.)
        /// </summary>
        public static void LogImmediate(CombatEvent evt)
        {
            switch (evt)
            {
                case DamageEvent damage:
                    LogSingleDamage(damage);
                    break;
                case StatusEffectEvent status:
                    LogSingleStatusEffect(status);
                    break;
                case StatusEffectExpiredEvent expired:
                    LogStatusExpired(expired);
                    break;
                case RestorationEvent restoration:
                    LogSingleRestoration(restoration);
                    break;
                case AttackRollEvent attack:
                    LogSingleAttackRoll(attack);
                    break;
            }
        }
        
        // ==================== ATTACK ROLL LOGGING ====================
        
        private static void LogAttackRolls(CombatAction action)
        {
            foreach (var attackEvent in action.GetAttackRollEvents())
            {
                LogSingleAttackRoll(attackEvent, action);
            }
        }
        
        private static void LogSingleAttackRoll(AttackRollEvent evt, CombatAction action = null)
        {
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string attackerName = attackerVehicle?.vehicleName ?? evt.Source?.GetDisplayName() ?? "Unknown";
            string targetName = targetVehicle?.vehicleName ?? evt.Target?.GetDisplayName() ?? "Unknown";
            string sourceName = action?.SourceName ?? evt.CausalSource?.name ?? "attack";
            
            // Component targeting info
            string componentText = "";
            if (!string.IsNullOrEmpty(evt.TargetComponentName))
            {
                componentText = evt.IsChassisFallback 
                    ? "'s chassis" 
                    : $"'s {evt.TargetComponentName}";
            }
            
            // Result color
            string resultText = evt.IsHit 
                ? "<color=#44FF44>Hit</color>" 
                : "<color=#FF4444>Miss</color>";
            
            string message = $"{attackerName} attacks {targetName}{componentText}. {resultText}";
            
            var importance = evt.IsHit ? EventImportance.High : EventImportance.Medium;
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                importance,
                message,
                targetVehicle?.currentStage,
                attackerVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("skillName", sourceName)
                  .WithMetadata("result", evt.IsHit ? "hit" : "miss")
                  .WithMetadata("rollBreakdown", evt.Roll?.ToDetailedString() ?? "");
            
            if (!string.IsNullOrEmpty(evt.TargetComponentName))
            {
                logEvt.WithMetadata("targetComponent", evt.TargetComponentName);
            }
        }
        
        // ==================== DAMAGE LOGGING ====================
        
        private static void LogDamageByTarget(CombatAction action)
        {
            var damageByTarget = action.GetDamageByTarget();
            
            foreach (var kvp in damageByTarget)
            {
                Entity target = kvp.Key;
                List<DamageEvent> damages = kvp.Value;
                
                if (damages.Count == 0) continue;
                
                LogCombinedDamage(damages, action, target);
            }
        }
        
        private static void LogCombinedDamage(List<DamageEvent> damages, CombatAction action, Entity target)
        {
            Vehicle attackerVehicle = action.ActorVehicle;
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
            
            string attackerName = attackerVehicle?.vehicleName ?? "Unknown";
            string targetName = GetTargetDisplayName(target, targetVehicle);
            
            // Check if self-damage
            bool isSelfDamage = attackerVehicle != null && attackerVehicle == targetVehicle;
            
            // Build damage breakdown string
            string damageText = BuildCombinedDamageText(damages);
            int totalDamage = damages.Sum(d => d.Breakdown.finalDamage);
            
            // Build message
            string message;
            if (isSelfDamage)
            {
                message = $"{targetName} takes {damageText}";
            }
            else
            {
                message = $"{attackerName} deals {damageText} to {targetName}";
            }
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                message,
                targetVehicle?.currentStage ?? attackerVehicle?.currentStage,
                attackerVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("skillName", action.SourceName)
                  .WithMetadata("totalDamage", totalDamage)
                  .WithMetadata("isSelfDamage", isSelfDamage)
                  .WithMetadata("damageBreakdown", BuildDetailedDamageBreakdown(damages));
            
            if (target is VehicleComponent comp)
            {
                logEvt.WithMetadata("targetComponent", comp.name);
            }
        }
        
        private static void LogSingleDamage(DamageEvent evt)
        {
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string targetName = GetTargetDisplayName(evt.Target, targetVehicle);
            string sourceName = evt.CausalSource?.name ?? "Unknown";
            
            int damage = evt.Breakdown.finalDamage;
            string damageType = evt.Breakdown.damageType.ToString();
            
            // For immediate events (DoT, environmental), format differently
            string message;
            if (evt.Source == null)
            {
                message = $"{targetName} takes <color=#FFA500>{damage}</color> {damageType} damage from {sourceName}";
            }
            else
            {
                string attackerName = attackerVehicle?.vehicleName ?? evt.Source.GetDisplayName();
                bool isSelfDamage = attackerVehicle != null && attackerVehicle == targetVehicle;
                
                if (isSelfDamage)
                {
                    message = $"{targetName} takes <color=#FFA500>{damage}</color> {damageType} damage";
                }
                else
                {
                    message = $"{attackerName} deals <color=#FFA500>{damage}</color> {damageType} damage to {targetName}";
                }
            }
            
            bool playerInvolved = (attackerVehicle?.controlType == ControlType.Player) ||
                                  (targetVehicle?.controlType == ControlType.Player);
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                playerInvolved ? EventImportance.High : EventImportance.Medium,
                message,
                targetVehicle?.currentStage,
                attackerVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("damage", damage)
                  .WithMetadata("damageType", damageType)
                  .WithMetadata("source", sourceName)
                  .WithMetadata("damageBreakdown", evt.Breakdown.ToDetailedString());
        }
        
        // ==================== STATUS EFFECT LOGGING ====================
        
        private static void LogStatusEffects(CombatAction action)
        {
            foreach (var statusEvent in action.GetStatusEffectEvents().Where(e => !e.WasBlocked))
            {
                LogSingleStatusEffect(statusEvent, action);
            }
        }
        
        private static void LogSingleStatusEffect(StatusEffectEvent evt, CombatAction action = null)
        {
            if (evt.WasBlocked || evt.Applied == null) return;
            
            var applied = evt.Applied;
            var effect = applied.template;
            
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string targetName = GetTargetDisplayName(evt.Target, targetVehicle);
            
            // Determine buff/debuff
            bool isBuff = DetermineIfBuff(effect);
            string color = isBuff ? "#44FF44" : "#FF4444";
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";
            
            // Build message
            bool isSelfTarget = sourceVehicle != null && sourceVehicle == targetVehicle;
            string message;
            
            if (sourceVehicle == null)
            {
                string sourceName = evt.CausalSource?.name ?? action?.SourceName ?? "Unknown";
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> from {sourceName} ({durationText})";
            }
            else if (isSelfTarget)
            {
                message = $"{sourceVehicle.vehicleName} gains <color={color}>{effect.effectName}</color> ({durationText})";
            }
            else
            {
                string actionVerb = isBuff ? "grants" : "inflicts";
                message = $"{sourceVehicle.vehicleName} {actionVerb} <color={color}>{effect.effectName}</color> on {targetName} ({durationText})";
            }
            
            // Determine importance
            bool playerInvolved = (sourceVehicle?.controlType == ControlType.Player) ||
                                  (targetVehicle?.controlType == ControlType.Player);
            EventImportance importance;
            if (playerInvolved && !isBuff)
                importance = EventImportance.High;
            else if (playerInvolved)
                importance = EventImportance.Medium;
            else
                importance = EventImportance.Low;
            
            var logEvt = RaceHistory.Log(
                EventType.StatusEffect,
                importance,
                message,
                targetVehicle?.currentStage,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("statusEffectName", effect.effectName)
                  .WithMetadata("duration", applied.turnsRemaining)
                  .WithMetadata("isIndefinite", applied.IsIndefinite)
                  .WithMetadata("isBuff", isBuff)
                  .WithMetadata("isSelfTarget", isSelfTarget);
        }
        
        private static void LogStatusExpired(StatusEffectExpiredEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = targetVehicle?.vehicleName ?? evt.Target?.GetDisplayName() ?? "Unknown";
            
            var logEvt = RaceHistory.Log(
                EventType.StatusEffect,
                EventImportance.Low,
                $"{targetName}'s {evt.Expired.template.effectName} has expired",
                targetVehicle?.currentStage,
                null, targetVehicle
            );
            
            logEvt.WithMetadata("statusEffectName", evt.Expired.template.effectName)
                  .WithMetadata("expired", true);
        }
        
        // ==================== RESTORATION LOGGING ====================
        
        private static void LogRestorations(CombatAction action)
        {
            var restorationByTarget = action.GetRestorationByTarget();
            
            foreach (var kvp in restorationByTarget)
            {
                Entity target = kvp.Key;
                List<RestorationEvent> restorations = kvp.Value;
                
                LogCombinedRestoration(restorations, action, target);
            }
        }
        
        private static void LogCombinedRestoration(List<RestorationEvent> restorations, CombatAction action, Entity target)
        {
            Vehicle sourceVehicle = action.ActorVehicle;
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
            
            string targetName = targetVehicle?.vehicleName ?? target?.GetDisplayName() ?? "Unknown";
            bool isSelfTarget = sourceVehicle != null && sourceVehicle == targetVehicle;
            
            // Group by resource type
            var healthRestorations = restorations.Where(r => r.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health).ToList();
            var energyRestorations = restorations.Where(r => r.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Energy).ToList();
            
            // Build message parts
            var parts = new List<string>();
            
            if (healthRestorations.Count > 0)
            {
                int totalHealth = healthRestorations.Sum(r => r.Breakdown.actualChange);
                if (totalHealth != 0)
                {
                    string action_verb = totalHealth > 0 ? "restores" : "drains";
                    parts.Add($"{action_verb} <color=#44FF44>{Math.Abs(totalHealth)}</color> HP");
                }
            }
            
            if (energyRestorations.Count > 0)
            {
                int totalEnergy = energyRestorations.Sum(r => r.Breakdown.actualChange);
                if (totalEnergy != 0)
                {
                    string action_verb = totalEnergy > 0 ? "restores" : "drains";
                    parts.Add($"{action_verb} <color=#88DDFF>{Math.Abs(totalEnergy)}</color> energy");
                }
            }
            
            if (parts.Count == 0) return;
            
            string restorationText = string.Join(" and ", parts);
            string message;
            
            if (isSelfTarget || sourceVehicle == null)
            {
                message = $"{targetName} {restorationText}";
            }
            else
            {
                message = $"{sourceVehicle.vehicleName} {restorationText} for {targetName}";
            }
            
            // Determine importance
            bool playerInvolved = (sourceVehicle?.controlType == ControlType.Player) ||
                                  (targetVehicle?.controlType == ControlType.Player);
            EventImportance importance = playerInvolved ? EventImportance.Medium : EventImportance.Low;
            
            var logEvt = RaceHistory.Log(
                EventType.Resource,
                importance,
                message,
                targetVehicle?.currentStage,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("skillName", action.SourceName)
                  .WithMetadata("isSelfTarget", isSelfTarget);
        }
        
        private static void LogSingleRestoration(RestorationEvent evt)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string targetName = targetVehicle?.vehicleName ?? evt.Target?.GetDisplayName() ?? "Unknown";
            
            string resourceName = evt.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health ? "HP" : "energy";
            string color = evt.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health ? "#44FF44" : "#88DDFF";
            string action_verb = evt.Breakdown.actualChange > 0 ? "restores" : "drains";
            int absChange = Math.Abs(evt.Breakdown.actualChange);
            
            string message = $"{targetName} {action_verb} <color={color}>{absChange}</color> {resourceName}";
            
            bool playerInvolved = (targetVehicle?.controlType == ControlType.Player);
            
            var logEvt = RaceHistory.Log(
                EventType.Resource,
                playerInvolved ? EventImportance.Medium : EventImportance.Low,
                message,
                targetVehicle?.currentStage,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("resourceType", evt.Breakdown.resourceType.ToString())
                  .WithMetadata("actualChange", evt.Breakdown.actualChange);
        }
        
        // ==================== HELPER METHODS ====================
        
        private static string GetTargetDisplayName(Entity target, Vehicle targetVehicle)
        {
            if (target is VehicleComponent component)
            {
                string vehicleName = targetVehicle?.vehicleName ?? "Unknown";
                return $"{vehicleName}'s {component.name}";
            }
            return targetVehicle?.vehicleName ?? target?.GetDisplayName() ?? "Unknown";
        }
        
        private static string BuildCombinedDamageText(List<DamageEvent> damages)
        {
            if (damages.Count == 0) return "0 damage";
            
            if (damages.Count == 1)
            {
                var d = damages[0].Breakdown;
                return $"<color=#FFA500>{d.finalDamage}</color> {d.damageType} damage";
            }
            
            // Multiple damage types - combine
            var parts = damages.Select(d => 
                $"<color=#FFA500>{d.Breakdown.finalDamage}</color> {d.Breakdown.damageType}");
            
            int total = damages.Sum(d => d.Breakdown.finalDamage);
            
            return $"{string.Join(" + ", parts)} (<color=#FFA500>{total} total</color>) damage";
        }
        
        private static string BuildDetailedDamageBreakdown(List<DamageEvent> damages)
        {
            var sb = new StringBuilder();
            
            // Calculate total damage
            int totalDamage = damages.Sum(d => d.Breakdown.finalDamage);
            
            // Add total header
            sb.AppendLine($"Damage Total: {totalDamage}");
            
            // Add source header
            if (damages.Count > 0 && damages[0].CausalSource != null)
            {
                sb.AppendLine($"Damage Source: {damages[0].CausalSource.name}");
                sb.AppendLine();
            }
            
            // Add each damage breakdown with dice notation
            foreach (var damageEvent in damages)
            {
                var breakdown = damageEvent.Breakdown;
                
                // For each component in the breakdown
                foreach (var comp in breakdown.components)
                {
                    // Build dice notation
                    string diceNotation = "";
                    
                    if (comp.diceCount > 0 && comp.dieSize > 0)
                    {
                        // Has dice: (XdY)
                        diceNotation = $"({comp.diceCount}d{comp.dieSize})";
                        
                        // Add bonus if present
                        if (comp.bonus != 0)
                        {
                            string sign = comp.bonus > 0 ? "+" : "";
                            diceNotation += $"{sign}{comp.bonus}";
                        }
                    }
                    else if (comp.bonus != 0)
                    {
                        // No dice, just flat bonus
                        string sign = comp.bonus > 0 ? "+" : "";
                        diceNotation = $"{sign}{comp.bonus}";
                    }
                    
                    // Add resistance modifier if applicable
                    string resistMod = "";
                    if (breakdown.resistanceLevel != ResistanceLevel.Normal)
                    {
                        resistMod = breakdown.resistanceLevel switch
                        {
                            ResistanceLevel.Vulnerable => " (×2)",
                            ResistanceLevel.Resistant => " (×0.5)",
                            ResistanceLevel.Immune => " (×0)",
                            _ => ""
                        };
                    }
                    
                    // Build final line: (1d8)+2 (×0.5) = 6 (Physical)
                    string damageTypeLabel = $"({breakdown.damageType})";
                    if (breakdown.resistanceLevel != ResistanceLevel.Normal)
                    {
                        damageTypeLabel = $"({breakdown.damageType}, {breakdown.resistanceLevel})";
                    }
                    
                    sb.AppendLine($"{diceNotation}{resistMod} = {breakdown.finalDamage} {damageTypeLabel}");
                }
            }
            
            return sb.ToString().Trim();
        }
        
        private static bool DetermineIfBuff(StatusEffect statusEffect)
        {
            // Check modifiers - positive values generally mean buff
            float totalModifierValue = statusEffect.modifiers.Sum(m => m.value);
            
            // Check periodic effects
            bool hasPeriodicDamage = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.Damage);
            bool hasPeriodicHealing = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.Healing);
            bool hasEnergyDrain = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.EnergyDrain);
            bool hasEnergyRestore = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.EnergyRestore);
            
            // Check behavioral effects
            bool hasBehavioralRestrictions = statusEffect.behavioralEffects != null &&
                (statusEffect.behavioralEffects.preventsActions ||
                 statusEffect.behavioralEffects.preventsMovement ||
                 statusEffect.behavioralEffects.preventsSkillUse ||
                 statusEffect.behavioralEffects.damageAmplification > 1f);
            
            // Debuff indicators outweigh buff indicators
            if (hasPeriodicDamage || hasEnergyDrain || hasBehavioralRestrictions)
                return false;
            
            // Buff indicators
            if (hasPeriodicHealing || hasEnergyRestore || totalModifierValue > 0)
                return true;
            
            // Neutral or unclear - assume debuff if negative modifiers
            return totalModifierValue >= 0;
        }
    }
}
