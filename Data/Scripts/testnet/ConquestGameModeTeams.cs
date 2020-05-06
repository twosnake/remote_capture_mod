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
    using VehicleSpawnPointDict = Dictionary<long, ConquestGameModeTeamsVehicleSpawnPoint>;

    class ConquestGameModeTeams : IConquestGameMode
    {
        public bool IsServer { get; set; }

        private ConquestGameModeTeamsFactions FactionMgr;
        private VehicleSpawnPointDict VehicleSpawnPoints;
        private ConquestGameModeTeamsResourceNodeMgr ResourceNodeMgr;
        private bool initMessageHandler = false;

        public void Setup() {
            FactionMgr = new ConquestGameModeTeamsFactions();

            VehicleSpawnPoints = new VehicleSpawnPointDict();
            foreach(var faction in FactionMgr.getListOfPlayerFactions())
            {
                VehicleSpawnPoints.Add(faction.Key, new ConquestGameModeTeamsVehicleSpawnPoint(faction.Value));
            }

            ResourceNodeMgr = new ConquestGameModeTeamsResourceNodeMgr();
        }

        public void UnloadData() {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(OPTIONS.SpawnVehicleRequestHandlerId, MessageHandler);
        }

        private void createMessageHandler()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(OPTIONS.SpawnVehicleRequestHandlerId, MessageHandler);
            initMessageHandler = true;
        }

        public void MessageHandler(byte[] data)
        {
            var str = Encoding.UTF8.GetString(data, 0, data.Length);
            string recivedContext;
            try
            {
                recivedContext = str.ToString();
                string[] stringArr = recivedContext.Split(' ');

                if (stringArr[0] == MESSAGES.SpawnVehicleRequest) {
                    long playerId = System.Int64.Parse(stringArr[1]);

                    var faction = ConquestGameModeTeamsFactions.GetFactionByPlayerId(playerId);
                    if (faction == null) {
                        return;
                    }

                    if (!VehicleSpawnPoints.ContainsKey(faction.FactionId)) {
                        return;
                    }

                    VehicleSpawnPoints[faction.FactionId].AddSpawnRequest(playerId, faction.FactionId);
                }
            }
            catch (Exception e)
            {
                Debug.d("Error in message handler: " + str + "\n" + e.Message);
            }
        }

        public void UpdateEachSecond() {
            if (!initMessageHandler) {
                createMessageHandler();
            }

            foreach(var vehicleSpawnPoint in VehicleSpawnPoints) {
                vehicleSpawnPoint.Value.UpdateEachSecond();
            }
        }

        public void PlayerSpawned(System.Int64 playerId) {

            IMyMultiplayer sync = MyAPIGateway.Multiplayer;
            if (IsServer)
            {
                /*String faction = Sandbox.Game.MyVisualScriptLogicProvider.GetPlayersFactionTag(playerId);
                if (faction == BLUE_FACTION)
                {
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersColorInRGB(playerId, Color.Blue);
                }
                else if (faction == RED_FACTION)
                {
                    Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersColorInRGB(playerId, Color.Red);
                }*/
            }

        }
    }

}