using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Events;
using Assets.Scripts.VehicleComponents;
using EventType = RacingGame.Events.EventType;

/// <summary>
/// Manages player input, UI interactions, and immediate action resolution.
/// Uses component-based role system with tabbed interface.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("UI References")]
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

    // Role-based state
    private List<VehicleRole> availableRoles = new List<VehicleRole>();
    private int selectedRoleIndex = -1;
    private VehicleRole? currentRole = null;

    // Player selection state
    private Vehicle selectedTarget = null;
    private Skill selectedSkill = null;
    private VehicleComponent selectedSkillSourceComponent = null;
    private bool isSelectingStage = false;
    private bool isPlayerTurnActive = false;

    // UI button caches
    private List<Button> roleTabButtons = new List<Button>();
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
        
        // Reset all components for new turn
        playerVehicle.ResetComponentsForNewTurn();
        
        // Discover available roles
        availableRoles = playerVehicle.GetAvailableRoles();
        
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
    /// Shows the player turn UI panel, role tabs, and skill selection.
    /// </summary>
    private void ShowPlayerUI()
    {
        if (playerTurnPanel != null)
            playerTurnPanel.SetActive(true);

        if (endTurnButton != null)
            endTurnButton.interactable = true;

        ShowRoleTabs();
        
        // Select first role by default
        if (availableRoles.Count > 0)
        {
            SelectRole(0);
        }
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
    /// Always available - no requirement for all components to act.
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
            $"{playerVehicle.vehicleName} ended turn",
            playerVehicle.currentStage,
            playerVehicle
        );

        onPlayerTurnComplete?.Invoke();
    }

    #endregion

    #region Role Tab UI

    /// <summary>
    /// Displays role tabs for all available roles.
    /// Shows visual feedback for which roles have acted.
    /// </summary>
    private void ShowRoleTabs()
    {
        if (roleTabContainer == null || roleTabPrefab == null) return;

        // Ensure we have enough tab buttons
        while (roleTabButtons.Count < availableRoles.Count)
        {
            Button btn = Instantiate(roleTabPrefab, roleTabContainer);
            roleTabButtons.Add(btn);
        }

        // Update tab buttons
        for (int i = 0; i < roleTabButtons.Count; i++)
        {
            if (i < availableRoles.Count)
            {
                VehicleRole role = availableRoles[i];
                roleTabButtons[i].gameObject.SetActive(true);
                
                // Build tab text: "? Driver (Alice)" or "? Gunner 1 (Bob)"
                string statusIcon = role.sourceComponent.hasActedThisTurn ? "?" : "?";
                string characterName = role.assignedCharacter?.characterName ?? "Unassigned";
                string tabText = $"{statusIcon} {role.roleName} ({characterName})";
                
                roleTabButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = tabText;
                
                // Greyed out if already acted
                roleTabButtons[i].interactable = !role.sourceComponent.hasActedThisTurn;
                
                int roleIndex = i;
                roleTabButtons[i].onClick.RemoveAllListeners();
                roleTabButtons[i].onClick.AddListener(() => SelectRole(roleIndex));
            }
            else
            {
                roleTabButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Selects a role and displays its available skills.
    /// </summary>
    private void SelectRole(int roleIndex)
    {
        if (roleIndex < 0 || roleIndex >= availableRoles.Count) return;

        selectedRoleIndex = roleIndex;
        currentRole = availableRoles[roleIndex];

        // Update current role display
        if (currentRoleText != null)
        {
            string characterName = currentRole.Value.assignedCharacter?.characterName ?? "Unassigned";
            string status = currentRole.Value.sourceComponent.hasActedThisTurn ? "- ACTED" : "- Ready";
            currentRoleText.text = $"<b>{currentRole.Value.roleName}</b> ({characterName}) {status}";
        }

        // Show skills for this role
        ShowSkillSelection();
    }

    #endregion

    #region Skill Selection and Execution

    /// <summary>
    /// Displays skill selection UI for the currently selected role.
    /// Shows all skills from the role's component and assigned character.
    /// </summary>
    private void ShowSkillSelection()
    {
        if (skillButtonContainer == null || skillButtonPrefab == null || !currentRole.HasValue) return;

        List<Skill> availableSkills = currentRole.Value.availableSkills;
        bool roleHasActed = currentRole.Value.sourceComponent.hasActedThisTurn;

        // Ensure we have enough skill buttons
        while (skillButtons.Count < availableSkills.Count)
        {
            Button btn = Instantiate(skillButtonPrefab, skillButtonContainer);
            skillButtons.Add(btn);
        }

        // Update skill buttons
        for (int i = 0; i < skillButtons.Count; i++)
        {
            if (i < availableSkills.Count)
            {
                Skill skill = availableSkills[i];
                skillButtons[i].gameObject.SetActive(true);
                
                // Show skill name + energy cost
                string skillText = $"{skill.name} ({skill.energyCost} EN)";
                skillButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = skillText;
                
                // Disable if not enough energy OR role has already acted
                bool canAfford = playerVehicle.energy >= skill.energyCost;
                bool canUse = canAfford && !roleHasActed;
                skillButtons[i].interactable = canUse;
                
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
        if (!currentRole.HasValue) return;

        List<Skill> availableSkills = currentRole.Value.availableSkills;
        if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

        selectedSkill = availableSkills[skillIndex];
        selectedSkillSourceComponent = currentRole.Value.sourceComponent;

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
    /// Executes the selected skill immediately and marks the component as acted.
    /// </summary>
    private void ExecuteSkillImmediately()
    {
        if (selectedSkill == null || selectedSkillSourceComponent == null) return;

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
            
            // Mark component as acted
            selectedSkillSourceComponent.hasActedThisTurn = true;
            
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{currentRole.Value.roleName} ({currentRole.Value.assignedCharacter?.characterName ?? "Unassigned"}) used {selectedSkill.name}",
                playerVehicle.currentStage,
                playerVehicle
            ).WithMetadata("roleName", currentRole.Value.roleName)
             .WithMetadata("skillName", selectedSkill.name)
             .WithMetadata("componentName", selectedSkillSourceComponent.componentName);
        }

        // Refresh UI immediately
        UpdateTurnStatusDisplay();
        ShowRoleTabs(); // Update tab icons (? for acted roles)
        ShowSkillSelection(); // Disable skill buttons for acted role
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
        selectedSkillSourceComponent = null;
        
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
    /// Clears all player selections (role, skill, target).
    /// </summary>
    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillSourceComponent = null;
        selectedTarget = null;
    }

    #endregion
}