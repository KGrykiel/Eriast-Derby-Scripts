using System;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.PlayerUI;

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
    private PlayerInputCoordinator inputCoordinator;

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
        inputCoordinator = new PlayerInputCoordinator(
            turnController,
            uiCoordinator,
            ui,
            onEndTurnRequested: OnEndTurnClicked,
            onMoveForwardRequested: OnMoveForwardClicked);

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
        var seats = vehicle.GetActiveSeats();
        inputCoordinator.BeginTurn(vehicle, seats);

        if (ui.moveForwardButton != null)
            ui.moveForwardButton.interactable = true;

        uiCoordinator.ShowTurnUI(seats, inputCoordinator.OnSeatSelected, inputCoordinator.OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);

        TurnEventBus.EmitPlayerActionPhaseStarted(vehicle);
    }

    public void OnEndTurnClicked()
    {
        if (!isPlayerTurnActive) return;
        
        var vehicle = CurrentPlayerVehicle;

        isPlayerTurnActive = false;
        inputCoordinator.ClearSelections();
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
}