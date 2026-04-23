using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using Assets.Scripts.Combat.Logging;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.VehicleConditions;
using Assets.Scripts.UI.Tabs.EventFeed;

namespace Assets.Scripts.UI.Components
{
    [RequireComponent(typeof(Image))]
    internal class StatusEffectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [Tooltip("Image component for the status effect icon")]
        public Image iconImage;
        
        [Tooltip("Text component for displaying duration (turns remaining)")]
        public TextMeshProUGUI durationText;
        
        [Header("Default Icon")]
        [Tooltip("Default icon sprite if status effect has no icon")]
        public Sprite defaultIcon;
        
        [Header("Color Settings")]
        [Tooltip("If true, tint the icon based on buff/debuff. If false, keep original sprite colors.")]
        public bool tintIconByEffectType = false;
        
        [Tooltip("Background color for buff effects (only used if tintIconByEffectType is true)")]
        public Color buffColor = new(0.2f, 0.8f, 0.2f, 0.8f);
        
        [Tooltip("Background color for debuff effects (only used if tintIconByEffectType is true)")]
        public Color debuffColor = new(0.8f, 0.2f, 0.2f, 0.8f);
        
        [Tooltip("Background color for neutral effects (only used if tintIconByEffectType is true)")]
        public Color neutralColor = new(0.5f, 0.5f, 0.5f, 0.8f);
        
        private AppliedConditionBase activeCondition;
        private RectTransform rectTransform;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (iconImage == null)
                iconImage = GetComponent<Image>();

            if (durationText == null)
                durationText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        public void Initialize(AppliedConditionBase condition)
        {
            activeCondition = condition;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (activeCondition == null || activeCondition.Template == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            ConditionBase template = activeCondition.Template;

            if (iconImage != null)
            {
                Sprite spriteToUse = template.icon != null ? template.icon : defaultIcon;

                if (spriteToUse != null)
                {
                    iconImage.sprite = spriteToUse;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.enabled = true;
                }

                if (tintIconByEffectType)
                    iconImage.color = GetEffectColor();
                else
                    iconImage.color = Color.white;
            }

            if (durationText != null)
            {
                if (activeCondition.IsIndefinite)
                {
                    durationText.text = "inf";
                }
                else
                {
                    durationText.text = activeCondition.turnsRemaining.ToString();
                }
            }
        }
        
        private Color GetEffectColor()
        {
            if (activeCondition == null || activeCondition.Template == null) return neutralColor;

            List<EntityModifierData> modifiers = null;
            List<IPeriodicEffect> periodicEffects = null;

            if (activeCondition.Template is EntityCondition ec)
            {
                modifiers = ec.modifiers;
                periodicEffects = ec.periodicEffects;
            }
            else if (activeCondition.Template is VehicleCondition vc)
            {
                modifiers = vc.modifiers;
                periodicEffects = vc.periodicEffects;
            }

            if (modifiers == null) return neutralColor;

            bool isBuff = DetermineIfBuff(modifiers, periodicEffects ?? new(), activeCondition.Template.behavioralEffects);
            return isBuff ? buffColor : debuffColor;
        }

        private bool DetermineIfBuff(List<EntityModifierData> modifiers, List<IPeriodicEffect> periodicEffects, BehavioralEffectData behavioralEffects)
        {
            float totalModifierValue = 0f;
            foreach (var mod in modifiers)
                totalModifierValue += mod.value;

            bool hasPeriodicDamage = false;
            bool hasPeriodicRestoration = false;

            foreach (var periodic in periodicEffects)
            {
                switch (periodic)
                {
                    case PeriodicDamageEffect:
                        hasPeriodicDamage = true;
                        break;
                    case PeriodicRestorationEffect res:
                        if (res.formula.isDrain)
                            hasPeriodicDamage = true;
                        else
                            hasPeriodicRestoration = true;
                        break;
                }
            }

            bool hasBehavioralRestrictions = behavioralEffects != null &&
                (behavioralEffects.preventsActions ||
                 behavioralEffects.preventsMovement);

            if (hasPeriodicDamage || hasBehavioralRestrictions)
                return false;

            if (hasPeriodicRestoration || totalModifierValue > 0)
                return true;

            return totalModifierValue >= 0;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (activeCondition == null || activeCondition.Template == null) return;

            string tooltipContent = null;
            if (activeCondition is AppliedEntityCondition entity)
                tooltipContent = CombatFormatter.FormatEntityConditionTooltip(entity);
            else if (activeCondition is AppliedVehicleCondition vehicle)
                tooltipContent = CombatFormatter.FormatVehicleConditionTooltip(vehicle);

            if (tooltipContent != null)
                RollTooltip.ShowNow(tooltipContent, rectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RollTooltip.Hide();
        }

        void OnDisable()
        {
            if (RollTooltip.Instance != null)
                RollTooltip.Hide();
        }
    }
}
