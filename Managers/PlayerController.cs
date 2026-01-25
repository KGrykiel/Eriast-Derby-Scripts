using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Skills.Helpers;

/// <summary>
/// Orchestrates player input and coordinates between UI controllers and game systems.
/// UI display logic has been extracted to specialized controllers.
/// Fires events for logging (TurnEventLogger subscribes).
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
    private Action onPlayerTurnComplete;
    
    // UI Coordinator (owns all UI sub-controllers)
    private PlayerUICoordinator uiCoordinator;

    // Seat-based state
    private List<VehicleSeat> availableSeats = new();
    private VehicleSeat currentSeat = null;

    // Player selection state
    private Vehicle selectedTarget = null;
    private Skill selectedSkill = null;
    private VehicleComponent selectedSkillSourceComponent = null;
    private VehicleComponent selectedTargetComponent = null;
    private bool isSelectingStage = false;
    private bool isPlayerTurnActive = false;
    
    #endregion
    
    #region Events
    
    /// <summary>Fired when player vehicle cannot act. Args: (vehicle, reason)</summary>
    public event Action<Vehicle, string> OnPlayerCannotAct;
    
    /// <summary>Fired when player action phase starts. Args: vehicle</summary>
    public event Action<Vehicle> OnPlayerActionPhaseStarted;
    
    /// <summary>Fired when player ends their turn. Args: vehicle</summary>
    public event Action<Vehicle> OnPlayerEndedTurn;
    
    /// <summary>Fired when player triggers movement manually. Args: vehicle</summary>
    public event Action<Vehicle> OnPlayerTriggeredMovement;
    
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the player controller with required references.
    /// </summary>
    public void Initialize(Vehicle player, TurnController controller, GameManager manager, Action turnCompleteCallback)
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
        
        if (ui.moveForwardButton != null)
            ui.moveForwardButton.onClick.AddListener(OnMoveForwardClicked);

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
            OnPlayerCannotAct?.Invoke(playerVehicle, reason);
            
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
        
        // Enable move button (player can trigger movement anytime during action phase)
        if (ui.moveForwardButton != null)
            ui.moveForwardButton.interactable = true;
        
        // Show UI via coordinator
        uiCoordinator.ShowTurnUI(availableSeats, playerVehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(playerVehicle);
        
        OnPlayerActionPhaseStarted?.Invoke(playerVehicle);
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

        OnPlayerEndedTurn?.Invoke(playerVehicle);
        onPlayerTurnComplete?.Invoke();
    }
    
    /// <summary>
    /// Handles Move Forward button click. Triggers movement during action phase.
    /// Movement is FREE (power already paid at turn start).
    /// Can only be clicked once per turn.
    /// </summary>
    public void OnMoveForwardClicked()
    {
        if (!isPlayerTurnActive) return;
        if (playerVehicle == null || gameManager == null) return;
        
        // Trigger movement via GameManager
        bool success = gameManager.TriggerPlayerMovement();
        
        if (success)
        {
            // Disable move button after successful movement
            if (ui.moveForwardButton != null)
                ui.moveForwardButton.interactable = false;
            
            OnPlayerTriggeredMovement?.Invoke(playerVehicle);
        }
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

        List<Skill> availableSkills = uiCoordinator.GetAvailableSkills(currentSeat);
        if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

        selectedSkill = availableSkills[skillIndex];
        
        // Determine source: component that provides this skill, or null if it's a character personal skill
        selectedSkillSourceComponent = currentSeat.GetComponentForSkill(selectedSkill);

        // Source component selection first (self-targeting skills)
        if (SkillNeedsSourceComponentSelection(selectedSkill))
        {
            uiCoordinator.TargetSelection.ShowSourceComponentSelection(playerVehicle, OnSourceComponentSelected);
            return;
        }

        // Self-targeted or AoE - execute immediately
        if (!SkillNeedsTarget(selectedSkill))
        {
            selectedTarget = playerVehicle;
            ExecuteSkillImmediately();
            return;
        }

        // Needs target selection
        List<Vehicle> validTargets = turnController.GetValidTargets(playerVehicle);
        uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
    }

    #endregion

    #region Skill Execution

    /// <summary>
    /// Executes the selected skill.
    /// Builds SkillContext here (PlayerController has full knowledge of seat, character, selections).
    /// Vehicle handles resource validation and consumption.
    /// SkillExecutor handles resolution.
    /// </summary>
    private void ExecuteSkillImmediately()
    {
        if (selectedSkill == null) return;

        Vehicle target = selectedTarget != null ? selectedTarget : playerVehicle;
        Entity targetEntity = selectedTargetComponent != null ? selectedTargetComponent : target.chassis;
        PlayerCharacter character = currentSeat?.assignedCharacter;

        var ctx = new SkillContext
        {
            Skill = selectedSkill,
            SourceVehicle = playerVehicle,
            SourceEntity = selectedSkillSourceComponent,
            SourceCharacter = character,
            TargetEntity = targetEntity,
            IsCriticalHit = false
        };

        playerVehicle.ExecuteSkill(ctx);

        currentSeat?.MarkAsActed();
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