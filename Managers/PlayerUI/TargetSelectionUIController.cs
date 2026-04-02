using System;
using System.Collections.Generic;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using Assets.Scripts.Stages.Lanes;
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
                int hp = v.Chassis != null ? v.Chassis.GetCurrentHealth() : 0;
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
            int hp = targetVehicle.Chassis != null ? targetVehicle.Chassis.GetCurrentHealth() : 0;
            int maxHp = targetVehicle.Chassis != null ? targetVehicle.Chassis.GetMaxHealth() : 0;
            int ac = targetVehicle.Chassis != null ? targetVehicle.Chassis.GetArmorClass() : 10;
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
                btn.interactable = isAccessible && !component.IsDestroyed();

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
            int hp = playerVehicle.Chassis != null ? playerVehicle.Chassis.GetCurrentHealth() : 0;
            int maxHp = playerVehicle.Chassis != null ? playerVehicle.Chassis.GetMaxHealth() : 0;
            int ac = playerVehicle.Chassis != null ? playerVehicle.Chassis.GetArmorClass() : 10;
            chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
                $"[#] Chassis (HP: {hp}/{maxHp}, AC: {ac})";
            chassisBtn.onClick.AddListener(() => onComponentSelected?.Invoke(null));

            foreach (var component in playerVehicle.AllComponents)
            {
                if (component == null) continue;
                if (component is ChassisComponent) continue;

                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = BuildComponentButtonText(playerVehicle, component);
                btn.interactable = !component.IsDestroyed();

                VehicleComponent comp = component;
                btn.onClick.AddListener(() => onComponentSelected?.Invoke(comp));
            }
        }
        
        public void ShowLaneSelection(List<StageLane> lanes, Action<StageLane> onLaneSelected, Action onCancelled)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            ui.targetSelectionPanel.SetActive(true);

            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            if (lanes.Count == 0)
            {
                Hide();
                onCancelled?.Invoke();
                return;
            }

            foreach (var lane in lanes)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                int count = lane.vehiclesInLane.Count;
                btn.GetComponentInChildren<TextMeshProUGUI>().text =
                    $"{lane.laneName} ({count} vehicle{(count == 1 ? "" : "s")})";
                StageLane captured = lane;
                btn.onClick.AddListener(() => onLaneSelected?.Invoke(captured));
            }

            if (ui.targetCancelButton != null)
            {
                ui.targetCancelButton.onClick.RemoveAllListeners();
                ui.targetCancelButton.onClick.AddListener(() => onCancelled?.Invoke());
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

            if (component.IsDestroyed())
                text = $"[X] {text} - DESTROYED";
            else if (!targetVehicle.IsComponentAccessible(component))
                text = $"[?] {text} - {targetVehicle.GetInaccessibilityReason(component)}";
            else
                text = $"[>] {text}";

            return text;
        }
    }
}
