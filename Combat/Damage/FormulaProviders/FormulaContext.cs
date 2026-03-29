using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles.VehicleComponents.ComponentTypes;

namespace Assets.Scripts.Combat.Damage
{
    /// <summary>
    /// Context object containing information needed to resolve damage formulas.
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
