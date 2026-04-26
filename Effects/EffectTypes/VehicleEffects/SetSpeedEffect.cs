using Assets.Scripts.Combat;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    /// <summary>Sets the target speed percentage on the vehicle's drive component.</summary>
    [System.Serializable]
    [SRName("Set Speed")]
    public class SetSpeedEffect : IVehicleEffect
    {
        [Tooltip("Target speed as a percentage of max speed (0 = stop, 100 = full speed).")]
        [Range(0, 100)]
        public int targetSpeedPercent = 100;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            int oldSpeedPercent = target.Drive != null ? target.Drive.GetTargetSpeedPercent() : 0;
            target.SetTargetSpeed(targetSpeedPercent);
            CombatEventBus.Emit(new SpeedChangeEvent(target, oldSpeedPercent, targetSpeedPercent, context.CausalSource));
        }
    }
}
