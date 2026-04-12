using System;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers.PlayerUI
{
    public class PlayerUICoordinator
    {
        private readonly PlayerUIReferences ui;
        private readonly SeatSkillUIController seatSkillUI;
        private readonly TargetSelectionUIController targetSelectionUI;

        public PlayerUICoordinator(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
            seatSkillUI = new SeatSkillUIController(ui);
            targetSelectionUI = new TargetSelectionUIController(ui);
        }

        // ==================== TURN UI ====================

        public void ShowTurnUI(
            List<VehicleSeat> availableSeats, 
            Vehicle vehicle,
            Action<int> onSeatSelected,
            Action<int> onSkillSelected,
            Action<int> onConsumableSelected)
        {
            if (ui.playerTurnPanel != null)
                ui.playerTurnPanel.SetActive(true);

            if (ui.endTurnButton != null)
                ui.endTurnButton.interactable = true;

            seatSkillUI.ShowSeatTabs(availableSeats, onSeatSelected);

            if (availableSeats.Count > 0)
                ShowSeatDetails(0, availableSeats, vehicle, onSkillSelected, onConsumableSelected);
        }

        public void ShowSeatDetails(
            int seatIndex, 
            List<VehicleSeat> availableSeats, 
            Vehicle vehicle, 
            Action<int> onSkillSelected,
            Action<int> onConsumableSelected)
        {
            if (seatIndex < 0 || seatIndex >= availableSeats.Count) return;

            VehicleSeat seat = availableSeats[seatIndex];
            seatSkillUI.UpdateCurrentSeatDisplay(seat);
            seatSkillUI.ShowSkillSelection(seat, vehicle, onSkillSelected, onConsumableSelected);
        }
        
        public void RefreshAfterSkill(
            List<VehicleSeat> availableSeats,
            VehicleSeat currentSeat,
            Vehicle vehicle,
            Action<int> onSeatSelected,
            Action<int> onSkillSelected,
            Action<int> onConsumableSelected)
        {
            seatSkillUI.ShowSeatTabs(availableSeats, onSeatSelected);

            if (currentSeat != null)
            {
                seatSkillUI.UpdateCurrentSeatDisplay(currentSeat);
                seatSkillUI.ShowSkillSelection(currentSeat, vehicle, onSkillSelected, onConsumableSelected);
            }
        }
        
        public void HideTurnUI()
        {
            if (ui.playerTurnPanel != null)
                ui.playerTurnPanel.SetActive(false);

            targetSelectionUI.Hide();
        }
        
        public void UpdateTurnStatusDisplay(Vehicle vehicle)
        {
            if (ui.turnStatusText != null)
            {
                ui.turnStatusText.text = $"<b>{vehicle.vehicleName}'s Turn</b>\n" +
                                      $"Stage: {(RacePositionTracker.GetStage(vehicle) != null ? RacePositionTracker.GetStage(vehicle).stageName : "Unknown")}\n" +
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
        
        // ==================== UTILITY ====================
        
        public List<Skill> GetAvailableSkills(VehicleSeat seat)
        {
            return seatSkillUI.GetAvailableSkills(seat);
        }

        public TargetSelectionUIController TargetSelection => targetSelectionUI;
    }
}
