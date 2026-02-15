using System;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicle;
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
                    bool hasActed = seat.HasActedThisTurn();

                    string statusIcon;
                    if (!canAct)
                        statusIcon = "[X]";
                    else if (hasActed)
                        statusIcon = "[v]";
                    else
                        statusIcon = "[ ]";

                    string characterName = seat.assignedCharacter != null ? seat.assignedCharacter.characterName : null ?? "Unassigned";
                    string tabText = $"{statusIcon} {seat.seatName} ({characterName})";

                    seatTabButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = tabText;
                    seatTabButtons[i].interactable = canAct && !hasActed;

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
            
            string characterName = currentSeat.assignedCharacter != null ? currentSeat.assignedCharacter.characterName : null ?? "Unassigned";
            string status = currentSeat.HasActedThisTurn() ? "- ACTED" : "- Ready";
            ui.currentRoleText.text = $"<b>{currentSeat.seatName}</b> ({characterName}) {status}";
        }
        
        public void ShowSkillSelection(VehicleSeat currentSeat, Vehicle playerVehicle, Action<int> onSkillSelected)
        {
            if (ui.skillButtonContainer == null || ui.skillButtonPrefab == null || currentSeat == null) return;

            List<Skill> availableSkills = new();

            foreach (var component in currentSeat.GetOperationalComponents())
                availableSkills.AddRange(component.GetAllSkills());

            if (currentSeat.assignedCharacter != null)
            {
                var personalSkills = currentSeat.assignedCharacter.GetPersonalAbilities();
                if (personalSkills != null)
                    availableSkills.AddRange(personalSkills);
            }

            if (availableSkills.Count == 0)
            {
                Debug.LogWarning($"[SeatSkillUIController] No available skills for seat {currentSeat.seatName}");
                return;
            }

            bool seatHasActed = currentSeat.HasActedThisTurn();

            while (skillButtons.Count < availableSkills.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.skillButtonPrefab, ui.skillButtonContainer);
                skillButtons.Add(btn);
            }

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
                    
                    int currentEnergy = playerVehicle.powerCore != null ? playerVehicle.powerCore.currentEnergy : 0;
                    bool canAfford = currentEnergy >= skill.energyCost;
                    bool canUse = canAfford && !seatHasActed;
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
        }
        
        public List<Skill> GetAvailableSkills(VehicleSeat currentSeat)
        {
            List<Skill> availableSkills = new();
            
            if (currentSeat == null) return availableSkills;
            
            foreach (var component in currentSeat.GetOperationalComponents())
            {
                availableSkills.AddRange(component.GetAllSkills());
            }

            if (currentSeat.assignedCharacter != null)
            {
                var personalSkills = currentSeat.assignedCharacter.GetPersonalAbilities();
                if (personalSkills != null)
                {
                    availableSkills.AddRange(personalSkills);
                }
            }
            
            return availableSkills;
        }
    }
}
