using UnityEngine;
using Assets.Scripts.Combat.Damage;
using SerializeReferenceEditor;
using Assets.Scripts.Combat.Damage.FormulaProviders;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using Assets.Scripts.Entities;

namespace Assets.Scripts.Effects.EffectTypes
{
    /// <summary>
    /// The effect for applying damage to an entity target.
    /// Uses a formula provider to determine the damage formula, which is then processed by the DamageCalculator and applied via the DamageApplicator.
    /// </summary>
    [System.Serializable]
    [SRName("Damage")]
    public class DamageEffect : EffectBase
    {
        [SerializeReference, SR]
        [Tooltip("Strategy for resolving damage formula. StaticFormulaProvider for fixed damage, WeaponFormulaProvider for weapon-based attacks.")]
        public IFormulaProvider formulaProvider = new StaticFormulaProvider();

        public override void Apply(IEffectTarget target, EffectContext context)
        {
            Entity entity = ResolveEntity(target);
            if (entity == null) return;

            Entity user = context.SourceActor?.GetEntity();
            var formulaContext = new FormulaContext(user);
            DamageFormula formula = formulaProvider.GetFormula(formulaContext);

            ResistanceLevel resistance = entity.GetResistance(formula.damageType);
            DamageResult result = DamageCalculator.Compute(formula, resistance, context.IsCriticalHit);

            DamageApplicator.Apply(
                result: result,
                target: entity,
                actor: context.SourceActor,
                causalSource: context.CausalSource ?? user?.name
            );
        }
    }
}