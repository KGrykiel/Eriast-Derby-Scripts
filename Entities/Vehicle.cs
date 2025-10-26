using UnityEngine;
using TMPro;

public enum ControlType
{
    Player,
    AI
}

public class Vehicle : Entity
{
    public string vehicleName;

    [Header("Base Attributes (editable per vehicle)")]
    public float speed = 4f;
    public int magicResistance = 10; // Temporary
    public int maxEnergy = 50;
    public float energyRegen = 1f;

    [HideInInspector] public int energy; // Current energy, can be modified by skills or events

    // Active modifiers
    private System.Collections.Generic.List<AttributeModifier> activeModifiers = new System.Collections.Generic.List<AttributeModifier>();

    public ControlType controlType = ControlType.Player;
    [HideInInspector] public Stage currentStage;
    [HideInInspector] public float progress = 0f;

    private TextMeshPro nameLabel;
    [Header("Skills")]
    public System.Collections.Generic.List<Skill> skills = new System.Collections.Generic.List<Skill>();

    public System.Collections.Generic.List<AttributeModifier> GetActiveModifiers()
    {
        return activeModifiers;
    }
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    void Awake()
    {
        // Set health and energy to max at start
        health = maxHealth;
        energy = maxEnergy;

        var labelTransform = transform.Find("NameLabel");
        if (labelTransform != null)
        {
            nameLabel = labelTransform.GetComponent<TextMeshPro>();
        }
    }

    void Start()
    {
        if (nameLabel != null)
        {
            nameLabel.text = vehicleName;
        }
        MoveToCurrentStage();
    }

    public void AddModifier(AttributeModifier modifier)
    {
        activeModifiers.Add(modifier);
    }

    public void RemoveModifier(AttributeModifier modifier)
    {
        activeModifiers.Remove(modifier);
    }

    public void RemoveModifiersFromSource(UnityEngine.Object source, bool localOnly)
    {
        if (localOnly)
        {
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (activeModifiers[i].Source == source && activeModifiers[i].local)
                {
                    activeModifiers.RemoveAt(i);
                }
            }
        }
        else
        {
            // Remove all modifiers from the source, regardless of local flag
            for (int i = activeModifiers.Count - 1; i >= 0; i--)
            {
                if (activeModifiers[i].Source == source)
                {
                    activeModifiers.RemoveAt(i);
                }
            }
        }
    }
    public void UpdateModifiers()
    {
        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            var mod = activeModifiers[i];
            if (mod.DurationTurns == 0)
            {
                activeModifiers.RemoveAt(i);
                continue;
            }
            if (mod.DurationTurns > 0)
                mod.DurationTurns--;
        }
    }

    public float GetAttribute(Attribute attr)
    {
        float baseValue = 0f;
        switch (attr)
        {
            case Attribute.Speed: baseValue = speed; break;
            case Attribute.ArmorClass: baseValue = armorClass; break;
            case Attribute.MagicResistance: baseValue = magicResistance; break;
            case Attribute.MaxHealth: baseValue = maxHealth; break;
            case Attribute.MaxEnergy: baseValue = maxEnergy; break;
            case Attribute.EnergyRegen: baseValue = energyRegen; break;
            default: return 0f;
        }

        float flatBonus = 0f;
        float percentMultiplier = 1f;

        foreach (var mod in activeModifiers)
        {
            if (mod.Attribute != attr) continue;
            if (mod.Type == ModifierType.Flat)
                flatBonus += mod.Value;
            else if (mod.Type == ModifierType.Percent)
                percentMultiplier *= (1f + mod.Value / 100f);
        }

        return (baseValue + flatBonus) * percentMultiplier;
    }


    public override int GetArmorClass()
    {
        // Use the attribute system for dynamic armor class
        return Mathf.RoundToInt(GetAttribute(Attribute.ArmorClass));
    }

    public override void TakeDamage(int damage)
    {
        // Reduce health and log the event
        health -= damage;
        if (health <= 0)
        {
            DestroyVehicle();
        }
        else
        {
            SimulationLogger.LogEvent($"{vehicleName} took {damage} damage! Remaining health: {health}");
        }
        SimulationLogger.LogEvent($"{vehicleName} received damage!");
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

    public void DestroyVehicle()
    {
        Status = VehicleStatus.Destroyed;
        SimulationLogger.LogEvent($"{vehicleName} has been destroyed!");
        
        // Remove from turn order
        var turnController = FindObjectOfType<TurnController>();
        if (turnController != null)
            turnController.RemoveDestroyedVehicle(this);
    }
}
