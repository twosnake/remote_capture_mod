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
        public const string FactionColorReplace = "#FF00FF";
        // quick thing to stop it changing my block colors while debugging
        public const bool DisableColorReplace = false;
    }

    interface IConquestGameMode
    {
        bool IsServer { get; set; }
        void Setup();
        void UpdateEachSecond();
        void PlayerSpawned(System.Int64 playerId);
        void PlayerDied(System.Int64 playerId);
        void PlayerConnected(System.Int64 playerId);
        void UnloadData();
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ConquestGameMain : MySessionComponentBase
    {
        private bool IsDedicated;
        private bool IsServer;
        private bool GameInit = false;
        private bool GameReady = false;
        private bool hasMessageHandeler = false;
        private DateTime nextUpdate = new DateTime();
        private string ExceptionMessage = "";
        private bool GameErrored = false;

        private IConquestGameMode GameMode;

        public override void UpdateBeforeSimulation()
        {
            if (!GameInit && MyAPIGateway.Session != null && MyAPIGateway.Session.Player != null && MyAPIGateway.Entities != null)
            {
                IsServer = MyAPIGateway.Session.IsServer;
                IsDedicated = MyAPIGateway.Utilities.IsDedicated;

                MyAPIGateway.Utilities.GetObjectiveLine().Objectives.Clear();
                MyAPIGateway.Utilities.GetObjectiveLine().Hide();
                Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogVisible(false);
                Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogVisibleLocal(false);

                // Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogVisibleLocal(true);

                // Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogTitleLocal("foobar", MyAPIGateway.Session.Player.IdentityId);
                // Sandbox.Game.MyVisualScriptLogicProvider.AddQuestlogDetailLocal("hello", true, true, MyAPIGateway.Session.Player.IdentityId);
                // Sandbox.Game.MyVisualScriptLogicProvider.AddQuestlogObjective("there", true, true, MyAPIGateway.Session.Player.IdentityId);

                GameInit = true;
            }

            if (GameInit && GameReady) {
                if (MyAPIGateway.Session.GameDateTime > nextUpdate) {
                    nextUpdate = MyAPIGateway.Session.GameDateTime + TimeSpan.FromSeconds(1);
                    updateEachSecond();
                }
            }

            MyAPIGateway.Session.CameraController.ForceFirstPersonCamera = false;
        }

        public void updateEachSecond() {
            try {
                GameMode.UpdateEachSecond();
            } catch(Exception e) {
                ExceptionMessage = "Update Exception: "+e.Message;
                GameErrored = true;
            }
        }

        public override void Draw()
        {
            if (!GameInit && !IsServer && !GameErrored)
            {
                return;
            }

            MyAPIGateway.Utilities.GetObjectiveLine().Show();
            MyAPIGateway.Utilities.GetObjectiveLine().Objectives.Add(string.Format(
                "\n" +
                ExceptionMessage + "\n" +
                "\n".PadRight(232, ' ')));
        }

        public override void LoadData()
        {
            Sandbox.Game.MyVisualScriptLogicProvider.PlayerDied += PlayerDied;
            Sandbox.Game.MyVisualScriptLogicProvider.PlayerSpawned += PlayerSpawned;
            Sandbox.Game.MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
        }

        protected override void UnloadData()
        {
            Sandbox.Game.MyVisualScriptLogicProvider.PlayerDied -= PlayerDied;
            Sandbox.Game.MyVisualScriptLogicProvider.PlayerSpawned -= PlayerSpawned;
            Sandbox.Game.MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            GameMode.UnloadData();
        }

        public override void BeforeStart()
        {
            IsServer = MyAPIGateway.Session.IsServer;

            GameMode = new ConquestGameModeTeams();
            GameMode.IsServer = IsServer;

            if (IsServer)
            {
                try {
                    GameMode.Setup();
                } catch(Exception e) {
                    ExceptionMessage = "Setup Exception: "+e.Message;
                    GameErrored = true;
                }
            }

            if (!GameErrored) {
                GameReady = true;
            }
        }

        public void PlayerConnected(System.Int64 playerId)
        {
            if (GameReady && IsServer) {
                GameMode.PlayerConnected(playerId);
            }
        }

        public void PlayerSpawned(System.Int64 playerId)
        {
            GameMode.PlayerSpawned(playerId);
        }

        public void PlayerDied(System.Int64 playerId)
        {
            GameMode.PlayerDied(playerId);
        }
    }
}