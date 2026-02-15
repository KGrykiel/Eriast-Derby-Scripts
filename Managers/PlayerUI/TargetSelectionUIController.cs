using System;
using System.Collections.Generic;
using Assets.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    public class TargetSelectionUIController
    {
        private readonly PlayerUIReferences ui;

        public TargetSelectionUIController(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
        }

        public void ShowTargetSelection(List<Vehicle> validTargets, Action<Vehicle> onTargetSelected, Action onCancelled)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            ui.targetSelectionPanel.SetActive(true);

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
                int hp = v.chassis != null ? v.chassis.GetCurrentHealth() : 0;
                btn.GetComponentInChildren<TextMeshProUGUI>().text = $"{v.vehicleName} (HP: {hp})";
                btn.onClick.AddListener(() => onTargetSelected?.Invoke(v));
            }

            if (ui.targetCancelButton != null)
            {
                ui.targetCancelButton.onClick.RemoveAllListeners();
                ui.targetCancelButton.onClick.AddListener(() => onCancelled?.Invoke());
            }
        }
        
        public void ShowComponentSelection(Vehicle targetVehicle, Action<VehicleComponent> onComponentSelected)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            ui.targetSelectionPanel.SetActive(true);

            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            Button chassisBtn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
            int hp = targetVehicle.chassis != null ? targetVehicle.chassis.GetCurrentHealth() : 0;
            int maxHp = targetVehicle.chassis != null ? targetVehicle.chassis.GetMaxHealth() : 0;
            int ac = targetVehicle.chassis != null ? targetVehicle.chassis.GetArmorClass() : 10;
            chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"[#] Chassis (HP: {hp}/{maxHp}, AC: {ac})";
            chassisBtn.onClick.AddListener(() => onComponentSelected?.Invoke(null));

            foreach (var component in targetVehicle.AllComponents)
            {
                if (component == null) continue;
                if (component is ChassisComponent) continue;

                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = BuildComponentButtonText(targetVehicle, component);

                bool isAccessible = targetVehicle.IsComponentAccessible(component);
                btn.interactable = isAccessible && !component.isDestroyed;

                VehicleComponent comp = component;
                btn.onClick.AddListener(() => onComponentSelected?.Invoke(comp));
            }
        }
        
        public void ShowSourceComponentSelection(Vehicle playerVehicle, Action<VehicleComponent> onComponentSelected)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            ui.targetSelectionPanel.SetActive(true);

            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            Button chassisBtn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
            int hp = playerVehicle.chassis != null ? playerVehicle.chassis.GetCurrentHealth() : 0;
            int maxHp = playerVehicle.chassis != null ? playerVehicle.chassis.GetMaxHealth() : 0;
            int ac = playerVehicle.chassis != null ? playerVehicle.chassis.GetArmorClass() : 10;
            chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"[#] Chassis (HP: {hp}/{maxHp}, AC: {ac})";
            chassisBtn.onClick.AddListener(() => onComponentSelected?.Invoke(null));

            foreach (var component in playerVehicle.AllComponents)
            {
                if (component == null) continue;
                if (component is ChassisComponent) continue;

                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = BuildComponentButtonText(playerVehicle, component);
                btn.interactable = !component.isDestroyed;

                VehicleComponent comp = component;
                btn.onClick.AddListener(() => onComponentSelected?.Invoke(comp));
            }
        }
        
        public void Hide()
        {
            if (ui.targetSelectionPanel != null)
                ui.targetSelectionPanel.SetActive(false);
        }

        private string BuildComponentButtonText(Vehicle targetVehicle, VehicleComponent component)
        {
            var (modifiedAC, _, _) = StatCalculator.GatherDefenseValueWithBreakdown(component);
            int modifiedMaxHP = component.GetMaxHealth();

            string text = $"{component.name} (HP: {component.GetCurrentHealth()}/{modifiedMaxHP}, AC: {modifiedAC})";

            if (component.isDestroyed)
                text = $"[X] {text} - DESTROYED";
            else if (!targetVehicle.IsComponentAccessible(component))
                text = $"[?] {text} - {targetVehicle.GetInaccessibilityReason(component)}";
            else
                text = $"[>] {text}";

            return text;
        }
    }
}
