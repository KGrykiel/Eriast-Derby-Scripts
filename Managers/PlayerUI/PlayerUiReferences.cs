using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Holds all Unity Inspector references for Player UI.
    /// Using [Serializable] preserves all assignments when refactoring!
    /// </summary>
    [System.Serializable]
    public class PlayerUIReferences
    {
        [Header("Seat & Skill Selection")]
        [Tooltip("Container for role tab buttons")]
        public Transform roleTabContainer;
        [Tooltip("Prefab for role tab buttons")]
        public Button roleTabPrefab;
        [Tooltip("Container for skill buttons (scrollable)")]
        public Transform skillButtonContainer;
        [Tooltip("Prefab for skill buttons")]
        public Button skillButtonPrefab;
        [Tooltip("Text showing current role info")]
        public TextMeshProUGUI currentRoleText;
        
        [Header("Target Selection")]
        public GameObject targetSelectionPanel;
        public Transform targetButtonContainer;
        public Button targetButtonPrefab;
        public Button targetCancelButton;
        
        //DEPRECATED
        [Header("Stage Selection")]
        public GameObject stageSelectionPanel;
        public Transform stageButtonContainer;
        public Button stageButtonPrefab;
        
        [Header("Turn State UI")]
        public GameObject playerTurnPanel;
        public TextMeshProUGUI turnStatusText;
        public TextMeshProUGUI actionsRemainingText;
        public Button endTurnButton;
        public Button moveForwardButton;
    }
}
