namespace Assets.Scripts.Entities
{
    /// <summary>
    /// Marker interface for any valid target of a roll or effect.
    /// Implemented by Vehicle (whole-vehicle targeting), Entity (component targeting),
    /// StageLane (lane-wide targeting), and VehicleSeat (character/seat targeting).
    /// </summary>
    public interface IRollTarget { }
}
