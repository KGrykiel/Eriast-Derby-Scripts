using Assets.Scripts.Managers.Selection;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Dumb display panel. Receives fully-formed <see cref="SelectionOption{T}"/> lists
    /// and renders buttons. No domain knowledge.
    /// </summary>
    public class TargetSelectionUIController
    {
        private readonly PlayerUIReferences ui;

        public TargetSelectionUIController(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
        }

        public void Show<T>(
            IEnumerable<SelectionOption<T>> options,
            Action<T> onSelected)
        {
            if (ui.targetSelectionPanel == null || ui.targetButtonContainer == null || ui.targetButtonPrefab == null)
                return;

            var optionList = new List<SelectionOption<T>>(options);

            if (optionList.Count == 0)
            {
                Hide();
                return;
            }

            ui.targetSelectionPanel.SetActive(true);

            foreach (Transform child in ui.targetButtonContainer)
                UnityEngine.Object.Destroy(child.gameObject);

            foreach (var option in optionList)
            {
                Button btn = UnityEngine.Object.Instantiate(ui.targetButtonPrefab, ui.targetButtonContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = option.Label;
                btn.interactable = option.Interactable;

                T value = option.Value;
                btn.onClick.AddListener(() => onSelected?.Invoke(value));
            }
        }

        public void Hide()
        {
            if (ui.targetSelectionPanel != null)
                ui.targetSelectionPanel.SetActive(false);
        }
    }
}

