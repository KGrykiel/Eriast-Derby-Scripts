using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Formats damage breakdowns for tooltips and combat logs.
    /// Pure formatting logic - no game state modification.
    /// </summary>
    public static class SkillDamageFormatter
    {
        /// <summary>
        /// Builds a combined damage breakdown string from multiple damage effects.
        /// Format: (4d6+5) (×0.5) = 12 fire (resistant)
        /// </summary>
        public static string BuildCombinedDamageBreakdown(List<DamageBreakdown> breakdowns, string sourceName)
        {
            if (breakdowns == null || breakdowns.Count == 0)
                return "";
            
            var sb = new StringBuilder();
            int totalDamage = breakdowns.Sum(d => d.finalDamage);
            
            sb.AppendLine($"Damage Result: {totalDamage}");
            sb.AppendLine($"Damage Source: {sourceName}");
            sb.AppendLine();
            
            // List each damage breakdown with inline resistance
            foreach (var breakdown in breakdowns)
            {
                // Show each component in the breakdown
                foreach (var comp in breakdown.components)
                {
                    string diceStr = "";
                    if (comp.diceCount > 0)
                    {
                        diceStr = $"({comp.ToDiceString()})";
                    }
                    else if (comp.bonus != 0)
                    {
                        string sign = comp.bonus >= 0 ? "+" : "";
                        diceStr = $"({sign}{comp.bonus})";
                    }
                    
                    // Add resistance multiplier if applicable
                    string resistMod = "";
                    string resistLabel = "";
                    if (breakdown.resistanceLevel != ResistanceLevel.Normal)
                    {
                        resistMod = breakdown.resistanceLevel switch
                        {
                            ResistanceLevel.Vulnerable => " (×2)",
                            ResistanceLevel.Resistant => " (×0.5)",
                            ResistanceLevel.Immune => " (×0)",
                            _ => ""
                        };
                        resistLabel = $" ({breakdown.resistanceLevel.ToString().ToLower()})";
                    }
                    
                    // Calculate this component's contribution after resistance
                    int componentDamage = comp.total;
                    if (breakdown.resistanceLevel == ResistanceLevel.Vulnerable)
                        componentDamage *= 2;
                    else if (breakdown.resistanceLevel == ResistanceLevel.Resistant)
                        componentDamage /= 2;
                    else if (breakdown.resistanceLevel == ResistanceLevel.Immune)
                        componentDamage = 0;
                    
                    // Format: (1d8+2) (×0.5) = 5 physical (resistant)
                    sb.AppendLine($"{diceStr}{resistMod} = {componentDamage} {breakdown.damageType.ToString().ToLower()}{resistLabel}");
                }
            }
            
            return sb.ToString().Trim();
        }
    }
}
