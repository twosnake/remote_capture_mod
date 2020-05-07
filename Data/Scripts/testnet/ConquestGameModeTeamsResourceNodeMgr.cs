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
    using ResourceNodeDict = Dictionary<long, ConquestGameModeTeamsResourceNode>;

    class ConquestGameModeTeamsResourceNodeMgr
    {
        private ResourceNodeDict ResourceNodes;

        public ConquestGameModeTeamsResourceNodeMgr() {

            ConquestGameHelper.RemoveAllSafeZones();
            ResourceNodes = new ResourceNodeDict();

            var grids = new Dictionary<long, MyCubeGrid>();
            var ents  = new HashSet<IMyEntity>();
            int gridCount = 0;

            MyAPIGateway.Entities.GetEntities(ents);
            foreach (var ent in ents)
            {
                var grid = ent as MyCubeGrid;
                if (grid == null)
                {
                    continue;
                }

                if (grid.DisplayName == OPTIONS.ResourceNodeGridName) {
                    ResourceNodes.Add(gridCount, new ConquestGameModeTeamsResourceNode(gridCount, grid));
                    gridCount++;
                }
            }
        }

        public void UpdateEachSecond() {

            foreach(var resourceNode in ResourceNodes) {
                resourceNode.Value.UpdateEachSecond();
            }

        }
    }
}