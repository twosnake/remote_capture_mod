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

    public class Debug {
        static public void d(string str) {
            Sandbox.Game.MyVisualScriptLogicProvider.SendChatMessage(str);
        }
    }

    public struct MESSAGES {
        public const string SpawnVehicleRequest = "SpawnVehicleRequest";
    }

    public struct OPTIONS {
        public const string VehicleSpawnBlockName = "spawn_vehicle";
        public const string VehicleSpawnButtonBlockName = "spawn_button";
        public const string VehicleSpawnStatusBlockName = "spawn_status";
        public const ushort SpawnVehicleRequestHandlerId = 5289;
        // How long new vehicles take to spawn
        public const ushort SpawnTimerCountdown = 10;
        public const string SpawnVehiclePrefab = "PV-5 Buggy Welder";
        public const string ResourceNodeGridName = "Resource Node";
        // Number of seconds to tick by for scoring
        public const ushort NodeResourceScoreTick = 1;
        // Number of points a faction needs to win the node
        public const ushort NodeResourceScoreWin = 10;
        // Number of seconds to keep the node point locked before allowing capture again
        public const ushort NodeResourceLockExpireCountdown = 10;
        // Number of seconds for new resources to spawn in a captured base
        public const ushort NodeResourceSpawnCountdown = 10;
    }

    interface IConquestGameMode
    {
        bool IsServer { get; set; }
        void Setup();
        void UpdateEachSecond();
        void PlayerSpawned(System.Int64 playerId);
        void UnloadData();
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ConquestGameMain : MySessionComponentBase
    {
        private bool isDedicated;
        private bool isServer;
        private bool init = false;
        private bool ready = false;
        private string serverMessage;
        private long playerId;
        private string debugMessage = "", debugMessage2 = "";
        private bool hasMessageHandeler = false;
        private DateTime nextUpdate = new DateTime();

        private IConquestGameMode GameMode;

        public override void UpdateBeforeSimulation()
        {
            if (!init && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Entities != null)
            {
                isServer = MyAPIGateway.Session.IsServer;
                isDedicated = MyAPIGateway.Utilities.IsDedicated;
                if (isDedicated)
                {
                    MyAPIGateway.Utilities.GetObjectiveLine().Title = "Controlpoint Gamemode Dedicated vTest";
                }
                else
                {
                    MyAPIGateway.Utilities.GetObjectiveLine().Title = "Controlpoint Gamemode";
                }
                MyAPIGateway.Utilities.GetObjectiveLine().Objectives.Clear();
                MyAPIGateway.Utilities.GetObjectiveLine().Objectives.Add("");
                MyAPIGateway.Utilities.GetObjectiveLine().Show();
                Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogVisible(false);
                init = true;
            }

            if (init && ready) {
                if (MyAPIGateway.Session.GameDateTime > nextUpdate) {
                    nextUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(1);
                    updateEachSecond();
                }
            }
        }

        public void updateEachSecond() {
            try {
                GameMode.UpdateEachSecond();
            } catch(Exception e) {
                debugMessage = "Update Exception: "+e.Message;
            }
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
            GameMode.UnloadData();
        }

        public override void BeforeStart()
        {
            bool error = false;
            isServer = MyAPIGateway.Session.IsServer;

            GameMode = new ConquestGameModeTeams();
            GameMode.IsServer = isServer;

            if (isServer)
            {
                try {
                    GameMode.Setup();
                } catch(Exception e) {
                    debugMessage = "Setup Exception: "+e.Message;
                    error = true;
                }
            }

            if (!error) {
                ready = true;
            }
        }

        public void PlayerSpawned(System.Int64 playerId)
        {
            GameMode.PlayerSpawned(playerId);
        }
    }
}