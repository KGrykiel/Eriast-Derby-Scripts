using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.VehicleComponents
{
    /// <summary>
    /// Defines how exposed/accessible a component is for targeting in combat.
    /// Determines whether a component can be directly targeted or requires special conditions.
    /// </summary>
    public enum ComponentExposure
    {
        /// <summary>
        /// Fully exposed, easy to target (weapons, sensors, external armor).
        /// Can be targeted without restrictions.
        /// </summary>
        External,
        
        /// <summary>
        /// Protected by armor or shielding components.
        /// Can only be targeted if shielding component is destroyed or using penetrating attacks.
        /// </summary>
        Protected,
        
        /// <summary>
        /// Deep inside the vehicle (power core, critical systems).
        /// Requires chassis damage (below 50% HP) or special abilities to access.
        /// </summary>
        Internal,
        
        /// <summary>
        /// Actively shielded by specific defensive components.
        /// Cannot be targeted while shield component is active.
        /// </summary>
        Shielded
    }
}
