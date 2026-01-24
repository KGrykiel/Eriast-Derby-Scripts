using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventType = Assets.Scripts.Logging.EventType;
using Assets.Scripts.Logging;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Managers.PlayerUI;

/// <summary>
/// Orchestrates player input and coordinates between UI controllers and game systems.
/// UI display logic has been extracted to specialized controllers.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Fields & Configuration
    
    [Header("UI References")]
    [SerializeField]
    private PlayerUIReferences ui;

    private TurnController turnController;
    private GameManager gameManager;
    private Vehicle playerVehicle;
    private System.Action onPlayerTurnComplete;
    
    // UI Coordinator (owns all UI sub-controllers)
    private PlayerUICoordinator uiCoordinator;

    // Seat-based state
    private List<VehicleSeat> availableSeats = new List<VehicleSeat>();
    private VehicleSeat currentSeat = null;

    // Player selection state
    private Vehicle selectedTarget = null;
    private Skill selectedSkill = null;
    private VehicleComponent selectedSkillSourceComponent = null;
    private VehicleComponent selectedTargetComponent = null;
    private bool isSelectingStage = false;
    private bool isPlayerTurnActive = false;

    /// <summary>
    /// Initializes the player controller with required references.
    /// </summary>
    public void Initialize(Vehicle player, TurnController controller, GameManager manager, System.Action turnCompleteCallback)
    {
        playerVehicle = player;
        turnController = controller;
        gameManager = manager;
        onPlayerTurnComplete = turnCompleteCallback;
        
        // Initialize UI coordinator (owns all UI sub-controllers)
        uiCoordinator = new PlayerUICoordinator(ui);

        // Setup button listeners
        if (ui.targetCancelButton != null)
            ui.targetCancelButton.onClick.AddListener(OnTargetCancelClicked);

        if (ui.endTurnButton != null)
            ui.endTurnButton.onClick.AddListener(OnEndTurnClicked);

        uiCoordinator.HideTurnUI();
    }
    
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
                uiCoordinator.StageSelection.ShowStageSelection(playerVehicle.currentStage.nextStages, OnStageSelected);
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
        
        // Get available seats (seats that can act)
        availableSeats = playerVehicle.GetActiveSeats();
        
        // Show UI via coordinator
        uiCoordinator.ShowTurnUI(availableSeats, playerVehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(playerVehicle);
        
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
        uiCoordinator.HideTurnUI();

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

    #region Skill & Seat Callbacks

    /// <summary>
    /// Called when player selects a seat tab.
    /// Updates current seat state and shows seat details.
    /// </summary>
    private void OnSeatSelected(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= availableSeats.Count) return;
        
        currentSeat = availableSeats[seatIndex];
        uiCoordinator.ShowSeatDetails(seatIndex, availableSeats, playerVehicle, OnSkillSelected);
    }

    /// <summary>
    /// Called when player clicks a skill button.
    /// Initiates skill targeting flow or executes immediately if self-targeted.
    /// </summary>
    private void OnSkillSelected(int skillIndex)
    {
        if (currentSeat == null) return;

        // Get available skills for this seat
        List<Skill> availableSkills = uiCoordinator.GetAvailableSkills(currentSeat);
        
        if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

        selectedSkill = availableSkills[skillIndex];
        
        // Use first operational component as source (for skill execution)
        selectedSkillSourceComponent = currentSeat.GetOperationalComponents().FirstOrDefault();

        // Check if skill needs source component selection first
        if (SkillNeedsSourceComponentSelection(selectedSkill))
        {
            uiCoordinator.TargetSelection.ShowSourceComponentSelection(playerVehicle, OnSourceComponentSelected);
            return;
        }

        // Check if skill needs target selection
        bool needsTarget = SkillNeedsTarget(selectedSkill);
        
        if (needsTarget)
        {
            List<Vehicle> validTargets = turnController.GetValidTargets(playerVehicle);
            uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
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
    /// Executes the selected skill via Vehicle.ExecuteSkill().
    /// Handles only player-specific aftermath: marking seat as acted and UI refresh.
    /// All validation, resource management, and execution handled by Vehicle.
    /// </summary>
    private void ExecuteSkillImmediately()
    {
        if (selectedSkill == null || selectedSkillSourceComponent == null) return;

        Vehicle target = selectedTarget ?? playerVehicle;

        // Delegate to Vehicle (handles validation, power consumption, and execution)
        bool skillSucceeded = playerVehicle.ExecuteSkill(
            selectedSkill,
            target,
            selectedSkillSourceComponent,
            selectedTargetComponent
        );

        // Handle player-specific aftermath
        if (skillSucceeded || true) // Always mark seat as acted (even on miss/fail)
        {
            currentSeat?.MarkAsActed();
        }

        // Refresh UI via coordinator
        uiCoordinator.RefreshAfterSkill(availableSeats, currentSeat, playerVehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(playerVehicle);
        gameManager.RefreshAllPanels();
        
        ClearPlayerSelections();
    }

    #endregion

    #region UI Callbacks

    /// <summary>
    /// Called when player selects a vehicle target.
    /// </summary>
    private void OnTargetSelected(Vehicle targetVehicle)
    {
        selectedTarget = targetVehicle;
        uiCoordinator.TargetSelection.Hide();

        // Check if selected skill requires precise component targeting
        if (selectedSkill != null && selectedSkill.targetPrecision == TargetPrecision.Precise)
        {
            // Show component selection UI for precise targeting
            uiCoordinator.TargetSelection.ShowComponentSelection(targetVehicle, OnComponentSelected);
        }
        else
        {
            // VehicleOnly or Auto targeting - execute immediately
            ExecuteSkillImmediately();
        }
    }

    /// <summary>
    /// Called when player selects a component target.
    /// </summary>
    private void OnComponentSelected(VehicleComponent component)
    {
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();
        ExecuteSkillImmediately();
    }

    /// <summary>
    /// Called when player selects a source component (self-targeting).
    /// </summary>
    private void OnSourceComponentSelected(VehicleComponent component)
    {
        // For source component selection, we're self-targeting
        selectedTarget = playerVehicle;
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();

        // After selecting source component, check if skill also needs enemy target selection
        bool needsEnemyTarget = SkillNeedsTarget(selectedSkill);
        
        if (needsEnemyTarget)
        {
            // Skill needs to target an enemy vehicle - show normal target selection
            List<Vehicle> validTargets = turnController.GetValidTargets(playerVehicle);
            uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
        }
        else
        {
            // Pure self-targeted skill - execute immediately
            ExecuteSkillImmediately();
        }
    }

    /// <summary>
    /// Called when player cancels target selection.
    /// </summary>
    private void OnTargetCancelClicked()
    {
        ClearPlayerSelections();
        uiCoordinator.TargetSelection.Hide();
    }

    /// <summary>
    /// Called when player selects a stage at a crossroads.
    /// </summary>
    private void OnStageSelected(Stage selectedStage)
    {
        uiCoordinator.StageSelection.Hide();
        isSelectingStage = false;

        turnController.MoveToStage(playerVehicle, selectedStage);
        ProcessPlayerMovement();
    }

    #endregion

    #region Helpers & Utilities

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