using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Logging;

public class EventEntryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RaceEvent linkedEvent;
    private string cachedTooltipContent;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

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
            
            RollTooltip.ShowNow(cachedTooltipContent, rectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RollTooltip.Hide();
    }

    private string BuildTooltipContent(RaceEvent evt)
    {
        if (evt == null || evt.metadata == null || evt.metadata.Count == 0)
            return null;

        var sb = new System.Text.StringBuilder();

        if (evt.metadata.ContainsKey("rollBreakdown") && evt.metadata["rollBreakdown"] is string rollBreakdown)
        {
            sb.AppendLine("<b>Attack Roll:</b>");
            sb.AppendLine(rollBreakdown);
        }

        if (evt.metadata.ContainsKey("defenseBreakdown") && evt.metadata["defenseBreakdown"] is string defenseBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine("<b>Target Defense:</b>");
            sb.AppendLine(defenseBreakdown);
        }

        if (evt.metadata.ContainsKey("damageBreakdown") && evt.metadata["damageBreakdown"] is string damageBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(damageBreakdown);
        }

        if (evt.metadata.ContainsKey("effectBreakdown") && evt.metadata["effectBreakdown"] is string effectBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(effectBreakdown);
        }

        if (evt.metadata.ContainsKey("dcBreakdown") && evt.metadata["dcBreakdown"] is string dcBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(dcBreakdown);
        }

        if (evt.metadata.ContainsKey("saveModifiersBreakdown") && evt.metadata["saveModifiersBreakdown"] is string saveModBreakdown)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine(saveModBreakdown);
        }

        if (evt.metadata.ContainsKey("actualChange") && evt.metadata.ContainsKey("resourceType"))
        {
            if (sb.Length > 0) sb.AppendLine();
            int change = System.Convert.ToInt32(evt.metadata["actualChange"]);
            string resourceType = evt.metadata["resourceType"].ToString();
            string verb = change > 0 ? "Restored" : "Drained";
            sb.AppendLine($"<b>Resource Change:</b>");
            sb.AppendLine($"  {verb}: {System.Math.Abs(change)} {resourceType}");
        }

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

        if (sb.Length == 0)
        {
            bool hasAnyData = false;

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
        RollTooltip.Hide();
    }
}
