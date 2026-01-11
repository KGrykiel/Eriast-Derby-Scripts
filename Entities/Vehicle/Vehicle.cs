using UnityEngine;
using TMPro;
using RacingGame.Events;
using System.Collections.Generic;
using System.Linq;
using EventType = RacingGame.Events.EventType;
using Assets.Scripts.Entities.Vehicle.VehicleComponents;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.ComponentTypes;
using Assets.Scripts.Entities.Vehicle.VehicleComponents.Enums;

public enum ControlType
{
    Player,
    AI
}

/// <summary>
/// Vehicle is a CONTAINER/COORDINATOR for Entity components.
/// Vehicle itself is NOT an Entity - its components (chassis, weapons, etc.) ARE entities.
/// 
/// The vehicle aggregates stats from components and provides convenience properties.
/// Damage to "the vehicle" is actually damage to its chassis component.
/// 
/// MODIFIER SYSTEM: Modifiers are stored on individual components, not the vehicle.
/// When a modifier is applied to a vehicle, it's automatically routed to the correct component
/// based on attribute type (HP modifiers → Chassis, Speed modifiers → Drive, etc.)
/// Vehicle.GetActiveModifiers() aggregates modifiers from all components for UI display.
/// 
/// For targeting:
/// - Target Vehicle → actually targets chassis (the "body" of the vehicle)
/// - Target Component → targets specific component directly
/// </summary>
public class Vehicle : MonoBehaviour
{
    [Header("Vehicle Identity")]
    public string vehicleName;

    public ControlType controlType = ControlType.Player;
    [HideInInspector] public Stage currentStage;
    [HideInInspector] public float progress = 0f;
    [HideInInspector] public bool hasLoggedMovementWarningThisTurn = false;

    private TextMeshProUGUI nameLabel;
    
    [Header("Vehicle Components")]
    [Tooltip("Chassis - MANDATORY structural component (stores HP and AC)")]
    public ChassisComponent chassis;
    
    [Tooltip("Power Core - MANDATORY power supply component (stores energy)")]
    public PowerCoreComponent powerCore;
    
    [Tooltip("Optional components (Drive, Weapons, Utilities, etc.)")]
    public List<VehicleComponent> optionalComponents = new List<VehicleComponent>();

    // Component coordinator (handles component management)
    private Assets.Scripts.Entities.Vehicle.VehicleComponentCoordinator componentCoordinator;
    
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    void Awake()
    {
        // Initialize component coordinator
        componentCoordinator = new Assets.Scripts.Entities.Vehicle.VehicleComponentCoordinator(this);
        componentCoordinator.InitializeComponents();

        var labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            nameLabel = labelTransform.GetComponent<TextMeshProUGUI>();
        }
    }
    
    void OnValidate()
    {
        // Validate component space usage
        if (chassis == null) return;
        
        int netSpace = componentCoordinator?.CalculateNetComponentSpace() ?? 0;
        
        if (netSpace > 0)
        {
            Debug.LogError($"[Vehicle] {vehicleName} exceeds component space by {netSpace} units! " +
                          $"Remove components or upgrade chassis.");
        }
    }
    
    // ==================== CONVENIENCE PROPERTIES (delegate to VehicleProperties) ====================
    
    public int health
    {
        get => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetHealth(this);
        set => Assets.Scripts.Entities.Vehicle.VehicleProperties.SetHealth(this, value);
    }
    
    public int maxHealth => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetMaxHealth(this);
    
    public int energy
    {
        get => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetEnergy(this);
        set => Assets.Scripts.Entities.Vehicle.VehicleProperties.SetEnergy(this, value);
    }
    
    public int maxEnergy => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetMaxEnergy(this);
    
    public float energyRegen => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetEnergyRegen(this);
    
    public float speed => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetSpeed(this);
    
    public int armorClass => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetArmorClass(this);
    
    public int GetComponentAC(VehicleComponent targetComponent) 
        => Assets.Scripts.Entities.Vehicle.VehicleProperties.GetComponentAC(this, targetComponent);

    // ==================== COMPONENT ACCESS (delegate to coordinator) ====================
    
    public List<VehicleComponent> AllComponents => componentCoordinator?.GetAllComponents() ?? new List<VehicleComponent>();
    
    public List<VehicleRole> GetAvailableRoles() => componentCoordinator?.GetAvailableRoles() ?? new List<VehicleRole>();
    
    public bool IsComponentAccessible(VehicleComponent target) => componentCoordinator?.IsComponentAccessible(target) ?? false;
    
    public string GetInaccessibilityReason(VehicleComponent target) => componentCoordinator?.GetInaccessibilityReason(target);
    
    public float GetComponentStat(string statName) => componentCoordinator?.GetComponentStat(statName) ?? 0f;
    
    public void ResetComponentsForNewTurn()
    {
        hasLoggedMovementWarningThisTurn = false;
        componentCoordinator?.ResetComponentsForNewTurn();
    }

    // ==================== ENTITY ACCESS (for targeting systems) ====================
    
    public Entity GetPrimaryTarget() => chassis;
    
    public List<Entity> GetAllTargetableEntities()
    {
        return AllComponents
            .Where(c => !c.isDestroyed)
            .Cast<Entity>()
            .ToList();
    }

    // ==================== MODIFIER SYSTEM ====================

    public List<AttributeModifier> GetActiveModifiers()
    {
        List<AttributeModifier> allModifiers = new List<AttributeModifier>();
        
        foreach (var component in AllComponents)
        {
            allModifiers.AddRange(component.GetModifiers());
        }
        
        return allModifiers;
    }
    
    public void AddModifier(AttributeModifier modifier)
    {
        VehicleComponent targetComponent = ResolveModifierTarget(modifier.Attribute);
        
        if (targetComponent == null)
        {
            Debug.LogWarning($"[Vehicle] {vehicleName}: No component found to apply {modifier.Attribute} modifier!");
            return;
        }
        
        targetComponent.AddModifier(modifier);
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        foreach (var component in AllComponents)
        {
            component.RemoveModifier(modifier);
        }
    }

    public void RemoveModifiersFromSource(Object source, bool localOnly)
    {
        int totalRemoved = 0;
        
        foreach (var component in AllComponents)
        {
            var modifiers = component.GetModifiers();
            
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var mod = modifiers[i];
                
                // TODO (Phase 2): 'localOnly' parameter was for stage-specific modifiers
                // When StatusEffect system is implemented, this will be handled by AppliedStatusEffect.applier
                // For now, just check source match
                bool shouldRemove = mod.Source == source;
                
                if (shouldRemove)
                {
                    component.RemoveModifier(mod);
                    totalRemoved++;
                }
            }
        }
        
        if (totalRemoved > 0)
        {
            string sourceText = source != null ? source.name : "unknown source";
            
            RaceHistory.Log(
                EventType.Modifier,
                EventImportance.Low,
                $"{vehicleName} lost {totalRemoved} modifier(s) from {sourceText}",
                currentStage,
                this
            ).WithMetadata("removedCount", totalRemoved)
             .WithMetadata("source", sourceText);
        }
    }
    
    public void UpdateModifiers()
    {
        foreach (var component in AllComponents)
        {
            component.UpdateStatusEffects();
        }
    }
    
    public VehicleComponent ResolveModifierTarget(Attribute attribute)
    {
        return attribute switch
        {
            Attribute.MaxHealth => chassis,
            Attribute.ArmorClass => chassis,
            Attribute.MagicResistance => chassis,
            Attribute.MaxEnergy => powerCore,
            Attribute.EnergyRegen => powerCore,
            Attribute.Speed => optionalComponents.OfType<DriveComponent>().FirstOrDefault(),
            _ => chassis
        };
    }
    
    public Entity RouteEffectTarget(IEffect effect, Entity defaultTarget)
    {
        if (effect == null)
            return defaultTarget;
        
        if (effect is AttributeModifierEffect modifierEffect)
        {
            VehicleComponent component = ResolveModifierTarget(modifierEffect.attribute);
            return component ?? defaultTarget;
        }
        
        if (effect is DamageEffect)
        {
            return chassis ?? defaultTarget;
        }
        
        if (effect is ResourceRestorationEffect)
        {
            return chassis ?? defaultTarget;
        }
        
        return defaultTarget;
    }

    // ==================== STAGE MANAGEMENT ====================

    void Start()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
        MoveToCurrentStage();
    }

    public void UpdateNameLabel()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
    }

    public void SetCurrentStage(Stage stage)
    {
        if (currentStage != null)
        {
            currentStage.TriggerLeave(this);
        }
        
        currentStage = stage;
        MoveToCurrentStage();
        
        if (currentStage != null)
        {
            currentStage.TriggerEnter(this);
            
            if (stage.isFinishLine)
            {
                RaceHistory.Log(
                    EventType.FinishLine,
                    EventImportance.Critical,
                    $"[FINISH] {vehicleName} crossed the finish line!",
                    stage,
                    this
                );
            }
        }
    }

    private void MoveToCurrentStage()
    {
        if (currentStage != null)
        {
            Vector3 stagePos = currentStage.transform.position;
            transform.position = new Vector3(stagePos.x, stagePos.y, transform.position.z);
        }
    }

    // ==================== VEHICLE DESTRUCTION ====================

    public void DestroyVehicle()
    {
        if (Status == VehicleStatus.Destroyed) return;
        
        VehicleStatus oldStatus = Status;
        Status = VehicleStatus.Destroyed;
        
        RaceHistory.Log(
            EventType.Destruction,
            EventImportance.Critical,
            $"[DEAD] {vehicleName} has been destroyed!",
            currentStage,
            this
        ).WithMetadata("previousStatus", oldStatus.ToString())
         .WithMetadata("finalHealth", health)
         .WithMetadata("finalStage", currentStage?.stageName ?? "None");
        
        bool wasLeading = DetermineIfLeading();
        if (wasLeading)
        {
            RaceHistory.Log(
                EventType.TragicMoment,
                EventImportance.High,
                $"[TRAGIC] {vehicleName} was destroyed while in the lead!",
                currentStage,
                this
            );
        }
        
        var turnController = FindFirstObjectByType<TurnController>();
        if (turnController != null)
            turnController.RemoveDestroyedVehicle(this);
    }
    
    private bool DetermineIfLeading()
    {
        return progress > 50f;
    }
    
    // ==================== ENERGY MANAGEMENT ====================
    
    public void RegenerateEnergy()
    {
        if (powerCore == null || powerCore.isDestroyed)
        {
            if (powerCore != null && powerCore.isDestroyed)
            {
                RaceHistory.Log(
                    EventType.Resource,
                    EventImportance.Medium,
                    $"{vehicleName} cannot regenerate energy - Power Core destroyed!",
                    currentStage,
                    this
                ).WithMetadata("powerCoreDestroyed", true)
                 .WithMetadata("currentEnergy", 0);
            }
            return;
        }
        
        powerCore.RegenerateEnergy();
    }

    // ==================== OPERATIONAL STATUS ====================

    public bool IsOperational()
    {
        if (chassis == null || chassis.isDestroyed)
            return false;
        
        if (powerCore == null || powerCore.isDestroyed)
            return false;
        
        if (Status == VehicleStatus.Destroyed)
            return false;
        
        return true;
    }
    
    public string GetNonOperationalReason()
    {
        if (chassis == null)
            return "No chassis installed";
        if (chassis.isDestroyed)
            return "Chassis destroyed";
        
        if (powerCore == null)
            return "No power core installed";
        if (powerCore.isDestroyed)
            return "Power core destroyed - no power";
        
        if (Status == VehicleStatus.Destroyed)
            return "Vehicle destroyed";
        
        return null;
    }

    public bool CanMove()
    {
        if (!IsOperational()) 
            return false;
        
        var driveComponent = optionalComponents.FirstOrDefault(c => c is DriveComponent);
        if (driveComponent == null || driveComponent.isDestroyed || driveComponent.isDisabled)
        {
            return false;
        }
        
        return true;
    }
    
    public string GetCannotMoveReason()
    {
        string operationalReason = GetNonOperationalReason();
        if (operationalReason != null)
            return operationalReason;
        
        var driveComponent = optionalComponents.FirstOrDefault(c => c is DriveComponent);
        if (driveComponent == null)
            return "No drive system installed";
        if (driveComponent.isDestroyed)
            return "Drive system destroyed";
        if (driveComponent.isDisabled)
            return "Drive system disabled";
        
        return null;
    }
}
