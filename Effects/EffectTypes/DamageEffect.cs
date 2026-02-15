using UnityEngine;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Effects;
using SerializeReferenceEditor;

/// <summary>
/// The effect for applying damage to an entity target.
/// Uses a formula provider to determine the damage formula, which is then processed by the DamageCalculator and applied via the DamageApplicator.
/// </summary>
[System.Serializable]
public class DamageEffect : EffectBase
{
    [SerializeReference, SR]
    [Tooltip("Strategy for resolving damage formula. StaticFormulaProvider for fixed damage, WeaponFormulaProvider for weapon-based attacks.")]
    public IFormulaProvider formulaProvider = new StaticFormulaProvider();

    public override void Apply(Entity user, Entity target, EffectContext context, Object source = null)
    {
        var formulaContext = new FormulaContext(user);
        DamageFormula formula = formulaProvider.GetFormula(formulaContext);

        ResistanceLevel resistance = target.GetResistance(formula.damageType);
        DamageResult result = DamageCalculator.Compute(formula, resistance, context.IsCriticalHit);
        DamageSource sourceType = user is WeaponComponent ? DamageSource.Weapon : DamageSource.Ability;

        DamageApplicator.Apply(
            result: result,
            target: target,
            attacker: user,
            causalSource: source != null ? source : user,
            sourceType: sourceType
        );
    }
}
