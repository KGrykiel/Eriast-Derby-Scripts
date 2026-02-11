namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Context object containing information needed to resolve damage formulas.
    /// Providers extract only the data they need.
    /// 
    /// YAGNI: Only contains fields needed by current providers.
    /// Extend as new provider types require additional context.
    /// </summary>
    public class FormulaContext
    {
        /// <summary>Entity using this effect (may be null for environmental damage)</summary>
        public Entity User { get; }

        /// <summary>Weapon component if user is a weapon (cached for convenience)</summary>
        public WeaponComponent Weapon { get; }

        public FormulaContext(Entity user)
        {
            User = user;
            Weapon = user as WeaponComponent;
        }
    }
}
