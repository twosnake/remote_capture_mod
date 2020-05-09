using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities;
using Sandbox.Common;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ConquestGame
{
    using BlocksDict = Dictionary<long, IMyCubeBlock>;
    using GridsDict = Dictionary<long, IMyCubeGrid>;
    using FactionDict = Dictionary<string, IMyFaction>;
    using FactionPointsDict = Dictionary<string, int>;
    using CargoResourceNodeList = List<ConquestGameModeTeamsResourceNodeSpawning>;

    public enum ConquestGameResourceNodeState
    {
        Neutral = 1,
        UnderAttack = 2,
        Locked = 4,
    }

    class ConquestGameModeTeamsResourceNode
    {
        public int GridId { get; }

        private IMyFaction ControllingFaction = null;
        private IMyFaction LeadingWinner = null;
        private FactionDict FactionsInSafeZone = new FactionDict();
        private MyCubeGrid Grid;
        private BlocksDict CargoBlocks;
        private MySafeZone SafeZone;
        private GridsDict GridsInSafeZone;
        private ushort SecondsCountDown = 3;
        private DateTime NextCaptureGoalUpdate = new DateTime();
        private DateTime NextResourceSpawnUpdate = new DateTime();
        private FactionPointsDict FactionsCapturePoints = new FactionPointsDict();
        private ConquestGameResourceNodeState NodeState = ConquestGameResourceNodeState.Neutral;
        private CargoResourceNodeList CargoResources = new CargoResourceNodeList();


        public ConquestGameModeTeamsResourceNode(int gridId, MyCubeGrid grid) {
            if (grid == null) {
                throw new System.Exception("Resource Node Grid is null");
            }
            GridId = gridId;
            Grid = grid;
            CargoBlocks = getCargoBlocks();
            GridsInSafeZone = new GridsDict();

            if (CargoBlocks.Count != 0) {
                // Spawn the safe zone around the first cargo block found
                var cargoBlock = CargoBlocks.First().Value as IMyCubeBlock;
                var offset = cargoBlock.WorldMatrix;

                offset += MatrixD.CreateFromAxisAngle(offset.Up, ConquestGameHelper.deg2rad(0));
                offset += MatrixD.CreateFromAxisAngle(offset.Right, ConquestGameHelper.deg2rad(0));
                var pos = new MyPositionAndOrientation(offset);
                SafeZone = ConquestGameHelper.CreateSafezone(OPTIONS.ResourceNodeGridName+" "+GridId, pos);
            }

            foreach(var cargoBlock in CargoBlocks) {
                CargoResources.Add(new ConquestGameModeTeamsResourceNodeSpawning(GridId, cargoBlock.Value));
            }
        }

        public void UpdateEachSecond() {

            if (SecondsCountDown < 1) {
                UpdateEntitiesInSafeZone();
                CountAttackingGrids();
                SafeZoneCaptureLogic();
                ResourceSpawningLogic();
                SecondsCountDown = 3;
            }
            SecondsCountDown--;

//            MySessionComponentSafeZones.RequestDeleteSafeZone(SafeZone.EntityId);
        }

        private void ChangeNodeState(ConquestGameResourceNodeState state) {

            NodeState = state;

            MyObjectBuilder_SafeZone ob;
            ob = SafeZone.GetObjectBuilder(false) as MyObjectBuilder_SafeZone;

            switch(state) {
                case ConquestGameResourceNodeState.Neutral:
                    if (ControllingFaction != null) {
                        ob.ModelColor = ConquestGameModeTeamsFactions.GetFactionColor(ControllingFaction).ToVector3();
                    } else {
                        ob.ModelColor = VRageMath.Color.Transparent.ToVector3();
                    }
                break;
                case ConquestGameResourceNodeState.UnderAttack:
                    var faction = GetHighestScoringFaction();
                    if (faction != null) {
                        ob.ModelColor = ConquestGameModeTeamsFactions.GetFactionColor(faction).ToVector3();
                    } else {
                        ob.ModelColor = VRageMath.Color.White.ToVector3();
                    }
                break;
                case ConquestGameResourceNodeState.Locked:
                    ob.ModelColor = ConquestGameModeTeamsFactions.GetFactionColor(ControllingFaction).ToVector3();
                break;
                default:
                break;
            }
            MySessionComponentSafeZones.RequestUpdateSafeZone(ob);
        }

        private bool HasControllingFaction() {
            if (ControllingFaction == null) {
                return false;
            }
            return true;
        }

        private bool OnlyControllingFactionInSafeZone() {
            if (ControllingFaction == null) {
                return false;
            }

            foreach(var faction in FactionsInSafeZone) {
                if (faction.Key != ControllingFaction.Tag) {
                    return false;
                }
            }

            return true;
        }

        private void RecalculateScoreForTick() {

            // Increment factions in the grid
            foreach(var faction in FactionsInSafeZone) {
                if (!FactionsCapturePoints.ContainsKey(faction.Value.Tag)) {
                    FactionsCapturePoints.Add(faction.Value.Tag, 0);
                }
                FactionsCapturePoints[faction.Value.Tag]++;
            }

            // Decrement factions not in the grid
            foreach(var factionKey in FactionsCapturePoints.Keys.ToList()) {
                if (!FactionsInSafeZone.ContainsKey(factionKey)) {
                    FactionsCapturePoints[factionKey]--;
                }
                if (FactionsCapturePoints[factionKey] < 1) {
                    FactionsCapturePoints.Remove(factionKey);
                }
            }
        }

        private IMyFaction GetHighestScoringFaction() {

            int score = 0;
            string bestFaction = null;
            foreach(var faction in FactionsCapturePoints) {
                if (faction.Value > score) {
                    bestFaction = faction.Key;
                    score       = faction.Value;
                }
            }

            if (bestFaction == null) {
                return null;
            }

            return MyAPIGateway.Session.Factions.TryGetFactionByTag(bestFaction);
        }

        private bool HasFactionGotHighestScore() {
            int score = 0;
            string bestFaction = null;
            foreach(var faction in FactionsCapturePoints) {
                if (faction.Value >= OPTIONS.NodeResourceScoreWin) {
                    return true;
                }
            }
            return false;
        }

        private void ResourceSpawningLogic() {

            if (NodeState == ConquestGameResourceNodeState.UnderAttack || ControllingFaction == null)
            {
                return;
            }
            if (NextResourceSpawnUpdate > MyAPIGateway.Session.GameDateTime) {
                return;
            }

            foreach(var cargoResource in CargoResources) {
                cargoResource.AddMoreResources();
            }

            Debug.d("Spawning more resources");
            NextResourceSpawnUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(OPTIONS.NodeResourceSpawnCountdown);
        }

        private void SafeZoneCaptureLogic() {
            switch(NodeState) {
                case ConquestGameResourceNodeState.Neutral:
                    if (FactionsInSafeZone.Count() == 0 || OnlyControllingFactionInSafeZone()) {
                        return;
                    }

                    Debug.d("FactionsInSafeZone.Count() "+ FactionsInSafeZone.Count().ToString());
                    Debug.d("OnlyControllingFactionInSafeZone() "+ OnlyControllingFactionInSafeZone().ToString());

                    Debug.d("Setting node under attack");
                    ChangeNodeState(ConquestGameResourceNodeState.UnderAttack);
                    NextCaptureGoalUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(OPTIONS.NodeResourceScoreTick);
                break;
                case ConquestGameResourceNodeState.UnderAttack:

                    if (MyAPIGateway.Session.GameDateTime > NextCaptureGoalUpdate) {

                        NextCaptureGoalUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(OPTIONS.NodeResourceScoreTick);
                        RecalculateScoreForTick();

                        string str = "";
                        foreach(var faction in FactionsCapturePoints) {
                            if (faction.Key != FactionsCapturePoints.First().Key) {
                                str += "\n";
                            }
                            str += "Faction: "+faction.Key+" "+faction.Value;
                        }
                        Debug.d(str);

                        if (FactionsCapturePoints.Count() == 0) {
                            ChangeNodeState(ConquestGameResourceNodeState.Neutral);
                            return;
                        }

                        if (LeadingWinner != GetHighestScoringFaction()) {
                            // Refresh SafeZone color
                            LeadingWinner = GetHighestScoringFaction();
                            ChangeNodeState(ConquestGameResourceNodeState.UnderAttack);
                        }

                        if (HasFactionGotHighestScore()) {
                            Debug.d("Locking node resource");
                            ControllingFaction = GetHighestScoringFaction();
                            NextCaptureGoalUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(OPTIONS.NodeResourceLockExpireCountdown);
                            ChangeNodeState(ConquestGameResourceNodeState.Locked);
                        }
                    }
                break;
                case ConquestGameResourceNodeState.Locked:
                    if (MyAPIGateway.Session.GameDateTime > NextCaptureGoalUpdate) {

                        Debug.d("unlocking node resource");
                        FactionsCapturePoints = new FactionPointsDict();
                        ChangeNodeState(ConquestGameResourceNodeState.Neutral);
                    }
                break;
                default:
                break;
            }
        }

        private void CountAttackingGrids() {
            FactionsInSafeZone.Clear();

            foreach (var grid in GridsInSafeZone) {
                var factionTag = ConquestGameHelper.DetermineFactionFromEntityBlocks(grid.Value as MyCubeGrid);
                if (factionTag == "" || factionTag == null) {
                    continue;
                }

                if (!FactionsInSafeZone.ContainsKey(factionTag)) {
                    var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
                    if (faction == null) {
                        continue;
                    }
                    FactionsInSafeZone.Add(factionTag, faction);
                }
            }
        }

        private bool isEntityInsideSafeZone(MyEntity entity) {
            MyOrientedBoundingBoxD myOrientedBoundingBoxD = new MyOrientedBoundingBoxD(entity.PositionComp.LocalAABB, entity.PositionComp.WorldMatrixRef);
            bool result;
            if (SafeZone.Shape == MySafeZoneShape.Sphere)
            {
                BoundingSphereD boundingSphereD = new BoundingSphereD(SafeZone.PositionComp.GetPosition(), (double)SafeZone.Radius);
                result = myOrientedBoundingBoxD.Intersects(ref boundingSphereD);
            }
            else
            {
                MyOrientedBoundingBoxD myOrientedBoundingBoxD2 = new MyOrientedBoundingBoxD(SafeZone.PositionComp.LocalAABB, SafeZone.PositionComp.WorldMatrixRef);
                result = myOrientedBoundingBoxD2.Intersects(ref myOrientedBoundingBoxD);
            }
            return result;
        }

        private void UpdateEntitiesInSafeZone() {

            GridsInSafeZone.Clear();

            var ents  = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                if (isEntityInsideSafeZone(grid)) {
                    GridsInSafeZone.Add(grid.EntityId, grid);
                }
            }
        }

        private BlocksDict getCargoBlocks() {

            BlocksDict blocks = new BlocksDict();

            foreach(IMySlimBlock slim in Grid.GetBlocks())
            {
                if (slim.FatBlock == null) {
                    continue;
                }
                var block = (IMyCubeBlock) slim.FatBlock;
                if (block is IMyCargoContainer) {
                    blocks.Add(block.EntityId, block);
                }
            }
            return blocks;
        }
    }
}