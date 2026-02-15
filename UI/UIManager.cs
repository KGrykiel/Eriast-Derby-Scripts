using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Canvases")]
    [Tooltip("The main DM interface canvas (tabs, leaderboards, events)")]
    public Canvas dmInterfaceCanvas;

    [Tooltip("The game view canvas (player actions, next turn button)")]
    public Canvas gameViewCanvas;

    [Header("Toggle Buttons")]
    [Tooltip("Button to show DM Interface (on Game View)")]
    public Button showDMInterfaceButton;

    [Tooltip("Button to return to Game View (on DM Interface)")]
    public Button returnToGameButton;

    [Header("Settings")]
    public Key toggleHotkey = Key.Tab;

    private bool isDMInterfaceActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (showDMInterfaceButton != null)
            showDMInterfaceButton.onClick.AddListener(ShowDMInterface);

        if (returnToGameButton != null)
            returnToGameButton.onClick.AddListener(ShowGameView);

        ShowGameView();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleHotkey].wasPressedThisFrame)
            ToggleView();
    }

    public void ShowDMInterface()
    {
        if (dmInterfaceCanvas != null)
            dmInterfaceCanvas.gameObject.SetActive(true);

        isDMInterfaceActive = true;

        Debug.Log("[UIManager] Switched to DM Interface");
    }

    public void ShowGameView()
    {
        if (dmInterfaceCanvas != null)
            dmInterfaceCanvas.gameObject.SetActive(false);

        if (gameViewCanvas != null)
            gameViewCanvas.gameObject.SetActive(true);

        isDMInterfaceActive = false;

        Debug.Log("[UIManager] Switched to Game View");
    }

    public void ToggleView()
    {
        if (isDMInterfaceActive)
            ShowGameView();
        else
            ShowDMInterface();
    }
}