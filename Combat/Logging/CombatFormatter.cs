using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Core;
using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.Combat.Logging
{
    /// <summary>
    /// Pure formatting methods for combat data display strings.
    /// Used by CombatLogManager for log formatting and by UI for tooltips.
    /// </summary>
    public static class CombatFormatter
    {
        // ==================== COLORS ====================

        public static class Colors
        {
            public const string Success = "#44FF44";
            public const string Failure = "#FF4444";
            public const string Damage = "#FFA500";
            public const string Energy = "#88DDFF";
            public const string Health = "#44FF44";
        }

        // ==================== D20 ROLL FORMATTING ====================
        /// <summary>
        /// Format any d20 roll result with full breakdown for tooltips.
        /// </summary>
        public static string FormatD20RollDetailed(
            D20RollOutcome roll,
            string header,
            string targetLabel,
            string successText,
            string failText)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{header}:");
            sb.AppendLine($"  d20: {roll.BaseRoll}");

            foreach (var bonus in roll.Bonuses)
            {
                string sign = bonus.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {bonus.Label}: {sign}{bonus.Value}");
            }

            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total: {roll.Total}");

            if (roll.TargetValue > 0)
            {
                sb.AppendLine($"  vs {targetLabel}: {roll.TargetValue}");
                string resultText = roll.Success ? successText : failText;
                sb.AppendLine($"  Result: {resultText}");
            }

            return sb.ToString();
        }

        // ==================== DOMAIN ROLL FORMATTERS ====================
        public static string FormatAttackDetailed(AttackResult result)
        {
            if (result == null) return "No roll data";
            return FormatD20RollDetailed(result.Roll, "Attack Roll Breakdown", "AC", "HIT", "MISS");
        }

        public static string FormatSaveDetailed(SaveResult result)
        {
            if (result == null) return "No roll data";
            return FormatD20RollDetailed(result.Roll, $"{result.Spec.DisplayName} Save", "DC", "SAVED", "FAILED");
        }

        public static string FormatSkillCheckDetailed(SkillCheckResult result)
        {
            if (result == null) return "No roll data";
            return FormatD20RollDetailed(result.Roll, $"{result.Spec.DisplayName} Check", "DC", "SUCCESS", "FAILURE");
        }

        // ==================== DC / DEFENSE FORMATTING ====================

        public static string FormatDCDetailed(int dc, string skillName, SaveSpec saveSpec)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{saveSpec.DisplayName} Save DC Breakdown:");
            sb.AppendLine($"  Base DC: {dc} ({skillName})");
            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total DC: {dc}");
            return sb.ToString();
        }

        public static string FormatDefenseDetailed(int total, float baseValue, List<AttributeModifier> modifiers, string defenseName = "AC")
        {
            if (modifiers == null)
                return $"{defenseName}: {total}";

            var sb = new StringBuilder();
            sb.AppendLine($"{defenseName} Breakdown:");
            sb.AppendLine($"  Base: {(int)baseValue}");

            foreach (var mod in modifiers)
            {
                string sign = mod.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {mod.SourceDisplayName}: {sign}{(int)mod.Value} ({mod.Category})");
            }

            sb.AppendLine("  ─────────────");
            sb.AppendLine($"  Total {defenseName}: {total}");
            return sb.ToString();
        }

        // ==================== DAMAGE FORMATTING ====================

        public static string FormatDamageDetailed(DamageResult result)
        {
            if (result == null) return "No damage";

            var sb = new StringBuilder();
            sb.AppendLine($"Damage Breakdown ({result.DamageType}):");

            string diceNotation = BuildDiceNotation(result);
            string resistMod = GetResistanceModifier(result.ResistanceLevel);
            sb.AppendLine($"  Roll: {diceNotation}{resistMod} = {result.RawTotal}");
            sb.AppendLine("  ─────────────");

            if (result.ResistanceLevel != ResistanceLevel.Normal)
            {
                sb.AppendLine($"  {result.ResistanceLevel}: {resistMod}");
            }

            sb.AppendLine($"  Final: {result.FinalDamage} {result.DamageType}");
            return sb.ToString();
        }

        public static string FormatCombinedDamageDetailed(List<DamageResult> results, string sourceName = null)
        {
            if (results == null || results.Count == 0) return "No damage";
            if (results.Count == 1) return FormatDamageDetailed(results[0]);

            var sb = new StringBuilder();
            int totalDamage = results.Sum(r => r.FinalDamage);
            sb.AppendLine($"Damage Total: {totalDamage}");

            if (!string.IsNullOrEmpty(sourceName))
            {
                sb.AppendLine($"Damage Source: {sourceName}");
            }
            sb.AppendLine();

            foreach (var result in results)
            {
                string diceNotation = BuildDiceNotation(result);
                string resistMod = GetResistanceModifier(result.ResistanceLevel);
                string label = result.ResistanceLevel != ResistanceLevel.Normal
                    ? $"({result.DamageType}, {result.ResistanceLevel})"
                    : $"({result.DamageType})";

                sb.AppendLine($"{diceNotation}{resistMod} = {result.FinalDamage} {label}");
            }

            return sb.ToString().Trim();
        }

        // ==================== STAT BREAKDOWN ====================

        public static string FormatStatBreakdown(
            Entity entity,
            Attribute attribute,
            int baseValue,
            int finalValue)
        {
            if (entity == null)
                return $"{attribute}: {finalValue}";

            var (calculatedTotal, returnedBase, modifiers) = StatCalculator.GatherAttributeValueWithBreakdown(
                entity, attribute);

            if (modifiers.Count == 0)
                return $"{attribute}: {baseValue}\n═══════════════════════════════\nBase value only (no modifiers)";

            var sb = new StringBuilder();
            sb.AppendLine($"{attribute}: {finalValue}");
            sb.AppendLine("═══════════════════════════════");
            sb.AppendLine($"Base:                      {returnedBase}");
            sb.AppendLine();

            AppendModifiersByCategory(sb, modifiers, attribute);

            float totalModifiers = finalValue - returnedBase;
            string totalSign = totalModifiers >= 0 ? "+" : "";
            sb.AppendLine("═══════════════════════════════");
            sb.AppendLine($"Total Modifiers:          {totalSign}{totalModifiers}  {attribute}");
            sb.AppendLine($"Final Value:               {finalValue}  {attribute}");

            return sb.ToString();
        }

        // ==================== STATUS EFFECT FORMATTING ====================

        public static string FormatStatusEffectTooltip(AppliedStatusEffect appliedEffect)
        {
            if (appliedEffect == null || appliedEffect.template == null)
                return "Unknown status effect";

            var effect = appliedEffect.template;
            var sb = new StringBuilder();

            sb.AppendLine($"{effect.effectName}");
            sb.AppendLine("═══════════════════════════════");

            string durationStr = appliedEffect.IsIndefinite
                ? "Indefinite (∞)"
                : $"{appliedEffect.turnsRemaining} turns remaining";
            sb.AppendLine($"Duration: {durationStr}");
            sb.AppendLine();

            AppendStatusModifiers(sb, effect);
            AppendPeriodicEffects(sb, effect);
            AppendBehavioralEffects(sb, effect);

            if (!string.IsNullOrEmpty(effect.description))
            {
                sb.AppendLine(effect.description);
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════");
            return sb.ToString().TrimEnd();
        }

        // ==================== PRIVATE HELPERS ====================

        private static string BuildDiceNotation(DamageResult result)
        {
            if (result.DiceCount > 0 && result.DieSize > 0)
            {
                string notation = $"({result.DiceCount}d{result.DieSize})";
                if (result.Bonus != 0)
                {
                    string sign = result.Bonus > 0 ? "+" : "";
                    notation += $"{sign}{result.Bonus}";
                }
                return notation;
            }
            if (result.Bonus != 0)
            {
                string sign = result.Bonus > 0 ? "+" : "";
                return $"{sign}{result.Bonus}";
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

        private static void AppendModifiersByCategory(StringBuilder sb, List<AttributeModifier> modifiers, Attribute attribute)
        {
            var grouped = new Dictionary<string, List<AttributeModifier>>
            {
                ["Status Effects"] = new List<AttributeModifier>(),
                ["Equipment"] = new List<AttributeModifier>(),
                ["Auras"] = new List<AttributeModifier>(),
                ["Skills"] = new List<AttributeModifier>(),
                ["Other"] = new List<AttributeModifier>()
            };

            foreach (var mod in modifiers)
            {
                string key = mod.Category switch
                {
                    ModifierCategory.StatusEffect => "Status Effects",
                    ModifierCategory.Equipment => "Equipment",
                    ModifierCategory.Aura => "Auras",
                    ModifierCategory.Skill => "Skills",
                    _ => "Other"
                };
                grouped[key].Add(mod);
            }

            foreach (var kvp in grouped)
            {
                if (kvp.Value.Count == 0) continue;

                sb.AppendLine($"{kvp.Key}:");
                foreach (var mod in kvp.Value)
                {
                    sb.AppendLine(FormatModifierLine(mod, attribute));
                }
                sb.AppendLine();
            }
        }

        private static string FormatModifierLine(AttributeModifier mod, Attribute attribute)
        {
            string sign = mod.Value >= 0 ? "+" : "";
            string typeStr = mod.Type == ModifierType.Multiplier ? "×" : "";
            string color = mod.Value >= 0 ? Colors.Success : Colors.Failure;

            if (mod.Type == ModifierType.Multiplier)
                return $"  <color={color}>{mod.SourceDisplayName,-25} {typeStr}{mod.Value}  {attribute}</color>";
            return $"  <color={color}>{mod.SourceDisplayName,-25} {sign}{mod.Value}  {attribute}</color>";
        }

        private static void AppendStatusModifiers(StringBuilder sb, StatusEffect effect)
        {
            if (effect.modifiers == null || effect.modifiers.Count == 0) return;

            sb.AppendLine("Effects:");
            foreach (var mod in effect.modifiers)
            {
                string color = mod.value >= 0 ? "#44FF44" : "#FF4444";
                string valueStr = mod.type == ModifierType.Multiplier
                    ? $"×{mod.value}"
                    : $"{(mod.value >= 0 ? "+" : "")}{mod.value}";
                sb.AppendLine($"  • <color={color}>{valueStr} {mod.attribute}</color>");
            }
            sb.AppendLine();
        }

        private static void AppendPeriodicEffects(StringBuilder sb, StatusEffect effect)
        {
            if (effect.periodicEffects == null || effect.periodicEffects.Count == 0) return;

            foreach (var periodic in effect.periodicEffects)
            {
                string effectText = periodic.type switch
                {
                    PeriodicEffectType.Damage => $"  • <color=#FF4444>{FormatFormulaNotation(periodic.damageFormula)} {periodic.damageFormula.damageType} damage per turn</color>",
                    PeriodicEffectType.Healing => $"  • <color=#44FF44>{periodic.amount} healing per turn</color>",
                    PeriodicEffectType.EnergyDrain => $"  • <color=#FF4444>-{periodic.amount} energy per turn</color>",
                    PeriodicEffectType.EnergyRestore => $"  • <color=#88DDFF>+{periodic.amount} energy per turn</color>",
                    _ => null
                };
                if (effectText != null)
                    sb.AppendLine(effectText);
            }
            sb.AppendLine();
        }

        public static string FormatFormulaNotation(DamageFormula formula)
        {
            if (formula.baseDice <= 0)
                return formula.bonus.ToString();

            string notation = $"{formula.baseDice}d{formula.dieSize}";
            if (formula.bonus != 0)
                notation += $"{formula.bonus:+0;-0}";
            return notation;
        }

        private static void AppendBehavioralEffects(StringBuilder sb, StatusEffect effect)
        {
            if (effect.behavioralEffects == null) return;

            bool hasAny = false;
            if (effect.behavioralEffects.preventsActions)
            {
                sb.AppendLine("  • <color=#FF4444>Prevents actions</color>");
                hasAny = true;
            }
            if (effect.behavioralEffects.preventsMovement)
            {
                sb.AppendLine("  • <color=#FF4444>Prevents movement</color>");
                hasAny = true;
            }
            if (effect.behavioralEffects.damageAmplification != 1f)
            {
                float percent = (effect.behavioralEffects.damageAmplification - 1f) * 100f;
                string color = percent > 0 ? "#FF4444" : "#44FF44";
                string sign = percent > 0 ? "+" : "";
                sb.AppendLine($"  • <color={color}>{sign}{percent:F0}% damage taken</color>");
                hasAny = true;
            }
            if (hasAny)
                sb.AppendLine();
        }
    }
}
