using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves <see cref="Entity"/> targets for an <see cref="EntityEffectInvocation"/>.
    /// The invocation loops the resolved entities through <c>IEntityEffect.Apply</c>.
    /// </summary>
    public interface IEntityEffectResolver
    {
        IReadOnlyList<Entity> Resolve(RollContext ctx);
    }

    /// <summary>
    /// Resolves <see cref="VehicleSeat"/> targets for a <see cref="SeatEffectInvocation"/>.
    /// The invocation loops the resolved seats through <c>ISeatEffect.Apply</c>.
    /// </summary>
    public interface ISeatEffectResolver
    {
        IReadOnlyList<VehicleSeat> Resolve(RollContext ctx);
    }

    /// <summary>
    /// Resolves <see cref="Vehicle"/> targets for a <see cref="VehicleEffectInvocation"/>.
    /// The invocation loops the resolved vehicles through <c>IVehicleEffect.Apply</c>.
    /// </summary>
    public interface IVehicleEffectResolver
    {
        IReadOnlyList<Vehicle> Resolve(RollContext ctx);
    }

    /// <summary>
    /// Selects whether a resolver targets the source vehicle or the target vehicle.
    /// </summary>
    public enum VehicleSource
    {
        TargetVehicle,
        SourceVehicle
    }
}
