namespace Assets.Scripts.AI.Perception
{
    /// <summary>
    /// Perception module. Reads the shared context and answers one question as a
    /// single normalised 0..1 signal. Trackers are personality-agnostic and must
    /// have no side effects.
    /// </summary>
    public interface ITracker
    {
        float Evaluate(VehicleAISharedContext context);
    }
}
