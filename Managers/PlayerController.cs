using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Consumables;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.Skills;

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

    private IRollTarget selectedTarget = null;
    private Skill selectedSkill = null;
    private Consumable selectedConsumable = null;
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

        uiCoordinator.ShowTurnUI(availableSeats, vehicle, OnSeatSelected, OnSkillSelected, OnConsumableSelected);
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
            uiCoordinator.ShowSeatDetails(seatIndex, availableSeats, vehicle, OnSkillSelected, OnConsumableSelected);
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

            case TargetingMode.Lane:
                if (vehicle.currentStage != null)
                    uiCoordinator.TargetSelection.ShowLaneSelection(
                        vehicle.currentStage.lanes, OnLaneSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.OwnLane:
                selectedTarget = vehicle.currentLane;
                selectedTargetComponent = null;
                ExecuteSkill();
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

        IRollTarget target = selectedTargetComponent != null ? (IRollTarget)selectedTargetComponent
            : selectedTarget != null ? selectedTarget
            : vehicle;

        RollActor sourceActor = null;
        if (currentSeat != null && selectedSkillSourceComponent != null)
            sourceActor = new CharacterWithToolActor(currentSeat, selectedSkillSourceComponent);
        else if (currentSeat != null)
            sourceActor = new CharacterActor(currentSeat);
        else if (selectedSkillSourceComponent != null)
            sourceActor = new ComponentActor(selectedSkillSourceComponent);

        var ctx = new RollContext
        {
            SourceActor = sourceActor,
            Target = target,
            CausalSource = selectedSkill.name
        };

        if (selectedSkill is ConsumableGatedSkill gated && !vehicle.TrySpendConsumable(gated.requiredConsumable, ctx.CausalSource))
            return;

        vehicle.ExecuteSkill(ctx, selectedSkill);

        currentSeat?.SpendAction(selectedSkill.actionCost);

        uiCoordinator.RefreshAfterSkill(availableSeats, currentSeat, vehicle, OnSeatSelected, OnSkillSelected, OnConsumableSelected);
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

    private void OnLaneSelected(StageLane lane)
    {
        selectedTarget = lane;
        uiCoordinator.TargetSelection.Hide();
        ExecuteSkill();
    }

    private void OnTargetCancelClicked()
    {
        ClearPlayerSelections();
        uiCoordinator.TargetSelection.Hide();
    }

    #endregion

    #region Consumable Execution

    public void OnConsumableSelected(int consumableIndex)
    {
        if (currentSeat == null) return;

        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        var available = vehicle.GetAvailableConsumables(currentSeat);
        if (consumableIndex < 0 || consumableIndex >= available.Count) return;

        selectedConsumable = available[consumableIndex].template as Consumable;
        if (selectedConsumable == null) return;

        switch (selectedConsumable.targetingMode)
        {
            case TargetingMode.Self:
                selectedTarget = vehicle;
                selectedTargetComponent = null;
                ExecuteConsumable();
                break;

            case TargetingMode.SourceComponent:
                uiCoordinator.TargetSelection.ShowSourceComponentSelection(vehicle, OnConsumableSourceComponentSelected);
                break;

            case TargetingMode.Enemy:
                List<Vehicle> validTargets = turnController.GetValidTargets(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnConsumableTargetSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.EnemyComponent:
                validTargets = turnController.GetValidTargets(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnConsumableTargetSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.Lane:
                if (vehicle.currentStage != null)
                    uiCoordinator.TargetSelection.ShowLaneSelection(
                        vehicle.currentStage.lanes, OnConsumableLaneSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.OwnLane:
                selectedTarget = vehicle.currentLane;
                selectedTargetComponent = null;
                ExecuteConsumable();
                break;
        }
    }

    private void OnConsumableTargetSelected(Vehicle targetVehicle)
    {
        selectedTarget = targetVehicle;
        uiCoordinator.TargetSelection.Hide();

        if (selectedConsumable != null && selectedConsumable.targetingMode == TargetingMode.EnemyComponent)
            uiCoordinator.TargetSelection.ShowComponentSelection(targetVehicle, OnConsumableComponentSelected);
        else
            ExecuteConsumable();
    }

    private void OnConsumableComponentSelected(VehicleComponent component)
    {
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();
        ExecuteConsumable();
    }

    private void OnConsumableSourceComponentSelected(VehicleComponent component)
    {
        var vehicle = CurrentPlayerVehicle;
        selectedTarget = vehicle;
        selectedTargetComponent = component;
        uiCoordinator.TargetSelection.Hide();
        ExecuteConsumable();
    }

    private void OnConsumableLaneSelected(StageLane lane)
    {
        selectedTarget = lane;
        uiCoordinator.TargetSelection.Hide();
        ExecuteConsumable();
    }

    private void ExecuteConsumable()
    {
        if (selectedConsumable == null) return;

        var vehicle = CurrentPlayerVehicle;
        if (vehicle == null) return;

        IRollTarget target = selectedTargetComponent != null ? (IRollTarget)selectedTargetComponent
            : selectedTarget != null ? selectedTarget
            : vehicle;

        RollActor sourceActor = null;
        if (currentSeat != null)
            sourceActor = new CharacterActor(currentSeat);

        var ctx = new RollContext
        {
            SourceActor = sourceActor,
            Target = target,
            CausalSource = selectedConsumable.name
        };

        bool valid = ConsumableValidator.Validate(ctx, selectedConsumable);
        if (!valid)
        {
            ClearPlayerSelections();
            return;
        }

        if (!vehicle.TrySpendConsumable(selectedConsumable, ctx.CausalSource))
        {
            ClearPlayerSelections();
            return;
        }
        RollNodeExecutor.Execute(selectedConsumable.onUseNode, ctx);
        currentSeat?.SpendAction(selectedConsumable.actionCost);

        uiCoordinator.RefreshAfterSkill(availableSeats, currentSeat, vehicle, OnSeatSelected, OnSkillSelected, OnConsumableSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);
        ClearPlayerSelections();
    }

    #endregion

    #region Helpers

    private void ClearPlayerSelections()
    {
        selectedSkill = null;
        selectedSkillSourceComponent = null;
        selectedTarget = null;
        selectedTargetComponent = null;
        selectedConsumable = null;
    }

    #endregion
}