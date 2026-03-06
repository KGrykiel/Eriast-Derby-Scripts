namespace Assets.Scripts.Combat.RollSpecs
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
        public Entity SourceEntity;
        public Character SourceCharacter;
        public bool IsCriticalHit;

        public readonly Vehicle TargetVehicle => TargetEntity is VehicleComponent comp ? comp.ParentVehicle : null;
        public readonly VehicleComponent SourceComponent => SourceEntity as VehicleComponent;
        public readonly VehicleComponent TargetComponent => TargetEntity as VehicleComponent;

        /// <summary>
        /// Helper to retarget. Used to handle component attack fallback.
        /// </summary>
        public readonly RollContext WithTarget(Entity newTarget)
        {
            var copy = this;
            copy.TargetEntity = newTarget;
            return copy;
        }

        /// <summary>
        /// Information about critical hits only available after resolution, so this is a helper to create a new context with the critical hit information.
        /// </summary>
        public readonly RollContext WithCriticalHit(bool isCrit)
        {
            var copy = this;
            copy.IsCriticalHit = isCrit;
            return copy;
        }
    }
}
