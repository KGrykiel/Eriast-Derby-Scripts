using Assets.Scripts.Consumables;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.VehicleEffects
{
    [System.Serializable]
    [SRName("Restore Consumable")]
    public class RestoreConsumableEffect : IVehicleEffect
    {
        [Header("Restore Configuration")]
        [Tooltip("Which consumable type to restore charges for.")]
        public ConsumableBase targetConsumable;

        [Tooltip("Number of charges to restore.")]
        public int amount = 1;

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            if (targetConsumable == null)
            {
                Debug.LogWarning($"[RestoreConsumableEffect] targetConsumable is not set.");
                return;
            }

            target.RestoreConsumable(targetConsumable, amount, context.CausalSource);
        }
    }
}
