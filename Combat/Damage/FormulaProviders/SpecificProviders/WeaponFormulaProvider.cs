using UnityEngine;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Dynamic formula that uses weapon stats at runtime.
    /// Used for: Weapon-based attacks and skills.
    /// 
    /// No configuration needed - automatically reads weapon's dice/bonus/type from context.
    /// Only works when user is a WeaponComponent.
    /// </summary>
    [System.Serializable]
    public class WeaponFormulaProvider : IFormulaProvider
    {
        public DamageFormula GetFormula(FormulaContext context)
        {
            if (context.Weapon == null)
            {
                Debug.LogWarning("[WeaponFormulaProvider] No weapon in context. User must be a WeaponComponent.");
                return new DamageFormula { damageType = DamageType.Physical };
            }

            return context.Weapon.GetDamageFormula();
        }
    }
}
