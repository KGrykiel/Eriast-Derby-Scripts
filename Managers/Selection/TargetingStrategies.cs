using Assets.Scripts.Managers.PlayerUI;

namespace Assets.Scripts.Managers.Selection
{
    /// <summary>Resolves to the source vehicle immediately — no UI.</summary>
    public class SelfStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            ctx.OnComplete?.Invoke(ctx.SourceVehicle);
        }
    }

    /// <summary>Resolves to the source vehicle's current lane immediately — no UI.</summary>
    public class OwnLaneStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            ctx.OnComplete?.Invoke(RacePositionTracker.GetLane(ctx.SourceVehicle));
        }
    }

    /// <summary>Shows the enemy vehicle selector.</summary>
    public class EnemyVehicleStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            var options = TargetOptionBuilder.VehicleOptions(
                ctx.TurnController.GetOtherVehiclesInStage(ctx.SourceVehicle));

            ctx.TargetSelection.Show(options, vehicle =>
            {
                ctx.TargetSelection.Hide();
                ctx.OnComplete?.Invoke(vehicle);
            });
        }
    }

    /// <summary>Shows the enemy vehicle selector, then the component selector for the chosen vehicle.</summary>
    public class EnemyComponentStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            var vehicleOptions = TargetOptionBuilder.VehicleOptions(
                ctx.TurnController.GetOtherVehiclesInStage(ctx.SourceVehicle));

            ctx.TargetSelection.Show(vehicleOptions, vehicle =>
            {
                ctx.TargetSelection.Hide();

                var componentOptions = TargetOptionBuilder.ComponentOptions(vehicle, sourceOnly: false);
                ctx.TargetSelection.Show(componentOptions, component =>
                {
                    ctx.TargetSelection.Hide();
                    ctx.OnComplete?.Invoke(component);
                });
            });
        }
    }

    /// <summary>Shows all vehicles in stage for selection.</summary>
    public class AnyVehicleStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            var options = TargetOptionBuilder.VehicleOptions(
                ctx.TurnController.GetAllTargets(ctx.SourceVehicle));

            ctx.TargetSelection.Show(options, vehicle =>
            {
                ctx.TargetSelection.Hide();
                ctx.OnComplete?.Invoke(vehicle);
            });
        }
    }

    /// <summary>Shows all vehicles in stage, then the component selector for the chosen vehicle.</summary>
    public class AnyComponentStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            var vehicleOptions = TargetOptionBuilder.VehicleOptions(
                ctx.TurnController.GetAllTargets(ctx.SourceVehicle));

            ctx.TargetSelection.Show(vehicleOptions, vehicle =>
            {
                ctx.TargetSelection.Hide();

                var componentOptions = TargetOptionBuilder.ComponentOptions(vehicle, sourceOnly: false);
                ctx.TargetSelection.Show(componentOptions, component =>
                {
                    ctx.TargetSelection.Hide();
                    ctx.OnComplete?.Invoke(component);
                });
            });
        }
    }

    /// <summary>Shows the source vehicle's own component selector.</summary>
    public class SourceComponentStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            var options = TargetOptionBuilder.ComponentOptions(ctx.SourceVehicle, sourceOnly: true);

            ctx.TargetSelection.Show(options, component =>
            {
                ctx.TargetSelection.Hide();
                ctx.OnComplete?.Invoke(component);
            });
        }
    }

    /// <summary>Shows the lane selector for the source vehicle's current stage.</summary>
    public class LaneStrategy : ITargetingStrategy
    {
        public void Execute(SelectionContext ctx)
        {
            var stage = RacePositionTracker.GetStage(ctx.SourceVehicle);
            if (stage == null)
            {
                ctx.TargetSelection.Hide();
                return;
            }

            var options = TargetOptionBuilder.LaneOptions(stage.lanes);

            ctx.TargetSelection.Show(options, lane =>
            {
                ctx.TargetSelection.Hide();
                ctx.OnComplete?.Invoke(lane);
            });
        }
    }
}
