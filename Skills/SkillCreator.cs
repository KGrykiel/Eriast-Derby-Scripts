#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.Scripts.Combat.Damage;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;

namespace Assets.Scripts.Skills
{
    /// <summary>
    /// Editor-only factory for creating skill assets with category-based presets.
    /// All preset and initialization logic lives here, keeping Skill.cs lean.
    /// </summary>
    public static class SkillCreator
    {
        private const string MenuPath = "Assets/Create/Racing/Skill/";

        [MenuItem(MenuPath + "Attack Skill")]
        public static void CreateAttackSkill()
        {
            CreateSkillAsset(SkillCategory.Attack, "NewAttackSkill");
        }

        [MenuItem(MenuPath + "Restoration Skill")]
        public static void CreateRestorationSkill()
        {
            CreateSkillAsset(SkillCategory.Restoration, "NewRestorationSkill");
        }

        [MenuItem(MenuPath + "Buff Skill")]
        public static void CreateBuffSkill()
        {
            CreateSkillAsset(SkillCategory.Buff, "NewBuffSkill");
        }

        [MenuItem(MenuPath + "Debuff Skill")]
        public static void CreateDebuffSkill()
        {
            CreateSkillAsset(SkillCategory.Debuff, "NewDebuffSkill");
        }

        [MenuItem(MenuPath + "Utility Skill")]
        public static void CreateUtilitySkill()
        {
            CreateSkillAsset(SkillCategory.Utility, "NewUtilitySkill");
        }

        [MenuItem(MenuPath + "Special Skill")]
        public static void CreateSpecialSkill()
        {
            CreateSkillAsset(SkillCategory.Special, "NewSpecialSkill");
        }

        [MenuItem(MenuPath + "Custom Skill")]
        public static void CreateCustomSkill()
        {
            CreateSkillAsset(SkillCategory.Custom, "NewCustomSkill");
        }

        /// <summary>
        /// Creates and saves a skill asset with the specified category and presets applied.
        /// </summary>
        private static void CreateSkillAsset(SkillCategory category, string defaultName)
        {
            // Create the skill instance
            Skill skill = ScriptableObject.CreateInstance<Skill>();
            skill.category = category;
            
            // Apply category-based presets
            ApplyPresets(skill);

            // Get the current folder path in the Project window
            string path = "Assets";
            
            // If something is selected, use that folder (or its parent if it's a file)
            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                
                // If the selection is a file, get its directory
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }
            }

            // Create unique asset path in the current folder
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");

            // Save the asset
            AssetDatabase.CreateAsset(skill, assetPath);
            AssetDatabase.SaveAssets();

            // Select the new asset
            Selection.activeObject = skill;
            EditorUtility.FocusProjectWindow();
        }
        
        /// <summary>
        /// Apply category-based presets to a skill.
        /// Configures roll types, targeting, and default effects.
        /// </summary>
        private static void ApplyPresets(Skill skill)
        {
            // Configure effect invocations
            skill.effectInvocations = skill.category switch
            {
                SkillCategory.Attack => GetAttackPreset(),
                SkillCategory.Restoration => GetRestorationPreset(),
                SkillCategory.Buff => GetBuffPreset(),
                SkillCategory.Debuff => GetDebuffPreset(),
                SkillCategory.Special => GetSpecialPreset(),
                _ => new List<EffectInvocation>()
            };
            
            // Configure roll and targeting based on category
            switch (skill.category)
            {
                case SkillCategory.Attack:
                    skill.skillRollType = SkillRollType.AttackRoll;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Enemy;
                    break;
                    
                case SkillCategory.Restoration:
                    skill.skillRollType = SkillRollType.None;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.SourceComponent;
                    break;
                    
                case SkillCategory.Buff:
                    skill.skillRollType = SkillRollType.SkillCheck;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility);
                    skill.checkDC = 15;
                    skill.targetingMode = TargetingMode.Self;
                    break;
                    
                case SkillCategory.Debuff:
                    skill.skillRollType = SkillRollType.SavingThrow;
                    skill.saveSpec = SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility);
                    skill.saveDCBase = 15;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Enemy;
                    break;
                    
                case SkillCategory.Utility:
                    skill.skillRollType = SkillRollType.None;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Self;
                    break;
                    
                case SkillCategory.Special:
                    skill.skillRollType = SkillRollType.None;
                    skill.saveSpec = SaveSpec.None;
                    skill.saveDCBase = 0;
                    skill.checkSpec = SkillCheckSpec.None;
                    skill.checkDC = 0;
                    skill.targetingMode = TargetingMode.Enemy;
                    break;
                    
                case SkillCategory.Custom:
                    // Leave at defaults for full customization
                    break;
            }
        }
        
        // ==================== PRESET GENERATORS ====================
        
        private static List<EffectInvocation> GetAttackPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new DamageEffect
                    {
                        formulaProvider = new StaticFormulaProvider
                        {
                            formula = new DamageFormula
                            {
                                baseDice = 1,
                                dieSize = 6,
                                bonus = 0,
                                damageType = DamageType.Physical
                            }
                        }
                    },
                    target = EffectTarget.SelectedTarget
                }
            };
        }
        
        private static List<EffectInvocation> GetRestorationPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ResourceRestorationEffect(),
                    target = EffectTarget.SourceVehicle
                }
            };
        }
        
        private static List<EffectInvocation> GetBuffPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ApplyStatusEffect(),
                    target = EffectTarget.SourceVehicle
                }
            };
        }

        private static List<EffectInvocation> GetDebuffPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ApplyStatusEffect(),
                    target = EffectTarget.SelectedTarget
                }
            };
        }

        private static List<EffectInvocation> GetSpecialPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new CustomEffect(),
                    target = EffectTarget.SelectedTarget
                }
            };
        }
    }
}
#endif
