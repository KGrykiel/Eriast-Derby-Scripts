using Assets.Scripts.Skills;
using System.Collections.Generic;

namespace Assets.Scripts.Managers.Selection
{
    /// <summary>
    /// Maps each <see cref="TargetingMode"/> to its <see cref="ITargetingStrategy"/>.
    /// </summary>
    public static class TargetingSequences
    {
        public static readonly Dictionary<TargetingMode, ITargetingStrategy> Strategies =
            new()
            {
            { TargetingMode.Self,            new SelfStrategy() },
            { TargetingMode.OwnLane,         new OwnLaneStrategy() },
            { TargetingMode.Enemy,           new EnemyVehicleStrategy() },
            { TargetingMode.EnemyComponent,  new EnemyComponentStrategy() },
            { TargetingMode.Any,             new AnyVehicleStrategy() },
            { TargetingMode.AnyComponent,    new AnyComponentStrategy() },
            { TargetingMode.ComponentOnSelf, new SourceComponentStrategy() },
            { TargetingMode.Lane,            new LaneStrategy() },
        };
    }
}
