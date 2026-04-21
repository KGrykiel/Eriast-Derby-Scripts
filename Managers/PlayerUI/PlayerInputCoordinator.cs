using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Selection;
using Assets.Scripts.Skills;
using Assets.Scripts.UI.Components;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Sole owner of all player-turn UI and input. Handles seat selection, skill targeting,
    /// and execution. PlayerController calls <see cref="BeginTurn"/> to start and
    /// <see cref="EndTurn"/> to stop; everything else is internal.
    /// </summary>
    public class PlayerInputCoordinator
    {
        #region Fields

        private readonly TurnService turnController;
        private readonly PlayerUIReferences ui;
        private readonly SeatSkillUIController seatSkillUI;
        private readonly TargetSelectionUIController targetSelectionUI;

        private Action<SkillAction> onActionReady;
        private Action onTurnComplete;

        private Vehicle vehicle;
        private List<VehicleSeat> availableSeats = new();
        private VehicleSeat currentSeat = null;
        private bool isTurnActive = false;

        private Skill selectedSkill = null;

        #endregion

        public PlayerInputCoordinator(
            TurnService turnController,
            PlayerUIReferences ui)
        {
            this.turnController = turnController;
            this.ui = ui;

            seatSkillUI = new SeatSkillUIController(ui);
            targetSelectionUI = new TargetSelectionUIController(ui);

            if (ui.targetCancelButton != null)
                ui.targetCancelButton.onClick.AddListener(OnTargetCancelClicked);

            if (ui.endTurnButton != null)
                ui.endTurnButton.onClick.AddListener(OnEndTurnClicked);

            if (ui.moveForwardButton != null)
                ui.moveForwardButton.onClick.AddListener(OnMoveForwardClicked);

            HideTurnUI();
        }

        // ==================== TURN HANDOFF ====================

        /// <summary>Called by PlayerTurnController before each turn to wire up the action and completion callbacks.</summary>
        public void SetCallbacks(Action<SkillAction> onActionReady, Action onTurnComplete)
        {
            this.onActionReady = onActionReady;
            this.onTurnComplete = onTurnComplete;
        }

        /// <summary>Called by PlayerActionHandler at the start of each player turn.</summary>
        public void BeginTurn(Vehicle currentVehicle)
        {
            vehicle = currentVehicle;
            availableSeats = vehicle.GetActiveSeats();
            isTurnActive = true;

            TurnEventBus.Emit(new PlayerActionPhaseStartedEvent(vehicle));
            ShowTurnUI();
        }

        // ==================== TURN UI ====================

        private void ShowTurnUI()
        {
            if (ui.playerTurnPanel != null)
                ui.playerTurnPanel.SetActive(true);

            if (ui.endTurnButton != null)
                ui.endTurnButton.interactable = true;

            if (ui.moveForwardButton != null)
                ui.moveForwardButton.interactable = true;

            seatSkillUI.ShowSeatTabs(SeatOptionBuilder.SeatTabOptions(availableSeats), OnSeatSelected);

            if (availableSeats.Count > 0)
                ShowSeatDetails(0);

            UpdateStatusDisplay();
        }

        private void HideTurnUI()
        {
            if (ui.playerTurnPanel != null)
                ui.playerTurnPanel.SetActive(false);

            targetSelectionUI.Hide();
        }

        private void ShowSeatDetails(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= availableSeats.Count) return;

            VehicleSeat seat = availableSeats[seatIndex];
            seatSkillUI.UpdateCurrentSeatDisplay(seat);
            seatSkillUI.ShowSkillSelection(SeatOptionBuilder.SkillOptions(seat, vehicle), OnSkillSelected);
        }

        private void RefreshAfterSkill()
        {
            seatSkillUI.ShowSeatTabs(SeatOptionBuilder.SeatTabOptions(availableSeats), OnSeatSelected);

            if (currentSeat != null)
            {
                seatSkillUI.UpdateCurrentSeatDisplay(currentSeat);
                seatSkillUI.ShowSkillSelection(SeatOptionBuilder.SkillOptions(currentSeat, vehicle), OnSkillSelected);
            }

            UpdateStatusDisplay();
        }

        private void UpdateStatusDisplay()
        {
            if (vehicle == null) return;

            if (ui.turnStatusText != null)
            {
                string stageName = RacePositionTracker.GetStage(vehicle) != null
                    ? RacePositionTracker.GetStage(vehicle).stageName
                    : "Unknown";
                ui.turnStatusText.text = $"<b>{vehicle.vehicleName}'s Turn</b>\n" +
                                         $"Stage: {stageName}\n" +
                                         $"Progress: {RacePositionTracker.GetProgress(vehicle):F1}m";
            }

            if (ui.actionsRemainingText != null)
            {
                int hp = vehicle.Chassis != null ? vehicle.Chassis.GetCurrentHealth() : 0;
                int maxHp = vehicle.Chassis != null ? vehicle.Chassis.GetMaxHealth() : 0;
                int energy = vehicle.PowerCore != null ? vehicle.PowerCore.GetCurrentEnergy() : 0;
                int maxEnergy = vehicle.PowerCore != null ? vehicle.PowerCore.GetMaxEnergy() : 0;
                ui.actionsRemainingText.text = $"HP: {hp}/{maxHp}  Energy: {energy}/{maxEnergy}";
            }
        }

        // ==================== SEAT & SKILL CALLBACKS ====================

        private void OnSeatSelected(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= availableSeats.Count) return;

            currentSeat = availableSeats[seatIndex];
            ShowSeatDetails(seatIndex);
        }

        private void OnSkillSelected(int skillIndex)
        {
            if (currentSeat == null || vehicle == null) return;
            if (IsBlockedByEventCard()) return;

            List<Skill> availableSkills = currentSeat.GetAvailableSkills();
            if (skillIndex < 0 || skillIndex >= availableSkills.Count) return;

            selectedSkill = availableSkills[skillIndex];

            var ctx = new SelectionContext(vehicle, turnController, targetSelectionUI, ExecuteSkill);

            if (TargetingSequences.Strategies.TryGetValue(selectedSkill.targetingMode, out var strategy))
                strategy.Execute(ctx);
        }

        // ==================== TARGET SELECTION PIPELINE ====================

        private void ExecuteSkill(IRollTarget target)
        {
            if (selectedSkill == null || vehicle == null) return;

            IRollTarget finalTarget = target ?? vehicle;
            RollActor sourceActor = currentSeat.BuildActorForSkill(selectedSkill);

            var action = new SkillAction(selectedSkill, sourceActor, finalTarget);
            onActionReady?.Invoke(action);

            ClearSelections();
            RefreshAfterSkill();
        }

        private void OnTargetCancelClicked()
        {
            ClearSelections();
            targetSelectionUI.Hide();
        }

        // ==================== BUTTON HANDLERS ====================

        private void OnEndTurnClicked()
        {
            if (!isTurnActive) return;
            if (IsBlockedByEventCard()) return;

            isTurnActive = false;

            ClearSelections();
            HideTurnUI();

            if (vehicle != null)
                TurnEventBus.Emit(new PlayerEndedTurnEvent(vehicle));

            onTurnComplete?.Invoke();
        }

        private void OnMoveForwardClicked()
        {
            if (!isTurnActive) return;
            if (IsBlockedByEventCard()) return;
            if (vehicle == null) return;

            bool success = RaceMovement.ExecuteMovement(vehicle);

            if (success)
            {
                TurnEventBus.Emit(new PlayerTriggeredMovementEvent(vehicle));

                if (ui.moveForwardButton != null)
                    ui.moveForwardButton.interactable = false;
            }
        }

        // ==================== HELPERS ====================

        public void ClearSelections()
        {
            selectedSkill = null;
        }

        private bool IsBlockedByEventCard()
            => EventCardUI.Instance != null && EventCardUI.Instance.IsShowing;
    }
}