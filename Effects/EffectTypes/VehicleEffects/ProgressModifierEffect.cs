using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>Modifies a vehicle's stage progress. Use negative amounts to push the vehicle backward.</summary>
    [System.Serializable]
    [SRName("Modify Progress")]
    public class ProgressModifierEffect : IVehicleEffect
    {
        public enum ProgressModifierMode
        {
            /// <summary>Adds a fixed number of progress units (positive = forward, negative = backward).</summary>
            Flat,
            /// <summary>Sets progress to a specific position in the current stage (0 = stage start, stage.length = stage end).</summary>
            SetAbsolute,
        }

        [Tooltip("Flat: adds/subtracts units from current progress. SetAbsolute: teleports to a fixed position in the stage.")]
        public ProgressModifierMode mode = ProgressModifierMode.Flat;

        [Tooltip("Amount to apply. Flat: signed delta (negative = backward). SetAbsolute: target position (clamped to stage length).")]
        public int amount;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            if (mode == ProgressModifierMode.Flat)
            {
                target.ModifyProgress(amount);
            }
            else
            {
                target.SetProgress(amount);
            }
        }
    }
}
