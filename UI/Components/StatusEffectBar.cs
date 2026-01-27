using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.StatusEffects;

namespace Assets.Scripts.UI.Components
{
    /// <summary>
    /// Container for displaying multiple status effect icons in a horizontal row.
    /// Dynamically creates/destroys StatusEffectIcon instances based on active effects on an entity.
    /// 
    /// Usage: Attach to a GameObject with HorizontalLayoutGroup (recommended).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class StatusEffectBar : MonoBehaviour
    {
        [Header("UI Setup")]
        [Tooltip("Prefab for individual status effect icons (must have StatusEffectIcon component)")]
        public GameObject iconPrefab;
        
        [Header("Settings")]
        [Tooltip("Maximum number of icons to display (0 = unlimited)")]
        public int maxIcons = 10;
        
        [Tooltip("Spacing between icons")]
        public float spacing = 5f;
        
        [Tooltip("Icon size (width and height)")]
        public float iconSize = 32f;
        
        // Private state
        private Entity targetEntity;
        private List<StatusEffectIcon> activeIcons = new();
        private RectTransform rectTransform;
        private HorizontalLayoutGroup layoutGroup;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // Ensure we have a HorizontalLayoutGroup
            layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            
            // Configure layout group for proper icon arrangement
            ConfigureLayoutGroup();
        }
        
        private void ConfigureLayoutGroup()
        {
            if (layoutGroup == null) return;
            
            layoutGroup.spacing = spacing;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            
            // These must be TRUE for LayoutElement to work
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            
            // These should be FALSE so icons don't stretch
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            // Padding (optional)
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        }
        
        /// <summary>
        /// Set the entity whose status effects should be displayed.
        /// </summary>
        public void SetEntity(Entity entity)
        {
            targetEntity = entity;
        }
        
        /// <summary>
        /// Refresh the status bar (recreate all icons based on current status effects).
        /// Call this when status effects change.
        /// </summary>
        public void Refresh()
        {
            // Clear existing icons
            ClearIcons();
            
            if (targetEntity == null)
            {
                return;
            }
            
            // Get active status effects
            var statusEffects = targetEntity.GetActiveStatusEffects();
            
            if (statusEffects == null || statusEffects.Count == 0)
            {
                return;
            }
            
            // Limit number of icons if maxIcons > 0
            int iconCount = maxIcons > 0 ? Mathf.Min(statusEffects.Count, maxIcons) : statusEffects.Count;
            
            // Create icons for each status effect
            for (int i = 0; i < iconCount; i++)
            {
                var statusEffect = statusEffects[i];
                CreateIcon(statusEffect);
            }
            
            // Force layout rebuild after all icons created
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        
        /// <summary>
        /// Update existing icons (refresh duration display without recreating).
        /// More efficient than Refresh() when only durations change.
        /// </summary>
        public void UpdateDurations()
        {
            foreach (var icon in activeIcons)
            {
                if (icon != null)
                {
                    icon.UpdateDisplay();
                }
            }
        }
        
        /// <summary>
        /// Create a status effect icon.
        /// </summary>
        private void CreateIcon(AppliedStatusEffect statusEffect)
        {
            if (iconPrefab == null)
            {
                Debug.LogError("[StatusEffectBar] Icon prefab not assigned!");
                return;
            }
            
            // Instantiate icon as child of this container
            GameObject iconObj = Instantiate(iconPrefab, transform);
            iconObj.name = $"StatusIcon_{statusEffect?.template?.effectName ?? "Unknown"}";
            
            // Ensure LayoutElement exists with proper size
            var layoutElement = iconObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = iconObj.AddComponent<LayoutElement>();
            }
            layoutElement.minWidth = iconSize;
            layoutElement.minHeight = iconSize;
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            // Initialize icon component
            StatusEffectIcon icon = iconObj.GetComponent<StatusEffectIcon>();
            if (icon == null)
            {
                Debug.LogError("[StatusEffectBar] Icon prefab missing StatusEffectIcon component!");
                Destroy(iconObj);
                return;
            }
            
            icon.Initialize(statusEffect);
            activeIcons.Add(icon);
        }
        
        /// <summary>
        /// Clear all icons.
        /// </summary>
        private void ClearIcons()
        {
            foreach (var icon in activeIcons)
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            
            activeIcons.Clear();
        }
        
        /// <summary>
        /// Get the number of visible icons.
        /// </summary>
        public int GetIconCount()
        {
            return activeIcons.Count;
        }
        
        /// <summary>
        /// Check if the bar has any visible icons.
        /// </summary>
        public bool HasIcons()
        {
            return activeIcons.Count > 0 && targetEntity != null && targetEntity.GetActiveStatusEffects().Count > 0;
        }
        
        void OnDestroy()
        {
            ClearIcons();
        }
    }
}
