#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicle;
using Assets.Scripts.Characters;
using StatusEffectTemplate = Assets.Scripts.StatusEffects.StatusEffect;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Rolls.Advantage;

namespace Assets.Scripts.Skills
{
    /// <summary>
    /// Convenience editor class to create new Skill assets with preset configurations based on category.
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

        private static void CreateSkillAsset(SkillCategory category, string defaultName)
        {
            Skill skill = ScriptableObject.CreateInstance<Skill>();
            skill.category = category;
            ApplyPresets(skill);

            string path = "Assets";

            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    path = System.IO.Path.GetDirectoryName(path);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");

            AssetDatabase.CreateAsset(skill, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = skill;
            EditorUtility.FocusProjectWindow();
        }
        
        private static void ApplyPresets(Skill skill)
        {
            var successEffects = skill.category switch
            {
                SkillCategory.Attack     => GetAttackPreset(),
                SkillCategory.Restoration => GetRestorationPreset(),
                SkillCategory.Buff       => GetBuffPreset(),
                SkillCategory.Debuff     => GetDebuffPreset(),
                SkillCategory.Special    => GetSpecialPreset(),
                _                        => new List<EffectInvocation>()
            };

            skill.rollNode = skill.category switch
            {
                SkillCategory.Attack => new RollNode
                {
                    rollSpec = new AttackSpec(),
                    successEffects = successEffects
                },
                SkillCategory.Buff => new RollNode
                {
                    rollSpec = CreateCheckWithDc(SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility), 15),
                    successEffects = successEffects
                },
                SkillCategory.Debuff => new RollNode
                {
                    rollSpec = CreateSaveWithDc(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 15),
                    failureEffects = successEffects  // debuff applies when target fails the save
                },
                _ => new RollNode { successEffects = successEffects }
            };

            skill.targetingMode = skill.category switch
            {
                SkillCategory.Attack  => TargetingMode.Enemy,
                SkillCategory.Debuff  => TargetingMode.Enemy,
                SkillCategory.Buff    => TargetingMode.Self,
                SkillCategory.Restoration => TargetingMode.SourceComponent,
                _                     => TargetingMode.Self
            };
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
        #region hardcoded skills
        // ==================== NAMED SKILL CATALOGUE ====================
        // Define all named skills here. Run "Assets/Racing/Regenerate All Skills" to write them to disk.
        // First run: drag generated assets to their prefabs once. Subsequent regenerations overwrite the
        // same .asset file (same GUID) so all prefab references are preserved automatically.

        private const string SkillsFolder = "Assets/Content/Skills";

        [MenuItem("Assets/Racing/Regenerate All Skills")]
        public static void RegenerateAllSkills()
        {
            System.IO.Directory.CreateDirectory(SkillsFolder);

            RegenerateSkill(DefineCannonShot());
            RegenerateSkill(DefineEmergencyPatch());
            RegenerateSkill(DefineOverclock());
            RegenerateSkill(DefineStabilityDrain());
            RegenerateSkill(DefineArmorPierce());
            RegenerateSkill(DefineHarpoon());
            RegenerateSkill(DefineTargetingLock());
            RegenerateSkill(DefineRecoilCannon());
            RegenerateSkill(DefineRammingContest());
            RegenerateSkill(DefineAimedShot());
            RegenerateSkill(DefineIncendiaryShot());

            AssetDatabase.SaveAssets();
            Debug.Log("[SkillCreator] All skills regenerated.");
        }

        // ---- Skill definitions ----

        // Pattern: simple attack, one effect.
        private static Skill DefineCannonShot()
            => Make("Cannon Shot",
                Attack(FX(Dmg(2, 6, 3, DamageType.Piercing))),
                TargetingMode.EnemyComponent,
                energyCost: 2);

        // Pattern: no roll, self-repair to a chosen component.
        private static Skill DefineEmergencyPatch()
            => Make("Emergency Patch",
                AlwaysApply(FX(Heal(8, EffectTarget.SourceComponent))),
                TargetingMode.SourceComponent,
                energyCost: 1);

        // Pattern: no roll, two effects on the same node targeting self — gain energy, pay force damage as cost.
        private static Skill DefineOverclock()
            => Make("Overclock",
                AlwaysApply(FX(
                    Energy(4),
                    Dmg(1, 4, 0, DamageType.Force, EffectTarget.SourceComponent))),
                TargetingMode.Self,
                energyCost: 0);

        // Pattern: save-based debuff — enemy rolls Stability or takes damage.
        private static Skill DefineStabilityDrain()
            => Make("Stability Drain",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 14,
                    FX(Dmg(1, 6, 2, DamageType.Bludgeoning))),
                TargetingMode.Enemy,
                energyCost: 2);

        // Pattern: attack with two effects on different targets — damage to enemy, self-heal on the same hit.
        private static Skill DefineArmorPierce()
            => Make("Armor Pierce",
                Attack(FX(
                    Dmg(1, 8, 2, DamageType.Piercing, EffectTarget.SelectedTarget),
                    Heal(3, EffectTarget.SourceComponent))),
                TargetingMode.EnemyComponent,
                energyCost: 2);

        // Pattern: chained nodes — attack hits, then enemy makes a Stability save or takes extra force damage.
        private static Skill DefineHarpoon()
            => Make("Harpoon",
                Attack(
                    FX(Dmg(1, 6, 2, DamageType.Piercing)),
                    successChain: Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 13,
                        FX(Dmg(1, 4, 0, DamageType.Force)))),
                TargetingMode.EnemyComponent,
                energyCost: 2);

        // Pattern: check gates an attack — Navigator makes a Perception check (requires Sensors);
        // on success the attack fires with a strong bonus, on fail nothing happens.
        private static Skill DefineTargetingLock()
            => Make("Targeting Lock",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 12,
                    FX(),
                    successChain: Attack(FX(Dmg(2, 8, 4, DamageType.Piercing)))),
                TargetingMode.EnemyComponent,
                energyCost: 2);

        // Pattern: heavy attack with self-damage recoil — both effects fire on a hit.
        private static Skill DefineRecoilCannon()
            => Make("Recoil Cannon",
                Attack(FX(
                    Dmg(3, 8, 2, DamageType.Piercing, EffectTarget.SelectedTarget),
                    Dmg(1, 6, 0, DamageType.Force, EffectTarget.SourceComponent))),
                TargetingMode.EnemyComponent,
                energyCost: 3);

        // Pattern: attack with granted advantage — the gunner lines up a careful shot.
        private static Skill DefineAimedShot()
            => Make("Aimed Shot",
                Attack(new AttackSpec { grantedMode = RollMode.Advantage }, FX(Dmg(2, 8, 2, DamageType.Piercing))),
                TargetingMode.EnemyComponent,
                energyCost: 3);

        // Pattern: opposed check — both sides roll Stability; attacker wins → deal damage;
        // defender wins → attacker takes self-damage from the failed ram.
        private static Skill DefineRammingContest()
            => Make("Ramming Contest",
                Opposed(
                    new OpposedCheckRollSpec
                    {
                        attackerSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility),
                        defenderSpec = SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility)
                    },
                    onWin:  FX(Dmg(2, 6, 2, DamageType.Bludgeoning, EffectTarget.SelectedTarget)),
                    onLose: FX(Dmg(1, 6, 0, DamageType.Bludgeoning, EffectTarget.SourceComponent))),
                TargetingMode.Enemy,
                energyCost: 1);

        // Pattern: attack with status effect — fire damage plus Burning DoT on hit.
        private static Skill DefineIncendiaryShot()
            => Make("Incendiary Shot",
                Attack(FX(
                    Dmg(1, 6, 2, DamageType.Fire, EffectTarget.SelectedTarget),
                    Status(LoadStatus("Assets/Content/StatusEffects/Burning.asset"), EffectTarget.SelectedTarget))),
                TargetingMode.EnemyComponent,
                energyCost: 2);

        private static void RegenerateSkill(Skill definition)
        {
            string path = $"{SkillsFolder}/{definition.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Skill>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(definition, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(definition, path);
            }
        }

        /// <summary>Load a StatusEffect asset by project path for use in skill definitions.</summary>
        private static StatusEffectTemplate LoadStatus(string assetPath)
            => AssetDatabase.LoadAssetAtPath<StatusEffectTemplate>(assetPath);

        // ==================== BUILDER METHODS ====================
        // Mirrors TestSkillFactory — kept private here to avoid an editor→test assembly dependency.

        private static List<EffectInvocation> FX(params EffectInvocation[] effects)
            => new List<EffectInvocation>(effects);

        private static EffectInvocation Dmg(
            int dice, int dieSize, int bonus = 0,
            DamageType type = DamageType.Physical,
            EffectTarget target = EffectTarget.SelectedTarget)
            => new EffectInvocation
            {
                target = target,
                effect = new DamageEffect
                {
                    formulaProvider = new StaticFormulaProvider
                    {
                        formula = new DamageFormula { baseDice = dice, dieSize = dieSize, bonus = bonus, damageType = type }
                    }
                }
            };

        private static EffectInvocation Heal(int amount, EffectTarget target = EffectTarget.SourceVehicle)
            => new EffectInvocation
            {
                target = target,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Health, isDrain = false, bonus = amount } }
            };

        private static EffectInvocation Energy(int amount, EffectTarget target = EffectTarget.SourceVehicle)
            => new EffectInvocation
            {
                target = target,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Energy, isDrain = false, bonus = amount } }
            };

        private static EffectInvocation Status(StatusEffectTemplate effect, EffectTarget target = EffectTarget.SelectedTarget)
            => new EffectInvocation
            {
                target = target,
                effect = new ApplyStatusEffect { statusEffect = effect }
            };

        private static RollNode AlwaysApply(List<EffectInvocation> effects)
            => new RollNode { successEffects = effects };

        private static RollNode Attack(
            List<EffectInvocation> onHit,
            List<EffectInvocation> onMiss = null,
            RollNode successChain = null)
            => new RollNode
            {
                rollSpec = new AttackSpec(),
                successEffects = onHit ?? new List<EffectInvocation>(),
                failureEffects = onMiss ?? new List<EffectInvocation>(),
                onSuccessChain = successChain
            };

        private static RollNode Attack(
            AttackSpec spec,
            List<EffectInvocation> onHit,
            List<EffectInvocation> onMiss = null,
            RollNode successChain = null)
            => new RollNode
            {
                rollSpec = spec,
                successEffects = onHit ?? new List<EffectInvocation>(),
                failureEffects = onMiss ?? new List<EffectInvocation>(),
                onSuccessChain = successChain
            };

        private static SkillCheckSpec CreateCheckWithDc(SkillCheckSpec spec, int dc)
        {
            spec.dc = dc;
            return spec;
        }

        private static SaveSpec CreateSaveWithDc(SaveSpec spec, int dc)
        {
            spec.dc = dc;
            return spec;
        }

        private static RollNode Save(
            SaveSpec spec, int dc,
            List<EffectInvocation> onFail,
            List<EffectInvocation> onPass = null,
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                rollSpec = spec,
                failureEffects = onFail ?? new List<EffectInvocation>(),
                successEffects = onPass ?? new List<EffectInvocation>(),
                onFailureChain = failChain
            };
        }

        private static RollNode Check(
            SkillCheckSpec spec, int dc,
            List<EffectInvocation> onSuccess,
            List<EffectInvocation> onFail = null,
            RollNode successChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                rollSpec = spec,
                successEffects = onSuccess ?? new List<EffectInvocation>(),
                failureEffects = onFail ?? new List<EffectInvocation>(),
                onSuccessChain = successChain
            };
        }

        private static RollNode Opposed(
            OpposedCheckRollSpec spec,
            List<EffectInvocation> onWin,
            List<EffectInvocation> onLose = null,
            RollNode winChain = null)
            => new RollNode
            {
                rollSpec = spec,
                successEffects = onWin ?? new List<EffectInvocation>(),
                failureEffects = onLose ?? new List<EffectInvocation>(),
                onSuccessChain = winChain
            };

        private static Skill Make(
            string name,
            RollNode rollNode,
            TargetingMode targeting = TargetingMode.Enemy,
            int energyCost = 1)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            skill.energyCost = energyCost;
            skill.rollNode = rollNode;
            skill.targetingMode = targeting;
            return skill;
        }
    }
     #endregion
}
#endif