using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Events;
using Assets.Scripts.VehicleComponents;
using EventType = RacingGame.Events.EventType;

/// <summary>
/// Base class for all vehicle components.
/// Components ARE Entities - they have HP, AC, and can be damaged/targeted.
/// Components are modular parts that contribute stats, enable roles, and provide skills.
/// This is abstract - use specific subclasses like ChassisComponent, WeaponComponent, etc.
/// 
/// NOTE: Uses Entity base class fields (health, maxHealth, armorClass) directly.
/// Display name uses Unity's GameObject.name (set in Inspector hierarchy).
/// </summary>
public abstract class VehicleComponent : Entity
{
    [Header("Component Identity")]
    [Tooltip("Category of this component (locked for specific component types)")]
    [ReadOnly]
    public ComponentType componentType = ComponentType.Custom;
    
    [Header("Component-Specific Stats")]
    [Tooltip("Component Space required (negative = uses space, positive = provides space)")]
    public int componentSpaceRequired = 0;
    
    [Tooltip("Power drawn per turn (0 = no power required)")]
    public int powerDrawPerTurn = 0;
    
    [Header("Component Targeting")]
    [Tooltip("How exposed this component is for targeting")]
    public ComponentExposure exposure = ComponentExposure.External;
    
    [Tooltip("Name of component that shields this one (leave empty if none)")]
    public string shieldedBy = "";
    
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
    
    [Tooltip("Name of the role this component enables (locked for specific component types)")]
    [ReadOnly]
    public string roleName = "";
    
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
            Debug.Log($"[Component] {name} initialized on {vehicle.vehicleName}, enables role: {roleName}");
        }
    }
    
    /// <summary>
    /// Reset turn-specific state (called at start of each round).
    /// </summary>
    public virtual void ResetTurnState()
    {
        hasActedThisTurn = false;
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
    
    // ==================== DAMAGE HANDLING (Override Entity) ====================
    
    /// <summary>
    /// Override Entity.TakeDamage for component-specific damage handling.
    /// </summary>
    public override void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        int oldHP = health;
        health = Mathf.Max(health - damage, 0);
        
        // Log component damage
        string vehicleName = parentVehicle?.vehicleName ?? "Unknown";
        RaceHistory.Log(
            EventType.Combat,
            EventImportance.Medium,
            $"{vehicleName}'s {name} took {damage} damage ({health}/{maxHealth} HP)",
            parentVehicle?.currentStage,
            parentVehicle
        ).WithMetadata("componentName", name)
         .WithMetadata("componentType", componentType.ToString())
         .WithMetadata("damage", damage)
         .WithMetadata("oldHP", oldHP)
         .WithMetadata("newHP", health)
         .WithMetadata("isDestroyed", health <= 0);
        
        if (health <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            OnEntityDestroyed();
        }
    }
    
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
            Debug.Log($"[Component] Role '{roleName}' is no longer available on {vehicleName}");
        }
        
        // Notify subclasses
        OnComponentDestroyed();
    }
    
    /// <summary>
    /// Called after OnEntityDestroyed. Override in subclasses for specific effects.
    /// </summary>
    protected virtual void OnComponentDestroyed()
    {
        // Override in subclasses (ChassisComponent, PowerCoreComponent, etc.)
    }
    
    // ==================== STAT CONTRIBUTION ====================
    
    /// <summary>
    /// Get stat modifiers this component contributes to the vehicle.
    /// Override in subclasses to provide specific bonuses.
    /// </summary>
    public virtual VehicleStatModifiers GetStatModifiers()
    {
        // If component is destroyed or disabled, it contributes nothing
        if (isDestroyed || isDisabled)
            return VehicleStatModifiers.Zero;
        
        // Base implementation: no stat contributions
        // Override in subclasses (e.g., ChassisComponent, PowerCoreComponent)
        return VehicleStatModifiers.Zero;
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
    /// Get component status summary for debugging/UI.
    /// </summary>
    public virtual string GetStatusSummary()
    {
        string status = $"<b>{name}</b> ({componentType})\n";
        status += $"HP: {health}/{maxHealth} | AC: {armorClass}\n";
        
        if (isDestroyed)
            status += "<color=red>[DESTROYED]</color>\n";
        else if (isDisabled)
            status += "<color=yellow>[DISABLED]</color>\n";
        
        if (enablesRole)
            status += $"Enables: {roleName}\n";
        
        if (assignedCharacter != null)
            status += $"Operated by: {assignedCharacter.characterName}\n";
        
        if (componentSkills.Count > 0)
            status += $"Skills: {componentSkills.Count}\n";
        
        return status;
    }
}
