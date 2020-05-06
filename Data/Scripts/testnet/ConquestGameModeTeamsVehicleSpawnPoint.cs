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
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ConquestGame
{
    using BlocksDict = Dictionary<long, IMyCubeBlock>;
    using SpawnRequestDict = Dictionary<long, ConquestGameModeTeamsSpawnRequest>;

    class ConquestGameModeTeamsVehicleSpawnPoint
    {
        private IMyFaction Faction;
        private MyCubeGrid BaseGrid;
        private BlocksDict VehicleSpawnBlocks;
        private BlocksDict VehicleSpawnButtonBlocks;
        private BlocksDict SpawnStatusTextPanelBlocks;
        private SpawnRequestDict SpawnRequests;

        public ConquestGameModeTeamsVehicleSpawnPoint(IMyFaction faction) {
            Faction = faction;
            BaseGrid = getSpawnPointGrid();
            VehicleSpawnBlocks = getVehicleSpawnBlocks();
            VehicleSpawnButtonBlocks = getVehicleSpawnButtonBlocks();
            SpawnStatusTextPanelBlocks = getSpawnStatusTextPanelBlocks();
            SpawnRequests = new SpawnRequestDict();
        }

        public void UpdateEachSecond() {

            UpdateSpawnRequests();
            UpdateTextPanels();
        }

        private void UpdateTextPanels() {

            string textPanelData = "";
            if (SpawnRequests.Count != 0) {
                var spawnRequest = SpawnRequests.First().Value;
                textPanelData = spawnRequest.CountdownTimer.ToString();
            }

            foreach(var textPanelBlock in SpawnStatusTextPanelBlocks) {

                var block = (IMyTextPanel) textPanelBlock.Value;
                if (block == null) {
                    continue;
                }

                block.WriteText(textPanelData, false);
            }
        }

        private void UpdateSpawnRequests() {

            if (SpawnRequests.Count == 0) {
                return;
            }
            var spawnRequest = SpawnRequests.First().Value;

            spawnRequest.Tick();
            if (!spawnRequest.IsReady()) {
                return;
            }

            SpawnPrefab(spawnRequest);
            SpawnRequests.Remove(SpawnRequests.First().Key);
        }

        public void AddSpawnRequest(long playerId, long factionId) {

            if (SpawnRequests.ContainsKey(playerId)) {
                return;
            }

            SpawnRequests.Add(playerId, new ConquestGameModeTeamsSpawnRequest(playerId, factionId));
        }

        private void SpawnPrefab(ConquestGameModeTeamsSpawnRequest spawnRequest)
        {
            var spawnBlock = (IMyFunctionalBlock) VehicleSpawnBlocks.First().Value;
            if (spawnBlock == null) {
                throw new System.Exception("Faction \""+Faction.Tag+"\" has no vehicle spawning button block. Name a block \""+OPTIONS.VehicleSpawnButtonBlockName+"\".\nIt needs to be a Button Panel that players can press.");
            }

            var offset = spawnBlock.WorldMatrix;
            offset += MatrixD.CreateFromAxisAngle(offset.Up, ConquestGameHelper.deg2rad(-90));
            offset += MatrixD.CreateFromAxisAngle(offset.Right, ConquestGameHelper.deg2rad(90));

            var prefab = Sandbox.Definitions.MyDefinitionManager.Static.GetPrefabDefinition(spawnRequest.SpawnPrefab);
            if (prefab == null) {
                Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("No prefab called \""+spawnRequest.SpawnPrefab+"\". Can't spawn vehicle.");
                return;
            }

            if (prefab.CubeGrids == null)
            {
                Sandbox.Definitions.MyDefinitionManager.Static.ReloadPrefabsFromFile(prefab.PrefabPath);
                prefab = Sandbox.Definitions.MyDefinitionManager.Static.GetPrefabDefinition(prefab.Id.SubtypeName);
            }

            var tempList = new List<MyObjectBuilder_EntityBase>();
            foreach (var grid in prefab.CubeGrids)
            {
                var gridBuilder = (MyObjectBuilder_CubeGrid) grid.Clone();
                gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(offset);
                tempList.Add(gridBuilder);
            }
            var entities = new List<IMyEntity>();
            MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
            foreach(var item in tempList) {

                var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(item);
                (entity as IMyCubeGrid).ChangeGridOwnership(spawnRequest.PlayerId, MyOwnershipShareModeEnum.Faction);
                entities.Add(entity);

            }
            MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);
        }

        private BlocksDict getSpawnStatusTextPanelBlocks() {

            BlocksDict blocks = new BlocksDict();

            var ents = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                foreach(IMySlimBlock slim in grid.GetBlocks())
                {
                    if (slim.FatBlock == null) {
                        continue;
                    }
                    var block = (IMyCubeBlock) slim.FatBlock;

                    if (block is IMyTextPanel) {
                        if ((block as IMyTextPanel).CustomName == OPTIONS.VehicleSpawnStatusBlockName &&
                            ((IMyFunctionalBlock)block).GetOwnerFactionTag() == Faction.Tag) {
                            blocks.Add(block.EntityId, block);
                        }
                    }
                }
            }

            return blocks;
        }

        private BlocksDict getVehicleSpawnButtonBlocks() {

            BlocksDict blocks = new BlocksDict();

            var ents  = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                foreach(IMySlimBlock slim in grid.GetBlocks())
                {
                    if (slim.FatBlock == null) {
                        continue;
                    }
                    var block = (IMyCubeBlock) slim.FatBlock;

                    if (block is IMyButtonPanel) {
                        if ((block as IMyButtonPanel).CustomName == OPTIONS.VehicleSpawnButtonBlockName &&
                            ((IMyFunctionalBlock)block).GetOwnerFactionTag() == Faction.Tag) {
                            blocks.Add(block.EntityId, block);
                        }
                    }
                }
            }

            if (blocks.Count() == 0) {
                throw new System.Exception("Faction \""+Faction.Tag+"\" has no vehicle spawning button block. Name a block \""+OPTIONS.VehicleSpawnButtonBlockName+"\".\nIt needs to be a Button Panel that players can press.");
            }

            return blocks;
        }

        private BlocksDict getVehicleSpawnBlocks() {

            BlocksDict blocks = new BlocksDict();

            var ents  = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                foreach(IMySlimBlock slim in grid.GetBlocks())
                {
                    if (slim.FatBlock == null) {
                        continue;
                    }
                    var block = (IMyCubeBlock) slim.FatBlock;

                    if (block is IMyTerminalBlock) {
                        if ((block as IMyTerminalBlock).CustomName == OPTIONS.VehicleSpawnBlockName &&
                            ((IMyFunctionalBlock)block).GetOwnerFactionTag() == Faction.Tag) {
                            blocks.Add(block.EntityId, block);
                        }
                    }
                }
            }

            if (blocks.Count() == 0) {
                throw new System.Exception("Faction \""+Faction.Tag+"\" has no vehicle spawning block. Name a block \""+OPTIONS.VehicleSpawnBlockName+"\". It will have a vehicle spawn above it");
            }

            return blocks;
        }

        private MyCubeGrid getSpawnPointGrid() {

            var grids = new Dictionary<long, MyCubeGrid>();
            var ents  = new HashSet<IMyEntity>();

            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                foreach(IMySlimBlock slim in grid.GetBlocks())
                {
                    if (grids.ContainsKey(grid.EntityId)) {
                        continue;
                    }
                    if (slim.FatBlock == null) {
                        continue;
                    }

                    var block = (IMyCubeBlock) slim.FatBlock;
                    if (block.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_MedicalRoom" &&
                        ((IMyFunctionalBlock)block).GetOwnerFactionTag() == Faction.Tag) {
                        grids.Add(grid.EntityId, grid);
                    }
                }
            }

            if (grids.Count() == 0) {
                throw new System.Exception("Faction \""+Faction.Tag+"\" has no medical room.");
            }

            if (grids.Count() > 1) {
                throw new System.Exception("Faction \""+Faction.Tag+"\" has too many spawn points. There should only be one grid with medical rooms.");
            }

            return grids.First().Value;
        }
    }
}