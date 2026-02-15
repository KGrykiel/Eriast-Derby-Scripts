using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Characters;
using System;

/// <summary>
/// representation of a character: pure data bag for attributes, proficiencies, and identity.
/// Character is not an entity in this system, it's more like a glorified character sheet. It has no inherent combat stats like HP or AC, and is not directly targetable.
/// Related alculations live in CharacterFormulas.
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "Racing/Player Character")]
public class Character : ScriptableObject
{
    [Header("Character Identity")]
    [Tooltip("Character's display name")]
    public string characterName = "Unnamed Character";
    
    [TextArea(2, 4)]
    [Tooltip("Character description/background")]
    public string description = "";

    [Header("Level")]
    [Tooltip("Character level (1-20). Proficiency bonus: +2 (lvl 1-4), +3 (5-8), +4 (9-12), +5 (13-16), +6 (17-20)")]
    [Min(1)]
    public int level = 1;
    
    // ==================== ATTRIBUTES ====================
    
    [Header("Attributes (3-20, default 10)")]
    [Tooltip("Raw physical power, lifting, breaking. May govern future melee/grapple skills.")]
    [SerializeField, Range(3, 20)]
    private int strength = 10;
    
    [Tooltip("Reflexes, hand-eye coordination, fine motor control. Governs: Piloting, Defensive Maneuvers, Stunts, Stealth.")]
    [SerializeField, Range(3, 20)]
    private int dexterity = 10;
    
    [Tooltip("Technical knowledge, problem-solving, understanding of magical systems. Governs: Mechanics, Arcana.")]
    [SerializeField, Range(3, 20)]
    private int intelligence = 10;
    
    [Tooltip("Awareness, intuition, reading situations. Governs: Perception, Survival.")]
    [SerializeField, Range(3, 20)]
    private int wisdom = 10;
    
    [Tooltip("Endurance, withstanding physical stress. Used for CON saves.")]
    [SerializeField, Range(3, 20)]
    private int constitution = 10;
    
    [Tooltip("Showmanship, force of personality, intimidation. Governs: Deception, Intimidation.")]
    [SerializeField, Range(3, 20)]
    private int charisma = 10;
    
    // ==================== PROFICIENCIES ====================
    
    [Header("Skill Proficiencies")]
    [Tooltip("Skills this character is proficient in. Grants bonus equal to level.")]
    [SerializeField]
    private List<CharacterSkill> proficientSkills = new();

    // ==================== COMBAT STATS ====================

    [Header("Combat Stats")]
    [Tooltip("Base attack bonus for weapon attacks.")]
    public int baseAttackBonus = 0;

    [Header("Personal Abilities")]
    [Tooltip("Character-specific abilities available regardless of component operated (e.g., Evasive Maneuver, Quick Scan). These are Skill ScriptableObject assets, not proficiency categories.")]
    public List<Skill> personalAbilities = new();

    // ==================== ATTRIBUTE METHODS ====================

    public int GetAttributeScore(CharacterAttribute attribute)
    {
        return attribute switch
        {
            CharacterAttribute.Strength => strength,
            CharacterAttribute.Dexterity => dexterity,
            CharacterAttribute.Intelligence => intelligence,
            CharacterAttribute.Wisdom => wisdom,
            CharacterAttribute.Constitution => constitution,
            CharacterAttribute.Charisma => charisma,
            _ => 10
        };
    }

    // ==================== PROFICIENCY METHODS ====================

    public bool IsProficient(CharacterSkill skill)
    {
        return proficientSkills != null && proficientSkills.Contains(skill);
    }
    
    // ==================== PERSONAL ABILITIES ====================

    /// <summary>Returns a defensive copy.</summary>
    public List<Skill> GetPersonalAbilities()
    {
        return personalAbilities != null ? new List<Skill>(personalAbilities) : new List<Skill>();
    }
}
