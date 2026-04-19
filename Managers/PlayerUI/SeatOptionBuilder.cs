using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.Selection;
using Assets.Scripts.Skills;

namespace Assets.Scripts.Managers.PlayerUI
{
    /// <summary>
    /// Builds display-ready <see cref="SelectionOption{T}"/> lists for seat tabs and skill buttons.
    /// Owns all status formatting and interactability logic so
    /// <see cref="SeatSkillUIController"/> stays dumb.
    /// </summary>
    public static class SeatOptionBuilder
    {
        public static List<SelectionOption<VehicleSeat>> SeatTabOptions(List<VehicleSeat> seats)
        {
            var options = new List<SelectionOption<VehicleSeat>>();

            foreach (var seat in seats)
            {
                bool canAct    = seat.CanAct();
                bool actionSpent = !seat.CanSpendAction(ActionType.Action);
                bool bonusSpent  = !seat.CanSpendAction(ActionType.BonusAction);

                string statusIcon;
                if (!canAct)
                    statusIcon = "[X]";
                else if (actionSpent && bonusSpent)
                    statusIcon = "[v]";
                else if (actionSpent || bonusSpent)
                    statusIcon = "[~]";
                else
                    statusIcon = "[ ]";

                string characterName = seat.GetDisplayName() ?? "Unassigned";
                string label = $"{statusIcon} {seat.seatName} ({characterName})";
                bool interactable = canAct && seat.HasAnyActionsRemaining();

                options.Add(new SelectionOption<VehicleSeat>(seat, label, interactable));
            }

            return options;
        }

        public static List<SelectionOption<Skill>> SkillOptions(VehicleSeat seat, Vehicle vehicle)
        {
            var options = new List<SelectionOption<Skill>>();

            foreach (var skill in seat.GetAvailableSkills())
            {
                string label = $"{skill.name} ({BuildCostDisplay(skill)})";
                bool interactable = CanPayAllCosts(skill, vehicle) && seat.CanSpendAction(skill.actionCost);

                options.Add(new SelectionOption<Skill>(skill, label, interactable));
            }

            return options;
        }

        public static string SeatStatusLine(VehicleSeat seat)
        {
            string characterName = seat.GetDisplayName() ?? "Unassigned";
            bool actionSpent = !seat.CanSpendAction(ActionType.Action);
            bool bonusSpent  = !seat.CanSpendAction(ActionType.BonusAction);
            string status = (actionSpent && bonusSpent) ? "- Done" : (actionSpent || bonusSpent) ? "- Partial" : "- Ready";
            return $"<b>{seat.seatName}</b> ({characterName}) {status}";
        }

        private static string BuildCostDisplay(Skill skill)
        {
            if (skill.costs.Count == 0)
                return "Free";

            var parts = new List<string>();
            foreach (var cost in skill.costs)
                parts.Add(cost.GetDescription());
            return string.Join(", ", parts);
        }

        private static bool CanPayAllCosts(Skill skill, Vehicle vehicle)
        {
            foreach (var cost in skill.costs)
            {
                if (!cost.CanPay(vehicle))
                    return false;
            }
            return true;
        }
    }
}
