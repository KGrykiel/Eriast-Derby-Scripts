namespace Assets.Scripts.Effects
{
    /// <summary>
    /// Marker interface for anything that can receive an effect.
    /// Distinct from IRollTarget (selection/targeting) — not everything targetable can receive effects
    /// (Vehicle routes to Entity, StageLane expands to vehicles) and not everything that receives
    /// effects is directly targetable (VehicleSeat receives character conditions).
    /// </summary>
    public interface IEffectTarget { }
}
