using UnityEngine;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Effects;
using SerializeReferenceEditor;

/// <summary>
/// Damage effect for skills and event cards.
/// Uses DamageCalculator to compute damage, then DamageApplicator to apply it.
/// 
/// Formula resolution is delegated to IFormulaProvider (Strategy Pattern).
/// This keeps DamageEffect general-purpose - weapon integration is handled by WeaponFormulaProvider.
/// 
/// Composite damage (weapon + extra): use two DamageEffects on the same skill.
/// CombatEventBus aggregates them naturally.
/// </summary>
[System.Serializable]
public class DamageEffect : EffectBase
{
    [SerializeReference, SR]
    [Tooltip("Strategy for resolving damage formula. StaticFormulaProvider for fixed damage, WeaponFormulaProvider for weapon-based attacks.")]
    public IFormulaProvider formulaProvider = new StaticFormulaProvider();

    public override void Apply(Entity user, Entity target, EffectContext context, Object source = null)
    {
        // Build context and resolve formula
        var formulaContext = new FormulaContext(user);
        DamageFormula formula = formulaProvider.GetFormula(formulaContext);

        // Resolve resistance from target
        ResistanceLevel resistance = target.GetResistance(formula.damageType);

        // Calculate damage
        DamageResult result = DamageCalculator.Compute(formula, resistance, context.IsCriticalHit);

        // Determine source type
        DamageSource sourceType = user is WeaponComponent ? DamageSource.Weapon : DamageSource.Ability;

        // Apply damage
        DamageApplicator.Apply(
            result: result,
            target: target,
            attacker: user,
            causalSource: source != null ? source : user,
            sourceType: sourceType
        );
    }
}
