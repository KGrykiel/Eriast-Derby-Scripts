using System;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Selection;
using Assets.Scripts.Skills;
using TMPro;
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

        public void ShowSeatTabs(List<SelectionOption<VehicleSeat>> options, Action<int> onSeatSelected)
        {
            if (ui.roleTabContainer == null || ui.roleTabPrefab == null) return;

            while (seatTabButtons.Count < options.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.roleTabPrefab, ui.roleTabContainer);
                seatTabButtons.Add(btn);
            }

            for (int i = 0; i < seatTabButtons.Count; i++)
            {
                if (i < options.Count)
                {
                    seatTabButtons[i].gameObject.SetActive(true);
                    seatTabButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i].Label;
                    seatTabButtons[i].interactable = options[i].Interactable;

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

            ui.currentRoleText.text = SeatOptionBuilder.SeatStatusLine(currentSeat);
        }

        public void ShowSkillSelection(List<SelectionOption<Skill>> options, Action<int> onSkillSelected)
        {
            if (ui.skillButtonContainer == null || ui.skillButtonPrefab == null) return;

            while (skillButtons.Count < options.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.skillButtonPrefab, ui.skillButtonContainer);
                skillButtons.Add(btn);
            }

            for (int i = 0; i < skillButtons.Count; i++)
            {
                if (i < options.Count)
                {
                    skillButtons[i].gameObject.SetActive(true);

                    var textComponent = skillButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                        textComponent.text = options[i].Label;

                    skillButtons[i].interactable = options[i].Interactable;

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
    }
}
