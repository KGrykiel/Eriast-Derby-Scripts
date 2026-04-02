using Assets.Scripts.Consumables;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    [System.Serializable]
    [SRName("Restore Consumable")]
    public class RestoreConsumableEffect : EffectBase
    {
        [Header("Restore Configuration")]
        [Tooltip("Which consumable type to restore charges for.")]
        public ConsumableBase targetConsumable;

        [Tooltip("Number of charges to restore.")]
        public int amount = 1;

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            Vehicle vehicle = ResolveVehicle(target);
            if (vehicle == null)
            {
                Debug.LogWarning($"[RestoreConsumableEffect] Could not resolve target to a Vehicle.");
                return;
            }

            if (targetConsumable == null)
            {
                Debug.LogWarning($"[RestoreConsumableEffect] targetConsumable is not set.");
                return;
            }

            vehicle.RestoreConsumable(targetConsumable, amount, context.CausalSource);
        }
    }
}
