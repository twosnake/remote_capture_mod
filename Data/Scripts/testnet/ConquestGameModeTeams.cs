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
            ConquestGameHelper.RemoveAllSafeZones();
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

            ResourceNodeMgr.UpdateEachSecond();
        }

        public void PlayerConnected(System.Int64 playerId) {
            if (IsServer)
            {
                var factionTag = Sandbox.Game.MyVisualScriptLogicProvider.GetPlayersFactionTag(playerId);
                if (factionTag == "") {

                    int playerCount = int.MaxValue;
                    IMyFaction selectedFaction = null;

                    foreach(var faction in FactionMgr.getListOfPlayerFactions()) {

                        if (faction.Value.Members.Count < playerCount) {
                            selectedFaction = faction.Value;
                        }
                    }

                    if (selectedFaction == null) {
                        // error
                        return;
                    }
                    ConquestGameModeTeamsFactions.SetPlayersFaction(playerId, selectedFaction);
                }

                SpawnPlayer(playerId);
            }
        }

        private void SpawnPlayer(long playerId) {

            // If player is dead spawn them
            var identity = ConquestGameModeTeamsFactions.GetIdentityById(playerId);
            if (identity == null) {
                // error
                return;
            }

            var faction = ConquestGameModeTeamsFactions.GetFactionByPlayerId(playerId);
            if (faction == null) {
                // error
                return;
            }

            if (identity.IsDead) {
                if (!VehicleSpawnPoints.ContainsKey(faction.FactionId)) {
                    // error
                    return;
                }
                VehicleSpawnPoints[faction.FactionId].SpawnPlayer(playerId);
            }
        }

        public void PlayerSpawned(System.Int64 playerId) {
            if (IsServer)
            {
                var factionTag = Sandbox.Game.MyVisualScriptLogicProvider.GetPlayersFactionTag(playerId);
                var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(factionTag);
                var color = ConquestGameModeTeamsFactions.GetFactionColor(faction);
                Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersColorInRGB(playerId, color);
            }
        }

        public void PlayerDied(System.Int64 playerId) {
            Debug.d(playerId.ToString());
            SpawnPlayer(playerId);
        }
    }

}