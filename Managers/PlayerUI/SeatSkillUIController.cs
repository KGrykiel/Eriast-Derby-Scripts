using System;
using System.Collections.Generic;
using Assets.Scripts.Consumables;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    public class SeatSkillUIController
    {
        private readonly PlayerUIReferences ui;
        private readonly List<Button> seatTabButtons = new();
        private readonly List<Button> skillButtons = new();
        private readonly List<Button> consumableButtons = new();

        public SeatSkillUIController(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
        }

        public void ShowSeatTabs(List<VehicleSeat> availableSeats, Action<int> onSeatSelected)
        {
            if (ui.roleTabContainer == null || ui.roleTabPrefab == null) return;

            while (seatTabButtons.Count < availableSeats.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.roleTabPrefab, ui.roleTabContainer);
                seatTabButtons.Add(btn);
            }

            for (int i = 0; i < seatTabButtons.Count; i++)
            {
                if (i < availableSeats.Count)
                {
                    VehicleSeat seat = availableSeats[i];
                    seatTabButtons[i].gameObject.SetActive(true);

                    bool canAct = seat.CanAct();
                    bool actionSpent = !seat.CanSpendAction(ActionType.Action);
                    bool bonusSpent = !seat.CanSpendAction(ActionType.BonusAction);

                    string statusIcon;
                    if (!canAct)
                        statusIcon = "[X]";
                    else if (actionSpent && bonusSpent)
                        statusIcon = "[v]";
                    else if (actionSpent || bonusSpent)
                        statusIcon = "[~]";
                    else
                        statusIcon = "[ ]";

                    string characterName = seat.GetDisplayName() ?? "Unassigned";
                    string tabText = $"{statusIcon} {seat.seatName} ({characterName})";

                    seatTabButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = tabText;
                    seatTabButtons[i].interactable = canAct && seat.HasAnyActionsRemaining();

                    int seatIndex = i;
                    seatTabButtons[i].onClick.RemoveAllListeners();
                    seatTabButtons[i].onClick.AddListener(() => onSeatSelected?.Invoke(seatIndex));
                }
                else
                {
                    seatTabButtons[i].gameObject.SetActive(false);
                }
            }
        }
        
        public void UpdateCurrentSeatDisplay(VehicleSeat currentSeat)
        {
            if (ui.currentRoleText == null || currentSeat == null) return;
            
            string characterName = currentSeat.GetDisplayName() ?? "Unassigned";
            bool actionSpent = !currentSeat.CanSpendAction(ActionType.Action);
            bool bonusSpent = !currentSeat.CanSpendAction(ActionType.BonusAction);
            string status = (actionSpent && bonusSpent) ? "- Done" : (actionSpent || bonusSpent) ? "- Partial" : "- Ready";
            ui.currentRoleText.text = $"<b>{currentSeat.seatName}</b> ({characterName}) {status}";
        }
        
        public void ShowSkillSelection(VehicleSeat currentSeat, Vehicle playerVehicle, Action<int> onSkillSelected, Action<int> onConsumableSelected)
        {
            if (ui.skillButtonContainer == null || ui.skillButtonPrefab == null || currentSeat == null) return;

            List<Skill> availableSkills = new();
            foreach (var component in currentSeat.GetOperationalComponents())
                availableSkills.AddRange(component.GetAllSkills());
            availableSkills.AddRange(currentSeat.GetPersonalAbilities());

            while (skillButtons.Count < availableSkills.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.skillButtonPrefab, ui.skillButtonContainer);
                skillButtons.Add(btn);
            }

            int currentEnergy = playerVehicle.powerCore != null ? playerVehicle.powerCore.currentEnergy : 0;

            for (int i = 0; i < skillButtons.Count; i++)
            {
                if (i < availableSkills.Count)
                {
                    Skill skill = availableSkills[i];
                    if (skill == null)
                    {
                        Debug.LogWarning($"[SeatSkillUIController] Null skill at index {i} for seat {currentSeat.seatName}");
                        skillButtons[i].gameObject.SetActive(false);
                        continue;
                    }

                    skillButtons[i].gameObject.SetActive(true);

                    string skillText = $"{skill.name} ({skill.energyCost} EN)";
                    var textComponent = skillButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                        textComponent.text = skillText;

                    bool canAfford = currentEnergy >= skill.energyCost;
                    bool hasRequiredConsumable = !(skill is ConsumableGatedSkill gated) || playerVehicle.HasChargesFor(gated.requiredConsumable);
                    bool canUse = canAfford && currentSeat.CanSpendAction(skill.actionCost) && hasRequiredConsumable;
                    skillButtons[i].interactable = canUse;

                    int skillIndex = i;
                    skillButtons[i].onClick.RemoveAllListeners();
                    skillButtons[i].onClick.AddListener(() => onSkillSelected?.Invoke(skillIndex));
                }
                else
                {
                    skillButtons[i].gameObject.SetActive(false);
                }
            }

            var availableConsumables = playerVehicle.GetAvailableConsumables(currentSeat);

            while (consumableButtons.Count < availableConsumables.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.skillButtonPrefab, ui.skillButtonContainer);
                consumableButtons.Add(btn);
            }

            for (int i = 0; i < consumableButtons.Count; i++)
            {
                if (i < availableConsumables.Count)
                {
                    ConsumableStack stack = availableConsumables[i];
                    Consumable consumable = stack.template as Consumable;

                    consumableButtons[i].gameObject.SetActive(true);

                    string label = $"{stack.template.name} ({stack.charges})";
                    var textComponent = consumableButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                        textComponent.text = label;

                    bool hasCharges = stack.charges > 0;
                    bool canUse = hasCharges && (consumable == null || currentSeat.CanSpendAction(consumable.actionCost));
                    consumableButtons[i].interactable = canUse;

                    int consumableIndex = i;
                    consumableButtons[i].onClick.RemoveAllListeners();
                    consumableButtons[i].onClick.AddListener(() => onConsumableSelected?.Invoke(consumableIndex));
                }
                else
                {
                    consumableButtons[i].gameObject.SetActive(false);
                }
            }
        }
        
        public List<Skill> GetAvailableSkills(VehicleSeat currentSeat)
        {
            List<Skill> availableSkills = new();
            
            if (currentSeat == null) return availableSkills;
            
            foreach (var component in currentSeat.GetOperationalComponents())
            {
                availableSkills.AddRange(component.GetAllSkills());
            }

            availableSkills.AddRange(currentSeat.GetPersonalAbilities());

            return availableSkills;
        }
    }
}
