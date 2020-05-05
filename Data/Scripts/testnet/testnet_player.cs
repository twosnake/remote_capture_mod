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
using testnet_util;

namespace testnet_player
{
    public class testnet_spawnrequest {
        public System.Int64 PlayerId;
        public int CountdownTimer;
        public string Faction;
        public string SpawnPrefab = "PV-4 Buggy"; // "PV-5 Buggy Welder"; <-- can't load by blueprint. Trying making it a prefab and broke the game.

        public testnet_spawnrequest(long playerId, string faction)
        {
            this.CountdownTimer = 2;
            this.PlayerId = playerId;
            this.Faction = faction;
        }
    }


    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class testnet_player : MySessionComponentBase
    {
        #region constant

        private const string TESTNET_BASE = "testnet_base";
        private const string TESTNET_LCD = "testnet_status";
        private const string RED_FACTION = "COR";
        private const string BLUE_FACTION = "SDF";

        #endregion

        #region settings

        #endregion

        #region Server

        #endregion

        private bool isDedicated;
        private bool isServer;
        private bool init = false;
        private VRage.Game.ModAPI.IMyGpsCollection gpscollection;
        private HashSet<IMyEntity> ents = new HashSet<IMyEntity>();
        private string serverMessage;
        private long playerId;
        private string debugMessage = "", debugMessage2 = "";
        private bool hasMessageHandeler = false;

        // block for spawning vehicle
        private Dictionary<string, Sandbox.ModAPI.Ingame.IMyTerminalBlock> spawnerBlock = new Dictionary<string, Sandbox.ModAPI.Ingame.IMyTerminalBlock>();

        // block for updating LCD status
        private Dictionary<string, Sandbox.ModAPI.Ingame.IMyTerminalBlock> spawnerStatusBlock = new Dictionary<string, Sandbox.ModAPI.Ingame.IMyTerminalBlock>();


        private Dictionary<string, testnet_spawnrequest> spawnRequests = new Dictionary<string, testnet_spawnrequest>();
        private DateTime nextUpdate = new DateTime();

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Entities != null)
            {
                isServer = MyAPIGateway.Session.IsServer;
                isDedicated = MyAPIGateway.Utilities.IsDedicated;
                if (isDedicated)
                {
                 //   MyAPIGateway.Utilities.GetObjectiveLine().Title = "Controlpoint Gamemode Dedicated vTest";
                }
                else
                {
                    //MyAPIGateway.Utilities.GetObjectiveLine().Title = "Controlpoint Gamemode";
                }
                MyAPIGateway.Utilities.GetObjectiveLine().Objectives.Clear();
                MyAPIGateway.Utilities.GetObjectiveLine().Objectives.Add("");
                MyAPIGateway.Utilities.GetObjectiveLine().Show();
                Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogVisible(false);
                init = true;
            }

            if (MyAPIGateway.Session.GameDateTime > nextUpdate) {
                nextUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(1);
                updateEachSecond();
            }

        }

        public void updateEachSecond() {
            // Check button pushes
            foreach(string faction in spawnRequests.Keys.ToList()) {

                spawnRequests[faction].CountdownTimer--;
                Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("Spawning vehicle in \""+spawnRequests[faction].CountdownTimer.ToString()+"\"");

                if (spawnRequests[faction].CountdownTimer < 1) {
                    Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("Spawning vehicle for faction \""+faction+"\" playerId \""+spawnRequests[faction].PlayerId+"\"");

                    SpawnPrefab(spawnRequests[faction]);

                    spawnRequests.Remove(faction);
                }
            }
        }

        public void UpdateQuestlog()
        {
            UpdateQuestlog(-1);
        }

        public void UpdateQuestlog(long playerId)
        {
            Sandbox.Game.MyVisualScriptLogicProvider.RemoveQuestlogDetails(playerId);
            Sandbox.Game.MyVisualScriptLogicProvider.AddQuestlogDetail("No active Stage (Try to respawn)", false, false, playerId);
            Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("No count");
        }

        public override void Draw()
        {
            if (init)
            {
                if (!isServer)
                {
                    debugMessage = "";
                    debugMessage2 = "";
                }

                MyAPIGateway.Utilities.GetObjectiveLine().Objectives[0] = string.Format(
                    "\n" +
                    //getmessage + "\n" +
                    debugMessage + "\n" +
                    debugMessage2 + "\n" +
                    //+ parsemessage +
                    //"\n" + activeButton +
                    //"\n" + serverMessage +
                    "\n".PadRight(232, ' '));
            }
        }

        public override void LoadData()
        {

        }

        protected override void UnloadData()
        {

            Dispose();
        }

        public override void BeforeStart()
        {
            isServer = MyAPIGateway.Session.IsServer;
            if (this.isServer)
            {
                MyAPIGateway.Entities.GetEntities(ents);
                foreach (var ent in ents)
                {
                    var grid = ent as MyCubeGrid;
                    if (grid != null)
                    {
                        if (grid.DisplayName == TESTNET_BASE)
                        {
                            grid.DestructibleBlocks = true;
                            grid.Editable = true;


                            // Get all soundblocks that we're using for our "spawner" block for vehicles
                            Sandbox.ModAPI.Ingame.IMyGridTerminalSystem GridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                            var blocks = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
                            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(blocks);
                            foreach(var block in blocks) {
                                string faction = ((IMyFunctionalBlock)block).GetOwnerFactionTag().ToString();
                                spawnerBlock.Add(faction, block);
                            }

                            // Get all LCDs that have the status name on them
                            blocks = new List<Sandbox.ModAPI.Ingame.IMyTerminalBlock>();
                            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks);
                            foreach(var block in blocks) {

                                //TESTNET_LCD

                                // can't figure out how to get the name
                                //Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage(grid.DisplayName+" - LCD name: "+block.Name+" "+block.CustomName);

                                // string faction = ((IMyFunctionalBlock)block).GetOwnerFactionTag().ToString();
                                // spawnerStatusBlock.Add(faction, block);
                            }
                        }
                    }
                }
                ents.Clear();
            }
            getMessageHandler();
        }

        private void getMessageHandler()
        {
            try
            {
                IMyMultiplayer sync = MyAPIGateway.Multiplayer;
                sync.RegisterMessageHandler(5289, data =>
                {
                    var str = Encoding.UTF8.GetString(data, 0, data.Length);
                    string recivedContext;
                    try
                    {
                        debugMessage = str.ToString();
                        recivedContext = str.ToString();
                        string[] stringArr = recivedContext.Split(' ');

                        if (stringArr[0] == "ButtonPushSpawner") {
                            System.Int64 playerId = System.Int64.Parse(stringArr[1]);
                            String faction = Sandbox.Game.MyVisualScriptLogicProvider.GetPlayersFactionTag(playerId);

                            if (!spawnRequests.ContainsKey(faction)) {
                                spawnRequests.Add(faction, new testnet_spawnrequest(playerId, faction));
                            }
                        }
                        debugMessage = "received " + str;
                    }
                    catch (Exception e)
                    {
                        debugMessage = "unable to convert: " + str + e.Message;
                    }
                });
                hasMessageHandeler = true;
            }
            catch
            {

            }

            Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage(debugMessage);
        }

        private void SpawnPrefab(testnet_spawnrequest spawnReq)
        {
            var faction = spawnReq.Faction;
            Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("SpawnPrefab "+ faction);

            if (!spawnerBlock.ContainsKey(faction)) {
                Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("No Spawner for faction \""+faction+"\". Place a soundblock for this faction.");
                return;
            }

            var spawn = (IMyFunctionalBlock) spawnerBlock[faction];
            var offset = spawn.WorldMatrix;
            offset += MatrixD.CreateFromAxisAngle(offset.Up, testnet_helper.deg2rad(-90));
            offset += MatrixD.CreateFromAxisAngle(offset.Right, testnet_helper.deg2rad(90));

            var prefab = Sandbox.Definitions.MyDefinitionManager.Static.GetPrefabDefinition(spawnReq.SpawnPrefab);
            if (prefab == null) {
                Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage("No prefab called \""+spawnReq.SpawnPrefab+"\". Can't spawn vehicle.");
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
                (entity as IMyCubeGrid).ChangeGridOwnership(spawnReq.PlayerId, MyOwnershipShareModeEnum.Faction);
                entities.Add(entity);

            }
            MyAPIGateway.Multiplayer.SendEntitiesCreated(tempList);
        }

        public void PlayerSpawned(System.Int64 playerId)
        {
            IMyMultiplayer sync = MyAPIGateway.Multiplayer;
            if (isServer)
            {
                String faction = Sandbox.Game.MyVisualScriptLogicProvider.GetPlayersFactionTag(playerId);
                if (faction == BLUE_FACTION)
                {
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersColorInRGB(playerId, Color.Blue);
                }
                else if (faction == RED_FACTION)
                {
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersColorInRGB(playerId, Color.Red);
                }
            }
        }

        public void PlayerDied(System.Int64 playerid)
        {

        }

        public IMyEntity getGrid(string name)
        {
            return MyAPIGateway.Entities.GetEntityByName(name);
        }

        private void Dispose()
        {

        }

        private void RemoveOldGPS(System.Int64 playerId)
        {

        }
    }
}