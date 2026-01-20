using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Logging;

/// <summary>
/// Attach to event entry UI elements to show detailed breakdowns on hover.
/// Reads breakdown data from RaceEvent metadata and displays via RollTooltip.
/// </summary>
public class EventEntryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RaceEvent linkedEvent;
    private string cachedTooltipContent;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Sets the event data for this entry.
    /// </summary>
    public void SetEvent(RaceEvent evt)
    {
        linkedEvent = evt;
        cachedTooltipContent = BuildTooltipContent(evt);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(cachedTooltipContent))
        {
            if (RollTooltip.Instance == null)
            {
                return;
            }
            
            // Pass this element's RectTransform so tooltip positions relative to it
            RollTooltip.ShowNow(cachedTooltipContent, rectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RollTooltip.Hide();
    }

    /// <summary>
    /// Builds the tooltip content from event metadata.
    /// </summary>
    private string BuildTooltipContent(RaceEvent evt)
    {
        if (evt == null || evt.metadata == null || evt.metadata.Count == 0)
            return null;

        var sb = new System.Text.StringBuilder();

        // Check for roll breakdown
        if (evt.metadata.ContainsKey("rollBreakdown") && evt.metadata["rollBreakdown"] is string rollBreakdown)
        {
            sb.AppendLine("<b>Attack Roll:</b>");
            sb.AppendLine(rollBreakdown);
        }
        
        // Check for defense breakdown (AC)
        if (evt.metadata.ContainsKey("defenseBreakdown") && evt.metadata["defenseBreakdown"] is string defenseBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine("<b>Target Defense:</b>");
            sb.AppendLine(defenseBreakdown);
        }

        // Check for damage breakdown
        if (evt.metadata.ContainsKey("damageBreakdown") && evt.metadata["damageBreakdown"] is string damageBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(damageBreakdown);
        }

        // Check for status effect breakdown (shows what the effect does)
        if (evt.metadata.ContainsKey("effectBreakdown") && evt.metadata["effectBreakdown"] is string effectBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(effectBreakdown);
        }
        
        // Check for DC breakdown (saving throw difficulty)
        if (evt.metadata.ContainsKey("dcBreakdown") && evt.metadata["dcBreakdown"] is string dcBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(dcBreakdown);
        }
        
        // Check for save modifiers breakdown (target's save bonus)
        if (evt.metadata.ContainsKey("saveModifiersBreakdown") && evt.metadata["saveModifiersBreakdown"] is string saveModBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(saveModBreakdown);
        }
        
        // Check for restoration/resource data
        if (evt.metadata.ContainsKey("actualChange") && evt.metadata.ContainsKey("resourceType"))
        {
            if (sb.Length > 0) sb.AppendLine();
            int change = System.Convert.ToInt32(evt.metadata["actualChange"]);
            string resourceType = evt.metadata["resourceType"].ToString();
            string verb = change > 0 ? "Restored" : "Drained";
            sb.AppendLine($"<b>Resource Change:</b>");
            sb.AppendLine($"  {verb}: {System.Math.Abs(change)} {resourceType}");
        }

        // Check for component targeting breakdowns
        if (evt.metadata.ContainsKey("componentRollBreakdown") && evt.metadata["componentRollBreakdown"] is string compRoll)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine("<b>Component Attack:</b>");
            sb.AppendLine(compRoll);
        }

        if (evt.metadata.ContainsKey("chassisRollBreakdown") && evt.metadata["chassisRollBreakdown"] is string chassisRoll)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine("<b>Chassis Attack:</b>");
            sb.AppendLine(chassisRoll);
        }

        // If no breakdown data, build from individual metadata fields
        if (sb.Length == 0)
        {
            bool hasAnyData = false;
            
            // Roll details
            if (evt.metadata.ContainsKey("baseRoll"))
            {
                if (!hasAnyData) { sb.AppendLine("<b>Event Details:</b>"); hasAnyData = true; }
                sb.AppendLine($"Base Roll: {evt.metadata["baseRoll"]}");
            }

            if (evt.metadata.ContainsKey("totalModifier"))
            {
                if (!hasAnyData) { sb.AppendLine("<b>Event Details:</b>"); hasAnyData = true; }
                int mod = System.Convert.ToInt32(evt.metadata["totalModifier"]);
                string sign = mod >= 0 ? "+" : "";
                sb.AppendLine($"Total Modifier: {sign}{mod}");
            }

            if (evt.metadata.ContainsKey("total"))
            {
                if (!hasAnyData) { sb.AppendLine("<b>Event Details:</b>"); hasAnyData = true; }
                sb.AppendLine($"Final Total: {evt.metadata["total"]}");
            }

            if (evt.metadata.ContainsKey("targetValue"))
            {
                if (!hasAnyData) { sb.AppendLine("<b>Event Details:</b>"); hasAnyData = true; }
                sb.AppendLine($"Target Value: {evt.metadata["targetValue"]}");
            }

            // Damage details
            if (evt.metadata.ContainsKey("totalDamage"))
            {
                if (!hasAnyData) { sb.AppendLine("<b>Event Details:</b>"); hasAnyData = true; }
                sb.AppendLine($"Total Damage: {evt.metadata["totalDamage"]}");
            }

            if (evt.metadata.ContainsKey("damageType"))
            {
                if (!hasAnyData) { sb.AppendLine("<b>Event Details:</b>"); hasAnyData = true; }
                sb.AppendLine($"Damage Type: {evt.metadata["damageType"]}");
            }

            // Individual modifiers (if stored separately)
            if (evt.metadata.ContainsKey("modifierCount"))
            {
                int modifierCount = System.Convert.ToInt32(evt.metadata["modifierCount"]);
                if (modifierCount > 0)
                {
                    sb.AppendLine("\n<b>Modifiers:</b>");
                    for (int i = 0; i < modifierCount; i++)
                    {
                        string nameKey = $"modifier_{i}_name";
                        string valueKey = $"modifier_{i}_value";
                        string sourceKey = $"modifier_{i}_source";
                        
                        if (evt.metadata.ContainsKey(nameKey) && evt.metadata.ContainsKey(valueKey))
                        {
                            object modName = evt.metadata[nameKey];
                            int val = System.Convert.ToInt32(evt.metadata[valueKey]);
                            string sign = val >= 0 ? "+" : "";
                            string source = evt.metadata.ContainsKey(sourceKey) ? $" ({evt.metadata[sourceKey]})" : "";
                            sb.AppendLine($"  {modName}: {sign}{val}{source}");
                        }
                    }
                }
            }

            // Component details
            if (evt.metadata.ContainsKey("componentCount"))
            {
                int compCount = System.Convert.ToInt32(evt.metadata["componentCount"]);
                if (compCount > 0)
                {
                    sb.AppendLine("\n<b>Damage Components:</b>");
                    for (int i = 0; i < compCount; i++)
                    {
                        string nameKey = $"component_{i}_name";
                        string diceKey = $"component_{i}_dice";
                        string totalKey = $"component_{i}_total";
                        string sourceKey = $"component_{i}_source";
                        
                        if (evt.metadata.ContainsKey(nameKey) && evt.metadata.ContainsKey(diceKey) && evt.metadata.ContainsKey(totalKey))
                        {
                            object compName = evt.metadata[nameKey];
                            object compDice = evt.metadata[diceKey];
                            object compTotal = evt.metadata[totalKey];
                            string source = evt.metadata.ContainsKey(sourceKey) ? $" ({evt.metadata[sourceKey]})" : "";
                            sb.AppendLine($"  {compName}: {compDice} = {compTotal}{source}");
                        }
                    }
                }
            }
        }

        string content = sb.ToString().Trim();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    void OnDisable()
    {
        // Hide tooltip when this object is disabled/destroyed
        RollTooltip.Hide();
    }
}
