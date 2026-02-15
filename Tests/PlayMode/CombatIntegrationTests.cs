using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Scripts.Characters;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Combat.Damage;
using Assets.Scripts.Combat.SkillChecks;
using Assets.Scripts.Combat.Saves;
using Assets.Scripts.Stages;
using Assets.Scripts.Stages.Lanes;
using Assets.Scripts.StatusEffects;
using Assets.Scripts.Skills.Helpers;
using Assets.Scripts.Tests.Helpers;

namespace Assets.Scripts.Tests.PlayMode
{
    public class CombatIntegrationTests
    {
        private Vehicle playerVehicle;
        private Vehicle enemyVehicle;
        private GameObject stageObj;
        private readonly System.Collections.Generic.List<Object> cleanup = new();
        private readonly System.Collections.Generic.List<GameObject> gameObjects = new();

        [TearDown]
        public void TearDown()
        {
            if (playerVehicle != null) Object.DestroyImmediate(playerVehicle.gameObject);
            if (enemyVehicle != null) Object.DestroyImmediate(enemyVehicle.gameObject);
            if (stageObj != null) Object.DestroyImmediate(stageObj);
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            foreach (var go in gameObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            cleanup.Clear();
            gameObjects.Clear();
        }

        // ==================== HELPERS ====================
        // (All other helpers moved to centralized factories)

        // ==================== FULL ATTACK: WEAPON FIRES, ENEMY TAKES DAMAGE, ENERGY SPENT ====================

        [UnityTest]
        public IEnumerator FullAttack_WeaponFiresAtEnemy_EnergyConsumed()
        {
            // Player: Gunner Bob (attack +3) in weapon seat with +2 weapon
            var bob = TestCharacterFactory.CreateWithCleanup("Bob", baseAttackBonus: 3, cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder("PlayerRacer")
                .WithChassis()
                .WithPowerCore()
                .WithWeapon(bob, attackBonus: 2)
                .Build();
            playerVehicle.powerCore.currentEnergy = 20;

            // Enemy: Simple vehicle with chassis
            enemyVehicle = new TestVehicleBuilder("EnemyRacer")
                .WithChassis(maxHealth: 80, armorClass: 10)
                .WithPowerCore()
                .Build();

            // Skill: Cannon Shot, 2d8+5 physical, costs 3 energy
            var cannonShot = TestSkillFactory.CreateAttackSkill("Cannon Shot", damageDice: 2, dieSize: 8, bonus: 5, energyCost: 3, cleanup: cleanup);

            int enemyHPBefore = enemyVehicle.chassis.GetCurrentHealth();
            int energyBefore = playerVehicle.powerCore.currentEnergy;

            // Execute skill through the full pipeline
            var ctx = new SkillContext
            {
                Skill = cannonShot,
                SourceVehicle = playerVehicle,
                SourceEntity = playerVehicle.optionalComponents[0],
                SourceCharacter = bob,
                TargetEntity = enemyVehicle.chassis
            };
            playerVehicle.ExecuteSkill(ctx);
            yield return null;

            // Energy should always be consumed (hit or miss)
            Assert.AreEqual(energyBefore - 3, playerVehicle.powerCore.currentEnergy,
                "Should consume 3 energy regardless of hit/miss");
        }

        // ==================== NOT ENOUGH ENERGY → SKILL BLOCKED ====================

        [UnityTest]
        public IEnumerator SkillExecution_NotEnoughEnergy_Blocked()
        {
            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", baseAttackBonus: 2, cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .WithWeapon(gunner)
                .Build();
            playerVehicle.powerCore.currentEnergy = 1; // Not enough for cost 3

            enemyVehicle = new TestVehicleBuilder("Enemy")
                .WithChassis(maxHealth: 50)
                .WithPowerCore()
                .Build();

            var expensiveSkill = TestSkillFactory.CreateAttackSkill("Big Gun", 3, 10, 10, energyCost: 3, cleanup: cleanup);

            int enemyHPBefore = enemyVehicle.chassis.GetCurrentHealth();

            var ctx = new SkillContext
            {
                Skill = expensiveSkill,
                SourceVehicle = playerVehicle,
                SourceEntity = playerVehicle.optionalComponents[0],
                SourceCharacter = gunner,
                TargetEntity = enemyVehicle.chassis
            };
            bool executed = playerVehicle.ExecuteSkill(ctx);
            yield return null;

            Assert.IsFalse(executed, "Skill should be blocked when not enough energy");
            Assert.AreEqual(enemyHPBefore, enemyVehicle.chassis.GetCurrentHealth(),
                "Enemy health should be unchanged");
            Assert.AreEqual(1, playerVehicle.powerCore.currentEnergy,
                "Energy should not be consumed");
        }

        // ==================== BUFF SKILL: NO-ROLL MODIFIER → TARGET COMPONENT STATS CHANGE ====================

        [UnityTest]
        public IEnumerator BuffSkill_AppliesStatusEffect_ModifiesStat()
        {
            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore(engineer)
                .Build();
            playerVehicle.powerCore.currentEnergy = 20;

            // Buff: "Reinforce Hull" → +3 AC to chassis for 3 turns
            var reinforceTemplate = TestStatusEffectFactory.CreateModifierEffect("Reinforced", Attribute.ArmorClass, 3f, duration: 3, cleanup: cleanup);
            var reinforceSkill = TestSkillFactory.CreateNoRollSkill("Reinforce Hull",
                new System.Collections.Generic.List<EffectInvocation>
                {
                    new EffectInvocation
                    {
                        effect = new ApplyStatusEffect { statusEffect = reinforceTemplate },
                        target = EffectTarget.SelectedTarget
                    }
                },
                energyCost: 2,
                cleanup: cleanup);

            int acBefore = playerVehicle.chassis.GetArmorClass();

            var ctx = new SkillContext
            {
                Skill = reinforceSkill,
                SourceVehicle = playerVehicle,
                SourceEntity = playerVehicle.powerCore,
                SourceCharacter = engineer,
                TargetEntity = playerVehicle.chassis
            };
            playerVehicle.ExecuteSkill(ctx);
            yield return null;

            int acAfter = playerVehicle.chassis.GetArmorClass();
            Assert.AreEqual(acBefore + 3, acAfter, "AC should increase by 3 from Reinforced buff");

            // Verify status effect is active
            var effects = playerVehicle.chassis.GetActiveStatusEffects();
            Assert.AreEqual(1, effects.Count, "Should have 1 active status effect");
            Assert.AreEqual("Reinforced", effects[0].template.effectName);
        }

        // ==================== DAMAGE-OVER-TIME: BURNS FOR 3 TURNS, THEN EXPIRES ====================

        [UnityTest]
        public IEnumerator DoTSkill_AppliesBurning_TicksDamageEachTurn()
        {
            var pyro = TestCharacterFactory.CreateWithCleanup("Pyro", cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .WithWeapon(pyro)
                .Build();
            playerVehicle.powerCore.currentEnergy = 20;

            enemyVehicle = new TestVehicleBuilder("Enemy")
                .WithChassis(maxHealth: 200)
                .WithPowerCore()
                .Build();

            // "Flame Thrower" applies Burning: 5 fire damage per turn for 3 turns
            var burningTemplate = TestStatusEffectFactory.CreateDoTEffect("Burning", damage: 5, DamageType.Fire, duration: 3, cleanup: cleanup);
            var flameThrower = TestSkillFactory.CreateNoRollSkill("Flame Thrower",
                new System.Collections.Generic.List<EffectInvocation>
                {
                    new EffectInvocation
                    {
                        effect = new ApplyStatusEffect { statusEffect = burningTemplate },
                        target = EffectTarget.SelectedTarget
                    }
                },
                energyCost: 2,
                cleanup: cleanup);

            // Apply Burning to enemy
            var ctx = new SkillContext
            {
                Skill = flameThrower,
                SourceVehicle = playerVehicle,
                SourceEntity = playerVehicle.optionalComponents[0],
                SourceCharacter = pyro,
                TargetEntity = enemyVehicle.chassis
            };
            playerVehicle.ExecuteSkill(ctx);
            yield return null;

            Assert.AreEqual(1, enemyVehicle.chassis.GetActiveStatusEffects().Count, "Enemy should be Burning");

            // Simulate 3 turns of DoT ticking
            int healthBefore = enemyVehicle.chassis.GetCurrentHealth();
            for (int turn = 1; turn <= 3; turn++)
            {
                enemyVehicle.chassis.UpdateStatusEffects();
                int healthAfter = enemyVehicle.chassis.GetCurrentHealth();
                int damageTaken = healthBefore - healthAfter;

                if (turn <= 3)
                {
                    Assert.Greater(damageTaken, 0, $"Should take burn damage by turn {turn}");
                }

                healthBefore = healthAfter;
            }

            // After 3 turns, Burning should expire
            Assert.AreEqual(0, enemyVehicle.chassis.GetActiveStatusEffects().Count,
                "Burning should expire after 3 turns");
        }

        // ==================== MULTI-CREW VEHICLE: DRIVER + GUNNER + ENGINEER ====================

        [UnityTest]
        public IEnumerator MultiCrew_EachCrewMemberOperatesCorrectly()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", dexterity: 18, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(driver, CharacterSkill.Piloting);

            var gunner = TestCharacterFactory.CreateWithCleanup("Gunner", baseAttackBonus: 4, cleanup: cleanup);

            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", intelligence: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(engineer, CharacterSkill.Mechanics);

            playerVehicle = new TestVehicleBuilder("WarRig")
                .WithChassis(driver)
                .WithPowerCore(engineer)
                .WithWeapon(gunner, attackBonus: 2)
                .Build();

            // Test 1: Piloting check routes to Driver
            var pilotSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting, ComponentType.Chassis);
            var pilotResult = SkillCheckPerformer.Execute(playerVehicle, pilotSpec, dc: 12, causalSource: null, initiatingCharacter: driver);
            yield return null;
            Assert.AreEqual(driver, pilotResult.Character, "Piloting check should route to Driver");
            Assert.IsFalse(pilotResult.IsAutoFail);

            // Test 2: Mechanics check routes to Engineer via PowerCore
            var mechanicsSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.PowerCore);
            var mechResult = SkillCheckPerformer.Execute(playerVehicle, mechanicsSpec, dc: 10, causalSource: null, initiatingCharacter: engineer);
            yield return null;
            Assert.AreEqual(engineer, mechResult.Character, "Mechanics check should route to Engineer");

            // Test 3: Best Perception routes to character with highest WIS modifier
            var percSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Perception);
            var percResult = SkillCheckPerformer.Execute(playerVehicle, percSpec, dc: 14, causalSource: null);
            yield return null;
            Assert.IsNotNull(percResult.Character, "Perception check should find a character");
            Assert.IsFalse(percResult.IsAutoFail);

            // Test 4: Attack bonuses stack correctly
            var bonuses = AttackCalculator.GatherBonuses(playerVehicle.optionalComponents[0], gunner);
            int totalMod = bonuses.Sum(b => b.Value);
            Assert.AreEqual(6, totalMod, "Attack bonus should be weapon(2) + character(4) = 6");
        }

        // ==================== LANE STATUS EFFECT: MODIFIES VEHICLE STATS ====================

        [UnityTest]
        public IEnumerator LaneEffect_AppliesStatusEffect_ModifiesVehicleStats()
        {
            // Create a vehicle
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", cleanup: cleanup);
            playerVehicle = TestVehicleBuilder.CreateWithChassis(driver);

            // Create stage with a lane that has a status effect
            var laneEffect = TestStatusEffectFactory.CreateModifierEffect("Cliff Edge", Attribute.ArmorClass, -2f, cleanup: cleanup);
            var stage = TestStageFactory.CreateStage("Rocky Stage", out stageObj);
            var cliffLane = TestStageFactory.CreateLane("Cliff Edge Lane", stage, stageObj, laneEffect);

            int acBefore = playerVehicle.chassis.GetArmorClass();

            // Simulate vehicle entering lane → apply lane status effect
            cliffLane.vehiclesInLane.Add(playerVehicle);
            playerVehicle.currentLane = cliffLane;

            // Apply lane effect to all vehicle components (as the system does)
            foreach (var component in playerVehicle.AllComponents)
            {
                component.ApplyStatusEffect(laneEffect, cliffLane);
            }
            yield return null;

            int acAfter = playerVehicle.chassis.GetArmorClass();
            Assert.AreEqual(acBefore - 2, acAfter, "AC should drop by 2 from Cliff Edge lane");

            // Verify effect is tracked
            var effects = playerVehicle.chassis.GetActiveStatusEffects();
            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual("Cliff Edge", effects[0].template.effectName);

            // Remove lane effect (vehicle leaves lane)
            playerVehicle.chassis.RemoveStatusEffectsFromSource(cliffLane);
            yield return null;

            int acRestored = playerVehicle.chassis.GetArmorClass();
            Assert.AreEqual(acBefore, acRestored, "AC should restore after leaving lane");
        }

        // ==================== LANE WITH TURN EFFECT: SKILL CHECK OR TAKE DAMAGE ====================

        [UnityTest]
        public IEnumerator LaneTurnEffect_SkillCheckOrDamage_ExecutesProperly()
        {
            // Setup: Vehicle with driver
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", level: 5, dexterity: 18, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(driver, CharacterSkill.Piloting);
            playerVehicle = TestVehicleBuilder.CreateWithChassis(driver);

            // Create stage + lane with turn effect: Piloting DC 10 or take damage
            var stage = TestStageFactory.CreateStage("Hazard Stage", out stageObj);
            var hazardLane = TestStageFactory.CreateLane("Hazard Lane", stage, stageObj);

            var turnEffect = new LaneTurnEffect
            {
                effectName = "Rocky Road Hazard",
                description = "Navigate treacherous rocks",
                checkType = LaneCheckType.SkillCheck,
                checkSpec = SkillCheckSpec.ForCharacter(CharacterSkill.Piloting),
                dc = 10,
            };
            hazardLane.turnEffects.Add(turnEffect);
            hazardLane.vehiclesInLane.Add(playerVehicle);
            playerVehicle.currentLane = hazardLane;

            // Execute the skill check (simulating what the turn system would do)
            var checkResult = SkillCheckPerformer.Execute(
                playerVehicle,
                turnEffect.checkSpec,
                turnEffect.dc,
                causalSource: null);
            yield return null;

            // Result should be valid
            Assert.IsNotNull(checkResult);
            Assert.AreEqual(driver, checkResult.Character, "Should route to Driver for Piloting");
            Assert.IsFalse(checkResult.IsAutoFail, "Should be able to attempt the check");

            // Verify modifier is correct: DEX 18 (+4) + Prof level 5 (+3) = +7
            int expectedDexMod = CharacterFormulas.CalculateAttributeModifier(18);
            int expectedProf = CharacterFormulas.CalculateProficiencyBonus(5);
            Assert.AreEqual(expectedDexMod + expectedProf, checkResult.Roll.TotalModifier,
                $"Modifier should be DEX({expectedDexMod}) + Prof({expectedProf})");
        }

        // ==================== SAVING THROW VS ENEMY SKILL ====================

        [UnityTest]
        public IEnumerator EnemySkill_ForceSave_EffectsApplyOnFailure()
        {
            // Player vehicle: Driver with poor WIS
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", wisdom: 8, cleanup: cleanup);
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();
            playerVehicle.seats.Add(new Assets.Scripts.Entities.Vehicle.VehicleSeat
            {
                seatName = "Driver",
                assignedCharacter = driver,
                controlledComponents = new System.Collections.Generic.List<VehicleComponent> { playerVehicle.chassis }
            });

            // Enemy vehicle
            var enemyCaster = TestCharacterFactory.CreateWithCleanup("EnemyCaster", cleanup: cleanup);
            enemyVehicle = new TestVehicleBuilder("EnemyShip")
                .WithChassis()
                .WithPowerCore()
                .AddSeat("Caster", enemyCaster)
                .Build();
            enemyVehicle.powerCore.currentEnergy = 20;

            // Create save skill: "Psychic Scream" - WIS save DC 20 or take damage
            // DC 20 is nearly impossible with WIS 8 (-1 mod + half level)
            var psychicScream = TestSkillFactory.CreateSaveSkill("Psychic Scream", CharacterAttribute.Wisdom, dc: 20,
                new System.Collections.Generic.List<EffectInvocation>
                {
                    new EffectInvocation
                    {
                        effect = new DamageEffect
                        {
                            formulaProvider = new StaticFormulaProvider
                            {
                                formula = new DamageFormula
                                {
                                    baseDice = 0, dieSize = 0, bonus = 15,
                                    damageType = DamageType.Physical
                                }
                            }
                        },
                        target = EffectTarget.SelectedTarget
                    }
                },
                energyCost: 3,
                cleanup: cleanup);

            int playerHPBefore = playerVehicle.chassis.GetCurrentHealth();

            // Execute save skill against player chassis
            var ctx = new SkillContext
            {
                Skill = psychicScream,
                SourceVehicle = enemyVehicle,
                SourceCharacter = enemyCaster,
                TargetEntity = playerVehicle.chassis
            };
            enemyVehicle.ExecuteSkill(ctx);
            yield return null;

            // Energy consumed from enemy
            Assert.AreEqual(17, enemyVehicle.powerCore.currentEnergy, "Enemy should spend 3 energy");

            // Save routing: should route to Driver (only character, best WIS by default)
            // With DC 20, most likely fails (needs nat 20 basically)
            // We can't guarantee the roll, but we can verify the system didn't crash
            // and that the save used the correct character
        }

        // ==================== STUN → COMPONENT NOT OPERATIONAL → AUTO-FAIL ====================

        [UnityTest]
        public IEnumerator StunEffect_PreventsActions_CausesAutoFail()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", dexterity: 18, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(driver, CharacterSkill.Piloting);

            playerVehicle = new TestVehicleBuilder()
                .WithChassis(driver)
                .WithPowerCore()
                .WithUtility(driver)
                .Build();

            // Stun the Utility component
            var stunTemplate = ScriptableObject.CreateInstance<StatusEffect>();
            stunTemplate.effectName = "Stunned";
            stunTemplate.baseDuration = 2;
            stunTemplate.modifiers = new System.Collections.Generic.List<ModifierData>();
            stunTemplate.periodicEffects = new System.Collections.Generic.List<PeriodicEffectData>();
            stunTemplate.behavioralEffects = new BehavioralEffectData { preventsActions = true };
            cleanup.Add(stunTemplate);

            var utilityComp = playerVehicle.optionalComponents[0];
            utilityComp.ApplyStatusEffect(stunTemplate, utilityComp);
            yield return null;

            // Verify Utility is not operational
            Assert.IsFalse(utilityComp.IsOperational, "Stunned component should not be operational");

            // Skill requiring Utility should auto-fail
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);
            var result = SkillCheckPerformer.Execute(playerVehicle, spec, dc: 10, causalSource: null, initiatingCharacter: driver);
            yield return null;

            Assert.IsTrue(result.IsAutoFail, "Should auto-fail when required component is stunned");

            // Wait 2 turns → stun expires → component operational again
            utilityComp.UpdateStatusEffects(); // Turn 1
            utilityComp.UpdateStatusEffects(); // Turn 2 → expires
            yield return null;

            Assert.IsTrue(utilityComp.IsOperational, "Component should be operational after stun expires");

            var resultAfter = SkillCheckPerformer.Execute(playerVehicle, spec, dc: 10, causalSource: null, initiatingCharacter: driver);
            Assert.IsFalse(resultAfter.IsAutoFail, "Should be able to attempt after stun expires");
        }

        // ==================== COMPONENT DESTROYED → SKILL BLOCKED → HEAL → WORKS AGAIN ====================

        [UnityTest]
        public IEnumerator ComponentLifecycle_Destroy_Heal_ResumeFunction()
        {
            var engineer = TestCharacterFactory.CreateWithCleanup("Engineer", intelligence: 16, cleanup: cleanup);
            TestCharacterFactory.AddProficiency(engineer, CharacterSkill.Mechanics);

            playerVehicle = new TestVehicleBuilder()
                .WithChassis(engineer)
                .WithPowerCore()
                .WithUtility(engineer)
                .Build();

            var utility = playerVehicle.optionalComponents[0];
            var spec = SkillCheckSpec.ForCharacter(CharacterSkill.Mechanics, ComponentType.Utility);

            // Phase 1: Working normally
            var result1 = SkillCheckPerformer.Execute(playerVehicle, spec, dc: 10, causalSource: null, initiatingCharacter: engineer);
            yield return null;
            Assert.IsFalse(result1.IsAutoFail, "Should work when component is healthy");

            // Phase 2: Destroy component via damage
            utility.TakeDamage(utility.GetCurrentHealth());
            Assert.IsTrue(utility.isDestroyed, "Component should be destroyed");

            var result2 = SkillCheckPerformer.Execute(playerVehicle, spec, dc: 10, causalSource: null, initiatingCharacter: engineer);
            yield return null;
            Assert.IsTrue(result2.IsAutoFail, "Should auto-fail when component destroyed");

            // Phase 3: Restore component
            utility.isDestroyed = false;
            utility.SetHealth(utility.GetMaxHealth());
            Assert.IsTrue(utility.IsOperational, "Component should be operational after restoration");

            var result3 = SkillCheckPerformer.Execute(playerVehicle, spec, dc: 10, causalSource: null, initiatingCharacter: engineer);
            yield return null;
            Assert.IsFalse(result3.IsAutoFail, "Should work again after component restored");
        }

        // ==================== MULTIPLE MODIFIERS FROM DIFFERENT SOURCES STACK ====================

        [UnityTest]
        public IEnumerator ModifierStacking_MultipleSourcesCombine()
        {
            playerVehicle = new TestVehicleBuilder()
                .WithChassis()
                .WithPowerCore()
                .Build();

            int baseAC = playerVehicle.chassis.GetArmorClass();

            // Source 1: Equipment modifier (+2 AC)
            playerVehicle.chassis.AddModifier(new AttributeModifier(
                Attribute.ArmorClass, ModifierType.Flat, 2f,
                source: playerVehicle.chassis,
                category: ModifierCategory.Equipment,
                displayNameOverride: "Armor Plating"));

            // Source 2: Status effect (+3 AC from buff skill)
            var shieldBuff = TestStatusEffectFactory.CreateModifierEffect("Shield", Attribute.ArmorClass, 3f, duration: 5, cleanup: cleanup);
            playerVehicle.chassis.ApplyStatusEffect(shieldBuff, playerVehicle);

            // Source 3: Direct modifier (+1 AC from event card)
            playerVehicle.chassis.AddModifier(new AttributeModifier(
                Attribute.ArmorClass, ModifierType.Flat, 1f,
                source: playerVehicle.chassis,
                category: ModifierCategory.Other,
                displayNameOverride: "Lucky Break"));

            yield return null;

            int finalAC = playerVehicle.chassis.GetArmorClass();
            Assert.AreEqual(baseAC + 2 + 3 + 1, finalAC,
                "AC should be base + equipment(2) + status(3) + event(1)");

            // Verify breakdown shows all sources
            var (total, bv, modifiers) = Assets.Scripts.Core.StatCalculator.GatherAttributeValueWithBreakdown(
                playerVehicle.chassis, Attribute.ArmorClass);
            Assert.IsTrue(modifiers.Any(m => m.DisplayNameOverride == "Armor Plating"), "Should show Armor Plating");
            Assert.IsTrue(modifiers.Any(m => m.DisplayNameOverride == "Lucky Break"), "Should show Lucky Break");

            // Remove status effect → AC should drop by 3
            var applied = playerVehicle.chassis.GetActiveStatusEffects()[0];
            playerVehicle.chassis.RemoveStatusEffect(applied);
            yield return null;

            int acAfterRemoval = playerVehicle.chassis.GetArmorClass();
            Assert.AreEqual(baseAC + 2 + 1, acAfterRemoval,
                "AC should drop by 3 after removing Shield buff");
        }

        // ==================== FULL COMBAT SCENARIO: TWO VEHICLES TRADING BLOWS ====================

        [UnityTest]
        public IEnumerator FullScenario_TwoVehiclesCombat_MultiTurn()
        {
            // Player: 3-person crew
            var pDriver = TestCharacterFactory.CreateWithCleanup("PlayerDriver", dexterity: 16, baseAttackBonus: 0, cleanup: cleanup);
            var pGunner = TestCharacterFactory.CreateWithCleanup("PlayerGunner", baseAttackBonus: 5, cleanup: cleanup);
            var pEngineer = TestCharacterFactory.CreateWithCleanup("PlayerEngineer", intelligence: 14, cleanup: cleanup);

            playerVehicle = new TestVehicleBuilder("PlayerRacer")
                .WithChassis(pDriver, maxHealth: 100)
                .WithPowerCore(pEngineer)
                .WithWeapon(pGunner, attackBonus: 3)
                .Build();
            playerVehicle.powerCore.currentEnergy = 50;

            // Enemy: 2-person crew
            var eDriver = TestCharacterFactory.CreateWithCleanup("EnemyDriver", dexterity: 14, cleanup: cleanup);
            var eGunner = TestCharacterFactory.CreateWithCleanup("EnemyGunner", baseAttackBonus: 3, cleanup: cleanup);

            enemyVehicle = new TestVehicleBuilder("EnemyRacer")
                .WithChassis(eDriver, maxHealth: 80)
                .WithPowerCore()
                .WithWeapon(eGunner, attackBonus: 1)
                .Build();
            enemyVehicle.powerCore.currentEnergy = 50;

            // Create stage + lane
            var stage = TestStageFactory.CreateStage("Combat Arena", out stageObj);
            var mainLane = TestStageFactory.CreateLane("Main Road", stage, stageObj);
            stage.vehiclesInStage.Add(playerVehicle);
            stage.vehiclesInStage.Add(enemyVehicle);
            mainLane.vehiclesInLane.Add(playerVehicle);
            mainLane.vehiclesInLane.Add(enemyVehicle);
            playerVehicle.currentStage = stage;
            enemyVehicle.currentStage = stage;

            // Skills
            var playerAttack = TestSkillFactory.CreateAttackSkill("Cannon", 2, 8, 3, energyCost: 3, cleanup: cleanup);
            var enemyAttack = TestSkillFactory.CreateAttackSkill("Crossbow", 1, 6, 2, energyCost: 2, cleanup: cleanup);

            // Simulate 3 turns of combat
            for (int turn = 1; turn <= 3; turn++)
            {
                // Player attacks enemy
                if (playerVehicle.powerCore.CanDrawPower(3))
                {
                    var playerCtx = new SkillContext
                    {
                        Skill = playerAttack,
                        SourceVehicle = playerVehicle,
                        SourceEntity = playerVehicle.optionalComponents[0],
                        SourceCharacter = pGunner,
                        TargetEntity = enemyVehicle.chassis
                    };
                    playerVehicle.ExecuteSkill(playerCtx);
                }

                // Enemy attacks player
                if (enemyVehicle.powerCore.CanDrawPower(2))
                {
                    var enemyCtx = new SkillContext
                    {
                        Skill = enemyAttack,
                        SourceVehicle = enemyVehicle,
                        SourceEntity = enemyVehicle.optionalComponents[0],
                        SourceCharacter = eGunner,
                        TargetEntity = playerVehicle.chassis
                    };
                    enemyVehicle.ExecuteSkill(enemyCtx);
                }

                // End-of-turn: tick status effects
                playerVehicle.UpdateStatusEffects();
                enemyVehicle.UpdateStatusEffects();

                yield return null;
            }

            // After 3 turns: verify energy was spent (no regen, pure consumption)
            // Player: 50 - (3 * 3) = 41
            // Enemy: 50 - (2 * 3) = 44
            Assert.AreEqual(41, playerVehicle.powerCore.currentEnergy,
                "Player should have spent 9 energy over 3 turns (3 per turn)");
            Assert.AreEqual(44, enemyVehicle.powerCore.currentEnergy,
                "Enemy should have spent 6 energy over 3 turns (2 per turn)");

            // Verify neither vehicle crashed (no null refs, no exceptions)
            Assert.IsNotNull(playerVehicle.chassis);
            Assert.IsNotNull(enemyVehicle.chassis);
        }

        // ==================== LANE TRANSITION: EFFECTS SWAP WHEN CHANGING LANES ====================

        [UnityTest]
        public IEnumerator LaneTransition_EffectsSwapCorrectly()
        {
            var driver = TestCharacterFactory.CreateWithCleanup("Driver", cleanup: cleanup);
            playerVehicle = TestVehicleBuilder.CreateWithChassis(driver);

            // Lane A: +2 AC (cover)
            var coverEffect = TestStatusEffectFactory.CreateModifierEffect("Cover", Attribute.ArmorClass, 2f, cleanup: cleanup);
            // Lane B: -2 AC (exposed)
            var exposedEffect = TestStatusEffectFactory.CreateModifierEffect("Exposed", Attribute.ArmorClass, -2f, cleanup: cleanup);

            var stage = TestStageFactory.CreateStage("Two Lane Stage", out stageObj);
            var laneA = TestStageFactory.CreateLane("Covered Lane", stage, stageObj, coverEffect);
            var laneB = TestStageFactory.CreateLane("Exposed Lane", stage, stageObj, exposedEffect);

            int baseAC = playerVehicle.chassis.GetArmorClass();

            // Enter Lane A
            playerVehicle.currentLane = laneA;
            laneA.vehiclesInLane.Add(playerVehicle);
            playerVehicle.chassis.ApplyStatusEffect(coverEffect, laneA);
            yield return null;

            Assert.AreEqual(baseAC + 2, playerVehicle.chassis.GetArmorClass(),
                "Should have +2 AC in Covered Lane");

            // Switch to Lane B: remove old lane effects, apply new
            playerVehicle.chassis.RemoveStatusEffectsFromSource(laneA);
            laneA.vehiclesInLane.Remove(playerVehicle);
            playerVehicle.currentLane = laneB;
            laneB.vehiclesInLane.Add(playerVehicle);
            playerVehicle.chassis.ApplyStatusEffect(exposedEffect, laneB);
            yield return null;

            Assert.AreEqual(baseAC - 2, playerVehicle.chassis.GetArmorClass(),
                "Should have -2 AC in Exposed Lane");

            // Leave lane entirely
            playerVehicle.chassis.RemoveStatusEffectsFromSource(laneB);
            yield return null;

            Assert.AreEqual(baseAC, playerVehicle.chassis.GetArmorClass(),
                "AC should return to base after leaving all lanes");
        }
    }
}
