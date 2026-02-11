namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Strategy interface for resolving damage formulas.
    /// 
    /// PATTERN: Strategy Pattern
    /// - Allows different formula resolution strategies without modifying DamageEffect
    /// - Each provider extracts only the context data it needs
    /// - Decouples weapon integration from general-purpose damage effects
    /// 
    /// HOW TO EXTEND:
    /// 1. Create a new class implementing IFormulaProvider
    /// 2. Add [System.Serializable] attribute for Unity serialization
    /// 3. Implement GetFormula() to extract needed data from context
    /// 4. If you need additional context data, extend FormulaContext (existing providers won't break)
    /// 
    /// EXAMPLES:
    /// - StaticFormulaProvider: Ignores context, returns fixed formula
    /// - WeaponFormulaProvider: Extracts weapon stats from context.Weapon
    /// - Future: ConditionalFormulaProvider could extract context.Target
    /// - Future: AmmunitionFormulaProvider could extract context.Weapon.GetLoadedAmmunition()
    /// </summary>
    public interface IFormulaProvider
    {
        /// <summary>
        /// Resolve a damage formula based on context.
        /// Implementations extract only the data they need from context.
        /// </summary>
        DamageFormula GetFormula(FormulaContext context);
    }
}
