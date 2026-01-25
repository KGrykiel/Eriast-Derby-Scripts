namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Minimal context for skill execution.
    /// Bundles all data needed for skill resolution, effect application, and logging.
    /// 
    /// Design principles:
    /// - Store only what can't be derived
    /// - SourceVehicle explicit (needed for power consumption, can't derive from null SourceEntity)
    /// - TargetVehicle derived (target is always an Entity, may not be vehicle-related)
    /// - Character optional (some skills come from components alone)
    /// 
    /// Replaces: (Skill, VehicleComponent, VehicleComponent) parameter pattern
    /// </summary>
    public struct SkillContext
    {
        // ===== REQUIRED (can't be derived) =====
        
        /// <summary>
        /// The skill being executed.
        /// </summary>
        public Skill Skill;
        
        /// <summary>
        /// Vehicle that pays power cost and owns the action.
        /// Required because SourceEntity can be null for character-only skills.
        /// </summary>
        public Vehicle SourceVehicle;
        
        /// <summary>
        /// Entity receiving the effects. Can be any Entity (VehicleComponent, Prop, NPC).
        /// </summary>
        public Entity TargetEntity;
        
        // ===== OPTIONAL (nullable) =====
        
        /// <summary>
        /// Component using the skill. Null for character-only personal skills.
        /// </summary>
        public Entity SourceEntity;
        
        /// <summary>
        /// Character providing bonuses (attack bonus, skill bonuses).
        /// Null for component-only skills or environmental effects.
        /// </summary>
        public PlayerCharacter SourceCharacter;
        
        // ===== COMBAT STATE (populated by resolvers) =====
        
        /// <summary>
        /// Natural 20 on attack roll - doubles damage dice.
        /// Set by attack resolver before effects are applied.
        /// </summary>
        public bool IsCriticalHit;
        
        // ===== DERIVED HELPERS (computed, not stored) =====
        
        /// <summary>
        /// Target's parent vehicle, if target is a VehicleComponent. Null for Props/NPCs.
        /// </summary>
        public Vehicle TargetVehicle => (TargetEntity as VehicleComponent)?.ParentVehicle;
        
        /// <summary>
        /// Source as VehicleComponent, if applicable. Null for character-only skills.
        /// </summary>
        public VehicleComponent SourceComponent => SourceEntity as VehicleComponent;
        
        /// <summary>
        /// Target as VehicleComponent, if applicable. Null for Props/NPCs.
        /// </summary>
        public VehicleComponent TargetComponent => TargetEntity as VehicleComponent;
        
        // ===== IMMUTABLE COPY METHODS =====

        /// <summary>
        /// Create a copy with a different target. Used for chassis fallback in two-stage attacks.
        /// </summary>
        public SkillContext WithTarget(Entity newTarget)
        {
            var copy = this;
            copy.TargetEntity = newTarget;
            return copy;
        }

        /// <summary>
        /// Create a copy with IsCriticalHit set. Used by resolvers after roll.
        /// </summary>
        public SkillContext WithCriticalHit(bool isCrit)
        {
            var copy = this;
            copy.IsCriticalHit = isCrit;
            return copy;
        }
    }
}
