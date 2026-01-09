using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RacingGame.Events;
using EventType = RacingGame.Events.EventType;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.Enums;

/// <summary>
/// Base class for all vehicle components.
/// Components ARE Entities - they have HP, AC, and can be damaged/targeted.
/// Components are modular parts that contribute stats, enable roles, and provide skills.
/// This is abstract - use specific subclasses like ChassisComponent, WeaponComponent, etc.
/// 
/// NOTE: Uses Entity base class fields (health, maxHealth, armorClass) directly.
/// Display name uses Unity's GameObject.name (set in Inspector hierarchy).
/// 
/// MODIFIER SYSTEM: Each component tracks its own modifiers (buffs/debuffs).
/// When a modifier is applied to a vehicle, it's automatically routed to the correct component.
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
    
    // Component-level modifiers (buffs/debuffs affecting this component)
    [SerializeField, HideInInspector]
    private List<AttributeModifier> componentModifiers = new List<AttributeModifier>();
    
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
    
    // ==================== MODIFIER SYSTEM ====================
    
    /// <summary>
    /// Add a modifier to this component.
    /// 
    /// NOTE: This method does NOT log the modifier application.
    /// Logging is handled by SkillCombatLogger when effects are applied from skills.
    /// This allows proper aggregation of multi-effect skills.
    /// </summary>
    public void AddModifier(AttributeModifier modifier)
    {
        componentModifiers.Add(modifier);
        
        // No logging here - Skill.cs/SkillCombatLogger handles it
    }
    
    /// <summary>
    /// Remove a specific modifier from this component.
    /// Logs removal for debugging purposes.
    /// </summary>
    public void RemoveModifier(AttributeModifier modifier)
    {
        if (componentModifiers.Remove(modifier))
        {
            // Log removal (for debugging/tracking)
            string vehicleName = parentVehicle?.vehicleName ?? "Unknown";
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Debug,  // Changed to Debug - less noise
                $"{vehicleName}'s {name} lost {modifier.Type} {modifier.Attribute} {modifier.Value:+0;-0} modifier",
                parentVehicle?.currentStage,
                parentVehicle
            ).WithMetadata("component", name)
             .WithMetadata("modifierType", modifier.Type.ToString())
             .WithMetadata("attribute", modifier.Attribute.ToString())
             .WithMetadata("removed", true);
        }
    }
    
    /// <summary>
    /// Update modifiers (decrement durations, remove expired).
    /// Called by Vehicle.UpdateModifiers() at end of turn.
    /// 
    /// Duration semantics:
    /// - DurationTurns = -1: Permanent, never expires
    /// - DurationTurns = 1: Last active turn, will expire after this update
    /// - DurationTurns > 1: Active for N more turns
    /// 
    /// Flow: A 3-turn buff starts at 3, decrements each turn (3?2?1?expired)
    /// </summary>
    public void UpdateModifiers()
    {
        for (int i = componentModifiers.Count - 1; i >= 0; i--)
        {
            var mod = componentModifiers[i];
            
            // Permanent modifiers (-1) never expire
            if (mod.DurationTurns < 0)
                continue;
            
            // Decrement duration
            mod.DurationTurns--;
            
            // Remove if expired (reached 0 after decrement)
            if (mod.DurationTurns <= 0)
            {
                string vehicleName = parentVehicle?.vehicleName ?? "Unknown";
                string sourceText = mod.Source != null ? $" from {mod.Source.name}" : "";
                
                RaceHistory.Log(
                    EventType.Modifier,
                    EventImportance.Low,
                    $"{vehicleName}'s {name}: {mod.Type} {mod.Attribute} {mod.Value:+0;-0} expired{sourceText}",
                    parentVehicle?.currentStage,
                    parentVehicle
                ).WithMetadata("component", name)
                 .WithMetadata("expired", true);
                
                componentModifiers.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Get all modifiers affecting this component.
    /// Used by Vehicle.GetActiveModifiers() for aggregation.
    /// </summary>
    public List<AttributeModifier> GetModifiers() => componentModifiers;
    
    /// <summary>
    /// Apply component-specific modifiers to an attribute value.
    /// Called by component subclasses when calculating stats.
    /// </summary>
    protected float ApplyModifiers(Attribute attr, float baseValue)
    {
        float flatBonus = 0f;
        float percentMultiplier = 1f;

        foreach (var mod in componentModifiers)
        {
            if (mod.Attribute != attr) continue;
            if (mod.Type == ModifierType.Flat)
                flatBonus += mod.Value;
            else if (mod.Type == ModifierType.Percent)
                percentMultiplier *= (1f + mod.Value / 100f);
        }

        return (baseValue + flatBonus) * percentMultiplier;
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
