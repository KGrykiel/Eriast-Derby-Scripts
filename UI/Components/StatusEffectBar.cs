using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Conditions;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.UI.Components
{
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
        
        private Entity targetEntity;
        private Vehicle targetVehicle;
        private List<StatusEffectIcon> activeIcons = new();
        private RectTransform rectTransform;
        private HorizontalLayoutGroup layoutGroup;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

            ConfigureLayoutGroup();
        }
        
        private void ConfigureLayoutGroup()
        {
            if (layoutGroup == null) return;
            
            layoutGroup.spacing = spacing;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;

            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        }
        
        public void SetEntity(Entity entity)
        {
            targetEntity = entity;
            targetVehicle = null;
        }

        public void SetVehicle(Vehicle vehicle)
        {
            targetVehicle = vehicle;
            targetEntity = null;
        }

        public void Refresh()
        {
            ClearIcons();

            IReadOnlyList<AppliedConditionBase> conditions = null;
            if (targetVehicle != null)
                conditions = targetVehicle.GetActiveVehicleConditions();
            else if (targetEntity != null)
                conditions = targetEntity.GetActiveConditions();

            if (conditions == null || conditions.Count == 0)
                return;

            int iconCount = maxIcons > 0 ? Mathf.Min(conditions.Count, maxIcons) : conditions.Count;

            for (int i = 0; i < iconCount; i++)
                CreateIcon(conditions[i]);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

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
        
        private void CreateIcon(AppliedConditionBase condition)
        {
            if (iconPrefab == null)
            {
                Debug.LogError("[StatusEffectBar] Icon prefab not assigned!");
                return;
            }

            GameObject iconObj = Instantiate(iconPrefab, transform);
            var template = condition != null ? condition.Template : null;
            iconObj.name = "StatusIcon_" + (template != null ? template.effectName : "Unknown");

            var layoutElement = iconObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = iconObj.AddComponent<LayoutElement>();
            layoutElement.minWidth = iconSize;
            layoutElement.minHeight = iconSize;
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;

            StatusEffectIcon icon = iconObj.GetComponent<StatusEffectIcon>();
            if (icon == null)
            {
                Debug.LogError("[StatusEffectBar] Icon prefab missing StatusEffectIcon component!");
                Destroy(iconObj);
                return;
            }

            icon.Initialize(condition);
            activeIcons.Add(icon);
        }
        
        private void ClearIcons()
        {
            foreach (var icon in activeIcons)
            {
                if (icon != null)
                    Destroy(icon.gameObject);
            }

            activeIcons.Clear();
        }

        void OnDestroy()
        {
            ClearIcons();
        }
    }
}
