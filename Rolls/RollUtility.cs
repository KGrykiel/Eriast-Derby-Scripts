using UnityEngine;
using System.Collections.Generic;

public static class RollUtility
{
    /// <summary>
    /// Roll to hit an entity with full breakdown tracking.
    /// </summary>
    /// <param name="user">The attacking vehicle</param>
    /// <param name="target">The target entity</param>
    /// <param name="targetType">What target number to use (AC, MR, etc.)</param>
    /// <param name="modifiers">List of all modifiers with sources</param>
    /// <param name="contextName">Name of the skill/effect for logging</param>
    /// <returns>RollBreakdown with full details</returns>
    public static RollBreakdown RollToHitWithBreakdown(Vehicle user, Entity target, TargetNumberType targetType, List<RollModifier> modifiers = null, string contextName = null)
    {
        // Get target vehicle from entity (if it's a component)
        Vehicle vehicleTarget = GetParentVehicle(target);
        
        // Roll the d20
        int baseRoll = Random.Range(1, 21);
        
        // Create breakdown
        RollBreakdown breakdown = RollBreakdown.D20(baseRoll, RollCategory.Attack);
        
        // Add all modifiers
        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                breakdown.WithModifier(mod.name, mod.value, mod.source);
            }
        }
        
        // Determine target value
        int targetValue = 0;
        string targetName = "AC";
        
        if (vehicleTarget != null)
        {
            switch (targetType)
            {
                case TargetNumberType.ArmorClass:
                    if (target is VehicleComponent component)
                    {
                        // Use modifier-adjusted AC for components
                        targetValue = vehicleTarget.GetComponentAC(component);
                    }
                    else
                    {
                        targetValue = vehicleTarget.armorClass;
                    }
                    targetName = "AC";
                    break;
                case TargetNumberType.MagicResistance:
                    targetValue = 10; // TODO: Move to component
                    targetName = "MR";
                    break;
            }
        }
        
        // Set target and determine success
        breakdown.Against(targetValue, targetName);
     
        return breakdown;
    }
    
    /// <summary>
    /// Get parent vehicle from an entity (if it's a VehicleComponent).
    /// </summary>
    private static Vehicle GetParentVehicle(Entity entity)
    {
        if (entity is VehicleComponent component)
        {
            return component.ParentVehicle;
        }
        return null;
    }
    
    /// <summary>
    /// Rolls dice and returns a DamageBreakdown with full details.
    /// </summary>
    public static DamageBreakdown RollDamageWithBreakdown(int diceCount, int dieSize, int bonus, string componentName, DamageType damageType, string source = null)
    {
        // Roll each die individually and sum
        int rolled = 0;
        for (int i = 0; i < diceCount; i++)
        {
            rolled += Random.Range(1, dieSize + 1);
        }
        
        var breakdown = DamageBreakdown.Create(damageType)
            .AddComponent(componentName, diceCount, dieSize, bonus, rolled, source)
            .WithResistance(ResistanceLevel.Normal);
           
        return breakdown;
    }
    
    /// <summary>
    /// Roll damage from a weapon component with full breakdown.
    /// </summary>
    public static DamageBreakdown RollWeaponDamageWithBreakdown(WeaponComponent weapon)
    {
        if (weapon == null)
        {
            return DamageBreakdown.Create(DamageType.Physical).WithResistance(ResistanceLevel.Normal);
        }
        
        return RollDamageWithBreakdown(
            weapon.damageDice, 
            weapon.damageDieSize, 
            weapon.damageBonus, 
            "Weapon",
            weapon.damageType,
            weapon.name
        );
    }

    /// <summary>
    /// Performs a skill check with full breakdown.
    /// </summary>
    public static RollBreakdown SkillCheckWithBreakdown(List<RollModifier> modifiers, int difficulty, string checkName = "Skill Check")
    {
        int baseRoll = Random.Range(1, 21);
        
        var breakdown = RollBreakdown.D20(baseRoll, RollCategory.SkillCheck);
        
        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                breakdown.WithModifier(mod.name, mod.value, mod.source);
            }
        }
        
        breakdown.Against(difficulty, "DC");
        
        return breakdown;
    }

    /// <summary>
    /// Create a modifier list builder for fluent API.
    /// </summary>
    public static ModifierListBuilder BuildModifiers()
    {
        return new ModifierListBuilder();
    }
}

/// <summary>
/// Fluent builder for creating modifier lists.
/// </summary>
public class ModifierListBuilder
{
    private List<RollModifier> modifiers = new List<RollModifier>();
    
    public ModifierListBuilder Add(string name, int value, string source = null)
    {
        if (value != 0)
        {
            modifiers.Add(new RollModifier(name, value, source));
        }
        return this;
    }
    
    public ModifierListBuilder AddIf(bool condition, string name, int value, string source = null)
    {
        if (condition && value != 0)
        {
            modifiers.Add(new RollModifier(name, value, source));
        }
        return this;
    }
    
    public List<RollModifier> Build()
    {
        return modifiers;
    }
    
    public static implicit operator List<RollModifier>(ModifierListBuilder builder)
    {
        return builder.Build();
    }
}

/// <summary>
/// Defines what target number to roll against.
/// Used internally by roll methods - skills should use SkillRollType instead.
/// </summary>
public enum TargetNumberType
{
    ArmorClass,       // Roll vs target's AC
    MagicResistance,  // Roll vs target's MR
    DifficultyClass   // Roll vs fixed DC (future: saving throws, skill checks)
}
