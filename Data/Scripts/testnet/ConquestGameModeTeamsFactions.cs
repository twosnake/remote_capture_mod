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
    using FactionDict = Dictionary<long, IMyFaction>;

    class ConquestGameModeTeamsFactions
    {
        private FactionDict Factions = new FactionDict();

        public ConquestGameModeTeamsFactions() {

            foreach(var faction in GetFactions()) {

                if (faction.Value.Tag.Length < 4) {
                    Factions.Add(faction.Key, faction.Value);
                }
            }

            CheckFactionsAreSetupCorrectly();
            //MyAPIGateway.Session.Factions.AcceptJoin(factionId, playerId);
            //MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);

            // get all factions
            //MyAPIGateway.Session.Factions\
        }

        private void CheckFactionsAreSetupCorrectly() {

            // Check factions all have one NPC member that is the leader
            foreach (var faction in Factions) {
                bool hasNPC = false;
                foreach (var member in faction.Value.Members) {
                    if (isNPC(member.Value.PlayerId)) {
                        hasNPC = true;
                    }
                }
                if (!hasNPC) {
                    throw new System.Exception("Faction \""+faction.Value.Tag+"\" has no NPC to ensure faction doesn't disappear. Please create one in the factions screen with \"Add NPC\" button.");
                }
            }
        }

        public static VRageMath.Color GetFactionColor(IMyFaction faction) {

            var colorList = new List<VRageMath.Color> {
                VRageMath.Color.Yellow,
                VRageMath.Color.Red,
                VRageMath.Color.Blue,
                VRageMath.Color.Green,
                VRageMath.Color.Black
            };

            var factions = GetFactions().ToList();
            factions.Sort((pair1,pair2) => pair1.Key.CompareTo(pair2.Key));

            int key = 0;
            foreach(var fact in factions) {
                if (faction.Tag == fact.Value.Tag) {
                    return colorList[key];
                }
                key++;
            }

            return VRageMath.Color.White;
        }


        public static bool isNPC(long playerId) {
            return GetPlayerByID(playerId) == null;
        }

        public static IMyIdentity GetIdentityById(long playerId) {
            var identList = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identList);
            return identList.FirstOrDefault(f => f.PlayerId == playerId);
        }

        public static IMyFaction GetFactionByPlayerId(long playerId) {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
        }

        public static IMyPlayer GetPlayerByID(long playerId) {
            List<IMyPlayer> playerList = new List<IMyPlayer>();
            MyAPIGateway.Multiplayer.Players.GetPlayers(playerList);
            return playerList.FirstOrDefault(f => f.PlayerID == playerId);
        }

        public static FactionDict GetFactions() {
            return MyAPIGateway.Session.Factions.Factions;
        }

        public static long GetNPCPlayerID(IMyFaction faction) {
            bool hasNPC = false;
            foreach (var member in faction.Members) {
                if (isNPC(member.Value.PlayerId)) {
                    return member.Value.PlayerId;
                }
            }
            return 0;
        }

        public FactionDict getListOfPlayerFactions() {
            return Factions;
        }

        public static void SetPlayersFaction(long playerId, IMyFaction faction) {
            Sandbox.Game.MyVisualScriptLogicProvider.SetPlayersFaction(playerId, faction.Tag);
        }
//         MySession.Static.Factions.TryGetFactionById(newFaction)
    }
}