using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventType = Assets.Scripts.Logging.EventType;
using Entities.Vehicle.VehicleComponents;
using System;
using Assets.Scripts.Logging;

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
    [ReadOnly]
    public ComponentType componentType = ComponentType.Custom;
    
    [Header("Component-Specific Stats")]
    [Tooltip("Component Space (positive = uses space, negative = provides space)")]
    public int componentSpace = 0;
    
    [Tooltip("Power drawn per turn (0 = passive component, no power draw)")]
    public int powerDrawPerTurn = 0;
    
    [Header("Provided Modifiers")]
    [Tooltip("Modifiers this component provides to OTHER components. Used for cross-component bonuses like armor upgrades, boosters, etc.")]
    public List<ComponentModifierData> providedModifiers = new List<ComponentModifierData>();
    
    [Header("Component Targeting")]
    [Tooltip("How exposed this component is for targeting")]
    public ComponentExposure exposure = ComponentExposure.External;
    
    [Tooltip("Component that shields this one (drag component reference here)")]
    public VehicleComponent shieldedByComponent = null;
    
    [Tooltip("For Internal exposure: Required chassis damage % to access (0-1, e.g., 0.5 = 50% damage)")]
    [Range(0f, 1f)]
    public float internalAccessThreshold = 0.5f;
    
    [Header("Component State")]
    [Tooltip("Is this component disabled? (Engineer can disable/enable)")]
    public bool isDisabled = false;
    
    [Header("Role Support")]
    [Tooltip("Does this component enable a role? (locked for specific component types)")]
    [ReadOnly]
    public bool enablesRole = false;
    
    [Tooltip("Type of role this component enables (locked for specific component types)")]
    [ReadOnly]
    public RoleType roleType = RoleType.None;
    
    [Header("Skills")]
    [Tooltip("Skills provided by this component (assigned in Inspector)")]
    public List<Skill> componentSkills = new List<Skill>();
    
    [Header("Character Assignment")]
    [Tooltip("Player character operating this component (null for AI or unassigned, only used if component enables a role)")]
    public PlayerCharacter assignedCharacter;
    
    [Header("Turn Tracking")]
    [HideInInspector]
    public bool hasActedThisTurn = false;
    
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
        if (enablesRole)
        {
            Debug.Log($"[Component] {name} initialized on {vehicle.vehicleName}, enables role: {roleType}");
        }
    }
    
    /// <summary>
    /// Reset turn-specific state (called at start of each round).
    /// </summary>
    public virtual void ResetTurnState()
    {
        hasActedThisTurn = false;
    }
    
    // ==================== CROSS-COMPONENT MODIFIER SYSTEM ====================
    
    /// <summary>
    /// Apply this component's provided modifiers to target components.
    /// Called during vehicle initialization and when component is re-enabled.
    /// </summary>
    public virtual void ApplyProvidedModifiers(global::Vehicle vehicle)
    {
        if (isDestroyed || isDisabled) return;
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
        string vehicleName = parentVehicle?.vehicleName ?? "Unknown";
        
        RaceHistory.Log(
            EventType.Modifier,
            EventImportance.Debug,
            $"{vehicleName}'s {name} lost {modifier.Type} {modifier.Attribute} {modifier.Value:+0;-0} modifier",
            parentVehicle?.currentStage,
            parentVehicle
        ).WithMetadata("component", name)
         .WithMetadata("modifierType", modifier.Type.ToString())
         .WithMetadata("attribute", modifier.Attribute.ToString())
         .WithMetadata("removed", true);
    }
    
    // ==================== ENTITY OVERRIDES ====================
    
    /// <summary>
    /// Get display name (override Entity).
    /// Shows component name + assigned character if present.
    /// </summary>
    public override string GetDisplayName()
    {
        if (assignedCharacter != null)
        {
            return $"{name} ({assignedCharacter.characterName})";
        }
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
    /// Can this component perform actions? (checks for stun/disable effects + component state)
    /// Checks both this component's status effects AND the vehicle's chassis (where vehicle-wide stuns are applied).
    /// </summary>
    public virtual bool CanAct()
    {
        if (isDestroyed || isDisabled) return false;
        
        // Check this component's own status effects
        foreach (var statusEffect in activeStatusEffects)
        {
            if (statusEffect.PreventsActions)
                return false;
        }
        
        // Also check chassis status effects (vehicle-wide stuns are applied to chassis)
        if (parentVehicle?.chassis != null && parentVehicle.chassis != this)
        {
            foreach (var statusEffect in parentVehicle.chassis.GetActiveStatusEffects())
            {
                if (statusEffect.PreventsActions)
                    return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Can this component contribute to vehicle movement?
    /// Checks component state and status effects that prevent movement.
    /// Also checks chassis status effects (vehicle-wide immobilization).
    /// </summary>
    public virtual bool CanContributeToMovement()
    {
        if (isDestroyed || isDisabled) return false;
        
        // Check this component's own status effects
        foreach (var statusEffect in activeStatusEffects)
        {
            Debug.Log(statusEffect.PreventsMovement);
            if (statusEffect.PreventsMovement)
                return false;
        }
        
        // Also check chassis status effects (vehicle-wide immobilization)
        if (parentVehicle?.chassis != null && parentVehicle.chassis != this)
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
        // Log destruction
        string vehicleName = parentVehicle?.vehicleName ?? "Unknown";
        RaceHistory.Log(
            EventType.Combat,
            EventImportance.High,
            $"[DESTROYED] {vehicleName}'s {name} was destroyed!",
            parentVehicle?.currentStage,
            parentVehicle
        ).WithMetadata("componentName", name)
         .WithMetadata("componentType", componentType.ToString());
        
        Debug.LogWarning($"[Component] {name} on {vehicleName} was destroyed!");
        
        // If this component enabled a role, that role is now unavailable
        if (enablesRole)
        {
            Debug.Log($"[Component] Role '{roleType}' is no longer available on {vehicleName}");
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
    
    /// <summary>
    /// Set the disabled state of this component.
    /// Handles modifier application/removal automatically.
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        if (isDisabled == disabled) return;
        
        isDisabled = disabled;
        
        if (disabled)
        {
            OnComponentDisabled();
        }
        else
        {
            OnComponentEnabled();
        }
    }
    
    // ==================== SKILL MANAGEMENT ====================
    
    /// <summary>
    /// Get all skills this component provides (component skills + character personal skills).
    /// </summary>
    public virtual List<Skill> GetAllSkills()
    {
        List<Skill> allSkills = new List<Skill>();
        
        // Add component's own skills
        if (componentSkills != null)
        {
            allSkills.AddRange(componentSkills);
        }
        
        // Add character's personal skills (if character is assigned)
        if (assignedCharacter != null)
        {
            var personalSkills = assignedCharacter.GetPersonalSkills();
            if (personalSkills != null)
            {
                allSkills.AddRange(personalSkills);
            }
        }
        
        return allSkills;
    }
    
    /// <summary>
    /// Can this component currently provide skills?
    /// (Not destroyed, not disabled, and has a character assigned)
    /// </summary>
    public virtual bool CanProvideSkills()
    {
        return !isDestroyed && !isDisabled && assignedCharacter != null;
    }
    
    // ==================== UI HELPERS ====================
    
    /// <summary>
    /// Get the stats to display in the UI for this component.
    /// Override in subclasses to provide component-specific stats.
    /// Base implementation returns common stats (power draw if non-zero).
    /// Uses StatCalculator for modified values.
    /// </summary>
    public virtual List<VehicleComponentUI.DisplayStat> GetDisplayStats()
    {
        var stats = new List<VehicleComponentUI.DisplayStat>();
        
        // Add power draw if non-zero (common to many components)
        if (powerDrawPerTurn > 0)
        {
            float modifiedPower = Core.StatCalculator.GatherAttributeValue(this, Attribute.PowerDraw, powerDrawPerTurn);
            stats.Add(VehicleComponentUI.DisplayStat.WithTooltip("Power", "PWR", Attribute.PowerDraw, powerDrawPerTurn, modifiedPower, "/turn"));
        }
        
        return stats;
    }
}
