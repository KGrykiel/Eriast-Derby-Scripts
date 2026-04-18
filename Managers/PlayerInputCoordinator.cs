using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Managers;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Skills;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.UI.Components;

/// <summary>
/// Handles all player input gathering for a single turn: seat selection, skill/consumable
/// targeting, and execution. Fully decoupled from turn lifecycle — PlayerController calls
/// <see cref="BeginTurn"/> to hand off the active vehicle and seats at the start of each
/// player turn, and <see cref="ClearSelections"/> when the turn ends.
/// </summary>
public class PlayerInputCoordinator
{
    #region Fields

    private readonly TurnService turnController;
    private readonly PlayerUICoordinator uiCoordinator;
    private readonly Action onEndTurnRequested;
    private readonly Action onMoveForwardRequested;

    private Vehicle vehicle;
    private List<VehicleSeat> availableSeats = new();
    private VehicleSeat currentSeat = null;

    private IRollTarget selectedTarget = null;
    private Skill selectedSkill = null;
    private VehicleComponent selectedTargetComponent = null;

    #endregion

    public PlayerInputCoordinator(
        TurnService turnController,
        PlayerUICoordinator uiCoordinator,
        PlayerUIReferences ui,
        Action onEndTurnRequested,
        Action onMoveForwardRequested)
    {
        this.turnController = turnController;
        this.uiCoordinator = uiCoordinator;
        this.onEndTurnRequested = onEndTurnRequested;
        this.onMoveForwardRequested = onMoveForwardRequested;

        if (ui.targetCancelButton != null)
            ui.targetCancelButton.onClick.AddListener(OnTargetCancelClicked);

        if (ui.endTurnButton != null)
            ui.endTurnButton.onClick.AddListener(OnEndTurnClicked);

        if (ui.moveForwardButton != null)
            ui.moveForwardButton.onClick.AddListener(OnMoveForwardClicked);
    }

    // ==================== TURN HANDOFF ====================

    /// <summary>Called by PlayerController at the start of each player turn.</summary>
    public void BeginTurn(Vehicle currentVehicle, List<VehicleSeat> seats)
    {
        vehicle = currentVehicle;
        availableSeats = seats;
    }

    // ==================== SEAT & SKILL CALLBACKS ====================

    public void OnSeatSelected(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= availableSeats.Count) return;

        currentSeat = availableSeats[seatIndex];

        if (vehicle != null)
        {
            uiCoordinator.ShowSeatDetails(seatIndex, availableSeats, OnSkillSelected);
        }
    }

    /// <summary>
    /// Calls additional UI based on targeting mode
    /// </summary>
    public void OnSkillSelected(int skillIndex)
    {
        if (currentSeat == null) return;
        if (IsBlockedByEventCard()) return;

        if (vehicle == null) return;

        List<Skill> availableSkills = currentSeat.GetAvailableSkills();
        if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

        selectedSkill = availableSkills[skillIndex];

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
                List<Vehicle> validTargets = turnController.GetOtherVehiclesInStage(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.EnemyComponent:
                validTargets = turnController.GetOtherVehiclesInStage(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(validTargets, OnTargetSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.Lane:
                var skillLaneStage = RacePositionTracker.GetStage(vehicle);
                if (skillLaneStage != null)
                    uiCoordinator.TargetSelection.ShowLaneSelection(
                        skillLaneStage.lanes, OnLaneSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.OwnLane:
                selectedTarget = RacePositionTracker.GetLane(vehicle);
                selectedTargetComponent = null;
                ExecuteSkill();
                break;

            case TargetingMode.Any:
                List<Vehicle> allTargets = turnController.GetAllTargets(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(allTargets, OnTargetSelected, OnTargetCancelClicked);
                break;

            case TargetingMode.AnyComponent:
                allTargets = turnController.GetAllTargets(vehicle);
                uiCoordinator.TargetSelection.ShowTargetSelection(allTargets, OnTargetSelected, OnTargetCancelClicked);
                break;
        }
    }

    // ==================== SKILL EXECUTION ====================

    private void ExecuteSkill()
    {
        if (selectedSkill == null) return;
        if (vehicle == null) return;

        IRollTarget target = selectedTargetComponent != null ? selectedTargetComponent
            : selectedTarget ?? vehicle;

        RollActor sourceActor = currentSeat.BuildActorForSkill(selectedSkill);

        SkillPipeline.Execute(new SkillAction(selectedSkill, sourceActor, target));

        uiCoordinator.RefreshAfterSkill(availableSeats, currentSeat, OnSeatSelected, OnSkillSelected);
        uiCoordinator.UpdateTurnStatusDisplay(vehicle);
        ClearSelections();
    }

    // ==================== SKILL UI CALLBACKS ====================

    /// <summary>
    /// Shows component selection if targeting is precise, otherwise executes skill immediately
    /// </summary>
    private void OnTargetSelected(Vehicle targetVehicle)
    {
        selectedTarget = targetVehicle;
        uiCoordinator.TargetSelection.Hide();

        bool needsComponentSelection = selectedSkill != null &&
            (selectedSkill.targetingMode == TargetingMode.EnemyComponent ||
             selectedSkill.targetingMode == TargetingMode.AnyComponent);

        if (needsComponentSelection)
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

    public void OnTargetCancelClicked()
    {
        ClearSelections();
        uiCoordinator.TargetSelection.Hide();
    }

    // ==================== HELPERS ====================

    public void ClearSelections()
    {
        selectedSkill = null;
        selectedTarget = null;
        selectedTargetComponent = null;
    }

    private void OnEndTurnClicked()
    {
        if (IsBlockedByEventCard()) return;
        onEndTurnRequested?.Invoke();
    }

    private void OnMoveForwardClicked()
    {
        if (IsBlockedByEventCard()) return;
        onMoveForwardRequested?.Invoke();
    }

    private bool IsBlockedByEventCard()
        => EventCardUI.Instance != null && EventCardUI.Instance.IsShowing;
}
