using Assets.Scripts.Entities.Vehicle;

namespace Assets.Scripts.Combat.Rolls
{
    /// <summary>
    /// Sealed hierarchy representing who or what makes a roll.
    /// Eliminates mutually exclusive nullable (Entity, Character) pairs throughout the roll pipeline.
    /// </summary>
    public abstract class RollActor
    {
        public abstract Entity GetEntity();
        public abstract VehicleSeat GetSeat();
        public abstract Vehicle GetVehicle();
    }

    /// <summary>A vehicle component making the roll (vehicle checks, vehicle saves).</summary>
    public sealed class ComponentActor : RollActor
    {
        public readonly Entity Component;

        public ComponentActor(Entity component) { Component = component; }

        public override Entity GetEntity() => Component;
        public override VehicleSeat GetSeat() => null;
        public override Vehicle GetVehicle() => EntityHelpers.GetParentVehicle(Component);
    }

    /// <summary>A character making the roll without a specific tool (character-only checks).</summary>
    public sealed class CharacterActor : RollActor
    {
        public readonly VehicleSeat Seat;

        public CharacterActor(VehicleSeat seat) { Seat = seat; }

        public override Entity GetEntity() => null;
        public override VehicleSeat GetSeat() => Seat;
        public override Vehicle GetVehicle()
        {
            if (Seat == null || Seat.controlledComponents.Count == 0) return null;
            return EntityHelpers.GetParentVehicle(Seat.controlledComponents[0]);
        }
    }

    /// <summary>A character making the roll through a tool component (e.g. gunner via weapon, engineer via power core).</summary>
    public sealed class CharacterWithToolActor : RollActor
    {
        public readonly VehicleSeat Seat;
        public readonly Entity Tool;

        public CharacterWithToolActor(VehicleSeat seat, Entity tool) { Seat = seat; Tool = tool; }

        public override Entity GetEntity() => Tool;
        public override VehicleSeat GetSeat() => Seat;
        public override Vehicle GetVehicle() => EntityHelpers.GetParentVehicle(Tool);
    }
}
