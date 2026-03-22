namespace Assets.Scripts.Combat.Rolls.RollSpecs
{
    /// <summary>
    /// Context struct to hold any relevant information about the action being executed.
    /// Used by skills, event cards, lane effects, and any future action sources.
    /// Most fields optional — only SourceVehicle is required.
    /// </summary>
    public struct RollContext
    {
        public Vehicle SourceVehicle;
        public Entity TargetEntity;
        public RollActor SourceActor;
    }
}
