using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages tab switching for the DM interface.
/// Controls visibility of tab panels based on button clicks.
/// </summary>
public class TabManager : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button focusTabButton;
    public Button overviewTabButton;
    public Button inspectorTabButton;
    public Button logTabButton;

    [Header("Tab Panels")]
    public GameObject focusPanel;
    public GameObject overviewPanel;
    public GameObject inspectorPanel;
    public GameObject logPanel;

    [Header("Active Tab Color")]
    public Color activeColor = new Color(0.3f, 0.5f, 0.8f);
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f);

    private Button currentActiveButton;

    void Start()
    {
        // Setup button listeners
        focusTabButton.onClick.AddListener(() => ShowTab(focusPanel, focusTabButton));
        overviewTabButton.onClick.AddListener(() => ShowTab(overviewPanel, overviewTabButton));
        inspectorTabButton.onClick.AddListener(() => ShowTab(inspectorPanel, inspectorTabButton));
        logTabButton.onClick.AddListener(() => ShowTab(logPanel, logTabButton));

        // Show focus tab by default
        ShowTab(focusPanel, focusTabButton);
    }

    private void ShowTab(GameObject panelToShow, Button buttonClicked)
    {
        // Hide all panels
        focusPanel.SetActive(false);
        overviewPanel.SetActive(false);
        inspectorPanel.SetActive(false);
        logPanel.SetActive(false);

        // Reset all button colors
        SetButtonColor(focusTabButton, inactiveColor);
        SetButtonColor(overviewTabButton, inactiveColor);
        SetButtonColor(inspectorTabButton, inactiveColor);
        SetButtonColor(logTabButton, inactiveColor);

        // Show selected panel
        panelToShow.SetActive(true);

        // Highlight selected button
        SetButtonColor(buttonClicked, activeColor);
        currentActiveButton = buttonClicked;
    }

    private void SetButtonColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        button.colors = colors;
    }
}