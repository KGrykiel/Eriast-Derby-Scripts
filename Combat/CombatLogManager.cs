using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Characters;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Core;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.SkillChecks;

namespace Assets.Scripts.Combat
{
    /// <summary>
    /// Central manager for all combat logging and formatting.
    /// 
    /// This is THE source of truth for how combat data is displayed.
    /// All formatting methods are public for use by tooltips and UI.
    /// 
    /// Responsibilities:
    /// - Formatting attack results, damage results, status effects, defense values
    /// - Logging combat events to RaceHistory
    /// - Aggregating multiple events within an action
    /// 
    /// MESSAGE PATTERNS:
    /// - Different entities: "{Source} [Vehicle] {verb} {Target} [Vehicle]"
    /// - Same vehicle:       "{Target} [Vehicle] takes/gains {effect}"
    /// - Same entity:        "{Entity} [Vehicle] takes/gains {effect}"
    /// </summary>
    public static class CombatLogManager
    {
        // ==================== COLOR CONSTANTS ====================
        
        private static class Colors
        {
            public const string Success = "#44FF44";   // Green - hits, saves, buffs
            public const string Failure = "#FF4444";   // Red - misses, failures, debuffs
            public const string Damage = "#FFA500";    // Orange - damage numbers
            public const string Energy = "#88DDFF";    // Blue - energy values
            public const string Health = "#44FF44";    // Green - health values
        }
        
        // ==================== PUBLIC FORMATTING API ====================
        
        /// <summary>
        /// Format an attack result for display.
        /// Short format: "15 (d20: 12, +3 modifiers) vs AC 14 - HIT"
        /// </summary>
        public static string FormatAttackShort(AttackResult result)
        {
            if (result == null) return "No roll";
            
            string modStr = result.TotalModifier >= 0 
                ? $"+{result.TotalModifier}" 
                : $"{result.TotalModifier}";
            string output = $"{result.Total} (d20: {result.BaseRoll}{modStr})";
            
            if (result.TargetValue > 0 && result.Success.HasValue)
            {
                output += $" vs AC {result.TargetValue}";
                output += result.Success.Value ? " - HIT" : " - MISS";
            }
            
            return output;
        }
        
        /// <summary>
        /// Format an attack result with full breakdown for tooltips.
        /// </summary>
        public static string FormatAttackDetailed(AttackResult result)
        {
            if (result == null) return "No roll data";
            
            var sb = new StringBuilder();
            sb.AppendLine("Attack Roll Breakdown:");
            sb.AppendLine($"  d20: {result.BaseRoll}");
            
            foreach (var bonus in result.Bonuses)
            {
                string sign = bonus.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {bonus.Label}: {sign}{bonus.Value}");
            }
            
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total: {result.Total}");
            
            if (result.TargetValue > 0)
            {
                sb.AppendLine($"  vs AC: {result.TargetValue}");
                if (result.Success.HasValue)
                {
                    sb.AppendLine($"  Result: {(result.Success.Value ? "HIT" : "MISS")}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a saving throw result for display.
        /// Short format: "18 (d20: 15, +3 Mobility) vs DC 15 - SAVED"
        /// </summary>
        public static string FormatSaveShort(SaveResult result)
        {
            if (result == null) return "No roll";
            
            string modStr = result.TotalModifier >= 0 
                ? $"+{result.TotalModifier}" 
                : $"{result.TotalModifier}";
            string output = $"{result.Total} (d20: {result.BaseRoll}{modStr})";
            
            if (result.TargetValue > 0 && result.Success.HasValue)
            {
                output += $" vs DC {result.TargetValue}";
                output += result.Succeeded ? " - SAVED" : " - FAILED";
            }
            
            return output;
        }
        
        /// <summary>
        /// Format a saving throw result with full breakdown for tooltips.
        /// </summary>
        public static string FormatSaveDetailed(SaveResult result)
        {
            if (result == null) return "No roll data";
            
            var sb = new StringBuilder();
            sb.AppendLine($"{result.saveSpec.DisplayName} Save:");
            sb.AppendLine($"  d20: {result.BaseRoll}");
            
            foreach (var bonus in result.Bonuses)
            {
                string sign = bonus.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {bonus.Label}: {sign}{bonus.Value}");
            }
            
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total: {result.Total}");
            
            if (result.TargetValue > 0)
            {
                sb.AppendLine($"  vs DC: {result.TargetValue}");
                if (result.Success.HasValue)
                {
                    string resultText = result.Succeeded ? "SAVED" : "FAILED";
                    sb.AppendLine($"  Result: {resultText}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a skill check result for display.
        /// Short format: "18 (d20: 15, +3 Mobility) vs DC 15 - SUCCESS"
        /// </summary>
        public static string FormatSkillCheckShort(SkillCheckResult result)
        {
            if (result == null) return "No roll";
            
            string modStr = result.TotalModifier >= 0 
                ? $"+{result.TotalModifier}" 
                : $"{result.TotalModifier}";
            string output = $"{result.Total} (d20: {result.BaseRoll}{modStr})";
            
            if (result.TargetValue > 0 && result.Success.HasValue)
            {
                output += $" vs DC {result.TargetValue}";
                output += result.Succeeded ? " - SUCCESS" : " - FAILURE";
            }
            
            return output;
        }
        
        /// <summary>
        /// Format a skill check result with full breakdown for tooltips.
        /// </summary>
        public static string FormatSkillCheckDetailed(SkillCheckResult result)
        {
            if (result == null) return "No roll data";
            
            var sb = new StringBuilder();
            sb.AppendLine($"{result.checkSpec.DisplayName} Check:");
            sb.AppendLine($"  d20: {result.BaseRoll}");
            
            foreach (var bonus in result.Bonuses)
            {
                string sign = bonus.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {bonus.Label}: {sign}{bonus.Value}");
            }
            
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total: {result.Total}");
            
            if (result.TargetValue > 0)
            {
                sb.AppendLine($"  vs DC: {result.TargetValue}");
                if (result.Success.HasValue)
                {
                    string resultText = result.Succeeded ? "SUCCESS" : "FAILURE";
                    sb.AppendLine($"  Result: {resultText}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a save DC breakdown for tooltips.
        /// Example output:
        /// Save DC Breakdown:
        ///   Base DC: 15 (Fireball)
        ///   ─────────────
        ///   Total DC: 15
        /// </summary>
        public static string FormatDCDetailed(int dc, string skillName, SaveSpec saveSpec)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{saveSpec.DisplayName} Save DC Breakdown:");
            sb.AppendLine($"  Base DC: {dc} ({skillName})");
            // Future: Add user bonuses to DC here
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total DC: {dc}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a save modifier breakdown for tooltips (target's save bonus).
        /// </summary>
        public static string FormatSaveModifiersDetailed(Entity target, SaveSpec saveSpec)
        {
            if (target == null)
            {
                return $"{saveSpec.DisplayName} Save: +0";
            }
            
            var bonuses = SaveCalculator.GatherBonuses(saveSpec, component: target);
            int total = 0;
            foreach (var b in bonuses) total += b.Value;
            
            var sb = new StringBuilder();
            sb.AppendLine($"{saveSpec.DisplayName} Save Breakdown:");
            
            if (bonuses.Count == 0)
            {
                sb.AppendLine("  No modifiers (+0)");
            }
            else
            {
                foreach (var bonus in bonuses)
                {
                    string sign = bonus.Value >= 0 ? "+" : "";
                    sb.AppendLine($"  {bonus.Label}: {sign}{bonus.Value}");
                }
            }
            
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total Bonus: {(total >= 0 ? "+" : "")}{total}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a defense value breakdown for tooltips.
        /// Example output:
        /// AC Breakdown:
        ///   Base: 14 (Chassis)
        ///   Shield Spell: +2 (StatusEffect)
        ///   Armor Plating: +2 (Equipment)
        ///   ─────────────
        ///   Total AC: 18
        /// </summary>
        public static string FormatDefenseDetailed(int total, float baseValue, List<AttributeModifier> modifiers, string defenseName = "AC")
        {
            if (modifiers == null)
            {
                return $"{defenseName}: {total}";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"{defenseName} Breakdown:");
            
            // Base value (not a modifier!)
            sb.AppendLine($"  Base: {(int)baseValue}");
            
            // Actual modifiers
            foreach (var mod in modifiers)
            {
                string sign = mod.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {mod.SourceDisplayName}: {sign}{(int)mod.Value} ({mod.Category})");
            }
            
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total {defenseName}: {total}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a damage result for display.
        /// Short format: "15 Fire" or "15 Fire (Resistant)"
        /// </summary>
        public static string FormatDamageShort(DamageResult result)
        {
            if (result == null) return "0 damage";
            
            string resistStr = result.resistanceLevel != ResistanceLevel.Normal 
                ? $" ({result.resistanceLevel})" 
                : "";
            return $"{result.finalDamage} {result.damageType}{resistStr}";
        }
        
        /// <summary>
        /// Format a damage result with full breakdown for tooltips.
        /// </summary>
        public static string FormatDamageDetailed(DamageResult result, string sourceName = null)
        {
            if (result == null) return "No damage";
            
            var sb = new StringBuilder();
            sb.AppendLine($"Damage Breakdown ({result.damageType}):");
            
            foreach (var source in result.sources)
            {
                if (source.diceCount > 0 && source.dieSize > 0)
                {
                    string resistMod = GetResistanceModifier(result.resistanceLevel);
                    sb.AppendLine($"  {source.name}: ({source.ToDiceString()}){resistMod} = {source.Total} ({source.sourceName})");
                }
                else if (source.bonus != 0)
                {
                    string sign = source.bonus >= 0 ? "+" : "";
                    sb.AppendLine($"  {source.name}: {sign}{source.bonus} ({source.sourceName})");
                }
            }
            
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Subtotal: {result.RawTotal}");
            
            if (result.resistanceLevel != ResistanceLevel.Normal)
            {
                sb.AppendLine($"  {result.resistanceLevel}: {GetResistanceModifier(result.resistanceLevel)}");
            }
            
            sb.AppendLine($"  Final: {result.finalDamage} {result.damageType}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format multiple damage results (aggregated) for tooltips.
        /// Used when a skill deals multiple damage types.
        /// </summary>
        public static string FormatCombinedDamageDetailed(List<DamageResult> results, string sourceName = null)
        {
            if (results == null || results.Count == 0) return "No damage";
            
            if (results.Count == 1)
            {
                return FormatDamageDetailed(results[0], sourceName);
            }
            
            var sb = new StringBuilder();
            
            // Calculate total
            int totalDamage = results.Sum(r => r.finalDamage);
            sb.AppendLine($"Damage Total: {totalDamage}");
            
            if (!string.IsNullOrEmpty(sourceName))
            {
                sb.AppendLine($"Damage Source: {sourceName}");
            }
            sb.AppendLine();
            
            // Each damage result
            foreach (var result in results)
            {
                foreach (var source in result.sources)
                {
                    string diceNotation = BuildDiceNotation(source);
                    string resistMod = GetResistanceModifier(result.resistanceLevel);
                    string damageTypeLabel = BuildDamageTypeLabel(result);
                    
                    sb.AppendLine($"{diceNotation}{resistMod} = {result.finalDamage} {damageTypeLabel}");
                }
            }
            
            return sb.ToString().Trim();
        }
        
        /// <summary>
        /// Format combined attack and damage for a single tooltip.
        /// </summary>
        public static string FormatCombinedAttackAndDamage(AttackResult attack, DamageResult damage)
        {
            var sb = new StringBuilder();
            
            if (attack != null)
            {
                sb.Append(FormatAttackDetailed(attack));
            }
            
            if (damage != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(FormatDamageDetailed(damage));
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a stat breakdown with modifiers for tooltips.
        /// Uses StatCalculator (single source of truth) to gather modifier data.
        /// Example output:
        /// Speed: 65
        /// ═══════════════════════════════
        /// Base:                      50
        /// 
        /// Status Effects:
        ///   [Icon] Haste            +10  Speed
        ///   [Icon] Slowed            -5  Speed
        /// 
        /// Equipment:
        ///   Turbo Booster           +10  Speed
        /// ═══════════════════════════════
        /// Total Modifiers:          +15  Speed
        /// Final Value:               65  Speed
        /// INTEGER-FIRST DESIGN: All values are integers (D&D discrete stats).
        /// </summary>
        public static string FormatStatBreakdown(
            Entity entity, 
            Attribute attribute, 
            int baseValue, 
            int finalValue)
        {
            if (entity == null)
            {
                return $"{attribute}: {finalValue}";
            }
            
            // Use StatCalculator to gather modifiers (single source of truth)
            // Base value is now returned separately, not in the modifiers list
            var (calculatedTotal, returnedBase, modifiers) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, 
                attribute, 
                baseValue);
            
            // If no modifiers, show simple message
            if (modifiers.Count == 0)
            {
                return $"{attribute}: {baseValue}\n═══════════════════════════════\nBase value only (no modifiers)";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"{attribute}: {finalValue}");
            sb.AppendLine("═══════════════════════════════");
            sb.AppendLine($"Base:                      {returnedBase}");
            sb.AppendLine();
            
            // Group modifiers by category
            var statusEffectMods = new List<AttributeModifier>();
            var equipmentMods = new List<AttributeModifier>();
            var auraMods = new List<AttributeModifier>();
            var skillMods = new List<AttributeModifier>();
            var otherMods = new List<AttributeModifier>();
            
            foreach (var mod in modifiers)
            {
                switch (mod.Category)
                {
                    case ModifierCategory.StatusEffect:
                        statusEffectMods.Add(mod);
                        break;
                    case ModifierCategory.Equipment:
                        equipmentMods.Add(mod);
                        break;
                    case ModifierCategory.Aura:
                        auraMods.Add(mod);
                        break;
                    case ModifierCategory.Skill:
                        skillMods.Add(mod);
                        break;
                    default:
                        otherMods.Add(mod);
                        break;
                }
            }
            
            // Status Effects section
            if (statusEffectMods.Count > 0)
            {
                sb.AppendLine("Status Effects:");
                foreach (var mod in statusEffectMods)
                {
                    sb.AppendLine(FormatModifierLine(mod, attribute));
                }
                sb.AppendLine();
            }
            
            // Equipment section
            if (equipmentMods.Count > 0)
            {
                sb.AppendLine("Equipment:");
                foreach (var mod in equipmentMods)
                {
                    sb.AppendLine(FormatModifierLine(mod, attribute));
                }
                sb.AppendLine();
            }
            
            // Aura section (future)
            if (auraMods.Count > 0)
            {
                sb.AppendLine("Auras:");
                foreach (var mod in auraMods)
                {
                    sb.AppendLine(FormatModifierLine(mod, attribute));
                }
                sb.AppendLine();
            }
            
            // Skill section (future)
            if (skillMods.Count > 0)
            {
                sb.AppendLine("Skills:");
                foreach (var mod in skillMods)
                {
                    sb.AppendLine(FormatModifierLine(mod, attribute));
                }
                sb.AppendLine();
            }
            
            // Other section
            if (otherMods.Count > 0)
            {
                sb.AppendLine("Other:");
                foreach (var mod in otherMods)
                {
                    sb.AppendLine(FormatModifierLine(mod, attribute));
                }
                sb.AppendLine();
            }
            
            // Total
            float totalModifiers = finalValue - returnedBase;
            string totalSign = totalModifiers >= 0 ? "+" : "";
            sb.AppendLine("═══════════════════════════════");
            sb.AppendLine($"Total Modifiers:          {totalSign}{totalModifiers}  {attribute}");
            sb.AppendLine($"Final Value:               {finalValue}  {attribute}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Format a status effect tooltip with full details.
        /// Example output:
        /// 🔥 Haste
        /// ═══════════════════════════════
        /// Duration: 3 turns remaining
        /// 
        /// Effects:
        ///   • +10 Speed
        ///   • +2 AC
        /// 
        /// Grants enhanced movement speed
        /// and improved reflexes.
        /// ═══════════════════════════════
        /// </summary>
        public static string FormatStatusEffectTooltip(AppliedStatusEffect appliedEffect)
        {
            if (appliedEffect == null || appliedEffect.template == null)
            {
                return "Unknown status effect";
            }
            
            var effect = appliedEffect.template;
            var sb = new StringBuilder();
            
            // Title
            sb.AppendLine($"{effect.effectName}");
            sb.AppendLine("═══════════════════════════════");
            
            // Duration
            if (appliedEffect.IsIndefinite)
            {
                sb.AppendLine("Duration: Indefinite (∞)");
            }
            else
            {
                sb.AppendLine($"Duration: {appliedEffect.turnsRemaining} turns remaining");
            }
            sb.AppendLine();
            
            // Effects (modifiers)
            if (effect.modifiers != null && effect.modifiers.Count > 0)
            {
                sb.AppendLine("Effects:");
                foreach (var mod in effect.modifiers)
                {
                    string sign = mod.value >= 0 ? "+" : "";
                    string typeStr = mod.type == ModifierType.Multiplier ? "×" : "";
                    string color = mod.value >= 0 ? "#44FF44" : "#FF4444";
                    
                    if (mod.type == ModifierType.Multiplier)
                    {
                        sb.AppendLine($"  • <color={color}>{typeStr}{mod.value} {mod.attribute}</color>");
                    }
                    else
                    {
                        sb.AppendLine($"  • <color={color}>{sign}{mod.value} {mod.attribute}</color>");
                    }
                }
                sb.AppendLine();
            }
            
            // Periodic effects
            if (effect.periodicEffects != null && effect.periodicEffects.Count > 0)
            {
                foreach (var periodic in effect.periodicEffects)
                {
                    string notation = periodic.GetNotation();
                    string effectText = periodic.type switch
                    {
                        PeriodicEffectType.Damage => $"  • <color=#FF4444>{notation} {periodic.damageType} damage per turn</color>",
                        PeriodicEffectType.Healing => $"  • <color=#44FF44>{notation} healing per turn</color>",
                        PeriodicEffectType.EnergyDrain => $"  • <color=#FF4444>-{notation} energy per turn</color>",
                        PeriodicEffectType.EnergyRestore => $"  • <color=#88DDFF>+{notation} energy per turn</color>",
                        _ => ""
                    };
                    if (!string.IsNullOrEmpty(effectText))
                    {
                        sb.AppendLine(effectText);
                    }
                }
                sb.AppendLine();
            }
            
            // Behavioral effects
            if (effect.behavioralEffects != null)
            {
                if (effect.behavioralEffects.preventsActions)
                {
                    sb.AppendLine("  • <color=#FF4444>Prevents actions</color>");
                }
                if (effect.behavioralEffects.preventsMovement)
                {
                    sb.AppendLine("  • <color=#FF4444>Prevents movement</color>");
                }
                if (effect.behavioralEffects.damageAmplification != 1f)
                {
                    float percent = (effect.behavioralEffects.damageAmplification - 1f) * 100f;
                    string color = percent > 0 ? "#FF4444" : "#44FF44";
                    string sign = percent > 0 ? "+" : "";
                    sb.AppendLine($"  • <color={color}>{sign}{percent:F0}% damage taken</color>");
                }
                sb.AppendLine();
            }
            
            // Description
            if (!string.IsNullOrEmpty(effect.description))
            {
                sb.AppendLine(effect.description);
                sb.AppendLine();
            }
            
            sb.AppendLine("═══════════════════════════════");
            
            return sb.ToString().TrimEnd();
        }
        
        /// <summary>
        /// Format a short status effect summary for icon labels.
        /// Example: "Haste (3)" or "Burning (∞)"
        /// </summary>
        public static string FormatStatusEffectSummary(AppliedStatusEffect appliedEffect)
        {
            if (appliedEffect == null || appliedEffect.template == null)
            {
                return "Unknown";
            }
            
            string duration = appliedEffect.IsIndefinite 
                ? "∞" 
                : appliedEffect.turnsRemaining.ToString();
            
            return $"{appliedEffect.template.effectName} ({duration})";
        }
        
        // ==================== MAIN LOGGING ENTRY POINTS ====================
        
        /// <summary>
        /// Log a completed combat action with all its events.
        /// Aggregates damage by target, status effects, etc.
        /// </summary>
        public static void LogAction(CombatAction action)
        {
            if (action == null || !action.HasEvents) return;
            
            LogAttackRolls(action);
            LogSavingThrows(action);
            LogSkillChecks(action);
            LogDamageByTarget(action);
            LogStatusEffects(action);
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
                case SavingThrowEvent save:
                    LogSingleSavingThrow(save);
                    break;
                case SkillCheckEvent check:
                    LogSingleSkillCheck(check);
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
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source) ?? action?.ActorVehicle;
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string sourceName = FormatSource(evt.Character, evt.Source, attackerVehicle);
            string targetName = FormatEntityWithVehicle(evt.Target, targetVehicle);
            string skillName = action?.SourceName ?? (evt.CausalSource != null ? evt.CausalSource.name : "attack");
            
            string resultText = evt.IsHit 
                ? $"<color={Colors.Success}>Hit</color>" 
                : $"<color={Colors.Failure}>Miss</color>";
            
            // Pattern: "{Source} attacks {Target}. {Result}"
            string message = $"{sourceName} attacks {targetName}. {resultText}";
            
            var importance = evt.IsHit ? EventImportance.High : EventImportance.Medium;
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                importance,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                attackerVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.IsHit ? "hit" : "miss")
                  .WithMetadata("rollBreakdown", evt.Result != null ? FormatAttackDetailed(evt.Result) : "");
            
            // Add target's AC breakdown for tooltip
            if (evt.Target != null)
            {
                var (totalAC, baseAC, acModifiers) = StatCalculator.GatherDefenseValueWithBreakdown(evt.Target);
                logEvt.WithMetadata("defenseBreakdown", FormatDefenseDetailed(totalAC, baseAC, acModifiers, "AC"));
            }
            
            if (evt.Source is VehicleComponent sourceComp)
            {
                logEvt.WithMetadata("sourceComponent", sourceComp.name);
            }
            if (evt.Target is VehicleComponent targetComp)
            {
                logEvt.WithMetadata("targetComponent", targetComp.name);
            }
        }
        
        // ==================== SAVING THROW LOGGING ====================
        
        private static void LogSavingThrows(CombatAction action)
        {
            foreach (var saveEvent in action.GetSavingThrowEvents())
            {
                LogSingleSavingThrow(saveEvent, action);
            }
        }
        
        private static void LogSingleSavingThrow(SavingThrowEvent evt, CombatAction action = null)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string targetName = FormatDefensiveSource(evt.Character, evt.Target, targetVehicle);
            string skillName = action?.SourceName ?? (evt.CausalSource != null ? evt.CausalSource.name : "effect");
            string saveTypeName = evt.Result?.saveSpec.DisplayName ?? "Mobility";
            
            string resultText = evt.Succeeded 
                ? $"<color={Colors.Success}>Saved</color>" 
                : (evt.Result?.IsAutoFail == true 
                    ? $"<color={Colors.Failure}>Auto-Failed</color>" 
                    : $"<color={Colors.Failure}>Failed</color>");
            
            // Pattern: "{Target} attempts {Type} save vs {Skill}. {Result}"
            string message = $"{targetName} attempts {saveTypeName} save vs {skillName}. {resultText}";
            
            // Failed saves are more impactful (effects will apply)
            var importance = evt.Succeeded ? EventImportance.Medium : EventImportance.High;
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                importance,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.Succeeded ? "saved" : "failed")
                  .WithMetadata("saveType", saveTypeName)
                  .WithMetadata("rollBreakdown", evt.Result != null ? FormatSaveDetailed(evt.Result) : "");
            
            // Add DC breakdown for tooltip
            if (evt.Result != null && evt.CausalSource is Skill skill)
            {
                logEvt.WithMetadata("dcBreakdown", FormatDCDetailed(evt.Result.TargetValue, skill.name, evt.Result.saveSpec));
            }
            
            //// Add target's save modifier breakdown
            //if (evt.Target != null && evt.Result != null)
            //{
            //    logEvt.WithMetadata("saveModifiersBreakdown", FormatSaveModifiersDetailed(evt.Target, evt.Result.saveSpec));
            //}
        }
        
        // ==================== SKILL CHECK LOGGING ====================
        
        private static void LogSkillChecks(CombatAction action)
        {
            foreach (var checkEvent in action.GetSkillCheckEvents())
            {
                LogSingleSkillCheck(checkEvent, action);
            }
        }
        
        private static void LogSingleSkillCheck(SkillCheckEvent evt, CombatAction action = null)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            
            string sourceName = FormatSource(evt.Character, evt.Source, sourceVehicle);
            string skillName = action?.SourceName ?? (evt.CausalSource != null ? evt.CausalSource.name : "task");
            string checkTypeName = evt.Result?.checkSpec.DisplayName ?? "Mobility";
            
            string resultText = evt.Succeeded 
                ? $"<color={Colors.Success}>Success</color>" 
                : (evt.Result?.IsAutoFail == true 
                    ? $"<color={Colors.Failure}>Auto-Failed</color>" 
                    : $"<color={Colors.Failure}>Failure</color>");
            
            // Pattern: "{Source} attempts {Type} check for {Skill}. {Result}"
            string message = $"{sourceName} attempts {checkTypeName} check for {skillName}. {resultText}";
            
            // Failed checks are more impactful (effects won't apply)
            var importance = evt.Succeeded ? EventImportance.Medium : EventImportance.High;
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                importance,
                message,
                sourceVehicle != null ? sourceVehicle.currentStage : null,
                sourceVehicle, null
            );
            
            logEvt.WithMetadata("skillName", skillName)
                  .WithMetadata("result", evt.Succeeded ? "success" : "failure")
                  .WithMetadata("checkType", checkTypeName)
                  .WithMetadata("rollBreakdown", evt.Result != null ? FormatSkillCheckDetailed(evt.Result) : "");
            
            // Add DC breakdown for tooltip
            if (evt.Result != null && evt.CausalSource is Skill skill)
            {
                logEvt.WithMetadata("dcBreakdown", $"{checkTypeName} Check DC: {evt.Result.TargetValue} ({skill.name})");
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
            
            string sourceName = FormatActionSource(action);
            string targetName = FormatEntityWithVehicle(target, targetVehicle);
            
            bool isSelfDamage = IsSelfDamage(action.Actor, target, attackerVehicle, targetVehicle);
            
            string damageText = BuildCombinedDamageText(damages);
            int totalDamage = damages.Sum(d => d.Result.finalDamage);
            
            // Pattern: Self-damage uses passive voice, otherwise active voice
            string message = isSelfDamage
                ? $"{targetName} takes {damageText}"
                : $"{sourceName} deals {damageText} to {targetName}";
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                EventImportance.High,
                message,
                targetVehicle != null ? targetVehicle.currentStage : (attackerVehicle != null ? attackerVehicle.currentStage : null),
                attackerVehicle, targetVehicle
            );
            
            // Use centralized formatting for breakdown
            var results = damages.Select(d => d.Result).ToList();
            string breakdown = FormatCombinedDamageDetailed(results, action.SourceName);
            
            logEvt.WithMetadata("skillName", action.SourceName)
                  .WithMetadata("totalDamage", totalDamage)
                  .WithMetadata("isSelfDamage", isSelfDamage)
                  .WithMetadata("damageBreakdown", breakdown);
            
            if (action.Actor is VehicleComponent sourceComp)
            {
                logEvt.WithMetadata("sourceComponent", sourceComp.name);
            }
            if (target is VehicleComponent targetComp)
            {
                logEvt.WithMetadata("targetComponent", targetComp.name);
            }
        }
        
        private static void LogSingleDamage(DamageEvent evt)
        {
            Vehicle attackerVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string targetName = FormatEntityWithVehicle(evt.Target, targetVehicle);
            string causalSourceName = evt.CausalSource != null ? evt.CausalSource.name : null ?? "Unknown";
            
            int damage = evt.Result.finalDamage;
            string damageType = evt.Result.damageType.ToString();
            
            string message;
            if (evt.Source == null)
            {
                // Environmental/no-source damage
                message = $"{targetName} takes <color={Colors.Damage}>{damage}</color> {damageType} damage from {causalSourceName}";
            }
            else
            {
                string sourceName = FormatEntityWithVehicle(evt.Source, attackerVehicle);
                bool isSelfDamage = IsSelfDamage(evt.Source, evt.Target, attackerVehicle, targetVehicle);
                
                message = isSelfDamage
                    ? $"{targetName} takes <color={Colors.Damage}>{damage}</color> {damageType} damage"
                    : $"{sourceName} deals <color={Colors.Damage}>{damage}</color> {damageType} damage to {targetName}";
            }
            
            bool playerInvolved = (attackerVehicle != null && attackerVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);
            
            var logEvt = RaceHistory.Log(
                EventType.Combat,
                playerInvolved ? EventImportance.High : EventImportance.Medium,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                attackerVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("damage", damage)
                  .WithMetadata("damageType", damageType)
                  .WithMetadata("source", causalSourceName)
                  .WithMetadata("damageBreakdown", FormatDamageDetailed(evt.Result));
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
            
            string sourceName = FormatEntityWithVehicle(evt.Source, sourceVehicle);
            string targetName = FormatEntityWithVehicle(evt.Target, targetVehicle);
            
            bool isBuff = DetermineIfBuff(effect);
            string color = isBuff ? Colors.Success : Colors.Failure;
            string durationText = applied.IsIndefinite ? "indefinite" : $"{applied.turnsRemaining} turns";
            
            bool isSelfTarget = IsSelfDamage(evt.Source, evt.Target, sourceVehicle, targetVehicle);
            string message;
            
            if (evt.Source == null)
            {
                // No source (environmental, etc.)
                string causalName = evt.CausalSource != null ? evt.CausalSource.name : null ?? action?.SourceName ?? "Unknown";
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> from {causalName} ({durationText})";
            }
            else if (isSelfTarget)
            {
                // Self-targeting: passive voice
                message = $"{targetName} gains <color={color}>{effect.effectName}</color> ({durationText})";
            }
            else
            {
                // Different targets: active voice
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
                EventType.StatusEffect,
                importance,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("statusEffectName", effect.effectName)
                  .WithMetadata("duration", applied.turnsRemaining)
                  .WithMetadata("isIndefinite", applied.IsIndefinite)
                  .WithMetadata("isBuff", isBuff)
                  .WithMetadata("isSelfTarget", isSelfTarget)
                  .WithMetadata("effectBreakdown", FormatStatusEffectTooltip(applied));  // Add tooltip!
        }
        
        private static void LogStatusExpired(StatusEffectExpiredEvent evt)
        {
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            string targetName = FormatEntityWithVehicle(evt.Target, targetVehicle);
            
            var logEvt = RaceHistory.Log(
                EventType.StatusEffect,
                EventImportance.Low,
                $"{targetName}'s {evt.Expired.template.effectName} has expired",
                targetVehicle != null ? targetVehicle.currentStage : null,
                null, targetVehicle
            );
            
            logEvt.WithMetadata("statusEffectName", evt.Expired.template.effectName)
                  .WithMetadata("expired", true)
                  .WithMetadata("effectBreakdown", FormatStatusEffectTooltip(evt.Expired));
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
            
            string sourceName = FormatActionSource(action);
            string targetName = FormatEntityWithVehicle(target, targetVehicle);
            bool isSelfTarget = IsSelfDamage(action.Actor, target, sourceVehicle, targetVehicle);
            
            var healthRestorations = restorations.Where(r => r.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health).ToList();
            var energyRestorations = restorations.Where(r => r.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Energy).ToList();
            
            var parts = new List<string>();
            
            if (healthRestorations.Count > 0)
            {
                int totalHealth = healthRestorations.Sum(r => r.Breakdown.actualChange);
                if (totalHealth != 0)
                {
                    string action_verb = totalHealth > 0 ? "restores" : "drains";
                    parts.Add($"{action_verb} <color={Colors.Health}>{Math.Abs(totalHealth)}</color> HP");
                }
            }
            
            if (energyRestorations.Count > 0)
            {
                int totalEnergy = energyRestorations.Sum(r => r.Breakdown.actualChange);
                if (totalEnergy != 0)
                {
                    string action_verb = totalEnergy > 0 ? "restores" : "drains";
                    parts.Add($"{action_verb} <color={Colors.Energy}>{Math.Abs(totalEnergy)}</color> energy");
                }
            }
            
            if (parts.Count == 0) return;
            
            string restorationText = string.Join(" and ", parts);
            
            // Pattern: Self-target uses passive voice, otherwise active voice
            string message = (isSelfTarget || action.Actor == null)
                ? $"{targetName} {restorationText}"
                : $"{sourceName} {restorationText} to {targetName}";
            
            bool playerInvolved = (sourceVehicle != null && sourceVehicle.controlType == ControlType.Player) ||
                                  (targetVehicle != null && targetVehicle.controlType == ControlType.Player);
            EventImportance importance = playerInvolved ? EventImportance.Medium : EventImportance.Low;
            
            var logEvt = RaceHistory.Log(
                EventType.Resource,
                importance,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("skillName", action.SourceName)
                  .WithMetadata("isSelfTarget", isSelfTarget);
        }
        
        private static void LogSingleRestoration(RestorationEvent evt)
        {
            Vehicle sourceVehicle = EntityHelpers.GetParentVehicle(evt.Source);
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(evt.Target);
            
            string targetName = FormatEntityWithVehicle(evt.Target, targetVehicle);
            
            string resourceName = evt.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health ? "HP" : "energy";
            string color = evt.Breakdown.resourceType == ResourceRestorationEffect.ResourceType.Health ? Colors.Health : Colors.Energy;
            string action_verb = evt.Breakdown.actualChange > 0 ? "restores" : "drains";
            int absChange = Math.Abs(evt.Breakdown.actualChange);
            
            string message = $"{targetName} {action_verb} <color={color}>{absChange}</color> {resourceName}";
            
            bool playerInvolved = targetVehicle != null && targetVehicle.controlType == ControlType.Player;
            
            var logEvt = RaceHistory.Log(
                EventType.Resource,
                playerInvolved ? EventImportance.Medium : EventImportance.Low,
                message,
                targetVehicle != null ? targetVehicle.currentStage : null,
                sourceVehicle, targetVehicle
            );
            
            logEvt.WithMetadata("resourceType", evt.Breakdown.resourceType.ToString())
                  .WithMetadata("actualChange", evt.Breakdown.actualChange);
        }
        
        // ==================== PRIVATE HELPERS ====================
        
        /// <summary>
        /// Format an entity with its parent vehicle in brackets.
        /// VehicleComponents: "ComponentName [VehicleName]"
        /// Standalone entities (golems, props): "EntityName"
        /// Null entity with vehicle: "VehicleName"
        /// Null entity, no vehicle: "Unknown"
        /// </summary>
        private static string FormatEntityWithVehicle(Entity entity, Vehicle parentVehicle = null)
        {
            if (entity == null)
            {
                return parentVehicle != null ? parentVehicle.vehicleName : "Unknown";
            }
            
            parentVehicle ??= EntityHelpers.GetParentVehicle(entity);
            
            if (entity is VehicleComponent component && parentVehicle != null)
            {
                return $"{component.name} [{parentVehicle.vehicleName}]";
            }
            
            // Standalone entity (golem, prop, NPC) — no vehicle context
            return entity.GetDisplayName();
        }
        
        /// <summary>
        /// Format a combat source showing all non-null participants.
        /// Character + Component + Vehicle: "Ada via Laser Cannon [Ironclad]"
        /// Character + Vehicle (personal skill): "Ada [Ironclad]"
        /// Component + Vehicle (automated):     "Laser Cannon [Ironclad]"
        /// Standalone entity (golem):            "Stone Golem"
        /// Character only:                       "Ada"
        /// Nothing:                              "Unknown"
        /// </summary>
        private static string FormatSource(Character character, Entity entity, Vehicle vehicle)
        {
            vehicle ??= EntityHelpers.GetParentVehicle(entity);
            
            // Get component name (only for VehicleComponents, not standalone entities)
            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle?.vehicleName;
            
            if (character != null)
            {
                // Character is the primary actor
                string suffix = BuildContextSuffix(componentName, vehicleName);
                return $"{character.characterName}{suffix}";
            }
            
            if (entity != null)
            {
                if (componentName != null && vehicleName != null)
                {
                    return $"{componentName} [{vehicleName}]";
                }
                
                // Standalone entity (golem, prop, NPC)
                return entity.GetDisplayName();
            }
            
            if (vehicleName != null)
            {
                return vehicleName;
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// Format a defensive source (for saves) showing all non-null participants.
        /// Uses "at" instead of "via" for component location.
        /// Character + Component + Vehicle: "Ada at Chassis [Ironclad]"
        /// Character + Vehicle (no component): "Ada [Ironclad]"
        /// Component + Vehicle (no character): "Chassis [Ironclad]"
        /// </summary>
        private static string FormatDefensiveSource(Character character, Entity entity, Vehicle vehicle)
        {
            vehicle ??= EntityHelpers.GetParentVehicle(entity);
            
            string componentName = (entity is VehicleComponent comp) ? comp.name : null;
            string vehicleName = vehicle?.vehicleName;
            
            if (character != null)
            {
                string suffix = BuildDefensiveContextSuffix(componentName, vehicleName);
                return $"{character.characterName}{suffix}";
            }
            
            if (entity != null)
            {
                if (componentName != null && vehicleName != null)
                {
                    return $"{componentName} [{vehicleName}]";
                }
                
                return entity.GetDisplayName();
            }
            
            if (vehicleName != null)
            {
                return vehicleName;
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// Build the bracketed suffix for a character-led action.
        /// Both component and vehicle: " via ComponentName [VehicleName]"
        /// Vehicle only:               " [VehicleName]"
        /// Neither:                     ""
        /// </summary>
        private static string BuildContextSuffix(string componentName, string vehicleName)
        {
            if (componentName != null && vehicleName != null)
            {
                return $" via {componentName} [{vehicleName}]";
            }
            
            if (vehicleName != null)
            {
                return $" [{vehicleName}]";
            }
            
            return "";
        }
        
        /// <summary>
        /// Build the bracketed suffix for a defensive action (save).
        /// Component is being defended, not used as a tool, so no "via".
        /// Both component and vehicle: " at ComponentName [VehicleName]"
        /// Vehicle only:               " [VehicleName]"
        /// Neither:                     ""
        /// </summary>
        private static string BuildDefensiveContextSuffix(string componentName, string vehicleName)
        {
            if (componentName != null && vehicleName != null)
            {
                return $" at {componentName} [{vehicleName}]";
            }
            
            if (vehicleName != null)
            {
                return $" [{vehicleName}]";
            }
            
            return "";
        }
        
        /// <summary>
        /// Format the source of a combat action for display.
        /// Uses all available context from the action.
        /// </summary>
        private static string FormatActionSource(CombatAction action)
        {
            return FormatSource(action.SourceCharacter, action.Actor, action.ActorVehicle);
        }
        
        /// <summary>
        /// Check if source and target represent self-damage.
        /// Returns true if same entity OR same vehicle.
        /// </summary>
        private static bool IsSelfDamage(Entity source, Entity target, Vehicle sourceVehicle, Vehicle targetVehicle)
        {
            // Same entity (component attacking itself)
            if (source != null && source == target)
                return true;
            
            // Same vehicle (component attacking different component on same vehicle)
            if (sourceVehicle != null && sourceVehicle == targetVehicle)
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Format a single modifier line for stat breakdowns.
        /// Eliminates duplication in FormatStatBreakdown.
        /// </summary>
        private static string FormatModifierLine(AttributeModifier mod, Attribute attribute)
        {
            string sign = mod.Value >= 0 ? "+" : "";
            string typeStr = mod.Type == ModifierType.Multiplier ? "×" : "";
            string color = mod.Value >= 0 ? Colors.Success : Colors.Failure;
            
            if (mod.Type == ModifierType.Multiplier)
            {
                return $"  <color={color}>{mod.SourceDisplayName,-25} {typeStr}{mod.Value}  {attribute}</color>";
            }
            return $"  <color={color}>{mod.SourceDisplayName,-25} {sign}{mod.Value}  {attribute}</color>";
        }
        
        /// <summary>
        /// Legacy helper - kept for compatibility, uses new FormatEntityWithVehicle internally.
        /// </summary>
        private static string GetTargetDisplayName(Entity target, Vehicle targetVehicle)
        {
            return FormatEntityWithVehicle(target, targetVehicle);
        }
        
        private static string BuildCombinedDamageText(List<DamageEvent> damages)
        {
            if (damages.Count == 0) return "0 damage";
            
            if (damages.Count == 1)
            {
                var d = damages[0].Result;
                return $"<color={Colors.Damage}>{d.finalDamage}</color> {d.damageType} damage";
            }
            
            var parts = damages.Select(d => 
                $"<color={Colors.Damage}>{d.Result.finalDamage}</color> {d.Result.damageType}");
            
            int total = damages.Sum(d => d.Result.finalDamage);
            
            return $"{string.Join(" + ", parts)} (<color={Colors.Damage}>{total} total</color>) damage";
        }
        
        private static string BuildDiceNotation(DamageSourceEntry source)
        {
            if (source.diceCount > 0 && source.dieSize > 0)
            {
                string notation = $"({source.diceCount}d{source.dieSize})";
                if (source.bonus != 0)
                {
                    string sign = source.bonus > 0 ? "+" : "";
                    notation += $"{sign}{source.bonus}";
                }
                return notation;
            }
            else if (source.bonus != 0)
            {
                string sign = source.bonus > 0 ? "+" : "";
                return $"{sign}{source.bonus}";
            }
            return "0";
        }
        
        private static string GetResistanceModifier(ResistanceLevel level)
        {
            return level switch
            {
                ResistanceLevel.Vulnerable => " (×2)",
                ResistanceLevel.Resistant => " (×0.5)",
                ResistanceLevel.Immune => " (×0)",
                _ => ""
            };
        }
        
        private static string BuildDamageTypeLabel(DamageResult result)
        {
            if (result.resistanceLevel != ResistanceLevel.Normal)
            {
                return $"({result.damageType}, {result.resistanceLevel})";
            }
            return $"({result.damageType})";
        }
        
        private static bool DetermineIfBuff(StatusEffect statusEffect)
        {
            float totalModifierValue = statusEffect.modifiers.Sum(m => m.value);
            
            bool hasPeriodicDamage = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.Damage);
            bool hasPeriodicHealing = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.Healing);
            bool hasEnergyDrain = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.EnergyDrain);
            bool hasEnergyRestore = statusEffect.periodicEffects.Any(p => p.type == PeriodicEffectType.EnergyRestore);
            
            bool hasBehavioralRestrictions = statusEffect.behavioralEffects != null &&
                (statusEffect.behavioralEffects.preventsActions ||
                 statusEffect.behavioralEffects.preventsMovement ||
                 statusEffect.behavioralEffects.damageAmplification > 1f);
            
            if (hasPeriodicDamage || hasEnergyDrain || hasBehavioralRestrictions)
                return false;
            
            if (hasPeriodicHealing || hasEnergyRestore || totalModifierValue > 0)
                return true;
            
            return totalModifierValue >= 0;
        }
    }
}
