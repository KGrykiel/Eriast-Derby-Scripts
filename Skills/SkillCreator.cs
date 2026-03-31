#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Combat.Damage.FormulaProviders.SpecificProviders;
using System.Collections.Generic;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Characters;
using Assets.Scripts.Conditions;
using Assets.Scripts.Conditions.CharacterConditions;
using Assets.Scripts.Conditions.EntityConditions;
using Assets.Scripts.Combat.Rolls.RollSpecs;
using Assets.Scripts.Combat.Rolls.RollSpecs.SpecTypes;
using Assets.Scripts.Combat.Rolls.Targeting;
using Assets.Scripts.Combat.Rolls.Advantage;
using Assets.Scripts.Effects;
using Assets.Scripts.Effects.EffectTypes;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;

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
                    effect = new ApplyEntityConditionEffect(),
                    target = EffectTarget.SourceVehicle
                }
            };
        }

        private static List<EffectInvocation> GetDebuffPreset()
        {
            return new List<EffectInvocation>
            {
                new() {
                    effect = new ApplyEntityConditionEffect(),
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
            RegenerateSkill(DefineWebShot());
            RegenerateSkill(DefineHarden());
            RegenerateSkill(DefineStimPack());
            RegenerateSkill(DefineEMPStrike());
            RegenerateSkill(DefineLancerStrike());

            // AoE skills
            RegenerateSkill(DefineShrapnelBurst());
            RegenerateSkill(DefineNapalmSpray());
            RegenerateSkill(DefineShockwave());
            RegenerateSkill(DefineOilSlick());
            RegenerateSkill(DefineSelfDestruct());

            // Worked examples from TargettingRefactor.md
            RegenerateSkill(DefineFireball());
            RegenerateSkill(DefineEMPPulse());
            RegenerateSkill(DefineSmokeScreen());
            RegenerateSkill(DefineFeedbackLoop());

            // Splash attack examples
            RegenerateSkill(DefineConcussionBlast());

            // Multi-hit examples
            RegenerateSkill(DefineRapidFire());

            AssetDatabase.SaveAssets();
            Debug.Log("[SkillCreator] All skills regenerated.");
        }

        // ---- Skill definitions ----

        // Pattern: simple attack, one effect.
        private static Skill DefineCannonShot()
            => Make("Cannon Shot",
                Attack(FX(Dmg(2, 6, 3, DamageType.Piercing))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Pattern: no roll, self-repair to a chosen component.
        private static Skill DefineEmergencyPatch()
            => Make("Emergency Patch",
                AlwaysApply(FX(Heal(8, EffectTarget.SourceComponent))),
                TargetingMode.SourceComponent,
                energyCost: 1,
                actionCost: ActionType.BonusAction);

        // Pattern: no roll, two effects on the same node targeting self — gain energy, pay force damage as cost.
        private static Skill DefineOverclock()
            => Make("Overclock",
                AlwaysApply(FX(
                    Energy(4),
                    Dmg(1, 4, 0, DamageType.Force, EffectTarget.SourceComponent))),
                TargetingMode.Self,
                energyCost: 0,
                actionCost: ActionType.BonusAction);

        // Pattern: save-based debuff — enemy rolls Stability or takes damage.
        private static Skill DefineStabilityDrain()
            => Make("Stability Drain",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 14,
                    FX(Dmg(1, 6, 2, DamageType.Bludgeoning))),
                TargetingMode.Enemy,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Pattern: attack with two effects on different targets — damage to enemy, self-heal on the same hit.
        private static Skill DefineArmorPierce()
            => Make("Armor Pierce",
                Attack(FX(
                    Dmg(1, 8, 2, DamageType.Piercing, EffectTarget.SelectedTarget),
                    Heal(3, EffectTarget.SourceComponent))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Pattern: chained nodes — attack hits, then enemy makes a Stability save or takes extra force damage.
        private static Skill DefineHarpoon()
            => Make("Harpoon",
                Attack(
                    FX(Dmg(1, 6, 2, DamageType.Piercing)),
                    successChain: Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 13,
                        FX(Dmg(1, 4, 0, DamageType.Force)))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Pattern: check gates an attack — Navigator makes a Perception check (requires Sensors);
        // on success the attack fires with a strong bonus, on fail nothing happens.
        private static Skill DefineTargetingLock()
            => Make("Targeting Lock",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 12,
                    FX(),
                    successChain: Attack(FX(Dmg(2, 8, 4, DamageType.Piercing)))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.FullAction);

        // Pattern: heavy attack with self-damage recoil — both effects fire on a hit.
        private static Skill DefineRecoilCannon()
            => Make("Recoil Cannon",
                Attack(FX(
                    Dmg(3, 8, 2, DamageType.Piercing, EffectTarget.SelectedTarget),
                    Dmg(1, 6, 0, DamageType.Force, EffectTarget.SourceComponent))),
                TargetingMode.EnemyComponent,
                energyCost: 3,
                actionCost: ActionType.FullAction);

        // Pattern: attack with granted advantage — the gunner lines up a careful shot.
        private static Skill DefineAimedShot()
            => Make("Aimed Shot",
                Attack(new AttackSpec { grantedMode = RollMode.Advantage }, FX(Dmg(2, 8, 2, DamageType.Piercing))),
                TargetingMode.EnemyComponent,
                energyCost: 3,
                actionCost: ActionType.FullAction);

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
                energyCost: 1,
                actionCost: ActionType.Action);

        // Pattern: attack with status effect — fire damage plus Burning DoT on hit.
        private static Skill DefineIncendiaryShot()
            => Make("Incendiary Shot",
                Attack(FX(
                    Dmg(1, 6, 2, DamageType.Fire, EffectTarget.SelectedTarget),
                    Status(LoadEntityCondition("Burning"), EffectTarget.SelectedTarget))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Pattern: attack with stackable debuff — applies Slowed (stacks up to 3 times) on hit.
        private static Skill DefineWebShot()
            => Make("Web Shot",
                Attack(FX(
                    Dmg(1, 4, 0, DamageType.Bludgeoning, EffectTarget.SelectedTarget),
                    Status(LoadEntityCondition("Slowed"), EffectTarget.SelectedTarget))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.BonusAction);

        // Pattern: no roll, apply Fortified buff to self — armour and integrity bonus for a short time.
        private static Skill DefineHarden()
            => Make("Harden",
                AlwaysApply(FX(Status(LoadEntityCondition("Fortified"), EffectTarget.SourceVehicle))),
                TargetingMode.Self,
                energyCost: 1,
                actionCost: ActionType.BonusAction);

        // Pattern: no roll, apply Regenerating HoT to self — health recovery over time.
        private static Skill DefineStimPack()
            => Make("Stim Pack",
                AlwaysApply(FX(Status(LoadEntityCondition("Regenerating"), EffectTarget.SourceVehicle))),
                TargetingMode.Self,
                energyCost: 2,
                actionCost: ActionType.BonusAction);

        // Pattern: attack with electronic debuff — force damage plus Overheating on hit (requires IsElectronic).
        private static Skill DefineEMPStrike()
            => Make("EMP Strike",
                Attack(FX(
                    Dmg(1, 6, 0, DamageType.Force, EffectTarget.SelectedTarget),
                    Status(LoadEntityCondition("Overheating"), EffectTarget.SelectedTarget))),
                TargetingMode.EnemyComponent,
                energyCost: 3,
                actionCost: ActionType.Action);

        // Pattern: attack with DoT — piercing damage plus Bleeding on hit.
        private static Skill DefineLancerStrike()
            => Make("Lancer Strike",
                Attack(FX(
                    Dmg(1, 8, 2, DamageType.Piercing, EffectTarget.SelectedTarget),
                    Status(LoadEntityCondition("Bleeding"), EffectTarget.SelectedTarget))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Pattern: attack gates AoE — on hit, splash damage to every vehicle in the target's lane.
        private static Skill DefineShrapnelBurst()
            => Make("Shrapnel Burst",
                Attack(FX(Dmg(2, 6, 0, DamageType.Piercing, EffectTarget.AllVehiclesInTargetLane))),
                TargetingMode.Enemy,
                energyCost: 3,
                actionCost: ActionType.Action);

        // Pattern: unconditional lane AoE excluding self — fire damage to everyone else in the caster's lane.
        private static Skill DefineNapalmSpray()
            => Make("Napalm Spray",
                AlwaysApply(FX(Dmg(1, 8, 2, DamageType.Fire, EffectTarget.AllOtherVehiclesInTargetLane))),
                TargetingMode.OwnLane,
                energyCost: 3,
                actionCost: ActionType.FullAction);

        // Pattern: save-based stage AoE — every other vehicle in the stage takes bludgeoning on a failed Stability save.
        private static Skill DefineShockwave()
            => Make("Shockwave",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 14,
                    FX(Dmg(2, 6, 2, DamageType.Bludgeoning, EffectTarget.AllOtherVehiclesInStage))),
                TargetingMode.Self,
                energyCost: 4,
                actionCost: ActionType.FullAction);

        // Pattern: status AoE — applies Slowed to all other vehicles in the caster's lane.
        private static Skill DefineOilSlick()
            => Make("Oil Slick",
                AlwaysApply(FX(Status(LoadEntityCondition("Slowed"), EffectTarget.AllOtherVehiclesInTargetLane))),
                TargetingMode.OwnLane,
                energyCost: 2,
                actionCost: ActionType.BonusAction);

        // Pattern: AoE with heavy self-harm — damages entire lane (including self), plus extra damage to self.
        private static Skill DefineSelfDestruct()
            => Make("Self-Destruct",
                AlwaysApply(FX(
                    Dmg(3, 6, 0, DamageType.Fire, EffectTarget.AllOtherVehiclesInTargetLane),
                    Dmg(4, 6, 0, DamageType.Fire, EffectTarget.SourceVehicle))),
                TargetingMode.OwnLane,
                energyCost: 0,
                actionCost: ActionType.FullAction);

        // ==================== WORKED EXAMPLES (TargettingRefactor.md) ====================

        // Pattern: fan-out save — each vehicle in the lane makes an independent Mobility save.
        // Full damage on fail, half on save. Caster excluded — they're outside the blast.
        private static Skill DefineFireball()
            => Make("Fireball",
                FanOut(new AllVehiclesInLaneResolver(excludeSelf: true),
                    Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 15,
                        onFail: FX(Dmg(3, 6, 0, DamageType.Fire, EffectTarget.SelectedTarget)),
                        onPass: FX(Dmg(1, 6, 2, DamageType.Fire, EffectTarget.SelectedTarget)))),
                TargetingMode.Lane,
                energyCost: 4,
                actionCost: ActionType.Action);

        // Pattern: chained fan-out — caster check gates per-target saves.
        // Node 1: Arcana check DC 14 (single execution). On success chains to Node 2.
        // Node 2: Mobility save DC 16 (fans out to each vehicle in lane). On fail: Overheating.
        private static Skill DefineEMPPulse()
            => Make("EMP Pulse",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Arcana, ComponentType.Sensors), 14,
                    onSuccess: FX(),
                    successChain: FanOut(new AllVehiclesInLaneResolver(),
                        Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 16,
                            onFail: FX(Status(LoadEntityCondition("Overheating"), EffectTarget.SelectedTarget))))),
                TargetingMode.Lane,
                energyCost: 3,
                actionCost: ActionType.Action);

        // Pattern: failed check applies character condition to the rolling character's seat.
        // Navigator makes a Perception check; on fail, Blinded applies to the navigator's own seat.
        private static Skill DefineSmokeScreen()
            => Make("Smoke Screen",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 13,
                    onSuccess: FX(),
                    successChain: null,
                    onFail: FX(),
                    failChain: AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Blinded"), EffectTarget.SourceActorSeat)))),
                TargetingMode.Enemy,
                energyCost: 2,
                actionCost: ActionType.BonusAction);

        // Pattern: role-targeted seat effect — applies Stunned to the navigator on the target vehicle.
        // SeatByRoleResolver fans out to the navigator seat; if no navigator exists, the effect is skipped.
        private static Skill DefineFeedbackLoop()
            => Make("Feedback Loop",
                FanOut(new SeatByRoleResolver(RoleType.Navigator, SeatSource.TargetVehicle),
                    AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Stunned"), EffectTarget.SelectedTarget)))),
                TargetingMode.Enemy,
                energyCost: 2,
                actionCost: ActionType.BonusAction);

        // Pattern: attack with lane splash — 5d6 to the primary target, 1d6 to every other vehicle
        // in the same lane. Caster and primary target are both excluded from the splash.
        private static Skill DefineConcussionBlast()
            => Make("Concussion Blast",
                Attack(
                    FX(Dmg(5, 6, 0, DamageType.Bludgeoning, EffectTarget.SelectedTarget)),
                    successChain: FanOut(new AllVehiclesInLaneResolver(excludeSelf: true, excludeTarget: true),
                        AlwaysApply(FX(Dmg(1, 6, 0, DamageType.Bludgeoning, EffectTarget.SelectedTarget))))),
                TargetingMode.EnemyComponent,
                energyCost: 4,
                actionCost: ActionType.FullAction);

        // Pattern: multi-hit attack — 3 independent attack rolls against the same target, each resolved separately.
        private static Skill DefineRapidFire()
            => Make("Rapid Fire",
                FanOut(new RepeatTargetResolver(3),
                    Attack(FX(Dmg(1, 6, 0, DamageType.Piercing)))),
                TargetingMode.EnemyComponent,
                energyCost: 2,
                actionCost: ActionType.Action);

        // Part 3 (ApplyCharacterConditionEffect pending): apply Inspired to the active crew seat.
        // private static Skill DefineBattleCry()
        //     => Make("Battle Cry",
        //         AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Inspired"), EffectTarget.SourceVehicle))),
        //         TargetingMode.Self,
        //         energyCost: 2);

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

        private static EntityCondition LoadEntityCondition(string name)
            => AssetDatabase.LoadAssetAtPath<EntityCondition>($"{ConditionCreator.StatusEffectsFolder}/{name}.asset");

        private static CharacterCondition LoadCharacterCondition(string name)
            => AssetDatabase.LoadAssetAtPath<CharacterCondition>($"{ConditionCreator.ConditionsFolder}/{name}.asset");

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

        private static EffectInvocation Status(EntityCondition effect, EffectTarget target = EffectTarget.SelectedTarget)
            => new EffectInvocation
            {
                target = target,
                effect = new ApplyEntityConditionEffect { condition = effect }
            };

        private static EffectInvocation CharacterStatus(CharacterCondition condition, EffectTarget target = EffectTarget.SourceActorSeat)
            => new EffectInvocation
            {
                target = target,
                effect = new ApplyCharacterConditionEffect { condition = condition }
            };

        private static RollNode AlwaysApply(List<EffectInvocation> effects)
            => new RollNode { successEffects = effects };

        /// <summary>Wraps a node with a targetResolver for fan-out execution.</summary>
        private static RollNode FanOut(ITargetResolver resolver, RollNode inner)
        {
            inner.targetResolver = resolver;
            return inner;
        }

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
            RollNode successChain = null,
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                rollSpec = spec,
                successEffects = onSuccess ?? new List<EffectInvocation>(),
                failureEffects = onFail ?? new List<EffectInvocation>(),
                onSuccessChain = successChain,
                onFailureChain = failChain
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
            int energyCost = 1,
            ActionType actionCost = ActionType.Action)
        {
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.name = name;
            skill.energyCost = energyCost;
            skill.actionCost = actionCost;
            skill.rollNode = rollNode;
            skill.targetingMode = targeting;
            return skill;
        }
    }
     #endregion
}
#endif