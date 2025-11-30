using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;

/// <summary>
/// Manages player input, UI interactions, and immediate action resolution.
/// NEW: Actions execute immediately when selected, not at end of turn.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("UI References")]
    public Transform skillButtonContainer;
    public Button skillButtonPrefab;
    public GameObject targetSelectionPanel;
    public Transform targetButtonContainer;
    public Button targetButtonPrefab;
    public Button targetCancelButton;
    public GameObject stageSelectionPanel;
    public Transform stageButtonContainer;
    public Button stageButtonPrefab;
    public Button endTurnButton;
    
    [Header("Turn State UI")]
    public GameObject playerTurnPanel;
    public TextMeshProUGUI turnStatusText;
    public TextMeshProUGUI actionsRemainingText;

    private TurnController turnController;
    private GameManager gameManager;
    private Vehicle playerVehicle;
    private System.Action onPlayerTurnComplete;

    // Player selection state
    private Vehicle selectedTarget = null;
    private Skill selectedSkill = null;
    private int selectedSkillIndex = -1;
    private bool isSelectingStage = false;
    private bool isPlayerTurnActive = false;

    // UI button caches
    private List<Button> skillButtons = new List<Button>();
    private List<Button> stageButtons = new List<Button>();

    /// <summary>
    /// Initializes the player controller with required references.
    /// </summary>
    public void Initialize(Vehicle player, TurnController controller, GameManager manager, System.Action turnCompleteCallback)
    {
        playerVehicle = player;
        turnController = controller;
        gameManager = manager;
        onPlayerTurnComplete = turnCompleteCallback;

        if (targetCancelButton != null)
            targetCancelButton.onClick.AddListener(OnTargetCancelClicked);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);

        HidePlayerUI();
    }

    /// <summary>
    /// Returns true if the player is currently making a decision.
    /// </summary>
    public bool IsAwaitingInput => isSelectingStage || isPlayerTurnActive;

    #region Player Turn Management

    /// <summary>
    /// Processes player movement through stages.
    /// Automatically moves through linear paths, pauses at crossroads for player choice.
    /// Then shows action UI for player to take actions.
    /// </summary>
    public void ProcessPlayerMovement()
    {
        if (isSelectingStage) return;

        // Handle stage transitions
        while (playerVehicle.progress >= playerVehicle.currentStage.length)
        {
            if (playerVehicle.currentStage.nextStages.Count == 0)
            {
                break;
            }
            else if (playerVehicle.currentStage.nextStages.Count == 1)
            {
                turnController.MoveToStage(playerVehicle, playerVehicle.currentStage.nextStages[0]);
            }
            else
            {
                isSelectingStage = true;
                ShowStageSelection(playerVehicle.currentStage.nextStages);
                return;
            }
        }

        // Movement complete, show action UI
        StartPlayerActionPhase();
    }

    /// <summary>
    /// Starts the player's action phase. Shows UI and enables skill usage.
    /// </summary>
    private void StartPlayerActionPhase()
    {
        isPlayerTurnActive = true;
        ShowPlayerUI();
        UpdateTurnStatusDisplay();
        
        RaceHistory.Log(
            EventType.System,
            EventImportance.Medium,
            $"{playerVehicle.vehicleName} can now take actions",
            playerVehicle.currentStage,
            playerVehicle
        );
    }

    /// <summary>
    /// Shows the player turn UI panel and skill selection.
    /// </summary>
    private void ShowPlayerUI()
    {
        if (playerTurnPanel != null)
            playerTurnPanel.SetActive(true);

        if (endTurnButton != null)
            endTurnButton.interactable = true;

        ShowSkillSelection();
    }

    /// <summary>
    /// Hides the player turn UI panel.
    /// </summary>
    private void HidePlayerUI()
    {
        if (playerTurnPanel != null)
            playerTurnPanel.SetActive(false);

        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        if (stageSelectionPanel != null)
            stageSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Updates the turn status display with current vehicle state.
    /// </summary>
    private void UpdateTurnStatusDisplay()
    {
        if (turnStatusText != null)
        {
            turnStatusText.text = $"<b>{playerVehicle.vehicleName}'s Turn</b>\n" +
                                  $"Stage: {playerVehicle.currentStage?.stageName ?? "Unknown"}\n" +
                                  $"Progress: {playerVehicle.progress:F1}m";
        }

        if (actionsRemainingText != null)
        {
            float maxHealth = playerVehicle.GetAttribute(Attribute.MaxHealth);
            float maxEnergy = playerVehicle.GetAttribute(Attribute.MaxEnergy);
            
            actionsRemainingText.text = $"HP: {playerVehicle.health}/{maxHealth:F0}  " +
                                        $"Energy: {playerVehicle.energy}/{maxEnergy:F0}";
        }
    }

    /// <summary>
    /// Handles End Turn button click. Completes player's turn.
    /// </summary>
    public void OnEndTurnClicked()
    {
        if (!isPlayerTurnActive) return;

        isPlayerTurnActive = false;
        ClearPlayerSelections();
        HidePlayerUI();

        RaceHistory.Log(
            EventType.System,
            EventImportance.Low,
            $"{playerVehicle.vehicleName} manually ended turn",
            playerVehicle.currentStage,
            playerVehicle
        );

        onPlayerTurnComplete?.Invoke();
    }

    #endregion

    #region Skill Selection and Execution

    /// <summary>
    /// Displays skill selection UI with buttons for each available skill.
    /// </summary>
    private void ShowSkillSelection()
    {
        if (skillButtonContainer == null || skillButtonPrefab == null || playerVehicle == null) return;

        while (skillButtons.Count < playerVehicle.skills.Count)
        {
            Button btn = Instantiate(skillButtonPrefab, skillButtonContainer);
            skillButtons.Add(btn);
        }

        for (int i = 0; i < skillButtons.Count; i++)
        {
            if (i < playerVehicle.skills.Count)
            {
                Skill skill = playerVehicle.skills[i];
                skillButtons[i].gameObject.SetActive(true);
                
                // Show skill name + energy cost
                string skillText = $"{skill.name} ({skill.energyCost} EN)";
                skillButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = skillText;
                
                // Disable if not enough energy
                bool canAfford = playerVehicle.energy >= skill.energyCost;
                skillButtons[i].interactable = canAfford;
                
                int skillIndex = i;
                skillButtons[i].onClick.RemoveAllListeners();
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Handles skill button click. Shows target selection or executes immediately.
    /// </summary>
    private void OnSkillButtonClicked(int skillIndex)
    {
        selectedSkill = playerVehicle.skills[skillIndex];
        selectedSkillIndex = skillIndex;

        // Check if skill needs target selection
        bool needsTarget = SkillNeedsTarget(selectedSkill);
        
        if (needsTarget)
        {
            ShowTargetSelection();
        }
        else
        {
            // Self-targeted or AoE skill - execute immediately
            selectedTarget = playerVehicle;
            ExecuteSkillImmediately();
        }
    }

    /// <summary>
    /// Executes the selected skill immediately and provides visual feedback.
    /// NEW: Skills resolve instantly during the turn, not at end of turn.
    /// </summary>
    private void ExecuteSkillImmediately()
    {
        if (selectedSkill == null) return;

        Vehicle target = selectedTarget ?? playerVehicle;

        // Validate energy cost
        if (playerVehicle.energy < selectedSkill.energyCost)
        {
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{playerVehicle.vehicleName} cannot afford {selectedSkill.name}",
                playerVehicle.currentStage,
                playerVehicle
            ).WithMetadata("skillName", selectedSkill.name)
             .WithMetadata("energyCost", selectedSkill.energyCost)
             .WithMetadata("currentEnergy", playerVehicle.energy)
             .WithMetadata("failed", true);
            
            ClearPlayerSelections();
            return;
        }

        // Execute skill
        bool result = selectedSkill.Use(playerVehicle, target);

        if (result)
        {
            playerVehicle.energy -= selectedSkill.energyCost;
        }

        // Refresh UI immediately to show updated HP/Energy
        UpdateTurnStatusDisplay();
        ShowSkillSelection(); // Refresh skill buttons (disable if can't afford)
        gameManager.RefreshAllPanels(); // Update all DM panels
        
        ClearPlayerSelections();
    }

    /// <summary>
    /// Checks if a skill requires target selection based on its effect invocations.
    /// </summary>
    private bool SkillNeedsTarget(Skill skill)
    {
        if (skill.effectInvocations == null) return false;

        foreach (var invocation in skill.effectInvocations)
        {
            if (invocation.targetMode == EffectTargetMode.Target ||
                invocation.targetMode == EffectTargetMode.Both)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Generates a display name for the main target based on skill targeting mode.
    /// </summary>
    private string GetSkillMainTargetName(Skill skill, Vehicle mainTarget)
    {
        if (skill.effectInvocations != null && skill.effectInvocations.Exists(ei => ei.targetMode == EffectTargetMode.AllInStage))
            return "all vehicles in stage";
        if (skill.effectInvocations != null && skill.effectInvocations.Exists(ei => ei.targetMode == EffectTargetMode.Both))
            return $"{playerVehicle.vehicleName} and {mainTarget.vehicleName}";
        return mainTarget != null ? mainTarget.vehicleName : "self";
    }

    #endregion

    #region Target Selection UI

    /// <summary>
    /// Displays target selection UI with buttons for each valid target.
    /// Only shows targets in the same stage and currently active.
    /// </summary>
    private void ShowTargetSelection()
    {
        if (targetSelectionPanel == null || targetButtonContainer == null || targetButtonPrefab == null)
            return;

        targetSelectionPanel.SetActive(true);

        foreach (Transform child in targetButtonContainer)
            Destroy(child.gameObject);

        List<Vehicle> validTargets = turnController.GetValidTargets(playerVehicle);

        if (validTargets.Count == 0)
        {
            targetSelectionPanel.SetActive(false);
            ClearPlayerSelections();
            return;
        }

        foreach (var v in validTargets)
        {
            Button btn = Instantiate(targetButtonPrefab, targetButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = $"{v.vehicleName} (HP: {v.health})";
            btn.onClick.AddListener(() => OnTargetButtonClicked(v));
        }
    }

    /// <summary>
    /// Handles target button click. Executes skill immediately.
    /// </summary>
    private void OnTargetButtonClicked(Vehicle v)
    {
        selectedTarget = v;
        
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        // Execute skill immediately with selected target
        ExecuteSkillImmediately();
    }

    /// <summary>
    /// Handles target cancel button. Clears selections and closes panel.
    /// </summary>
    private void OnTargetCancelClicked()
    {
        selectedTarget = null;
        selectedSkill = null;
        selectedSkillIndex = -1;
        
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
    }

    #endregion

    #region Stage Selection UI

    /// <summary>
    /// Displays stage selection UI when player reaches a crossroads.
    /// </summary>
    private void ShowStageSelection(List<Stage> options)
    {
        if (stageSelectionPanel == null || stageButtonContainer == null || stageButtonPrefab == null)
            return;

        stageSelectionPanel.SetActive(true);
        
        if (endTurnButton != null)
            endTurnButton.interactable = false;

        while (stageButtons.Count < options.Count)
        {
            Button btn = Instantiate(stageButtonPrefab, stageButtonContainer);
            stageButtons.Add(btn);
        }

        for (int i = 0; i < stageButtons.Count; i++)
        {
            if (i < options.Count)
            {
                stageButtons[i].gameObject.SetActive(true);
                stageButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i].stageName;
                Stage stage = options[i];
                stageButtons[i].onClick.RemoveAllListeners();
                stageButtons[i].onClick.AddListener(() => OnStageButtonClicked(stage));
            }
            else
            {
                stageButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Handles stage button click. Moves player to selected stage and continues movement processing.
    /// </summary>
    private void OnStageButtonClicked(Stage selectedStage)
    {
        stageSelectionPanel.SetActive(false);
        isSelectingStage = false;

        if (endTurnButton != null)
            endTurnButton.interactable = true;

        turnController.MoveToStage(playerVehicle, selectedStage);
        ProcessPlayerMovement();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Clears all player selections (skill, target).
    /// </summary>
    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillIndex = -1;
        selectedTarget = null;
    }

    #endregion
}