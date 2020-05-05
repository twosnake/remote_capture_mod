using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace testnet_util
{
    public class testnet_helper
    {
        public static double deg2rad(int degree) {
            return degree * (double)Math.PI / 180;
        }

        public static int rad2deg(float rad) {
            return (int) (rad * 180 / Math.PI);
        }
    }
}