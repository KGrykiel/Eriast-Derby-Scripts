using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Skills.Costs
{
    /// <summary>
    /// Represents a single resource requirement for using a skill.
    /// All costs on a skill are checked before any are paid.
    /// </summary>
    public interface ISkillCost
    {
        /// <summary>Returns true if this cost can currently be paid from the given vehicle's resources.</summary>
        bool CanPay(Vehicle vehicle);

        /// <summary>Deducts this cost from the vehicle's resources. Only called after CanPay returns true.</summary>
        void Pay(Vehicle vehicle, RollContext ctx);

        /// <summary>Short label for UI display and debug logging (e.g. "2 EN", "1x Nitro Fuel").</summary>
        string GetDescription();
    }
}
