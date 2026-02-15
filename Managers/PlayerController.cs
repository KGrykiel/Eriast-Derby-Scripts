using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Skills.Helpers;

/// <summary>Orchestrates player input for whichever vehicle is currently taking its turn.</summary>
public class PlayerController : MonoBehaviour
{
    #region Fields & Configuration
    
    [Header("UI References")]
    [SerializeField]
    private PlayerUIReferences ui;

    private TurnService turnController;
    private TurnStateMachine stateMachine;
    private Action onPlayerTurnComplete;
    
    private PlayerUICoordinator uiCoordinator;

    private List<VehicleSeat> availableSeats = new();
    private VehicleSeat currentSeat = null;

    private Vehicle selectedTarget = null;
    private Skill selectedSkill = null;
    private VehicleComponent selectedSkillSourceComponent = null;
    private VehicleComponent selectedTargetComponent = null;
    private bool isPlayerTurnActive = false;
    
    #endregion
    
    #region Properties

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

    public void Initialize(TurnService controller, TurnStateMachine machine, Action turnCompleteCallback)
    {
        turnController = controller;
        stateMachine = machine;
        onPlayerTurnComplete = turnCompleteCallback;

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

    public void ProcessPlayerMovement()
    {
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        if (!vehicle.IsOperational())
        {
            string reason = vehicle.GetNonOperationalReason();
            TurnEventBus.EmitPlayerCannotAct(vehicle, reason);
            onPlayerTurnComplete?.Invoke();
            return;
        }

        StartPlayerActionPhase();
    }

    private void StartPlayerActionPhase()
    {
        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        isPlayerTurnActive = true;
        vehicle.ResetComponentsForNewTurn();
        availableSeats = vehicle.GetActiveSeats();

        if (ui.moveForwardButton != null)
            ui.moveForwardButton.interactable = true;

        uiCoordinator.ShowTurnUI(availableSeats, vehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);

        TurnEventBus.EmitPlayerActionPhaseStarted(vehicle);
    }

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
    /// Once per turn (power already paid at turn start).
    /// if not used, moved automatically
    /// </summary>
    public void OnMoveForwardClicked()
    {
        if (!isPlayerTurnActive) return;

        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        bool success = turnController.ExecuteMovement(vehicle);

        if (success)
        {
            if (ui.moveForwardButton != null)
                ui.moveForwardButton.interactable = false;

            TurnEventBus.EmitPlayerTriggeredMovement(vehicle);
        }
    }

    #endregion

    #region Skill & Seat Callbacks

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
    /// Calls additional UI based on targeting mode
    /// </summary>
    private void OnSkillSelected(int skillIndex)
    {
        if (currentSeat == null) return;

        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        List<Skill> availableSkills = uiCoordinator.GetAvailableSkills(currentSeat);
        if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

        selectedSkill = availableSkills[skillIndex];
        selectedSkillSourceComponent = currentSeat.GetComponentForSkill(selectedSkill);

        switch (selectedSkill.targetingMode)
        {
            case TargetingMode.Self:
                selectedTarget = vehicle;
                selectedTargetComponent = null;
                ExecuteSkill();
                break;

            case TargetingMode.SourceComponent:
                uiCoordinator.TargetSelection.ShowSourceComponentSelection(vehicle, OnSourceComponentSelected);
                break;

            case TargetingMode.Enemy:
                List<Vehicle> validTargets = turnController.GetValidTargets(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.EnemyComponent:
                validTargets = turnController.GetValidTargets(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
                break;
        }
    }

    #endregion

    #region Skill Execution

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

        currentSeat?.MarkAsActed();

        uiCoordinator.RefreshAfterSkill(availableSeats, currentSeat, vehicle, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);
        ClearPlayerSelections();
    }

    #endregion


    #region UI Callbacks

    /// <summary>
    /// Shows component selection if targeting is precise, otherwise executes skill immediately
    /// </summary>
    private void OnTargetSelected(Vehicle targetVehicle)
    {
        selectedTarget = targetVehicle;
        uiCoordinator.TargetSelection.Hide();

        if (selectedSkill != null && selectedSkill.targetingMode == TargetingMode.EnemyComponent)
            uiCoordinator.TargetSelection.ShowComponentSelection(targetVehicle, OnComponentSelected);
        else
            ExecuteSkill();
    }

    private void OnComponentSelected(VehicleComponent component)
    {
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();
        ExecuteSkill();
    }

    private void OnSourceComponentSelected(VehicleComponent component)
    {
        var vehicle = CurrentPlayerVehicle;
        selectedTarget = vehicle;
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();
        ExecuteSkill();
    }

    private void OnTargetCancelClicked()
    {
        ClearPlayerSelections();
        uiCoordinator.TargetSelection.Hide();
    }

    #endregion

    #region Helpers & Utilities

    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillSourceComponent = null;
        selectedTarget = null;
        selectedTargetComponent = null;
    }

    #endregion
}