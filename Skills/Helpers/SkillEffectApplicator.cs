using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Skills.Helpers
{
    /// <summary>
    /// Applies effects to targets and tracks damage breakdowns.
    /// Handles target resolution and effect routing.
    /// </summary>
    public static class SkillEffectApplicator
    {
        /// <summary>
        /// Applies all effects to their targets and tracks damage breakdowns.
        /// Returns dictionary of damage breakdowns by target entity.
        /// </summary>
        public static Dictionary<Entity, List<DamageBreakdown>> ApplyAllEffects(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponentOverride = null)
        {
            var damageByTarget = new Dictionary<Entity, List<DamageBreakdown>>();
            
            foreach (var invocation in skill.effectInvocations)
            {
                if (invocation.effect == null) continue;
                
                // Get actual target vehicles for this effect based on target mode
                List<Vehicle> targetVehicles = BuildTargetVehicleList(
                    invocation.targetMode, user, mainTarget);
                
                foreach (var targetVehicle in targetVehicles)
                {
                    // Let the vehicle route the effect to the correct component
                    Entity routedTarget = targetVehicle.RouteEffectTarget(
                        invocation.effect, 
                        targetComponentOverride ?? targetVehicle.chassis);
                    
                    // DEBUG: Log which target we're applying to
                    Debug.Log($"[Skill] Applying {invocation.effect.GetType().Name} to {routedTarget.GetDisplayName()} on {targetVehicle.vehicleName} (targetMode: {invocation.targetMode})");
                    
                    // Apply effect - source component, routed target, weapon for context, skill for metadata
                    invocation.effect.Apply(
                        sourceComponent ?? user.chassis,  // Source (weapon, power core, or chassis fallback)
                        routedTarget,                      // Routed target
                        sourceComponent as WeaponComponent, // Weapon context (null if not a weapon)
                        skill);                            // Skill metadata
                    
                    // Track damage breakdowns for DamageEffect
                    if (invocation.effect is DamageEffect damageEffect && damageEffect.LastBreakdown != null)
                    {
                        if (!damageByTarget.ContainsKey(routedTarget))
                        {
                            damageByTarget[routedTarget] = new List<DamageBreakdown>();
                        }
                        damageByTarget[routedTarget].Add(damageEffect.LastBreakdown);
                        
                        // DEBUG: Log damage tracking
                        Debug.Log($"[Skill] Tracked {damageEffect.LastBreakdown.finalDamage} damage to {targetVehicle.vehicleName}");
                    }
                }
            }
            
            return damageByTarget;
        }
        
        /// <summary>
        /// Pre-calculates damage for component-targeted attacks.
        /// Damage is calculated before attack roll for component targeting.
        /// IMPORTANT: Does NOT route through RouteEffectTarget - damage goes to the targeted component directly!
        /// </summary>
        public static Dictionary<Entity, List<DamageBreakdown>> PreCalculateDamage(
            Skill skill,
            Vehicle user,
            Vehicle mainTarget,
            VehicleComponent sourceComponent,
            VehicleComponent targetComponent)
        {
            var damageByTarget = new Dictionary<Entity, List<DamageBreakdown>>();
            WeaponComponent weapon = sourceComponent as WeaponComponent;
            
            foreach (var invocation in skill.effectInvocations)
            {
                if (invocation.effect is DamageEffect damageEffect)
                {
                    // Get actual target vehicles based on targetMode
                    List<Vehicle> targetVehicles = BuildTargetVehicleList(
                        invocation.targetMode, user, mainTarget);
                    
                    foreach (var targetVehicle in targetVehicles)
                    {
                        // COMPONENT TARGETING: Use the exact component specified, DO NOT route!
                        // Player explicitly targeted this component, so damage goes there.
                        Entity target = targetComponent;
                        
                        var breakdown = damageEffect.formula.ComputeDamageWithBreakdown(weapon);
                        if (breakdown != null && breakdown.rawTotal > 0)
                        {
                            // Apply damage to the correct target
                            DamagePacket packet = DamagePacket.Create(
                                breakdown.rawTotal, 
                                breakdown.damageType, 
                                sourceComponent ?? user.chassis);
                            int resolved = DamageResolver.ResolveDamage(packet, target);
                            target.TakeDamage(resolved);
                            
                            // Update breakdown with actual resistance info from target
                            ResistanceLevel resistance = target.GetResistance(breakdown.damageType);
                            breakdown.WithResistance(resistance);
                            breakdown.finalDamage = resolved;
                            
                            // Track for logging
                            if (!damageByTarget.ContainsKey(target))
                            {
                                damageByTarget[target] = new List<DamageBreakdown>();
                            }
                            damageByTarget[target].Add(breakdown);
                        }
                    }
                }
            }
            
            return damageByTarget;
        }
        
        /// <summary>
        /// Build the list of target vehicles based on target mode.
        /// Returns vehicles, not entities - let Vehicle.RouteEffectTarget() handle entity resolution.
        /// </summary>
        private static List<Vehicle> BuildTargetVehicleList(
            EffectTargetMode targetMode, 
            Vehicle user, 
            Vehicle mainTarget)
        {
            List<Vehicle> targets = new List<Vehicle>();
            
            switch (targetMode)
            {
                case EffectTargetMode.User:
                    targets.Add(user);
                    break;
                case EffectTargetMode.Target:
                    targets.Add(mainTarget);
                    break;
                case EffectTargetMode.Both:
                    targets.Add(user);
                    targets.Add(mainTarget);
                    break;
                case EffectTargetMode.AllInStage:
                    if (user.currentStage != null && user.currentStage.vehiclesInStage != null)
                    {
                        foreach (var vehicle in user.currentStage.vehiclesInStage)
                        {
                            if (vehicle != user && vehicle.Status == VehicleStatus.Active)
                            {
                                targets.Add(vehicle);
                            }
                        }
                    }
                    break;
            }
            
            return targets;
        }
        
        /// <summary>
        /// DEPRECATED: Old method that resolved to entities too early.
        /// Kept for backward compatibility - redirects to new implementation.
        /// </summary>
        [System.Obsolete("Use BuildTargetVehicleList instead - returns Vehicles not Entities")]
        public static List<Entity> BuildTargetList(EffectTargetMode targetMode, Entity user, Entity mainTarget, Stage context)
        {
            // This should not be used anymore, but keep for compilation
            Debug.LogWarning("[SkillEffectApplicator] BuildTargetList is deprecated - use BuildTargetVehicleList");
            return new List<Entity>();
        }
    }
}
