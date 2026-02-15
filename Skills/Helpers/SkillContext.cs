namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Context struct to hold any relevant information about the skill.
    /// Most fields optional.
    /// </summary>
    public struct SkillContext
    {
        public Skill Skill;
        public Vehicle SourceVehicle;
        public Entity TargetEntity;
        public Entity SourceEntity;
        public Character SourceCharacter;
        public bool IsCriticalHit;

        public readonly Vehicle TargetVehicle => (TargetEntity as VehicleComponent)?.ParentVehicle;
        public readonly VehicleComponent SourceComponent => SourceEntity as VehicleComponent;
        public readonly VehicleComponent TargetComponent => TargetEntity as VehicleComponent;

        /// <summary>
        /// Helper to retarget. Used to handle component attack fallback.
        /// </summary>
        public readonly SkillContext WithTarget(Entity newTarget)
        {
            var copy = this;
            copy.TargetEntity = newTarget;
            return copy;
        }

        /// <summary>
        /// Information about critical hits only available after resolution, so this is a helper to create a new context with the critical hit information.
        /// </summary>
        public readonly SkillContext WithCriticalHit(bool isCrit)
        {
            var copy = this;
            copy.IsCriticalHit = isCrit;
            return copy;
        }
    }
}
