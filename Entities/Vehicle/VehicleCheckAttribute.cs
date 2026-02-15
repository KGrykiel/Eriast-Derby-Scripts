namespace Assets.Scripts.Entities.Vehicle
{
    /// <summary>Constrained subset of Attribute for d20 checks/saves. Prevents rolling against MaxHealth, etc.</summary>
    public enum VehicleCheckAttribute
    {
        /// <summary>Dodging, evasion, reaction speed. Component: Chassis.</summary>
        Mobility,
        
        /// <summary>Holding course, resisting knockback/spin. Component: Drive.</summary>
        Stability,
        
        // Integrity
    }
    
    public static class VehicleCheckAttributeHelper
    {
        /// <summary>
        /// quick conversions between VehicleCheckAttribute and Attribute. Attribute is the enum for gathering modifiers.
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
