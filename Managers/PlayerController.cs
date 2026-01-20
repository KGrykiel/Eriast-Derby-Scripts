using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;
using Assets.Scripts.Core;

/// <summary>
/// Manages player input, UI interactions, and immediate action resolution.
/// Uses component-based role system with tabbed interface.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Fields & Configuration
    
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
    private VehicleComponent selectedTargetComponent = null;
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
    
    #endregion

    #region Turn Lifecycle

    /// <summary>
    /// Processes player movement through stages.
    /// Automatically moves through linear paths, pauses at crossroads for player choice.
    /// Then shows action UI for player to take actions.
    /// </summary>
    public void ProcessPlayerMovement()
    {
        if (isSelectingStage) return;
        
        // Check if vehicle can operate
        if (!playerVehicle.IsOperational())
        {
            string reason = playerVehicle.GetNonOperationalReason();
            RaceHistory.Log(
                EventType.System,
                EventImportance.High,
                $"{playerVehicle.vehicleName} cannot act: {reason}",
                playerVehicle.currentStage,
                playerVehicle
            ).WithMetadata("nonOperational", true)
             .WithMetadata("reason", reason);
            
            // Auto-end turn if vehicle is non-operational
            onPlayerTurnComplete?.Invoke();
            return;
        }

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

    #region Role & Skill Selection UI

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
                
                // Check if component can act (not destroyed, not disabled, no stun effects)
                bool canAct = role.sourceComponent.CanAct();
                bool hasActed = role.sourceComponent.hasActedThisTurn;
                
                // Build tab text with status indicators
                string statusIcon;
                if (!canAct)
                    statusIcon = "[X]";  // Stunned/disabled
                else if (hasActed)
                    statusIcon = "[v]";  // Already acted
                else
                    statusIcon = "[ ]";  // Ready to act
                
                string characterName = role.assignedCharacter?.characterName ?? "Unassigned";
                string tabText = $"{statusIcon} {role.roleType} ({characterName})";
                
                roleTabButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = tabText;
                
                // Greyed out if already acted OR cannot act (stunned/disabled)
                roleTabButtons[i].interactable = canAct && !hasActed;
                
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
            currentRoleText.text = $"<b>{currentRole.Value.roleType}</b> ({characterName}) {status}";
        }

        // Show skills for this role
        ShowSkillSelection();
    }

    /// <summary>
    /// Displays skill selection UI for the currently selected role.
    /// Shows all skills from the role's component and assigned character.
    /// </summary>
    private void ShowSkillSelection()
    {
        if (skillButtonContainer == null || skillButtonPrefab == null || !currentRole.HasValue) return;

        List<Skill> availableSkills = currentRole.Value.availableSkills;
        if (availableSkills == null)
        {
            Debug.LogWarning($"[PlayerController] No available skills for role {currentRole.Value.roleType}");
            return;
        }

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
                if (skill == null)
                {
                    Debug.LogWarning($"[PlayerController] Null skill at index {i} for role {currentRole.Value.roleType}");
                    skillButtons[i].gameObject.SetActive(false);
                    continue;
                }

                skillButtons[i].gameObject.SetActive(true);
                
                // Show skill name + energy cost
                string skillText = $"{skill.name} ({skill.energyCost} EN)";
                var textComponent = skillButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = skillText;
                }
                
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

        // Check if skill needs source component selection first
        if (SkillNeedsSourceComponentSelection(selectedSkill))
        {
            ShowSourceComponentSelection();
            return;
        }

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

    #endregion

    #region Skill Execution

    /// <summary>
    /// Executes the selected skill immediately and marks the component as acted.
    /// Orchestrates: validation ? execution ? resource consumption ? logging ? UI refresh.
    /// </summary>
    private void ExecuteSkillImmediately()
    {
        if (selectedSkill == null || selectedSkillSourceComponent == null) return;

        Vehicle target = selectedTarget ?? playerVehicle;

        // Validate energy cost
        if (!ValidateSkillEnergyCost())
            return;

        // Execute skill with appropriate targeting
        bool skillSucceeded = ExecuteSkillWithTargeting(target);

        // Consume resources (always happens, even on miss!)
        ConsumeSkillResources();
        
        // Log skill usage result
        LogSkillUsageResult(skillSucceeded);

        // Refresh UI
        RefreshPlayerUIAfterSkill();
        
        ClearPlayerSelections();
    }
    
    /// <summary>
    /// Validates that the player has enough energy to use the selected skill.
    /// Logs failure if validation fails.
    /// </summary>
    private bool ValidateSkillEnergyCost()
    {
        if (playerVehicle.energy >= selectedSkill.energyCost)
            return true;
        
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
        return false;
    }
    
    /// <summary>
    /// Executes the skill with appropriate targeting (component or standard).
    /// Returns true if skill succeeded (hit/applied), false if missed.
    /// </summary>
    private bool ExecuteSkillWithTargeting(Vehicle target)
    {
        if (selectedTargetComponent != null)
        {
            // Component-targeted skill - use 4-parameter overload
            return selectedSkill.Use(playerVehicle, target, selectedSkillSourceComponent, selectedTargetComponent);
        }
        else
        {
            // Standard skill - use 3-parameter overload
            return selectedSkill.Use(playerVehicle, target, selectedSkillSourceComponent);
        }
    }
    
    /// <summary>
    /// Consumes energy and marks the component as acted.
    /// Called after skill execution (even on miss - intended design).
    /// </summary>
    private void ConsumeSkillResources()
    {
        playerVehicle.energy -= selectedSkill.energyCost;
        selectedSkillSourceComponent.hasActedThisTurn = true;
    }
    
    /// <summary>
    /// Logs skill usage result to race history with role and component context.
    /// </summary>
    private void LogSkillUsageResult(bool skillSucceeded)
    {
        if (!currentRole.HasValue) return;
        
        string roleTypeName = currentRole.Value.roleType.ToString();
        string characterName = currentRole.Value.assignedCharacter?.characterName ?? "Unassigned";
        string fullRoleName = $"{roleTypeName} ({characterName})";
        
        if (skillSucceeded)
        {
            // Skill succeeded (hit or applied effect)
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{fullRoleName} used {selectedSkill.name}",
                playerVehicle.currentStage,
                playerVehicle
            ).WithMetadata("roleType", roleTypeName)
             .WithMetadata("skillName", selectedSkill.name)
             .WithMetadata("componentName", selectedSkillSourceComponent.name);
        }
        else
        {
            // Skill failed (missed), but still consumed energy
            RaceHistory.Log(
                EventType.SkillUse,
                EventImportance.Medium,
                $"{fullRoleName} used {selectedSkill.name} but missed!",
                playerVehicle.currentStage,
                playerVehicle
            ).WithMetadata("roleType", roleTypeName)
             .WithMetadata("skillName", selectedSkill.name)
             .WithMetadata("componentName", selectedSkillSourceComponent.name)
             .WithMetadata("missed", true);
        }
    }
    
    /// <summary>
    /// Refreshes all player UI elements after skill execution.
    /// Updates turn status, role tabs, skill buttons, and DM panels.
    /// </summary>
    private void RefreshPlayerUIAfterSkill()
    {
        UpdateTurnStatusDisplay();
        ShowRoleTabs();
        ShowSkillSelection();
        gameManager.RefreshAllPanels();
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
    /// Handles target button click. 
    /// If skill has Precise targeting, shows component selection.
    /// Otherwise executes skill immediately.
    /// </summary>
    private void OnTargetButtonClicked(Vehicle v)
    {
        selectedTarget = v;
        
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        // Check if selected skill requires precise component targeting
        if (selectedSkill != null && selectedSkill.targetPrecision == TargetPrecision.Precise)
        {
            // Show component selection UI for precise targeting
            ShowComponentSelection(v);
        }
        else
        {
            // VehicleOnly or Auto targeting - execute immediately
            ExecuteSkillImmediately();
        }
    }

    /// <summary>
    /// Handles target cancel button. Clears selections and closes panel.
    /// </summary>
    private void OnTargetCancelClicked()
    {
        selectedTarget = null;
        selectedSkill = null;
        selectedSkillSourceComponent = null;
        selectedTargetComponent = null;
        
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
    }

    #endregion

    #region Component Selection UI

    /// <summary>
    /// Displays component selection UI for the selected target vehicle.
    /// Shows chassis option and all components with HP, AC, and accessibility status.
    /// </summary>
    private void ShowComponentSelection(Vehicle targetVehicle)
    {
        if (targetSelectionPanel == null || targetButtonContainer == null || targetButtonPrefab == null)
            return;

        // Reuse target selection panel for component selection
        targetSelectionPanel.SetActive(true);

        // Clear existing buttons
        foreach (Transform child in targetButtonContainer)
            Destroy(child.gameObject);

        // Option 1: Target Chassis (vehicle HP)
        Button chassisBtn = Instantiate(targetButtonPrefab, targetButtonContainer);
        chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
            $"[#] Chassis (HP: {targetVehicle.health}/{targetVehicle.maxHealth}, AC: {targetVehicle.armorClass})";
        chassisBtn.onClick.AddListener(() => OnComponentButtonClicked(null)); // null = chassis

        // Option 2: All Components (EXCEPT chassis - it's already shown above)
        foreach (var component in targetVehicle.AllComponents)
        {
            if (component == null) continue;
            
            // Skip chassis - it's already shown as the first option
            if (component is ChassisComponent) continue;

            Button btn = Instantiate(targetButtonPrefab, targetButtonContainer);
            
            // Build component button text
            string componentText = BuildComponentButtonText(targetVehicle, component);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = componentText;
            
            // Check accessibility
            bool isAccessible = targetVehicle.IsComponentAccessible(component);
            btn.interactable = isAccessible && !component.isDestroyed;
            
            // Add click handler
            VehicleComponent comp = component; // Capture for lambda
            btn.onClick.AddListener(() => OnComponentButtonClicked(comp));
        }
    }

    /// <summary>
    /// Builds display text for a component button showing HP, AC, and status.
    /// Uses modified values from StatCalculator for accurate display.
    /// </summary>
    private string BuildComponentButtonText(Vehicle targetVehicle, VehicleComponent component)
    {
        // Get modified AC from StatCalculator
        var (modifiedAC, _, _) = StatCalculator.GatherDefenseValueWithBreakdown(component);
        
        // HP info using Entity fields (current health) and modified max HP
        int modifiedMaxHP = Mathf.RoundToInt(StatCalculator.GatherAttributeValue(
            component, Attribute.MaxHealth, component.maxHealth));
        
        string text = $"{component.name} (HP: {component.health}/{modifiedMaxHP}, AC: {modifiedAC})";
        
        // Status icons/text
        if (component.isDestroyed)
        {
            text = $"[X] {text} - DESTROYED";
        }
        else if (!targetVehicle.IsComponentAccessible(component))
        {
            string reason = targetVehicle.GetInaccessibilityReason(component);
            text = $"[?] {text} - {reason}";
        }
        else
        {
            text = $"[>] {text}";
        }
        
        return text;
    }

    /// <summary>
    /// Handles component button click.
    /// If component is null, targets chassis. Otherwise targets specific component.
    /// </summary>
    private void OnComponentButtonClicked(VehicleComponent component)
    {
        selectedTargetComponent = component;
        
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        // Execute skill with explicit component targeting
        ExecuteSkillImmediately();
    }

    #endregion

    #region Source Component Selection UI

    /// <summary>
    /// Displays source component selection UI for the player's own vehicle.
    /// Allows player to pick which component on their vehicle to target with a self-targeting skill.
    /// </summary>
    private void ShowSourceComponentSelection()
    {
        if (targetSelectionPanel == null || targetButtonContainer == null || targetButtonPrefab == null)
            return;

        // Reuse target selection panel for source component selection
        targetSelectionPanel.SetActive(true);

        // Clear existing buttons
        foreach (Transform child in targetButtonContainer)
            Destroy(child.gameObject);

        // Option 1: Target Chassis (vehicle HP)
        Button chassisBtn = Instantiate(targetButtonPrefab, targetButtonContainer);
        chassisBtn.GetComponentInChildren<TextMeshProUGUI>().text = 
            $"[#] Chassis (HP: {playerVehicle.health}/{playerVehicle.maxHealth}, AC: {playerVehicle.armorClass})";
        chassisBtn.onClick.AddListener(() => OnSourceComponentButtonClicked(null)); // null = chassis

        // Option 2: All Components (EXCEPT chassis - it's already shown above)
        foreach (var component in playerVehicle.AllComponents)
        {
            if (component == null) continue;
            
            // Skip chassis - it's already shown as the first option
            if (component is ChassisComponent) continue;

            Button btn = Instantiate(targetButtonPrefab, targetButtonContainer);
            
            // Build component button text
            string componentText = BuildComponentButtonText(playerVehicle, component);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = componentText;
            
            // All components are accessible on own vehicle
            btn.interactable = !component.isDestroyed;
            
            // Add click handler
            VehicleComponent comp = component; // Capture for lambda
            btn.onClick.AddListener(() => OnSourceComponentButtonClicked(comp));
        }
    }

    /// <summary>
    /// Handles source component button click.
    /// Sets target to self (playerVehicle) and stores selected component in targetComponent.
    /// After selection, proceeds to target selection if needed, or executes immediately.
    /// </summary>
    private void OnSourceComponentButtonClicked(VehicleComponent component)
    {
        // For source component selection, we're self-targeting
        selectedTarget = playerVehicle;
        selectedTargetComponent = component;
        
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);

        // After selecting source component, check if skill also needs enemy target selection
        bool needsEnemyTarget = SkillNeedsTarget(selectedSkill);
        
        if (needsEnemyTarget)
        {
            // Skill needs to target an enemy vehicle - show normal target selection
            ShowTargetSelection();
        }
        else
        {
            // Pure self-targeted skill - execute immediately
            ExecuteSkillImmediately();
        }
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

    #region Helpers & Utilities

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
            actionsRemainingText.text = $"HP: {playerVehicle.health}/{playerVehicle.maxHealth}  " +
                                        $"Energy: {playerVehicle.energy}/{playerVehicle.maxEnergy}";
        }
    }

    /// <summary>
    /// Checks if a skill requires target selection based on its effect invocations.
    /// </summary>
    private bool SkillNeedsTarget(Skill skill)
    {
        if (skill.effectInvocations == null) return false;

        foreach (var invocation in skill.effectInvocations)
        {
            // Check if any effect targets something other than the user
            if (invocation.target == EffectTarget.SelectedTarget ||
                invocation.target == EffectTarget.TargetVehicle ||
                invocation.target == EffectTarget.Both ||
                invocation.target == EffectTarget.AllEnemiesInStage ||
                invocation.target == EffectTarget.AllAlliesInStage)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a skill requires source component selection (player picks which component to affect on their own vehicle).
    /// </summary>
    private bool SkillNeedsSourceComponentSelection(Skill skill)
    {
        if (skill.effectInvocations == null) return false;

        foreach (var invocation in skill.effectInvocations)
        {
            if (invocation.target == EffectTarget.SourceComponentSelection)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all player selections (role, skill, target, component).
    /// </summary>
    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillSourceComponent = null;
        selectedTarget = null;
        selectedTargetComponent = null;
    }

    #endregion
}