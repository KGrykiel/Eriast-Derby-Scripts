using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;

namespace Assets.Scripts.Effects.Targeting
{
    /// <summary>
    /// Resolves which <see cref="IEffectTarget"/> instances an effect should be applied to,
    /// given the current execution context.
    /// Fully independent of <c>ITargetResolver</c> (the roll fan-out layer).
    /// </summary>
    public interface IEffectTargetResolver
    {
        IReadOnlyList<IEffectTarget> Resolve(RollContext ctx);
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
