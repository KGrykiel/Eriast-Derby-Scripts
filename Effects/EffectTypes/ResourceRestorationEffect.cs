using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// Restores or drains Health/Energy.
    /// When targeting a Vehicle, routes to the correct component: Health→chassis, Energy→power core.
    /// Uses RestorationFormula for dice-based or flat amounts, evaluated by RestorationCalculator.
    /// </summary>
    [System.Serializable]
    [SRName("Resource Restoration")]
    public class ResourceRestorationEffect : EffectBase
    {
        [Header("Restoration Configuration")]
        [Tooltip("Formula defining resource type, dice, and bonus for restoration/drain")]
        public RestorationFormula formula = new();

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            Entity entity = ResolveEntity(target);
            if (entity == null) return;

            int amount = RestorationCalculator.Roll(formula);
            RestorationApplicator.Apply(formula, amount, entity, context.SourceActor, context.CausalSource);
        }

        protected override Entity ResolveEntity(IEffectTarget target)
        {
            switch (target)
            {
                case Entity e:
                    return e;
                case Vehicle vehicle:
                    if (formula.resourceType == ResourceType.Energy && vehicle.powerCore != null)
                        return vehicle.powerCore;
                    return vehicle.chassis;
                case VehicleSeat:
                    Debug.LogWarning($"[{GetType().Name}] VehicleSeat is not a valid target for this effect.");
                    return null;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unsupported target type: {(target != null ? target.GetType().Name : "null")}");
                    return null;
            }
        }
    }
}

