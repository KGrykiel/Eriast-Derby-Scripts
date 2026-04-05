using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;
using UnityEngine;

namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    /// <summary>
    /// Restores or drains Health/Energy on a target entity.
    /// When used in a VehicleEffectInvocation, routes automatically: Energy goes to the
    /// power core, Health defaults to the chassis as the structural representative.
    /// Uses RestorationFormula for dice-based or flat amounts, evaluated by RestorationCalculator.
    /// </summary>
    [System.Serializable]
    [SRName("Resource Restoration")]
    public class ResourceRestorationEffect : IEntityEffect, IVehicleEffect
    {
        [Header("Restoration Configuration")]
        [Tooltip("Formula defining resource type, dice, and bonus for restoration/drain")]
        public RestorationFormula formula = new();

        void IEntityEffect.Apply(Entity target, EffectContext context)
        {
            int amount = RestorationCalculator.Compute(formula);
            RestorationApplicator.Apply(formula, amount, target, context.SourceActor, context.CausalSource);
        }

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            Entity entity = target.GetDefaultTargetForResource(formula.resourceType);
            if (entity == null) return;
            ((IEntityEffect)this).Apply(entity, context);
        }
    }
}

