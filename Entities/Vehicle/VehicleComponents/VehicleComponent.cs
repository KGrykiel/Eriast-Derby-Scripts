using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Assets.Scripts.Entities;
using Assets.Scripts.Core;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;

/// <summary>
/// Base class for all vehicle components.
/// Components ARE Entities - they have HP, AC, and can be damaged/targeted.
/// Components are modular parts that contribute stats, enable roles, and provide skills.
/// 
/// NOTE: Components store raw base values. Calculators handle modifier application.
/// - StatCalculator.GatherDefenseValue() computes AC with all modifiers
/// - Entity.armorClass is the BASE value only
/// 
/// CROSS-COMPONENT MODIFIERS:
/// Components can provide modifiers to OTHER components via providedModifiers list.
/// Example: Advanced Armor provides +2 AC to Chassis.
/// These are applied during vehicle initialization and refreshed when components change state.
/// </summary>
public abstract class VehicleComponent : Entity
{
    [Header("Component Identity")]
    [Tooltip("Category of this component (locked for specific component types)")]
    public ComponentType componentType = ComponentType.Custom;
    
    [Header("Component-Specific Stats")]
    [SerializeField]
    [Tooltip("Component Space (positive = uses space, negative = provides space) (base value before modifiers)")]
    protected int baseComponentSpace = 0;
    
    [SerializeField]
    [Tooltip("Power drawn per turn (0 = passive component, no continuous draw) (base value before modifiers)")]
    protected int basePowerDrawPerTurn = 0;
    
    [Header("Provided Modifiers")]
    [Tooltip("Modifiers this component provides to OTHER components. Used for cross-component bonuses like armor upgrades, boosters, etc.")]
    public List<ComponentModifierData> providedModifiers = new();
    
    [Header("Component Targeting")]
    [Tooltip("How exposed this component is for targeting")]
    public ComponentExposure exposure = ComponentExposure.External;
    
    [Tooltip("Component that shields this one (drag component reference here)")]
    public VehicleComponent shieldedByComponent = null;
    
    [Tooltip("For Internal exposure: Required chassis damage % to access (0-1, e.g., 0.5 = 50% damage)")]
    [Range(0f, 1f)]
    public float internalAccessThreshold = 0.5f;
    
    [Header("Component State")]
    [Tooltip("Has the engineer manually disabled this component? (Does not draw power, cannot use skills, does not provide bonuses)")]
    public bool isManuallyDisabled = false;
    
    [Header("Role Support")]
    [Tooltip("Type of role this component enables. Set to None if component doesn't enable a role.")]
    public RoleType roleType = RoleType.None;
    
    [Header("Skills")]
    [Tooltip("Skills provided by this component (assigned in Inspector)")]
    public List<Skill> componentSkills = new();
    
    // Reference to parent vehicle (set during initialization)
    protected Vehicle parentVehicle;
    
    /// <summary>
    /// Get the parent vehicle this component belongs to.
    /// </summary>
    public Vehicle ParentVehicle => parentVehicle;
    
    // ==================== INITIALIZATION ====================
    
    /// <summary>
    /// Initialize this component with a reference to its parent vehicle.
    /// Called by Vehicle.Awake() after component discovery.
    /// </summary>
    public virtual void Initialize(Vehicle vehicle)
    {
        parentVehicle = vehicle;
        
        // Log component initialization
        if (roleType != RoleType.None)
        {
            Debug.Log($"[Component] {name} initialized on {vehicle.vehicleName}, enables role: {roleType}");
        }
    }
    
    // ==================== STAT ACCESSORS ====================
    // Naming convention:
    // - GetBaseStat() returns raw field value (no modifiers)
    // - GetStat() returns effective value (with modifiers via StatCalculator)
    // Game code should almost always use GetStat() for gameplay calculations.
    
    // Inherited from Entity: GetCurrentHealth(), GetBaseMaxHealth(), GetMaxHealth(), GetBaseArmorClass(), GetArmorClass()
    
    // Base value accessors (return raw field values without modifiers)
    public int GetBaseComponentSpace() => baseComponentSpace;
    public int GetBasePowerDrawPerTurn() => basePowerDrawPerTurn;
    
    // Modified value accessors (return values with all modifiers applied via StatCalculator)
    public virtual int GetComponentSpace() => StatCalculator.GatherAttributeValue(this, Attribute.ComponentSpace, baseComponentSpace);
    public virtual int GetPowerDrawPerTurn() => StatCalculator.GatherAttributeValue(this, Attribute.PowerDraw, basePowerDrawPerTurn);
    
    // ==================== CROSS-COMPONENT MODIFIER SYSTEM ====================
    
    /// <summary>
    /// Apply this component's provided modifiers to target components.
    /// Called during vehicle initialization and when component is re-enabled.
    /// </summary>
    public virtual void ApplyProvidedModifiers(global::Vehicle vehicle)
    {
        if (!IsOperational) return;
        if (vehicle == null) return;
        
        foreach (var modData in providedModifiers)
        {
            var targets = ResolveModifierTargets(vehicle, modData);
            foreach (var target in targets)
            {
                if (target != null && !target.isDestroyed)
                {
                    target.AddModifier(new AttributeModifier(
                        modData.attribute,
                        modData.type,
                        modData.value,
                        source: this,
                        category: ModifierCategory.Equipment
                    ));
                }
            }
        }
    }
    
    /// <summary>
    /// Remove all modifiers this component has provided to other components.
    /// Called when component is destroyed or disabled.
    /// </summary>
    public virtual void RemoveProvidedModifiers(global::Vehicle vehicle)
    {
        if (vehicle == null) return;
        
        foreach (var component in vehicle.AllComponents)
        {
            component.RemoveModifiersFromSource(this);
        }
    }
    
    /// <summary>
    /// Resolve which components should receive a modifier based on target mode.
    /// </summary>
    private List<VehicleComponent> ResolveModifierTargets(global::Vehicle vehicle, ComponentModifierData modData)
    {
        var targets = new List<VehicleComponent>();
        
        switch (modData.targetMode)
        {
            case ComponentTargetMode.Chassis:
                if (vehicle.chassis != null) 
                    targets.Add(vehicle.chassis);
                break;
                
            case ComponentTargetMode.PowerCore:
                if (vehicle.powerCore != null) 
                    targets.Add(vehicle.powerCore);
                break;
                
            case ComponentTargetMode.Drive:
                var drive = vehicle.optionalComponents.OfType<DriveComponent>().FirstOrDefault();
                if (drive != null) 
                    targets.Add(drive);
                break;
                
            case ComponentTargetMode.AllWeapons:
                foreach (var weapon in vehicle.optionalComponents.OfType<WeaponComponent>())
                {
                    targets.Add(weapon);
                }
                break;
                
            case ComponentTargetMode.AllComponents:
                targets.AddRange(vehicle.AllComponents);
                break;
                
            case ComponentTargetMode.SpecificComponent:
                if (modData.specificTarget != null) 
                    targets.Add(modData.specificTarget);
                break;
        }
        
        return targets;
    }
    
    /// <summary>
    /// Remove all modifiers from a specific source.
    /// </summary>
    public void RemoveModifiersFromSource(UnityEngine.Object source)
    {
        for (int i = entityModifiers.Count - 1; i >= 0; i--)
        {
            if (entityModifiers[i].Source == source)
            {
                entityModifiers.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Remove all modifiers of a specific category.
    /// </summary>
    public void RemoveModifiersByCategory(ModifierCategory category)
    {
        for (int i = entityModifiers.Count - 1; i >= 0; i--)
        {
            if (entityModifiers[i].Category == category)
            {
                entityModifiers.RemoveAt(i);
            }
        }
    }
    
    // ==================== MODIFIER SYSTEM (Uses Unified Entity System) ====================
    
    /// <summary>
    /// Override RemoveModifier to add component-specific logging.
    /// </summary>
    public override void RemoveModifier(AttributeModifier modifier)
    {
        base.RemoveModifier(modifier);
        
        // Log removal (for debugging/tracking)
        this.LogModifierRemoved(modifier);
    }
    
    // ==================== ENTITY OVERRIDES ====================
    
    /// <summary>
    /// Get display name (override Entity).
    /// Shows component name. Character info comes from VehicleSeat now.
    /// </summary>
    public override string GetDisplayName()
    {
        return name; // Unity's GameObject.name
    }
    
    /// <summary>
    /// Can this component be targeted?
    /// Override to check accessibility based on exposure.
    /// </summary>
    public override bool CanBeTargeted()
    {
        if (isDestroyed) return false;
        
        // Check if accessible through parent vehicle
        if (parentVehicle != null)
        {
            return parentVehicle.IsComponentAccessible(this);
        }
        
        return true;
    }
    
    // ==================== BEHAVIORAL QUERIES ====================
    
    /// <summary>
    /// Is this component operational? (Can function, provide skills, draw power, provide bonuses)
    /// Checks: not destroyed, not manually disabled, not incapacitated by status effects.
    /// Status effects are checked both on this component and on the chassis (vehicle-wide effects).
    /// </summary>
    public virtual bool IsOperational
    {
        get
        {
            if (isDestroyed || isManuallyDisabled) return false;
            
            // Check this component's own status effects
            foreach (var statusEffect in activeStatusEffects)
            {
                if (statusEffect.PreventsActions)
                    return false;
            }
            
            // Also check chassis status effects (vehicle-wide stuns are applied to chassis)
            if (parentVehicle != null && parentVehicle.chassis != null && parentVehicle.chassis != this)
            {
                foreach (var statusEffect in parentVehicle.chassis.GetActiveStatusEffects())
                {
                    if (statusEffect.PreventsActions)
                        return false;
                }
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Can this component contribute to vehicle movement?
    /// Checks component state and status effects that prevent movement.
    /// Also checks chassis status effects (vehicle-wide immobilization).
    /// </summary>
    public virtual bool CanContributeToMovement()
    {
        if (!IsOperational) return false;
        
        // Check this component's own status effects
        foreach (var statusEffect in activeStatusEffects)
        {
            if (statusEffect.PreventsMovement)
                return false;
        }
        
        // Also check chassis status effects (vehicle-wide immobilization)
        if (parentVehicle != null && parentVehicle.chassis != null && parentVehicle.chassis != this)
        {
            foreach (var statusEffect in parentVehicle.chassis.GetActiveStatusEffects())
            {
                if (statusEffect.PreventsMovement)
                    return false;
            }
        }
        
        return true;
    }
    
    // ==================== DAMAGE HANDLING (Override Entity) ====================
    
    /// <summary>
    /// Called when this component is destroyed (HP reaches 0).
    /// Override in subclasses for component-specific destruction effects.
    /// </summary>
    protected override void OnEntityDestroyed()
    {
        this.LogComponentDestroyed();
        
        // If this component enabled a role, that role is now unavailable
        if (roleType != RoleType.None)
        {
            Debug.Log($"[Component] Role '{roleType}' is no longer available on {parentVehicle?.vehicleName ?? "Unknown"}");
        }
        
        // Notify subclasses
        OnComponentDestroyed();
    }
    
    /// <summary>
    /// Called after OnEntityDestroyed. Override in subclasses for specific effects.
    /// </summary>
    protected virtual void OnComponentDestroyed()
    {
        // Remove modifiers this component provided to others (targeted removal)
        RemoveProvidedModifiers(parentVehicle);
        
        // Override in subclasses (ChassisComponent, PowerCoreComponent, etc.)
    }
    
    /// <summary>
    /// Called when component is disabled. Removes provided modifiers.
    /// </summary>
    protected virtual void OnComponentDisabled()
    {
        RemoveProvidedModifiers(parentVehicle);
    }
    
    /// <summary>
    /// Called when component is re-enabled. Re-applies provided modifiers.
    /// </summary>
    protected virtual void OnComponentEnabled()
    {
        ApplyProvidedModifiers(parentVehicle);
    }
    
    // ==================== SKILL MANAGEMENT ====================
    
    /// <summary>
    /// Get all skills this component provides.
    /// Note: Character personal skills are now accessed via VehicleSeat.
    /// </summary>
    public virtual List<Skill> GetAllSkills()
    {
        List<Skill> allSkills = new();
        
        // Add component's own skills
        if (componentSkills != null)
        {
            allSkills.AddRange(componentSkills);
        }
        
        return allSkills;
    }
    
    /// <summary>
    /// Can this component currently provide skills?
    /// Checks if component is operational (not destroyed, not manually disabled, not incapacitated).
    /// Note: Character assignment is now checked via VehicleSeat.
    /// </summary>
    public virtual bool CanProvideSkills()
    {
        return IsOperational;
    }
    
    // ==================== UI HELPERS ====================
    
    /// <summary>
    /// Get the stats to display in the UI for this component.
    /// Override in subclasses to provide component-specific stats.
    /// Base implementation returns common stats (power draw if non-zero).
    /// Uses StatCalculator for modified values.
    /// INTEGER-FIRST: All stats are integers.
    /// </summary>
    public virtual List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Add power draw if non-zero (common to many components)
        if (basePowerDrawPerTurn > 0)
        {
            int modifiedPower = GetPowerDrawPerTurn();
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Power", "PWR", Attribute.PowerDraw, basePowerDrawPerTurn, modifiedPower, "/turn"));
        }
        
        return stats;
    }
    
    // ==================== POWER MANAGEMENT (Phase 1) ====================
    
    /// <summary>
    /// Attempt to draw this component's per-turn power cost.
    /// Returns true if successful, false if insufficient power.
    /// Components with powerDrawPerTurn = 0 are skipped automatically.
    /// </summary>
    public virtual bool DrawTurnPower()
    {
        // Skip if not operational (destroyed, manually disabled, or incapacitated)
        if (!IsOperational) return true;
        
        // Skip if no power core
        var powerCore = parentVehicle != null ? parentVehicle.powerCore : null;
        if (powerCore == null) return true;
        
        // Calculate actual power draw (may be modified by status effects)
        int actualDraw = GetActualPowerDraw();
        if (actualDraw <= 0) return true;  // No continuous draw (weapons, passive components)
        
        // Attempt to draw power
        bool success = powerCore.DrawPower(actualDraw, this, "Continuous operation");
        
        if (!success)
        {
            // Insufficient power - component shuts down
            OnPowerStarved();
        }
        
        return success;
    }
    
    /// <summary>
    /// Get the actual power draw for this component (base + modifiers).
    /// Uses StatCalculator for modifier application.
    /// </summary>
    public virtual int GetActualPowerDraw()
    {
        if (!IsOperational) return 0;
        
        // Use accessor which applies modifiers via StatCalculator
        return GetPowerDrawPerTurn();
    }
    
    /// <summary>
    /// Called when component cannot draw required power.
    /// Default behavior: log warning.
    /// Phase 1: Just warnings, no auto-disable.
    /// </summary>
    protected virtual void OnPowerStarved()
    {
        this.LogPowerStarved(GetActualPowerDraw());
        
        // Component becomes temporarily disabled until power is restored
        // (Or Technician manually re-enables it)
    }
    
    /// <summary>
    /// Set the engineer's manual disable state for this component.
    /// When disabled: no power draw, no skills, no bonuses.
    /// Returns false if operation not allowed (destroyed, or trying to disable mandatory components).
    /// </summary>
    public virtual bool SetManuallyDisabled(bool disabled)
    {
        // Cannot modify destroyed components
        if (isDestroyed) return false;
        
        // Cannot disable mandatory components (Chassis, PowerCore)
        if (disabled && (componentType == ComponentType.Chassis || componentType == ComponentType.PowerCore))
            return false;
        
        bool oldState = isManuallyDisabled;
        isManuallyDisabled = disabled;
        
        if (oldState != isManuallyDisabled)
        {
            if (disabled)
            {
                OnComponentDisabled();
            }
            else
            {
                OnComponentEnabled();
            }
            
            this.LogManualStateChange(isManuallyDisabled);
        }
        
        return true;
    }
}

