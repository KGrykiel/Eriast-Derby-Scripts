using System;
using System.Collections.Generic;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Entities;
using Assets.Scripts.Stages.Lanes;
using SerializeReferenceEditor;

namespace Assets.Scripts.Combat.Rolls.Targeting
{
    /// <summary>
    /// Resolves to a specific pre-configured <see cref="StageLane"/>.
    /// Used for bombardment and other effects that target a lane directly.
    /// </summary>
    [Serializable]
    [SRName("Specific Lane")]
    public class SpecificLaneResolver : IRollTargetResolver
    {
        public StageLane Lane;

        public SpecificLaneResolver() { }
        public SpecificLaneResolver(StageLane lane) { Lane = lane; }

        public IReadOnlyList<IRollTarget> ResolveFrom(RollContext ctx)
        {
            if (Lane != null)
                return new IRollTarget[] { Lane };

            return System.Array.Empty<IRollTarget>();
        }
    }
}
