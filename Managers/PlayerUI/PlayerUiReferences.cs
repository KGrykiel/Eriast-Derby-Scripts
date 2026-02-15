using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    [System.Serializable]
    public class PlayerUIReferences
    {
        [Header("Seat & Skill Selection")]
        public Transform roleTabContainer;
        public Button roleTabPrefab;
        public Transform skillButtonContainer;
        public Button skillButtonPrefab;
        public TextMeshProUGUI currentRoleText;

        [Header("Target Selection")]
        public GameObject targetSelectionPanel;
        public Transform targetButtonContainer;
        public Button targetButtonPrefab;
        public Button targetCancelButton;

        [Header("Turn State UI")]
        public GameObject playerTurnPanel;
        public TextMeshProUGUI turnStatusText;
        public TextMeshProUGUI actionsRemainingText;
        public Button endTurnButton;
        public Button moveForwardButton;
    }
}
