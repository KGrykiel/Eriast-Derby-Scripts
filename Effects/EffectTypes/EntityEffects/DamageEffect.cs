using UnityEngine;
using Assets.Scripts.Combat.Damage;
using SerializeReferenceEditor;
using Assets.Scripts.Combat.Damage.FormulaProviders;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Effects.EffectTypes.EntityEffects
{
    /// <summary>
    /// Applies damage to an entity target.
    /// When used in a VehicleEffectInvocation, defaults to the chassis as the vehicle's
    /// structural representative. All components have independent health pools; use an
    /// EntityEffectInvocation with a specific resolver to target a different component.
    /// </summary>
    [System.Serializable]
    [SRName("Damage")]
    public class DamageEffect : IEntityEffect, IVehicleEffect
    {
        [SerializeReference, SR]
        [Tooltip("Strategy for resolving damage formula. StaticFormulaProvider for fixed damage, WeaponFormulaProvider for weapon-based attacks.")]
        public IFormulaProvider formulaProvider = new StaticFormulaProvider();

        void IEntityEffect.Apply(Entity target, EffectContext context)
        {
            Entity user = context.SourceActor?.GetEntity();
            var formulaContext = new FormulaContext(user);
            DamageFormula formula = formulaProvider.GetFormula(formulaContext);

            ResistanceLevel resistance = target.GetResistance(formula.damageType);

            // Critical hits only apply to effects targeting enemies, not self-harm (recoil, backlash, etc.).
            // A crit is a property of the attack roll against the opponent — it doesn't double the shooter's own damage.
            Vehicle sourceVehicle = context.SourceActor?.GetVehicle();
            Vehicle targetVehicle = EntityHelpers.GetParentVehicle(target);
            bool isTargetingSelf = sourceVehicle != null && sourceVehicle == targetVehicle;
            bool isCrit = context.IsCriticalHit && !isTargetingSelf;

            DamageResult result = DamageCalculator.Compute(formula, resistance, isCrit);

            DamageApplicator.Apply(
                result: result,
                target: target,
                actor: context.SourceActor,
                causalSource: context.CausalSource
            );
        }

        void IVehicleEffect.Apply(Vehicle target, EffectContext context)
        {
            Entity entity = target.GetDefaultTargetForResource(ResourceType.Health);
            if (entity == null) return;
            ((IEntityEffect)this).Apply(entity, context);
        }
    }
}