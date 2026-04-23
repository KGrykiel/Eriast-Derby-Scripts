using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Logging;

namespace Assets.Scripts.UI.Tabs.EventFeed
{
    public class EventEntryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string cachedTooltipContent;
        private RectTransform rectTransform;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetEvent(RaceEvent evt)
        {
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

            if (evt.metadata.ContainsKey("restorationBreakdown") && evt.metadata["restorationBreakdown"] is string restorationBreakdown)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(restorationBreakdown);
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

            if (evt.metadata.ContainsKey("aiDecision") && evt.metadata["aiDecision"] is string aiDecision)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(aiDecision);
            }

            string content = sb.ToString().Trim();
            return string.IsNullOrEmpty(content) ? null : content;
        }

        void OnDisable()
        {
            RollTooltip.Hide();
        }
    }
}