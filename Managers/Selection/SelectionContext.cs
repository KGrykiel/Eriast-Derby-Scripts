using System;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Managers.Turn;

namespace Assets.Scripts.Managers.Selection
{
    /// <summary>
    /// Immutable inputs for a targeting sequence, including the callback to invoke
    /// when a target has been resolved.
    /// </summary>
    public class SelectionContext
    {
        public readonly Vehicle SourceVehicle;
        public readonly TurnService TurnController;
        public readonly TargetSelectionUIController TargetSelection;
        public readonly Action<IRollTarget> OnComplete;

        public SelectionContext(
            Vehicle sourceVehicle,
            TurnService turnController,
            TargetSelectionUIController targetSelection,
            Action<IRollTarget> onComplete)
        {
            SourceVehicle   = sourceVehicle;
            TurnController  = turnController;
            TargetSelection = targetSelection;
            OnComplete      = onComplete;
        }
    }
}
