namespace Assets.Scripts.Combat.Damage.FormulaProviders
{
    /// <summary>
    /// Strategy interface for resolving damage formulas.
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
