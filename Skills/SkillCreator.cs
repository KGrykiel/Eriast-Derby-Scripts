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
using Assets.Scripts.Effects.EffectTypes.EntityEffects;
using Assets.Scripts.Effects.EffectTypes.SeatEffects;
using Assets.Scripts.Effects.EffectTypes.VehicleEffects;
using Assets.Scripts.Effects.Invocations;
using Assets.Scripts.Effects.Targeting;
using Assets.Scripts.Effects.Targeting.VehicleTarget;
using Assets.Scripts.Effects.Targeting.EntityTarget;
using Assets.Scripts.Effects.Targeting.SeatTarget;
using Assets.Scripts.Entities.Vehicles.VehicleComponents;
using Assets.Scripts.Consumables;
using Assets.Scripts.Skills.Costs;

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

        [MenuItem(MenuPath + "Weapon Attack Skill")]
        public static void CreateWeaponAttackSkill()
        {
            CreateSkillAsset<WeaponAttackSkill>(SkillCategory.Attack, "NewWeaponAttackSkill");
        }

        private static void CreateSkillAsset(SkillCategory category, string defaultName)
            => CreateSkillAsset<Skill>(category, defaultName);

        private static void CreateSkillAsset<T>(SkillCategory category, string defaultName) where T : Skill
        {
            T skill = ScriptableObject.CreateInstance<T>();
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
                SkillCategory.Attack => GetAttackPreset(),
                SkillCategory.Restoration => GetRestorationPreset(),
                SkillCategory.Buff => GetBuffPreset(),
                SkillCategory.Debuff => GetDebuffPreset(),
                SkillCategory.Special => GetSpecialPreset(),
                _ => new List<IEffectInvocation>()
            };

            skill.rollNode = skill.category switch
            {
                SkillCategory.Attack => new RollNode
                {
                    targetResolver = new CurrentTargetResolver(),
                    rollSpec = new AttackSpec(),
                    successEffects = successEffects
                },
                SkillCategory.Buff => new RollNode
                {
                    targetResolver = new CurrentTargetResolver(),
                    rollSpec = CreateCheckWithDc(SkillCheckSpec.ForVehicle(VehicleCheckAttribute.Mobility), 15),
                    successEffects = successEffects
                },
                SkillCategory.Debuff => new RollNode
                {
                    targetResolver = new CurrentTargetResolver(),
                    rollSpec = CreateSaveWithDc(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 15),
                    failureEffects = successEffects  // debuff applies when target fails the save
                },
                _ => new RollNode { targetResolver = new CurrentTargetResolver(), successEffects = successEffects }
            };

            skill.targetingMode = skill.category switch
            {
                SkillCategory.Attack => TargetingMode.Enemy,
                SkillCategory.Debuff => TargetingMode.Enemy,
                SkillCategory.Special => TargetingMode.Enemy,
                SkillCategory.Buff => TargetingMode.Self,
                SkillCategory.Restoration => TargetingMode.SourceComponent,
                SkillCategory.Utility => TargetingMode.Self,
                SkillCategory.Custom => TargetingMode.Self,
                _ => TargetingMode.Self
            };
        }

        // ==================== PRESET GENERATORS ====================

        private static List<IEffectInvocation> GetAttackPreset()
        {
            return new List<IEffectInvocation>
            {
                Dmg(1, 6, 0, DamageType.Physical, OnTarget)
            };
        }

        private static List<IEffectInvocation> GetRestorationPreset()
        {
            return new List<IEffectInvocation>
            {
                Heal(0, OnSelf)
            };
        }

        private static List<IEffectInvocation> GetBuffPreset()
        {
            return new List<IEffectInvocation>
            {
                Status(null, OnSelf)
            };
        }

        private static List<IEffectInvocation> GetDebuffPreset()
        {
            return new List<IEffectInvocation>
            {
                Status(null, OnTarget)
            };
        }

        private static List<IEffectInvocation> GetSpecialPreset()
        {
            return new List<IEffectInvocation>
            {
                new VehicleEffectInvocation
                {
                    effect = new SetSpeedEffect(),
                    targetResolver = OnSelf
                }
            };
        }
        #region hardcoded skills
        // ==================== NAMED SKILL CATALOGUE ====================
        // Define all named skills here. Run "Assets/Racing/Regenerate All Skills" to write them to disk.
        // First run: drag generated assets to their prefabs once. Subsequent regenerations overwrite the
        // same .asset file (same GUID) so all prefab references are preserved automatically.

        private const string SkillsFolder = "Assets/Content/Skills";
        private const string AttackFolder = SkillsFolder + "/Attack";
        private const string AoEFolder = SkillsFolder + "/AoE";
        private const string BuffFolder = SkillsFolder + "/Buff";
        private const string DebuffFolder = SkillsFolder + "/Debuff";
        private const string UtilityFolder = SkillsFolder + "/Utility";
        private const string WeaponAttackFolder = SkillsFolder + "/WeaponAttack";
        private const string ConsumableGatedFolder = SkillsFolder + "/ConsumableGated";

        [MenuItem("Assets/Racing/Regenerate All Skills")]
        public static void RegenerateAllSkills()
        {
            foreach (string folder in new[] { AttackFolder, AoEFolder, BuffFolder, DebuffFolder, UtilityFolder, WeaponAttackFolder, ConsumableGatedFolder })
                System.IO.Directory.CreateDirectory(folder);

            // Attack
            RegenerateSkill(DefineCannonShot(), AttackFolder);
            RegenerateSkill(DefineArmorPierce(), AttackFolder);
            RegenerateSkill(DefineHarpoon(), AttackFolder);
            RegenerateSkill(DefineTargetingLock(), AttackFolder);
            RegenerateSkill(DefineRecoilCannon(), AttackFolder);
            RegenerateSkill(DefineRammingContest(), AttackFolder);
            RegenerateSkill(DefineAimedShot(), AttackFolder);
            RegenerateSkill(DefineIncendiaryShot(), AttackFolder);
            RegenerateSkill(DefineWebShot(), AttackFolder);
            RegenerateSkill(DefineEMPStrike(), AttackFolder);
            RegenerateSkill(DefineLancerStrike(), AttackFolder);
            RegenerateSkill(DefineConcussionBlast(), AttackFolder);
            RegenerateSkill(DefineRapidFire(), AttackFolder);

            // AoE
            RegenerateSkill(DefineShrapnelBurst(), AoEFolder);
            RegenerateSkill(DefineNapalmSpray(), AoEFolder);
            RegenerateSkill(DefineShockwave(), AoEFolder);
            RegenerateSkill(DefineOilSlick(), AoEFolder);
            RegenerateSkill(DefineSelfDestruct(), AoEFolder);
            RegenerateSkill(DefineFireball(), AoEFolder);
            RegenerateSkill(DefineEMPPulse(), AoEFolder);

            // Buff
            RegenerateSkill(DefineOverclock(), BuffFolder);
            RegenerateSkill(DefineHarden(), BuffFolder);
            RegenerateSkill(DefineStimPack(), BuffFolder);

            // Debuff
            RegenerateSkill(DefineStabilityDrain(), DebuffFolder);
            RegenerateSkill(DefineSmokeScreen(), DebuffFolder);
            RegenerateSkill(DefineFeedbackLoop(), DebuffFolder);

            // Utility
            RegenerateSkill(DefineEmergencyPatch(), UtilityFolder);

            // Weapon attack
            RegenerateSkill(DefineRifleShot(), WeaponAttackFolder);
            RegenerateSkill(DefineBurstFire(), WeaponAttackFolder);

            // Consumable-gated
            RegenerateSkill(DefineMolotovThrow(), ConsumableGatedFolder);
            RegenerateSkill(DefineFieldRepair(), ConsumableGatedFolder);

            AssetDatabase.SaveAssets();
            Debug.Log("[SkillCreator] All skills regenerated.");
        }

        // ---- Skill definitions ----

        // Pattern: simple attack, one effect.
        private static Skill DefineCannonShot()
            => Make("Cannon Shot",
                Attack(FX(Dmg(2, 6, 3, DamageType.Piercing, OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // Pattern: no roll, self-repair to a chosen component.
        private static Skill DefineEmergencyPatch()
            => Make("Emergency Patch",
                AlwaysApply(FX(Heal(8, OnSourceComp))),
                TargetingMode.SourceComponent,
                ActionType.BonusAction,
                new EnergyCost { amount = 1 });

        // Pattern: no roll, two effects on the same node targeting self — gain energy, pay force damage as cost.
        private static Skill DefineOverclock()
            => Make("Overclock",
                AlwaysApply(FX(
                    Energy(4, OnSelf),
                    Dmg(1, 4, 0, DamageType.Force, OnSourceComp))),
                TargetingMode.Self,
                ActionType.BonusAction);

        // Pattern: save-based debuff — enemy rolls Stability or takes damage.
        private static Skill DefineStabilityDrain()
            => Make("Stability Drain",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 14,
                    FX(Dmg(1, 6, 2, DamageType.Bludgeoning, OnTarget))),
                TargetingMode.Enemy,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // Pattern: attack with two effects on different targets — damage to enemy, self-heal on the same hit.
        private static Skill DefineArmorPierce()
            => Make("Armor Pierce",
                Attack(FX(
                    Dmg(1, 8, 2, DamageType.Piercing, OnTarget),
                    Heal(3, OnSourceComp))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // Pattern: chained nodes
        private static Skill DefineHarpoon()
            => Make("Harpoon",
                Attack(
                    FX(Dmg(1, 6, 2, DamageType.Piercing, OnTarget)),
                    successChain: Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 13,
                        FX(Dmg(1, 4, 0, DamageType.Force, OnTarget)))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // Pattern: check gates an attack — Navigator makes a Perception check (requires Sensors);
        // on success the attack fires with a strong bonus, on fail nothing happens.
        private static Skill DefineTargetingLock()
            => Make("Targeting Lock",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 12,
                    FX(),
                    successChain: Attack(FX(Dmg(2, 8, 4, DamageType.Piercing, OnTarget)))),
                TargetingMode.EnemyComponent,
                ActionType.FullAction,
                new EnergyCost { amount = 2 });

        // Pattern: heavy attack with self-damage recoil — both effects fire on a hit.
        private static Skill DefineRecoilCannon()
            => Make("Recoil Cannon",
                Attack(FX(
                    Dmg(3, 8, 2, DamageType.Piercing, OnTarget),
                    Dmg(1, 6, 0, DamageType.Force, OnSourceComp))),
                TargetingMode.EnemyComponent,
                ActionType.FullAction,
                new EnergyCost { amount = 3 });

        // Pattern: attack with granted advantage — the gunner lines up a careful shot.
        private static Skill DefineAimedShot()
            => Make("Aimed Shot",
                Attack(new AttackSpec { grantedMode = RollMode.Advantage }, FX(Dmg(2, 8, 2, DamageType.Piercing, OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.FullAction,
                new EnergyCost { amount = 3 });

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
                    onWin: FX(Dmg(2, 6, 2, DamageType.Bludgeoning, OnTarget)),
                    onLose: FX(Dmg(1, 6, 0, DamageType.Bludgeoning, OnSourceComp))),
                TargetingMode.Enemy,
                ActionType.Action,
                new EnergyCost { amount = 1 });

        // Pattern: attack with status effect — fire damage plus Burning DoT on hit.
        private static Skill DefineIncendiaryShot()
            => Make("Incendiary Shot",
                Attack(FX(
                    Dmg(1, 6, 2, DamageType.Fire, OnTarget),
                    Status(LoadEntityCondition("Burning"), OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // Pattern: attack with stackable debuff
        private static Skill DefineWebShot()
            => Make("Web Shot",
                Attack(FX(
                    Dmg(1, 4, 0, DamageType.Bludgeoning, OnTarget),
                    Status(LoadEntityCondition("Slowed"), OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.BonusAction,
                new EnergyCost { amount = 2 });

        // Pattern: no roll, apply Fortified buff to self — armour and integrity bonus for a short time.
        private static Skill DefineHarden()
            => Make("Harden",
                AlwaysApply(FX(Status(LoadEntityCondition("Fortified"), OnSelf))),
                TargetingMode.Self,
                ActionType.BonusAction,
                new EnergyCost { amount = 1 });

        // Pattern: no roll, apply Regenerating HoT to self — health recovery over time.
        private static Skill DefineStimPack()
            => Make("Stim Pack",
                AlwaysApply(FX(Status(LoadEntityCondition("Regenerating"), OnSelf))),
                TargetingMode.Self,
                ActionType.BonusAction,
                new EnergyCost { amount = 2 });

        // Pattern: attack with electronic debuff — force damage plus Overheating on hit (requires IsElectronic).
        private static Skill DefineEMPStrike()
            => Make("EMP Strike",
                Attack(FX(
                    Dmg(1, 6, 0, DamageType.Force, OnTarget),
                    Status(LoadEntityCondition("Overheating"), OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 3 });

        // Pattern: attack with DoT
        private static Skill DefineLancerStrike()
            => Make("Lancer Strike",
                Attack(FX(
                    Dmg(1, 8, 2, DamageType.Piercing, OnTarget),
                    Status(LoadEntityCondition("Bleeding"), OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // Pattern: attack gates AoE
        private static Skill DefineShrapnelBurst()
            => Make("Shrapnel Burst",
                Attack(FX(Dmg(2, 6, 0, DamageType.Piercing, OnLane))),
                TargetingMode.Enemy,
                ActionType.Action,
                new EnergyCost { amount = 3 });

        // Pattern: unconditional lane AoE excluding self — fire damage to everyone else in the caster's lane.
        private static Skill DefineNapalmSpray()
            => Make("Napalm Spray",
                AlwaysApply(FX(Dmg(1, 8, 2, DamageType.Fire, OnOtherLane))),
                TargetingMode.OwnLane,
                ActionType.FullAction,
                new EnergyCost { amount = 3 });

        // Pattern: save-based stage AoE — every other vehicle in the stage takes bludgeoning on a failed Stability save.
        private static Skill DefineShockwave()
            => Make("Shockwave",
                Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Stability), 14,
                    FX(Dmg(2, 6, 2, DamageType.Bludgeoning, OnOtherStage))),
                TargetingMode.Self,
                ActionType.FullAction,
                new EnergyCost { amount = 4 });

        // Pattern: status AoE — applies Slowed to all other vehicles in the caster's lane.
        private static Skill DefineOilSlick()
            => Make("Oil Slick",
                AlwaysApply(FX(Status(LoadEntityCondition("Slowed"), OnOtherLane))),
                TargetingMode.OwnLane,
                ActionType.BonusAction,
                new EnergyCost { amount = 2 });

        // Pattern: AoE with heavy self-harm — damages entire lane (including self), plus extra damage to self.
        private static Skill DefineSelfDestruct()
            => Make("Self-Destruct",
                AlwaysApply(FX(
                    Dmg(3, 6, 0, DamageType.Fire, OnOtherLane),
                    Dmg(4, 6, 0, DamageType.Fire, OnSelf))),
                TargetingMode.OwnLane,
                ActionType.FullAction);

        // ==================== WORKED EXAMPLES (TargettingRefactor.md) ====================

        // Pattern: fan-out save — each vehicle in the lane makes an independent Mobility save.
        // Full damage on fail, half on save. Caster excluded — they're outside the blast.
        private static Skill DefineFireball()
            => Make("Fireball",
                FanOut(new AllVehiclesInLaneResolver(excludeSelf: true),
                    Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 15,
                        onFail: FX(Dmg(3, 6, 0, DamageType.Fire, OnTarget)),
                        onPass: FX(Dmg(1, 6, 2, DamageType.Fire, OnTarget)))),
                TargetingMode.Lane,
                ActionType.Action,
                new EnergyCost { amount = 4 });

        // Pattern: chained fan-out — caster check gates per-target saves.
        // Node 1: Arcana check DC 14 (single execution). On success chains to Node 2.
        // Node 2: Mobility save DC 16 (fans out to each vehicle in lane). On fail: Overheating.
        private static Skill DefineEMPPulse()
            => Make("EMP Pulse",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Arcana, ComponentType.Sensors), 14,
                    onSuccess: FX(),
                    successChain: FanOut(new AllVehiclesInLaneResolver(),
                        Save(SaveSpec.ForVehicle(VehicleCheckAttribute.Mobility), 16,
                            onFail: FX(Status(LoadEntityCondition("Overheating"), OnTarget))))),
                TargetingMode.Lane,
                ActionType.Action,
                new EnergyCost { amount = 3 });

        // Pattern: failed check applies character condition to the rolling character's seat.
        // Navigator makes a Perception check; on fail, Blinded applies to the navigator's own seat.
        private static Skill DefineSmokeScreen()
            => Make("Smoke Screen",
                Check(SkillCheckSpec.ForCharacter(CharacterSkill.Perception, ComponentType.Sensors), 13,
                    onSuccess: FX(),
                    successChain: null,
                    onFail: FX(),
                    failChain: AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Blinded"), OnActorSeat)))),
                TargetingMode.Enemy,
                ActionType.BonusAction,
                new EnergyCost { amount = 2 });

        // Pattern: role-targeted seat effect — applies Stunned to the navigator on the target vehicle.
        // SeatByRoleResolver fans out to the navigator seat; if no navigator exists, the effect is skipped.
        private static Skill DefineFeedbackLoop()
            => Make("Feedback Loop",
                FanOut(new SeatByRoleResolver(RoleType.Navigator, SeatSource.TargetVehicle),
                    AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Stunned"), OnTargetSeat)))),
                TargetingMode.Enemy,
                ActionType.BonusAction,
                new EnergyCost { amount = 2 });

        // Pattern: attack with lane splash — 5d6 to the primary target, 1d6 to every other vehicle
        // in the same lane. Caster and primary target are both excluded from the splash.
        private static Skill DefineConcussionBlast()
            => Make("Concussion Blast",
                Attack(
                    FX(Dmg(5, 6, 0, DamageType.Bludgeoning, OnTarget)),
                    successChain: FanOut(new AllVehiclesInLaneResolver(excludeSelf: true, excludeTarget: true),
                        AlwaysApply(FX(Dmg(1, 6, 0, DamageType.Bludgeoning, OnTarget))))),
                TargetingMode.EnemyComponent,
                ActionType.FullAction,
                new EnergyCost { amount = 4 });

        // Pattern: multi-hit attack — 3 independent attack rolls against the same target, each resolved separately.
        private static Skill DefineRapidFire()
            => Make("Rapid Fire",
                FanOut(new RepeatTargetResolver { hitCount = 3 },
                    Attack(FX(Dmg(1, 6, 0, DamageType.Piercing, OnTarget)))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 2 });

        // ==================== WEAPON ATTACK SKILLS

        // Pattern: standard weapon attack — ammo-eligible, gains onHitNode from loaded AmmunitionType.
        private static WeaponAttackSkill DefineRifleShot()
            => Make<WeaponAttackSkill>("Rifle Shot",
                Attack(FX(Dmg(1, 8, 2, DamageType.Piercing, OnTarget))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 1 });

        // Pattern: rapid weapon burst — three independent attack rolls, each ammo-eligible.
        private static WeaponAttackSkill DefineBurstFire()
            => Make<WeaponAttackSkill>("Burst Fire",
                FanOut(new RepeatTargetResolver { hitCount = 3 },
                    Attack(FX(Dmg(1, 6, 0, DamageType.Piercing, OnTarget)))),
                TargetingMode.EnemyComponent,
                ActionType.Action,
                new EnergyCost { amount = 3 });

        // ==================== CONSUMABLE-GATED SKILLS

        // Pattern: consumable as fuel — requires one Incendiary Flask; fires the skill effects on use.
        private static Skill DefineMolotovThrow()
            => Make("Molotov Throw",
                AlwaysApply(FX(
                    Dmg(2, 6, 0, DamageType.Fire, OnTarget),
                    Status(LoadEntityCondition("Burning"), OnTarget))),
                TargetingMode.Enemy,
                ActionType.Action,
                new ConsumableCost { template = LoadConsumable("Incendiary Flask") });

        // Pattern: consumable as fuel — requires one Repair Kit; heals the chosen component on use.
        private static Skill DefineFieldRepair()
            => Make("Field Repair",
                AlwaysApply(FX(Heal(10, OnSourceComp))),
                TargetingMode.SourceComponent,
                ActionType.BonusAction,
                new ConsumableCost { template = LoadConsumable("Repair Kit") });

        // Part 3 (ApplyCharacterConditionEffect pending): apply Inspired to the active crew seat.
        // private static Skill DefineBattleCry()
        //     => Make("Battle Cry",
        //         AlwaysApply(FX(CharacterStatus(LoadCharacterCondition("Inspired"), EffectTarget.SourceVehicle))),
        //         TargetingMode.Self,
        //         energyCost: 2);

        private static void RegenerateSkill(Skill definition, string folder)
        {
            string path = $"{folder}/{definition.name}.asset";
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

        private static ConsumableBase LoadConsumable(string name)
            => AssetDatabase.LoadAssetAtPath<ConsumableBase>($"{ConsumableCreator.ConsumablesFolder}/{name}.asset");

        // ==================== BUILDER METHODS ====================
        // Mirrors TestSkillFactory — kept private here to avoid an editor→test assembly dependency.

        // ==================== RESOLVER SHORTCUTS ====================

        // Vehicle resolvers (default for Dmg, Heal, Energy, Status effects)
        private static IVehicleEffectResolver OnTarget => new TargetVehicleResolver();
        private static IVehicleEffectResolver OnSelf => new SourceVehicleResolver();
        private static IVehicleEffectResolver OnLane => new AllVehiclesInLaneEffectResolver();
        private static IVehicleEffectResolver OnOtherLane => new AllVehiclesInLaneEffectResolver { ExcludeSelf = true };
        private static IVehicleEffectResolver OnOtherStage => new AllVehiclesInStageEffectResolver { ExcludeSelf = true };

        // Entity resolvers (for component-specific and CustomEffect)
        private static IEntityEffectResolver OnTargetEntity => new TargetEntityResolver();
        private static IEntityEffectResolver OnSourceComp => new SourceComponentResolver();

        // Seat resolvers (for CharacterStatus effects)
        private static ISeatEffectResolver OnActorSeat => new SourceActorSeatResolver();
        private static ISeatEffectResolver OnTargetSeat => new TargetSeatResolver();

        private static List<IEffectInvocation> FX(params IEffectInvocation[] effects)
            => new(effects);

        private static IEffectInvocation Dmg(
            int dice, int dieSize, int bonus = 0,
            DamageType type = DamageType.Physical,
            IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnTarget,
                effect = new DamageEffect
                {
                    formulaProvider = new StaticFormulaProvider
                    {
                        formula = new DamageFormula { baseDice = dice, dieSize = dieSize, bonus = bonus, damageType = type }
                    }
                }
            };

        private static IEffectInvocation Dmg(
            int dice, int dieSize, int bonus,
            DamageType type,
            IEntityEffectResolver target)
            => new EntityEffectInvocation
            {
                targetResolver = target,
                effect = new DamageEffect
                {
                    formulaProvider = new StaticFormulaProvider
                    {
                        formula = new DamageFormula { baseDice = dice, dieSize = dieSize, bonus = bonus, damageType = type }
                    }
                }
            };

        private static IEffectInvocation Heal(int amount, IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Health, isDrain = false, bonus = amount } }
            };

        private static IEffectInvocation Heal(int amount, IEntityEffectResolver target)
            => new EntityEffectInvocation
            {
                targetResolver = target,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Health, isDrain = false, bonus = amount } }
            };

        private static IEffectInvocation Energy(int amount, IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnSelf,
                effect = new ResourceRestorationEffect { formula = new RestorationFormula { resourceType = ResourceType.Energy, isDrain = false, bonus = amount } }
            };

        private static IEffectInvocation Status(EntityCondition condition, IVehicleEffectResolver target = null)
            => new VehicleEffectInvocation
            {
                targetResolver = target ?? OnTarget,
                effect = new ApplyEntityConditionEffect { condition = condition }
            };

        private static IEffectInvocation CharacterStatus(CharacterCondition condition, ISeatEffectResolver target = null)
            => new SeatEffectInvocation
            {
                targetResolver = target ?? OnActorSeat,
                effect = new ApplyCharacterConditionEffect { condition = condition }
            };

        private static RollNode AlwaysApply(List<IEffectInvocation> effects)
            => new()
            { targetResolver = new CurrentTargetResolver(), successEffects = effects };

        /// <summary>Wraps a node with a targetResolver for fan-out execution.</summary>
        private static RollNode FanOut(IRollTargetResolver resolver, RollNode inner)
        {
            inner.targetResolver = resolver;
            return inner;
        }

        private static RollNode Attack(
            List<IEffectInvocation> onHit,
            List<IEffectInvocation> onMiss = null,
            RollNode successChain = null)
            => new()
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = new AttackSpec(),
                successEffects = onHit ?? new List<IEffectInvocation>(),
                failureEffects = onMiss ?? new List<IEffectInvocation>(),
                onSuccessChain = successChain
            };

        private static RollNode Attack(
            AttackSpec spec,
            List<IEffectInvocation> onHit,
            List<IEffectInvocation> onMiss = null,
            RollNode successChain = null)
            => new()
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                successEffects = onHit ?? new List<IEffectInvocation>(),
                failureEffects = onMiss ?? new List<IEffectInvocation>(),
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
            List<IEffectInvocation> onFail,
            List<IEffectInvocation> onPass = null,
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                failureEffects = onFail ?? new List<IEffectInvocation>(),
                successEffects = onPass ?? new List<IEffectInvocation>(),
                onFailureChain = failChain
            };
        }

        private static RollNode Check(
            SkillCheckSpec spec, int dc,
            List<IEffectInvocation> onSuccess,
            List<IEffectInvocation> onFail = null,
            RollNode successChain = null,
            RollNode failChain = null)
        {
            spec.dc = dc;
            return new RollNode
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                successEffects = onSuccess ?? new List<IEffectInvocation>(),
                failureEffects = onFail ?? new List<IEffectInvocation>(),
                onSuccessChain = successChain,
                onFailureChain = failChain
            };
        }

        private static RollNode Opposed(
            OpposedCheckRollSpec spec,
            List<IEffectInvocation> onWin,
            List<IEffectInvocation> onLose = null,
            RollNode winChain = null)
            => new()
            {
                targetResolver = new CurrentTargetResolver(),
                rollSpec = spec,
                successEffects = onWin ?? new List<IEffectInvocation>(),
                failureEffects = onLose ?? new List<IEffectInvocation>(),
                onSuccessChain = winChain
            };

        private static T Make<T>(
            string name,
            RollNode rollNode,
            TargetingMode targeting = TargetingMode.Enemy,
            ActionType actionCost = ActionType.Action,
            params ISkillCost[] costs) where T : Skill
        {
            var skill = ScriptableObject.CreateInstance<T>();
            skill.name = name;
            foreach (var cost in costs)
                skill.costs.Add(cost);
            skill.actionCost = actionCost;
            skill.rollNode = rollNode;
            skill.targetingMode = targeting;
            return skill;
        }

        private static Skill Make(
            string name,
            RollNode rollNode,
            TargetingMode targeting = TargetingMode.Enemy,
            ActionType actionCost = ActionType.Action,
            params ISkillCost[] costs)
            => Make<Skill>(name, rollNode, targeting, actionCost, costs);
    }
        #endregion
}
#endif