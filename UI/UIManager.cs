using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Assets.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Canvases")]
        [Tooltip("The DM interface canvas (tabs, inspector, event feed). Toggled by the hotkey and the show/hide buttons.")]
        public Canvas dmInterfaceCanvas;

        [Tooltip("The player action canvas (action buttons, next turn). Always active.")]
        public Canvas gameViewCanvas;

        [Header("Toggle Buttons")]
        [Tooltip("Button that shows the DM interface.")]
        public Button showDMInterfaceButton;

        [Tooltip("Button that hides the DM interface.")]
        public Button returnToGameButton;

        [Header("Settings")]
        [Tooltip("Key that toggles the DM interface panel.")]
        public Key toggleHotkey = Key.Tab;

        private InputAction _toggleAction;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            string keyName = toggleHotkey != Key.None ? toggleHotkey.ToString().ToLower() : "tab";

            _toggleAction = new InputAction("ToggleDMInterface", InputActionType.Button);
            _toggleAction.AddBinding($"<Keyboard>/{keyName}");
            _toggleAction.performed += OnTogglePerformed;
            _toggleAction.Enable();
        }

        void OnDestroy()
        {
            if (_toggleAction != null)
            {
                _toggleAction.performed -= OnTogglePerformed;
                _toggleAction.Dispose();
                _toggleAction = null;
            }
        }

        void Start()
        {
            if (showDMInterfaceButton != null)
                showDMInterfaceButton.onClick.AddListener(ShowDMInterface);

            if (returnToGameButton != null)
                returnToGameButton.onClick.AddListener(HideDMInterface);

            // Player action canvas is always visible.
            if (gameViewCanvas != null)
                gameViewCanvas.gameObject.SetActive(true);

            HideDMInterface();
        }

        private void OnTogglePerformed(InputAction.CallbackContext ctx) => ToggleView();

        public void ShowDMInterface()
        {
            if (dmInterfaceCanvas != null)
                dmInterfaceCanvas.gameObject.SetActive(true);
        }

        // Kept for backward compatibility with any inspector button bindings.
        public void ShowGameView() => HideDMInterface();

        public void HideDMInterface()
        {
            if (dmInterfaceCanvas != null)
                dmInterfaceCanvas.gameObject.SetActive(false);
        }

        public void ToggleView()
        {
            if (dmInterfaceCanvas == null)
                return;

            bool isVisible = dmInterfaceCanvas.gameObject.activeSelf;
            dmInterfaceCanvas.gameObject.SetActive(!isVisible);
        }
    }
}