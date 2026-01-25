using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Handles stage selection UI when player reaches a crossroads.
    /// </summary>
    public class StageSelectionUIController
    {
        private readonly PlayerUIReferences ui;
        private readonly List<Button> stageButtons = new List<Button>();
        
        public StageSelectionUIController(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
        }
        
        /// <summary>
        /// Shows stage selection UI with buttons for each available path.
        /// </summary>
        public void ShowStageSelection(List<Stage> options, Action<Stage> onStageSelected)
        {
            if (ui.stageSelectionPanel == null || ui.stageButtonContainer == null || ui.stageButtonPrefab == null)
                return;

            ui.stageSelectionPanel.SetActive(true);
            
            if (ui.endTurnButton != null)
                ui.endTurnButton.interactable = false;

            // Ensure we have enough buttons
            while (stageButtons.Count < options.Count)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.stageButtonPrefab, ui.stageButtonContainer);
                stageButtons.Add(btn);
            }

            // Update buttons
            for (int i = 0; i < stageButtons.Count; i++)
            {
                if (i < options.Count)
                {
                    stageButtons[i].gameObject.SetActive(true);
                    stageButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i].stageName;
                    Stage stage = options[i];
                    stageButtons[i].onClick.RemoveAllListeners();
                    stageButtons[i].onClick.AddListener(() => onStageSelected?.Invoke(stage));
                }
                else
                {
                    stageButtons[i].gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Hides the stage selection panel.
        /// </summary>
        public void Hide()
        {
            if (ui.stageSelectionPanel != null)
                ui.stageSelectionPanel.SetActive(false);
            
            if (ui.endTurnButton != null)
                ui.endTurnButton.interactable = true;
        }
    }
}
