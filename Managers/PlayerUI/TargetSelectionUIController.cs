using System;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Handles target selection UI (vehicles, components, and source components).
    /// </summary>
    public class TargetSelectionUIController
    {
        private readonly PlayerUIReferences ui;
        
        public TargetSelectionUIController(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
        }
        
        /// <summary>
        /// Shows vehicle target selection UI.
        /// </summary>
        public void ShowTargetSelection(List<Vehicle> validTargets, Action<Vehicle> onTargetSelected, Action onCancelled)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            ui.targetSelectionPanel.SetActive(true);

            // Clear existing buttons
            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            if (validTargets.Count == 0)
            {
                Hide();
                onCancelled?.Invoke();
                return;
            }

            foreach (var v in validTargets)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = $"{v.vehicleName} (HP: {v.health})";
                btn.onClick.AddListener(() => onTargetSelected?.Invoke(v));
            }
            
            // Setup cancel button
            if (ui.targetCancelButton != null)
            {
                ui.targetCancelButton.onClick.RemoveAllListeners();
                ui.targetCancelButton.onClick.AddListener(() => onCancelled?.Invoke());
            }
        }
        
        /// <summary>
        /// Shows component selection UI for precise targeting.
        /// </summary>
        public void ShowComponentSelection(Vehicle targetVehicle, Action<VehicleComponent> onComponentSelected)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            // Reuse target selection panel for component selection
            ui.targetSelectionPanel.SetActive(true);

            // Clear existing buttons
            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            // Option 1: Target Chassis (vehicle HP)
            Button chassisBtn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
            chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"[#] Chassis (HP: {targetVehicle.health}/{targetVehicle.maxHealth}, AC: {targetVehicle.armorClass})";
            chassisBtn.onClick.AddListener(() => onComponentSelected?.Invoke(null)); // null = chassis

            // Option 2: All Components (EXCEPT chassis - it's already shown above)
            foreach (var component in targetVehicle.AllComponents)
            {
                if (component == null) continue;
                
                // Skip chassis - it's already shown as the first option
                if (component is ChassisComponent) continue;

                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                
                // Build component button text
                string componentText = BuildComponentButtonText(targetVehicle, component);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = componentText;
                
                // Check accessibility
                bool isAccessible = targetVehicle.IsComponentAccessible(component);
                btn.interactable = isAccessible && !component.isDestroyed;
                
                // Add click handler
                VehicleComponent comp = component; // Capture for lambda
                btn.onClick.AddListener(() => onComponentSelected?.Invoke(comp));
            }
        }
        
        /// <summary>
        /// Shows source component selection UI (player's own vehicle).
        /// </summary>
        public void ShowSourceComponentSelection(Vehicle playerVehicle, Action<VehicleComponent> onComponentSelected)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            // Reuse target selection panel for source component selection
            ui.targetSelectionPanel.SetActive(true);

            // Clear existing buttons
            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            // Option 1: Target Chassis (vehicle HP)
            Button chassisBtn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
            chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"[#] Chassis (HP: {playerVehicle.health}/{playerVehicle.maxHealth}, AC: {playerVehicle.armorClass})";
            chassisBtn.onClick.AddListener(() => onComponentSelected?.Invoke(null)); // null = chassis

            // Option 2: All Components (EXCEPT chassis - it's already shown above)
            foreach (var component in playerVehicle.AllComponents)
            {
                if (component == null) continue;
                
                // Skip chassis - it's already shown as the first option
                if (component is ChassisComponent) continue;

                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                
                // Build component button text
                string componentText = BuildComponentButtonText(playerVehicle, component);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = componentText;
                
                // All components are accessible on own vehicle
                btn.interactable = !component.isDestroyed;
                
                // Add click handler
                VehicleComponent comp = component; // Capture for lambda
                btn.onClick.AddListener(() => onComponentSelected?.Invoke(comp));
            }
        }
        
        /// <summary>
        /// Hides the target selection panel.
        /// </summary>
        public void Hide()
        {
            if (ui.targetSelectionPanel != null)
                ui.targetSelectionPanel.SetActive(false);
        }
        
        /// <summary>
        /// Builds display text for a component button showing HP, AC, and status.
        /// </summary>
        private string BuildComponentButtonText(Vehicle targetVehicle, VehicleComponent component)
        {
            // Get modified AC from StatCalculator
            var (modifiedAC, _, _) = StatCalculator.GatherDefenseValueWithBreakdown(component);
            
            // HP info using Entity fields (current health) and modified max HP
            int modifiedMaxHP = Mathf.RoundToInt(StatCalculator.GatherAttributeValue(
                component, Attribute.MaxHealth, component.maxHealth));
            
            string text = $"{component.name} (HP: {component.health}/{modifiedMaxHP}, AC: {modifiedAC})";
            
            // Status icons/text
            if (component.isDestroyed)
            {
                text = $"[X] {text} - DESTROYED";
            }
            else if (!targetVehicle.IsComponentAccessible(component))
            {
                string reason = targetVehicle.GetInaccessibilityReason(component);
                text = $"[?] {text} - {reason}";
            }
            else
            {
                text = $"[>] {text}";
            }
            
            return text;
        }
    }
}
