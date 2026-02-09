namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>
    /// Vehicle attributes that can be used for d20 checks and saving throws.
    /// Constrained subset of the full Attribute enum — prevents designers from
    /// accidentally configuring a skill to roll against MaxHealth or DamageDice.
    /// 
    /// Each value maps to a full Attribute via ToAttribute() for modifier lookup.
    /// </summary>
    public enum VehicleCheckAttribute
    {
        /// <summary>Dodging, evasion, reaction speed. Component: Chassis.</summary>
        Mobility,
        
        /// <summary>Holding course, resisting knockback/spin. Component: Drive.</summary>
        Stability,
        
        // Future: Integrity (structural durability saves), Sensors (detection checks), etc.
    }
    
    public static class VehicleCheckAttributeHelper
    {
        /// <summary>
        /// Convert to the full Attribute enum for modifier/stat lookup.
        /// </summary>
        public static Attribute ToAttribute(this VehicleCheckAttribute checkAttr)
        {
            return checkAttr switch
            {
                VehicleCheckAttribute.Mobility => Attribute.Mobility,
                VehicleCheckAttribute.Stability => Attribute.Stability,
                _ => Attribute.Mobility
            };
        }
    }
}
