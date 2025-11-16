using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Add this

/// <summary>
/// Manages switching between DM Interface and Game View.
/// Allows DM to toggle between detailed analytics and actual gameplay.
/// </summary>
public class UIManager : MonoBehaviour
{
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
    public Key toggleHotkey = Key.Tab; // Changed from KeyCode to Key

    private bool isDMInterfaceActive = false;

    void Start()
    {
        // Setup button listeners
        if (showDMInterfaceButton != null)
            showDMInterfaceButton.onClick.AddListener(ShowDMInterface);

        if (returnToGameButton != null)
            returnToGameButton.onClick.AddListener(ShowGameView);

        // Start in Game View by default
        ShowGameView();
    }

    void Update()
    {
        // New Input System - check if Tab key was pressed this frame
        if (Keyboard.current != null && Keyboard.current[toggleHotkey].wasPressedThisFrame)
        {
            ToggleView();
        }
    }

    /// <summary>
    /// Shows the DM Interface (tabs, analytics, detailed view).
    /// </summary>
    public void ShowDMInterface()
    {
        if (dmInterfaceCanvas != null)
            dmInterfaceCanvas.gameObject.SetActive(true);

        //if (gameViewCanvas != null)
        //    gameViewCanvas.gameObject.SetActive(false);

        isDMInterfaceActive = true;

        Debug.Log("[UIManager] Switched to DM Interface");
    }

    /// <summary>
    /// Shows the Game View (gameplay, turn buttons, player actions).
    /// </summary>
    public void ShowGameView()
    {
        if (dmInterfaceCanvas != null)
            dmInterfaceCanvas.gameObject.SetActive(false);

        if (gameViewCanvas != null)
            gameViewCanvas.gameObject.SetActive(true);

        isDMInterfaceActive = false;

        Debug.Log("[UIManager] Switched to Game View");
    }

    /// <summary>
    /// Toggles between DM Interface and Game View.
    /// </summary>
    public void ToggleView()
    {
        if (isDMInterfaceActive)
            ShowGameView();
        else
            ShowDMInterface();
    }
}