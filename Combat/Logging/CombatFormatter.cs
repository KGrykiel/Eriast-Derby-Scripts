using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.Modifiers;
using Assets.Scripts.Entities;
using Assets.Scripts.Logging;

namespace Assets.Scripts.Combat.Logging
{
    /// <summary>
    /// Pure formatting methods for combat data display strings.
    /// Used by CombatLogManager for log formatting and by UI for tooltips.
    /// </summary>
    public static class CombatFormatter
    {
        // ==================== D20 ROLL FORMATTING ====================
        /// <summary>
        /// Format any d20 roll result with full breakdown for tooltips.
        /// </summary>
        public static string FormatD20RollDetailed(
            D20RollOutcome roll,
            string header,
            string targetLabel,
            string successText,
            string failText,
            bool showResult = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{header}:");

            if (roll.Advantage.DroppedRoll.HasValue)
                sb.AppendLine($"  d20: {roll.BaseRoll} (dropped: {roll.Advantage.DroppedRoll.Value}) [{roll.Advantage.Mode}]");
            else
                sb.AppendLine($"  d20: {roll.BaseRoll}");

            if (roll.Advantage.Sources != null && roll.Advantage.Sources.Count > 0)
            {
                foreach (var src in roll.Advantage.Sources)
                {
                    sb.AppendLine($"  {src.Label}: {src.Type}");
                }
            }

            foreach (var bonus in roll.Bonuses)
            {
                string sign = bonus.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {bonus.Label}: {sign}{bonus.Value}");
            }

            sb.AppendLine("  -------------");
            sb.AppendLine($"  Total: {LogColors.Number(roll.Total.ToString())}");

            if (roll.TargetValue > 0)
            {
                sb.AppendLine($"  vs {targetLabel}: {LogColors.DC(roll.TargetValue.ToString())}");
                if (showResult)
                {
                    string resultText = roll.Success ? successText : failText;
                    sb.AppendLine($"  Result: {(roll.Success ? LogColors.Success(resultText) : LogColors.Failure(resultText))}");
                }
            }

            return sb.ToString();
        }

        // ==================== DOMAIN ROLL FORMATTERS ====================
        public static string FormatAttackDetailed(D20RollOutcome roll)
        {
            return FormatD20RollDetailed(roll, "Attack Roll Breakdown", "AC", "HIT", "MISS");
        }

        public static string FormatSaveDetailed(D20RollOutcome roll, string checkName)
        {
            return FormatD20RollDetailed(roll, $"{checkName} Save", "DC", "SAVED", "FAILED");
        }

        public static string FormatSkillCheckDetailed(D20RollOutcome roll, string checkName)
        {
            return FormatD20RollDetailed(roll, $"{checkName} Check", "DC", "SUCCESS", "FAILURE");
        }

        public static string FormatOpposedCheckDetailed(D20RollOutcome roll, D20RollOutcome? defenderRoll, string attackerCheckName, string defenderCheckName)
        {
            string winner = roll.Success ? "Attacker wins" : "Defender wins";
            string formattedWinner = roll.Success ? LogColors.Success(winner) : LogColors.Failure(winner);

            var sb = new StringBuilder();
            sb.AppendLine($"{formattedWinner} - {roll.Total} vs {roll.TargetValue}");
            sb.AppendLine();
            sb.Append(FormatD20RollDetailed(
                roll,
                $"Attacker ({attackerCheckName})",
                $"Defender ({defenderCheckName})", "WINS", "LOSES"));

            if (defenderRoll.HasValue)
            {
                sb.AppendLine();
                sb.Append(FormatD20RollDetailed(
                    defenderRoll.Value,
                    $"Defender ({defenderCheckName})",
                    "Opponent", "WINS", "LOSES",
                    showResult: false));
            }

            return sb.ToString();
        }

        // ==================== DC / DEFENSE FORMATTING ====================

        public static string FormatDCDetailed(int dc, string skillName, string checkTypeName, string checkType = "Save")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{checkTypeName} {checkType} DC Breakdown:");
            sb.AppendLine($"  Base DC: {LogColors.DC(dc.ToString())} ({skillName})");
            sb.AppendLine("  -------------");
            sb.AppendLine($"  Total DC: {LogColors.DC(dc.ToString())}");
            return sb.ToString();
        }

        public static string FormatDefenseDetailed(int total, int baseValue, List<EntityAttributeModifier> modifiers, string defenseName = "AC")
        {
            if (modifiers == null)
                return $"{defenseName}: {total}";

            var sb = new StringBuilder();
            sb.AppendLine($"{defenseName} Breakdown:");
            sb.AppendLine($"  Base: {baseValue}");

            foreach (var mod in modifiers)
            {
                string sign = mod.Value >= 0 ? "+" : "";
                sb.AppendLine($"  {mod.Label}: {sign}{(int)mod.Value}");
            }

            sb.AppendLine("  -------------");
            sb.AppendLine($"  Total {defenseName}: {LogColors.Number(total.ToString())}");
            return sb.ToString();
        }

        // ==================== DAMAGE FORMATTING ====================

        public static string FormatDamageDetailed(DamageResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Damage Breakdown ({result.DamageType}):");

            string diceNotation = BuildDiceNotation(result);
            string resistMod = GetResistanceModifier(result.ResistanceLevel);
            sb.AppendLine($"  Roll: {diceNotation}{resistMod} = {result.RawTotal}");
            sb.AppendLine("  -------------");

            if (result.ResistanceLevel != ResistanceLevel.Normal)
            {
                sb.AppendLine($"  {result.ResistanceLevel}: {resistMod}");
            }

            sb.AppendLine($"  Final: {LogColors.DamageType(result.DamageType, $"{result.FinalDamage} {result.DamageType}")}");
            return sb.ToString();
        }

        public static string FormatCombinedDamageDetailed(List<DamageResult> results, string sourceName = null)
        {
            if (results == null || results.Count == 0) return "No damage";
            if (results.Count == 1) return FormatDamageDetailed(results[0]);

            var sb = new StringBuilder();
            int totalDamage = results.Sum(r => r.FinalDamage);
            sb.AppendLine($"Damage Total: {LogColors.Damage(totalDamage.ToString())}");

            if (!string.IsNullOrEmpty(sourceName))
            {
                sb.AppendLine($"Damage Source: {sourceName}");
            }
            sb.AppendLine();

            foreach (var result in results)
            {
                string diceNotation = BuildDiceNotation(result);
                string resistMod = GetResistanceModifier(result.ResistanceLevel);
                string resistLabel = result.ResistanceLevel != ResistanceLevel.Normal
                    ? $" ({result.ResistanceLevel})"
                    : "";

                sb.AppendLine($"{diceNotation}{resistMod} = {LogColors.DamageType(result.DamageType, $"{result.FinalDamage} {result.DamageType}")}{resistLabel}");
            }

            return sb.ToString().Trim();
        }

        public static string FormatRestorationDetailed(RestorationResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Restoration Breakdown ({result.ResourceType}):");

            string diceNotation = BuildDiceNotation(result);
            sb.AppendLine($"  Roll: {diceNotation} = {result.RawTotal}");
            sb.AppendLine("  -------------");
            sb.AppendLine($"  Requested: {result.RequestedChange:+0;-0}");
            string resUnit = result.ResourceType == ResourceType.Energy ? "energy" : "HP";
            string formattedActual = result.ResourceType == ResourceType.Energy
                ? LogColors.Energy($"{result.ActualChange:+0;-0} {resUnit}")
                : LogColors.Health($"{result.ActualChange:+0;-0} {resUnit}");
            sb.AppendLine($"  Actual: {formattedActual}");
            sb.AppendLine($"  Resource: {result.OldValue} -> {result.NewValue} / {result.MaxValue}");
            return sb.ToString();
        }

        public static string FormatCombinedRestorationDetailed(List<RestorationResult> results, string sourceName = null)
        {
            if (results == null || results.Count == 0) return "No restoration";
            if (results.Count == 1) return FormatRestorationDetailed(results[0]);

            var sb = new StringBuilder();
            int totalChange = results.Sum(r => r.ActualChange);
            sb.AppendLine($"Restoration Total: {LogColors.Health($"{totalChange:+0;-0}")}");

            if (!string.IsNullOrEmpty(sourceName))
            {
                sb.AppendLine($"Restoration Source: {sourceName}");
            }
            sb.AppendLine();

            foreach (var result in results)
            {
                string diceNotation = BuildDiceNotation(result);
                sb.AppendLine($"{diceNotation} = {result.ActualChange:+0;-0} ({result.ResourceType})");
            }

            return sb.ToString().Trim();
        }

        // ==================== STAT BREAKDOWN ====================

        public static string FormatStatBreakdown(
            EntityAttribute attribute,
            int baseValue,
            int finalValue,
            List<EntityAttributeModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
                return $"{attribute}: {baseValue}\n===============================\nBase value only (no modifiers)";

            var sb = new StringBuilder();
            sb.AppendLine($"{attribute}: {finalValue}");
            sb.AppendLine("===============================");
            sb.AppendLine($"Base:                      {baseValue}");
            sb.AppendLine();

            AppendGroupedModifiers(sb, modifiers, attribute);

            float totalModifiers = finalValue - baseValue;
            string totalSign = totalModifiers >= 0 ? "+" : "";
            sb.AppendLine("===============================");
            sb.AppendLine($"Total Modifiers:          {totalSign}{totalModifiers}  {attribute}");
            sb.AppendLine($"Final Value:               {finalValue}  {attribute}");

            return sb.ToString();
        }

        // ==================== STATUS EFFECT FORMATTING ====================

        public static string FormatEntityConditionTooltip(AppliedEntityCondition appliedEffect)
        {
            if (appliedEffect == null || appliedEffect.template == null)
                return "Unknown status effect";

            var effect = appliedEffect.template;
            var sb = new StringBuilder();

            sb.AppendLine($"{effect.effectName}");
            sb.AppendLine("===============================");

            string durationStr = appliedEffect.IsIndefinite
                ? "Indefinite"
                : $"{appliedEffect.turnsRemaining} turns remaining";
            sb.AppendLine($"Duration: {durationStr}");
            sb.AppendLine();

            AppendStatusModifiers(sb, effect.modifiers);
            AppendPeriodicEffects(sb, effect.periodicEffects);
            AppendBehavioralEffects(sb, effect);

            if (!string.IsNullOrEmpty(effect.description))
            {
                sb.AppendLine(effect.description);
                sb.AppendLine();
            }

            sb.AppendLine("===============================");
            return sb.ToString().TrimEnd();
        }

        public static string FormatCharacterConditionTooltip(AppliedCharacterCondition applied)
        {
            if (applied == null || applied.template == null)
                return "Unknown character condition";

            var condition = applied.template;
            var sb = new StringBuilder();

            sb.AppendLine($"{condition.effectName}");
            sb.AppendLine("===============================");

            string durationStr = applied.IsIndefinite
                ? "Indefinite"
                : $"{applied.turnsRemaining} turns remaining";
            sb.AppendLine($"Duration: {durationStr}");
            sb.AppendLine();

            AppendCharacterModifiers(sb, condition);
            AppendBehavioralEffects(sb, condition);

            if (!string.IsNullOrEmpty(condition.description))
            {
                sb.AppendLine(condition.description);
                sb.AppendLine();
            }

            sb.AppendLine("===============================");
            return sb.ToString().TrimEnd();
        }

        public static string FormatVehicleConditionTooltip(AppliedVehicleCondition applied)
        {
            if (applied == null || applied.template == null)
                return "Unknown vehicle condition";

            var condition = applied.template;
            var sb = new StringBuilder();

            sb.AppendLine($"{condition.effectName}");
            sb.AppendLine("===============================");

            string durationStr = applied.IsIndefinite
                ? "Indefinite"
                : $"{applied.turnsRemaining} turns remaining";
            sb.AppendLine($"Duration: {durationStr}");
            sb.AppendLine();

            AppendStatusModifiers(sb, condition.modifiers);
            AppendPeriodicEffects(sb, condition.periodicEffects);
            AppendBehavioralEffects(sb, condition);

            if (!string.IsNullOrEmpty(condition.description))
            {
                sb.AppendLine(condition.description);
                sb.AppendLine();
            }

            sb.AppendLine("===============================");
            return sb.ToString().TrimEnd();
        }

        private static void AppendCharacterModifiers(StringBuilder sb, CharacterCondition condition)
        {
            if (condition.modifiers == null || condition.modifiers.Count == 0) return;

            sb.AppendLine("Effects:");
            foreach (var mod in condition.modifiers)
            {
                string valueStr = mod.type == ModifierType.Multiplier
                    ? $"x{mod.value}"
                    : $"{(mod.value >= 0 ? "+" : "")}{mod.value}";

                string target = mod switch
                {
                    CharacterSkillModifierData skillData => skillData.skill.ToString(),
                    CharacterAttributeModifierData attrData => attrData.attribute.ToString(),
                    _ => "Unknown"
                };
                bool positive = mod.value >= 0;
                sb.AppendLine($"  - {(positive ? LogColors.Success($"{valueStr} {target}") : LogColors.Failure($"{valueStr} {target}"))}");
            }
            sb.AppendLine();
        }

        // ==================== PRIVATE HELPERS ====================

        private static string BuildDiceNotation(DamageResult result)
            => BuildDiceNotation(result.DiceCount, result.DieSize, result.Bonus);

        private static string BuildDiceNotation(RestorationResult result)
            => BuildDiceNotation(result.DiceCount, result.DieSize, result.Bonus);

        private static string BuildDiceNotation(int diceCount, int dieSize, int bonus)
        {
            if (diceCount > 0 && dieSize > 0)
            {
                string notation = $"({diceCount}d{dieSize})";
                if (bonus != 0)
                {
                    string sign = bonus > 0 ? "+" : "";
                    notation += $"{sign}{bonus}";
                }
                return notation;
            }
            if (bonus != 0)
            {
                string sign = bonus > 0 ? "+" : "";
                return $"{sign}{bonus}";
            }
            return "0";
        }

        private static string GetResistanceModifier(ResistanceLevel level)
        {
            return level switch
            {
                ResistanceLevel.Vulnerable => " (x2)",
                ResistanceLevel.Resistant => " (x0.5)",
                ResistanceLevel.Immune => " (x0)",
                _ => ""
            };
        }

        private static void AppendGroupedModifiers(StringBuilder sb, List<EntityAttributeModifier> modifiers, EntityAttribute attribute)
        {
            foreach (var mod in modifiers)
                sb.AppendLine(FormatModifierLine(mod, attribute));
            sb.AppendLine();
        }

        private static string FormatModifierLine(EntityAttributeModifier mod, EntityAttribute attribute)
        {
            string sign = mod.Value >= 0 ? "+" : "";
            string typeStr = mod.Type == ModifierType.Multiplier ? "x" : "";
            bool positive = mod.Value >= 0;

            string line = mod.Type == ModifierType.Multiplier
                ? $"  {mod.Label,-25} {typeStr}{mod.Value}  {attribute}"
                : $"  {mod.Label,-25} {sign}{mod.Value}  {attribute}";

            return positive ? LogColors.Success(line) : LogColors.Failure(line);
        }

        private static void AppendStatusModifiers(StringBuilder sb, List<EntityModifierData> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0) return;

            sb.AppendLine("Effects:");
            foreach (var mod in modifiers)
            {
                string valueStr = mod.type == ModifierType.Multiplier
                    ? $"x{mod.value}"
                    : $"{(mod.value >= 0 ? "+" : "")}{mod.value}";
                bool positive = mod.value >= 0;
                sb.AppendLine($"  - {(positive ? LogColors.Success($"{valueStr} {mod.attribute}") : LogColors.Failure($"{valueStr} {mod.attribute}"))}");
            }
            sb.AppendLine();
        }

        private static void AppendPeriodicEffects(StringBuilder sb, List<IPeriodicEffect> periodicEffects)
        {
            if (periodicEffects == null || periodicEffects.Count == 0) return;

            foreach (var periodic in periodicEffects)
            {
                string effectText = periodic switch
                {
                    PeriodicDamageEffect dmg => FormatPeriodicDamage(dmg),
                    PeriodicRestorationEffect res => FormatPeriodicRestoration(res),
                    _ => null
                };
                if (effectText != null)
                    sb.AppendLine(effectText);
            }
            sb.AppendLine();
        }

        private static string FormatPeriodicDamage(PeriodicDamageEffect dmg)
        {
            return $"  - {LogColors.DamageType(dmg.damageFormula.damageType, $"{FormatFormulaNotation(dmg.damageFormula)} {dmg.damageFormula.damageType} damage per turn")}";
        }

        private static string FormatPeriodicRestoration(PeriodicRestorationEffect res)
        {
            string notation = FormatRestorationNotation(res.formula);
            string resourceName = res.formula.resourceType == ResourceType.Health ? "HP" : "energy";
            bool isPositive = res.formula.baseDice > 0 || res.formula.bonus >= 0;
            string verb = isPositive ? "restores" : "drains";
            string body = $"{verb} {notation} {resourceName} per turn";
            return $"  - {(isPositive ? LogColors.Success(body) : LogColors.Failure(body))}";
        }

        public static string FormatFormulaNotation(DamageFormula formula)
            => FormatFormulaNotation(formula.baseDice, formula.dieSize, formula.bonus);

        public static string FormatRestorationNotation(RestorationFormula formula)
            => FormatFormulaNotation(formula.baseDice, formula.dieSize, System.Math.Abs(formula.bonus));

        private static string FormatFormulaNotation(int baseDice, int dieSize, int bonus)
        {
            if (baseDice <= 0)
                return bonus.ToString();

            string notation = $"{baseDice}d{dieSize}";
            if (bonus != 0)
                notation += $"{bonus:+0;-0}";
            return notation;
        }

        private static void AppendBehavioralEffects(StringBuilder sb, ConditionBase effect)
        {
            if (effect.behavioralEffects == null) return;

            bool hasAny = false;
            if (effect.behavioralEffects.preventsActions)
            {
                sb.AppendLine($"  - {LogColors.Failure("Prevents actions")}");
                hasAny = true;
            }
            if (effect.behavioralEffects.preventsMovement)
            {
                sb.AppendLine($"  - {LogColors.Failure("Prevents movement")}");
                hasAny = true;
            }
            if (hasAny)
                sb.AppendLine();
        }
    }
}
