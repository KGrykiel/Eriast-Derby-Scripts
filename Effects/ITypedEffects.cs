using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Effect that applies to <see cref="Entity"/> targets.
    /// Receives the final resolved entity — no routing required.
    /// </summary>
    public interface IEntityEffect
    {
        void Apply(Entity target, EffectContext context);
    }

    /// <summary>
    /// Effect that applies to <see cref="VehicleSeat"/> targets.
    /// Receives the final resolved seat — no routing required.
    /// </summary>
    public interface ISeatEffect
    {
        void Apply(VehicleSeat target, EffectContext context);
    }

    /// <summary>
    /// Effect that applies to <see cref="Vehicle"/> targets.
    /// Receives the final resolved vehicle — no routing required.
    /// </summary>
    public interface IVehicleEffect
    {
        void Apply(Vehicle target, EffectContext context);
    }
}
