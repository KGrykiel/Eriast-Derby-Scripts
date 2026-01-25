using System;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Coordinates all player UI controllers and provides high-level orchestration.
    /// Owns sub-controllers for seats, skills, targets, and stages.
    /// </summary>
    public class PlayerUICoordinator
    {
        private readonly PlayerUIReferences ui;
        private readonly SeatSkillUIController seatSkillUI;
        private readonly TargetSelectionUIController targetSelectionUI;
        private readonly StageSelectionUIController stageSelectionUI;
        
        public PlayerUICoordinator(PlayerUIReferences uiReferences)
        {
            ui = uiReferences;
            seatSkillUI = new SeatSkillUIController(ui);
            targetSelectionUI = new TargetSelectionUIController(ui);
            stageSelectionUI = new StageSelectionUIController(ui);
        }
        
        // ==================== TURN UI ====================
        
        /// <summary>
        /// Shows the complete turn UI: panel, end turn button, and seat tabs.
        /// Automatically selects the first seat if available.
        /// </summary>
        public void ShowTurnUI(
            List<VehicleSeat> availableSeats, 
            Vehicle vehicle,
            Action<int> onSeatSelected,
            Action<int> onSkillSelected)
        {
            if (ui.playerTurnPanel != null)
                ui.playerTurnPanel.SetActive(true);

            if (ui.endTurnButton != null)
                ui.endTurnButton.interactable = true;

            seatSkillUI.ShowSeatTabs(availableSeats, onSeatSelected);
            
            // Select first seat by default
            if (availableSeats.Count > 0)
            {
                ShowSeatDetails(0, availableSeats, vehicle, onSkillSelected);
            }
        }
        
        /// <summary>
        /// Shows details for a specific seat: updates display and shows skills.
        /// </summary>
        public void ShowSeatDetails(
            int seatIndex, 
            List<VehicleSeat> availableSeats, 
            Vehicle vehicle, 
            Action<int> onSkillSelected)
        {
            if (seatIndex < 0 || seatIndex >= availableSeats.Count) return;

            VehicleSeat seat = availableSeats[seatIndex];
            seatSkillUI.UpdateCurrentSeatDisplay(seat);
            seatSkillUI.ShowSkillSelection(seat, vehicle, onSkillSelected);
        }
        
        /// <summary>
        /// Refreshes seat tabs and skill selection after a skill is used.
        /// </summary>
        public void RefreshAfterSkill(
            List<VehicleSeat> availableSeats,
            VehicleSeat currentSeat,
            Vehicle vehicle,
            Action<int> onSeatSelected,
            Action<int> onSkillSelected)
        {
            seatSkillUI.ShowSeatTabs(availableSeats, onSeatSelected);
            
            if (currentSeat != null)
            {
                seatSkillUI.ShowSkillSelection(currentSeat, vehicle, onSkillSelected);
            }
        }
        
        /// <summary>
        /// Hides the turn UI panel and all sub-panels.
        /// </summary>
        public void HideTurnUI()
        {
            if (ui.playerTurnPanel != null)
                ui.playerTurnPanel.SetActive(false);

            targetSelectionUI.Hide();
            stageSelectionUI.Hide();
        }
        
        /// <summary>
        /// Updates the turn status display with vehicle state.
        /// </summary>
        public void UpdateTurnStatusDisplay(Vehicle vehicle)
        {
            if (ui.turnStatusText != null)
            {
                ui.turnStatusText.text = $"<b>{vehicle.vehicleName}'s Turn</b>\n" +
                                      $"Stage: {(vehicle.currentStage != null ? vehicle.currentStage.stageName : "Unknown")}\n" +
                                      $"Progress: {vehicle.progress:F1}m";
            }

            if (ui.actionsRemainingText != null)
            {
                int hp = vehicle.chassis != null ? vehicle.chassis.GetCurrentHealth() : 0;
                int maxHp = vehicle.chassis != null ? vehicle.chassis.GetMaxHealth() : 0;
                int energy = vehicle.powerCore != null ? vehicle.powerCore.GetCurrentEnergy() : 0;
                int maxEnergy = vehicle.powerCore != null ? vehicle.powerCore.GetMaxEnergy() : 0;
                
                ui.actionsRemainingText.text = $"HP: {hp}/{maxHp}  Energy: {energy}/{maxEnergy}";
            }
        }
        
        // ==================== UTILITY ====================
        
        /// <summary>
        /// Gets available skills for a seat (delegates to sub-controller).
        /// </summary>
        public List<Skill> GetAvailableSkills(VehicleSeat seat)
        {
            return seatSkillUI.GetAvailableSkills(seat);
        }
        
        // ==================== SUB-CONTROLLER ACCESS ====================
        
        /// <summary>
        /// Access target selection UI controller.
        /// </summary>
        public TargetSelectionUIController TargetSelection => targetSelectionUI;
        
        /// <summary>
        /// Access stage selection UI controller.
        /// </summary>
        public StageSelectionUIController StageSelection => stageSelectionUI;
    }
}
