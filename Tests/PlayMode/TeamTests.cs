using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Assets.Scripts.Entities.Vehicles;
using Assets.Scripts.Stages;
using Assets.Scripts.Tests.Helpers;
using Assets.Scripts.Managers.Turn;
using Assets.Scripts.Managers.Race;

namespace Assets.Scripts.Tests.PlayMode
{
    internal class TeamTests
    {
        private readonly List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in cleanup)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            cleanup.Clear();
        }

        // ==================== HELPERS ====================

        private VehicleTeam CreateTeam(string name = "Team")
        {
            var team = ScriptableObject.CreateInstance<VehicleTeam>();
            team.name = name;
            cleanup.Add(team);
            return team;
        }

        private Vehicle CreateVehicle(string name = "Vehicle", VehicleTeam team = null, Stage stage = null)
        {
            var vehicle = new TestVehicleBuilder(name).WithChassis().Build();
            vehicle.team = team;
            if (stage != null)
            {
                RacePositionTracker.SetStage(vehicle, stage);
            }
            cleanup.Add(vehicle.gameObject);
            return vehicle;
        }

        private Stage CreateStage(string name = "Stage")
        {
            var stage = TestStageFactory.CreateStage(name, out var stageObj);
            cleanup.Add(stageObj);
            return stage;
        }

        // ==================== AreAllied ====================

        [Test]
        public void AreAllied_SameTeam_ReturnsTrue()
        {
            var team = CreateTeam();
            var a = CreateVehicle("A", team);
            var b = CreateVehicle("B", team);

            Assert.IsTrue(TurnService.AreAllied(a, b));
        }

        [Test]
        public void AreAllied_DifferentTeams_ReturnsFalse()
        {
            var a = CreateVehicle("A", CreateTeam("Red"));
            var b = CreateVehicle("B", CreateTeam("Blue"));

            Assert.IsFalse(TurnService.AreAllied(a, b));
        }

        [Test]
        public void AreAllied_OneIndependent_ReturnsFalse()
        {
            var a = CreateVehicle("A", CreateTeam());
            var b = CreateVehicle("B", team: null);

            Assert.IsFalse(TurnService.AreAllied(a, b));
        }

        [Test]
        public void AreAllied_BothIndependent_ReturnsFalse()
        {
            var a = CreateVehicle("A", team: null);
            var b = CreateVehicle("B", team: null);

            Assert.IsFalse(TurnService.AreAllied(a, b));
        }

        // ==================== AreHostile ====================

        [Test]
        public void AreHostile_DifferentTeams_ReturnsTrue()
        {
            var a = CreateVehicle("A", CreateTeam("Red"));
            var b = CreateVehicle("B", CreateTeam("Blue"));

            Assert.IsTrue(TurnService.AreHostile(a, b));
        }

        [Test]
        public void AreHostile_BothIndependent_ReturnsTrue()
        {
            var a = CreateVehicle("A", team: null);
            var b = CreateVehicle("B", team: null);

            Assert.IsTrue(TurnService.AreHostile(a, b));
        }

        [Test]
        public void AreHostile_SameTeam_ReturnsFalse()
        {
            var team = CreateTeam();
            var a = CreateVehicle("A", team);
            var b = CreateVehicle("B", team);

            Assert.IsFalse(TurnService.AreHostile(a, b));
        }

        [Test]
        public void AreHostile_SameVehicle_ReturnsFalse()
        {
            var a = CreateVehicle("A", CreateTeam());

            Assert.IsFalse(TurnService.AreHostile(a, a));
        }

        // ==================== GetOtherVehiclesInStage ====================

        [Test]
        public void GetOtherVehiclesInStage_ExcludesSelf()
        {
            var stage = CreateStage();
            var source = CreateVehicle("Source", stage: stage);
            var service = new TurnService(new List<Vehicle> { source });

            var result = service.GetOtherVehiclesInStage(source);

            Assert.IsFalse(result.Contains(source));
        }

        [Test]
        public void GetOtherVehiclesInStage_IncludesOthersInSameStage()
        {
            var stage = CreateStage();
            var source = CreateVehicle("Source", stage: stage);
            var other = CreateVehicle("Other", stage: stage);
            var service = new TurnService(new List<Vehicle> { source, other });

            var result = service.GetOtherVehiclesInStage(source);

            Assert.IsTrue(result.Contains(other));
        }

        [Test]
        public void GetOtherVehiclesInStage_ExcludesDifferentStage()
        {
            var stage1 = CreateStage("Stage1");
            var stage2 = CreateStage("Stage2");
            var source = CreateVehicle("Source", stage: stage1);
            var distant = CreateVehicle("Distant", stage: stage2);
            var service = new TurnService(new List<Vehicle> { source, distant });

            var result = service.GetOtherVehiclesInStage(source);

            Assert.IsFalse(result.Contains(distant));
        }

        [Test]
        public void GetOtherVehiclesInStage_IgnoresTeamBoundaries()
        {
            var stage = CreateStage();
            var team = CreateTeam();
            var source = CreateVehicle("Source", team, stage);
            var ally = CreateVehicle("Ally", team, stage);
            var enemy = CreateVehicle("Enemy", CreateTeam("Other"), stage);
            var service = new TurnService(new List<Vehicle> { source, ally, enemy });

            var result = service.GetOtherVehiclesInStage(source);

            Assert.IsTrue(result.Contains(ally));
            Assert.IsTrue(result.Contains(enemy));
        }

        // ==================== GetAllTargets ====================

        [Test]
        public void GetAllTargets_IncludesSelf()
        {
            var stage = CreateStage();
            var source = CreateVehicle("Source", stage: stage);
            var service = new TurnService(new List<Vehicle> { source });

            var result = service.GetAllTargets(source);

            Assert.IsTrue(result.Contains(source));
        }

        [Test]
        public void GetAllTargets_IncludesAllInStage()
        {
            var stage = CreateStage();
            var team = CreateTeam();
            var source = CreateVehicle("Source", team, stage);
            var ally = CreateVehicle("Ally", team, stage);
            var enemy = CreateVehicle("Enemy", CreateTeam("Other"), stage);
            var service = new TurnService(new List<Vehicle> { source, ally, enemy });

            var result = service.GetAllTargets(source);

            Assert.AreEqual(3, result.Count);
        }

        // ==================== GetAlliedTargets ====================

        [Test]
        public void GetAlliedTargets_ReturnsOnlySameTeamInStage()
        {
            var stage = CreateStage();
            var team = CreateTeam();
            var source = CreateVehicle("Source", team, stage);
            var ally = CreateVehicle("Ally", team, stage);
            var enemy = CreateVehicle("Enemy", CreateTeam("Other"), stage);
            var service = new TurnService(new List<Vehicle> { source, ally, enemy });

            var result = service.GetAlliedTargets(source);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(ally));
            Assert.IsFalse(result.Contains(enemy));
        }

        [Test]
        public void GetAlliedTargets_ExcludesSelf()
        {
            var stage = CreateStage();
            var team = CreateTeam();
            var source = CreateVehicle("Source", team, stage);
            var service = new TurnService(new List<Vehicle> { source });

            var result = service.GetAlliedTargets(source);

            Assert.IsFalse(result.Contains(source));
        }

        [Test]
        public void GetAlliedTargets_IndependentVehicle_ReturnsEmpty()
        {
            var stage = CreateStage();
            var source = CreateVehicle("Source", team: null, stage: stage);
            var other = CreateVehicle("Other", team: null, stage: stage);
            var service = new TurnService(new List<Vehicle> { source, other });

            var result = service.GetAlliedTargets(source);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAlliedTargets_ExcludesAlliesInDifferentStage()
        {
            var stage1 = CreateStage("Stage1");
            var stage2 = CreateStage("Stage2");
            var team = CreateTeam();
            var source = CreateVehicle("Source", team, stage1);
            var allyElsewhere = CreateVehicle("AllyElsewhere", team, stage2);
            var service = new TurnService(new List<Vehicle> { source, allyElsewhere });

            var result = service.GetAlliedTargets(source);

            Assert.AreEqual(0, result.Count);
        }
    }
}
