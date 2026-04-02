using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Entities.Vehicles;
using SerializeReferenceEditor;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to the context vehicle — derived from <c>SourceActor?.GetVehicle()</c>
    /// or falling back to <c>Target as Vehicle</c>.
    /// Replaces <c>CardTargetMode.DrawingVehicle</c> and <c>LaneManager</c> per-vehicle hardcoding.
    /// </summary>
    [Serializable]
    [SRName("Context Vehicle")]
    public class ContextVehicleResolver : IRollTargetResolver
    {
        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            Vehicle vehicle = ctx.SourceActor?.GetVehicle();
            if (vehicle == null) vehicle = ctx.Target as Vehicle;
            if (vehicle != null)
                return new IRollTarget[] { vehicle };

            return System.Array.Empty<IRollTarget>();
        }
    }
}
