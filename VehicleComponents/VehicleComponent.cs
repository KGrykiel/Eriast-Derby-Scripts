using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;

/// <summary>
/// Base class for all vehicle components.
/// Components are modular parts that contribute stats, enable roles, and provide skills.
/// This is abstract - use specific subclasses like ChassisComponent, WeaponComponent, etc.
/// </summary>
public abstract class VehicleComponent : MonoBehaviour
{
    [Header("Component Identity")]
    [Tooltip("Display name of this component")]
    public string componentName = "Unnamed Component";
    
    [Tooltip("Category of this component")]
    public ComponentType componentType = ComponentType.Custom;
    
    [Header("Component Stats")]
    [Tooltip("Hit points of this component (can be damaged individually)")]
    public int componentHP = 50;
    
    [Tooltip("Armor Class of this component")]
    public int componentAC = 15;
    
    [Tooltip("Component Space required (negative = uses space)")]
    public int componentSpaceRequired = 0;
    
    [Tooltip("Power drawn per turn (0 = no power required)")]
    public int powerDrawPerTurn = 0;
    
    [Header("Component State")]
    [Tooltip("Current HP of this component")]
    public int currentHP;
    
    [Tooltip("Is this component destroyed? (HP <= 0)")]
    public bool isDestroyed = false;
    
    [Tooltip("Is this component disabled? (Engineer can disable/enable)")]
    public bool isDisabled = false;
    
    [Header("Role Support")]
    [Tooltip("Does this component enable a role? (e.g., Weapon enables Gunner)")]
    public bool enablesRole = false;
    
    [Tooltip("Name of the role this component enables (e.g., 'Driver', 'Gunner', 'Navigator')")]
    public string roleName = "";
    
    [Header("Skills")]
    [Tooltip("Skills provided by this component (assigned in Inspector)")]
    public List<Skill> componentSkills = new List<Skill>();
    
    [Header("Character Assignment")]
    [Tooltip("Player character operating this component (null for AI or unassigned)")]
    public PlayerCharacter assignedCharacter;
    
    [Header("Turn Tracking")]
    [HideInInspector]
    public bool hasActedThisTurn = false;
    
    // Reference to parent vehicle (set during initialization)
    protected Vehicle parentVehicle;
    
    /// <summary>
    /// Initialize this component with a reference to its parent vehicle.
    /// Called by Vehicle.Awake() after component discovery.
    /// </summary>
    public virtual void Initialize(Vehicle vehicle)
    {
        parentVehicle = vehicle;
        currentHP = componentHP;
        
        // Log component initialization
        if (enablesRole)
        {
            Debug.Log($"[Component] {componentName} initialized on {vehicle.vehicleName}, enables role: {roleName}");
        }
    }
    
    /// <summary>
    /// Reset turn-specific state (called at start of each round).
    /// </summary>
    public virtual void ResetTurnState()
    {
        hasActedThisTurn = false;
    }
    
    /// <summary>
    /// Damage this component specifically.
    /// If HP reaches 0, component is destroyed.
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        int oldHP = currentHP;
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            isDestroyed = true;
            OnComponentDestroyed();
        }
        
        // Log component damage
        RaceHistory.Log(
            EventType.Combat,
            EventImportance.Medium,
            $"{parentVehicle.vehicleName}'s {componentName} took {damage} damage ({currentHP}/{componentHP} HP)",
            parentVehicle?.currentStage,
            parentVehicle
        ).WithMetadata("componentName", componentName)
         .WithMetadata("componentType", componentType.ToString())
         .WithMetadata("damage", damage)
         .WithMetadata("oldHP", oldHP)
         .WithMetadata("newHP", currentHP)
         .WithMetadata("isDestroyed", isDestroyed);
    }
    
    /// <summary>
    /// Called when this component is destroyed (HP reaches 0).
    /// Override in subclasses for component-specific destruction effects.
    /// </summary>
    protected virtual void OnComponentDestroyed()
    {
        // Log destruction
        RaceHistory.Log(
            EventType.Combat,
            EventImportance.High,
            $"[DESTROYED] {parentVehicle.vehicleName}'s {componentName} was destroyed!",
            parentVehicle?.currentStage,
            parentVehicle
        ).WithMetadata("componentName", componentName)
         .WithMetadata("componentType", componentType.ToString());
        
        Debug.LogWarning($"[Component] {componentName} on {parentVehicle.vehicleName} was destroyed!");
        
        // If this component enabled a role, that role is now unavailable
        if (enablesRole)
        {
            Debug.Log($"[Component] Role '{roleName}' is no longer available on {parentVehicle.vehicleName}");
        }
    }
    
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
    
    /// <summary>
    /// Get display name for this component (includes character name if assigned).
    /// Used for UI display.
    /// </summary>
    public virtual string GetDisplayName()
    {
        if (assignedCharacter != null)
        {
            return $"{componentName} ({assignedCharacter.characterName})";
        }
        return componentName;
    }
    
    /// <summary>
    /// Get component status summary for debugging/UI.
    /// </summary>
    public virtual string GetStatusSummary()
    {
        string status = $"<b>{componentName}</b> ({componentType})\n";
        status += $"HP: {currentHP}/{componentHP} | AC: {componentAC}\n";
        
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
