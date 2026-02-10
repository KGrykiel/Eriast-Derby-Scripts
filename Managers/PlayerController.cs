using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Skills.Helpers;

/// <summary>
/// Orchestrates player input for player-controlled vehicles.
/// Works with WHATEVER vehicle is currently taking its turn (if player-controlled).
/// Supports multiple player vehicles - each triggers PlayerAction phase when it's their turn.
/// 
/// UI display logic has been extracted to specialized controllers.
/// Events are emitted via TurnEventBus for logging.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Fields & Configuration
    
    [Header("UI References")]
    [SerializeField]
    private PlayerUIReferences ui;

    private TurnService turnController;
    private TurnStateMachine stateMachine;
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
    private bool isPlayerTurnActive = false;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Get the current player vehicle (the one whose turn it is).
    /// Returns null if not in a player's turn.
    /// </summary>
    private Vehicle CurrentPlayerVehicle
    {
        get
        {
            if (stateMachine == null) return null;
            var current = stateMachine.CurrentVehicle;
            if (current != null && current.controlType == ControlType.Player)
                return current;
            return null;
        }
    }
    
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the player controller with required references.
    /// No specific vehicle needed - works with whatever player vehicle is taking its turn.
    /// </summary>
    public void Initialize(TurnService controller, TurnStateMachine machine, Action turnCompleteCallback)
    {
        turnController = controller;
        stateMachine = machine;
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
    /// Stage transitions are now handled by the lane system - no manual selection needed.
    /// Shows action UI for player to take actions.
    /// </summary>
    public void ProcessPlayerMovement()
    {
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;
        
        // Check if vehicle can operate
        if (!vehicle.IsOperational())
        {
            string reason = vehicle.GetNonOperationalReason();
            TurnEventBus.EmitPlayerCannotAct(vehicle, reason);
            
            // Auto-end turn if vehicle is non-operational
            onPlayerTurnComplete?.Invoke();
            return;
        }

        // Stage transitions are handled by lane system (StageLane.nextStage)
        // No manual crossroads selection - lane determines which stage you enter
        
        // Movement complete, show action UI
        StartPlayerActionPhase();
    }

    /// <summary>
    /// Starts the player's action phase. Shows UI and enables skill usage.
    /// </summary>
    private void StartPlayerActionPhase()
    {
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;
        
        isPlayerTurnActive = true;
        
        // Reset all components for new turn
        vehicle.ResetComponentsForNewTurn();
        
        // Get available seats (seats that can act)
        availableSeats = vehicle.GetActiveSeats();
        
        
        // Enable move button (player can trigger movement anytime during action phase)
        if (ui.moveForwardButton != null)
            ui.moveForwardButton.interactable = true;
        
        // Show UI via coordinator
        uiCoordinator.ShowTurnUI(availableSeats, vehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);
        
        TurnEventBus.EmitPlayerActionPhaseStarted(vehicle);
    }

    /// <summary>
    /// Handles End Turn button click. Completes player's turn.
    /// Always available - no requirement for all components to act.
    /// </summary>
    public void OnEndTurnClicked()
    {
        if (!isPlayerTurnActive) return;
        
        var vehicle = CurrentPlayerVehicle;

        isPlayerTurnActive = false;
        ClearPlayerSelections();
        uiCoordinator.HideTurnUI();

        if (vehicle != null)
        {
            TurnEventBus.EmitPlayerEndedTurn(vehicle);
        }
        onPlayerTurnComplete?.Invoke();
    }
    
    /// <summary>
    /// Handles Move Forward button click. Triggers movement during action phase.
    /// Movement is FREE (power already paid at turn start).
    /// Can only be clicked once per turn.
    /// Stage transitions are handled automatically by lane system.
    /// </summary>
    public void OnMoveForwardClicked()
    {
        if (!isPlayerTurnActive) return;
        
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;
        
        bool success = turnController.ExecuteMovement(vehicle);
        
        if (success)
        {
            // Disable move button after successful movement
            if (ui.moveForwardButton != null)
                ui.moveForwardButton.interactable = false;
            
            // Stage transitions handled by lane system (no UI popup needed)
            
            TurnEventBus.EmitPlayerTriggeredMovement(vehicle);
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
        
        var vehicle = CurrentPlayerVehicle;
        if (vehicle != null)
        {
            uiCoordinator.ShowSeatDetails(seatIndex, availableSeats, vehicle, OnSkillSelected);
        }
    }

    /// <summary>
    /// Called when player clicks a skill button.
    /// Initiates skill targeting flow or executes immediately if self-targeted.
    /// </summary>
    private void OnSkillSelected(int skillIndex)
    {
        if (currentSeat == null) return;
        
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        List<Skill> availableSkills = uiCoordinator.GetAvailableSkills(currentSeat);
        if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

        selectedSkill = availableSkills[skillIndex];
        
        // Determine source: component that provides this skill, or null if it's a character personal skill
        selectedSkillSourceComponent = currentSeat.GetComponentForSkill(selectedSkill);

        // Source component selection first (self-targeting skills)
        if (SkillNeedsSourceComponentSelection(selectedSkill))
        {
            uiCoordinator.TargetSelection.ShowSourceComponentSelection(vehicle, OnSourceComponentSelected);
            return;
        }

        // Self-targeted or AoE - execute immediately
        if (!SkillNeedsTarget(selectedSkill))
        {
            selectedTarget = vehicle;
            ExecuteSkill();
            return;
        }

        // Needs target selection
        List<Vehicle> validTargets = turnController.GetValidTargets(vehicle);
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
    private void ExecuteSkill()
    {
        if (selectedSkill == null) return;
        
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        Vehicle target = selectedTarget != null ? selectedTarget : vehicle;
        Entity targetEntity = selectedTargetComponent != null ? selectedTargetComponent : target.chassis;
        Character character = currentSeat != null ? currentSeat.assignedCharacter : null;

        var ctx = new SkillContext
        {
            Skill = selectedSkill,
            SourceVehicle = vehicle,
            SourceEntity = selectedSkillSourceComponent,
            SourceCharacter = character,
            TargetEntity = targetEntity,
            IsCriticalHit = false
        };

        vehicle.ExecuteSkill(ctx);

        if (currentSeat != null)
        {
            currentSeat.MarkAsActed();
        }
        uiCoordinator.RefreshAfterSkill(availableSeats, currentSeat, vehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);
        
        // Panels handle their own refresh via Update() polling
        // No need to manually trigger refresh here
        
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
        if (selectedSkill != null && selectedSkill.targetingMode == TargetingMode.EnemyComponent)
        {
            // Show component selection UI for precise targeting
            uiCoordinator.TargetSelection.ShowComponentSelection(targetVehicle, OnComponentSelected);
        }
        else
        {
            // Enemy (auto-route) targeting - execute immediately
            ExecuteSkill();
        }
    }

    /// <summary>
    /// Called when player selects a component target.
    /// </summary>
    private void OnComponentSelected(VehicleComponent component)
    {
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();
        ExecuteSkill();
    }

    /// <summary>
    /// Called when player selects a source component (self-targeting).
    /// </summary>
    private void OnSourceComponentSelected(VehicleComponent component)
    {
        var vehicle = CurrentPlayerVehicle;
        
        // For source component selection, we're self-targeting
        selectedTarget = vehicle;
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();

        // After selecting source component, check if skill also needs enemy target selection
        bool needsEnemyTarget = SkillNeedsTarget(selectedSkill);
        
        if (needsEnemyTarget && vehicle != null)
        {
            // Skill needs to target an enemy vehicle - show normal target selection
            List<Vehicle> validTargets = turnController.GetValidTargets(vehicle);
            uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
        }
        else
        {
            // Pure self-targeted skill - execute immediately
            ExecuteSkill();
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