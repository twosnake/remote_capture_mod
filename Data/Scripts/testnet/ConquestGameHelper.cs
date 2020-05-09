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

namespace ConquestGame
{
    public enum MySafeZoneAction
    {
        Damage = 1,
        Shooting = 2,
        Drilling = 4,
        Welding = 8,
        Grinding = 16,
        VoxelHand = 32,
        Building = 64,
        LandingGearLock = 128,
        ConvertToStation = 256,
        All = 511,
        AdminIgnore = 382
    }

    public class ConquestGameHelper
    {
        public static T CastProhibit<T>(T ptr, object val) => (T)val;

        public static double deg2rad(int degree) {
            return degree * (double)Math.PI / 180;
        }

        public static int rad2deg(float rad) {
            return (int) (rad * 180 / Math.PI);
        }

        /* uses same function as color picker */
        public static VRageMath.Color ConvertHexToColor(string hex) {
            if (hex.Length > 6)
            {
                hex = hex.Substring(1);
            }
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color((int)r, (int)g, (int)b);
        }

        public static Vector3 ColorMaskToFriendlyHSV(Vector3 colorMask)
        {
            var hsv = MyColorPickerConstants.HSVOffsetToHSV(colorMask);
            return new Vector3(Math.Round(hsv.X * 360, 1), Math.Round(hsv.Y * 100, 1), Math.Round(hsv.Z * 100, 1));
        }

        public static Color ColorMaskToRGB(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }

        public static Vector3 ToHsvColor(VRageMath.Color color)
        {
            return VRageMath.ColorExtensions.ColorToHSV(color);
        }

        public static VRageMath.Color ToColor(Vector3 hsv)
        {
            return VRageMath.ColorExtensions.HSVtoColor(hsv);
        }

        public static void RemoveAllSafeZones()
        {
            HashSet<IMyEntity> entity_list = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entity_list);
            int entityDelete = 0;
            foreach (var entity in entity_list)
            {
                if (entity == null || MyAPIGateway.Entities.Exist(entity) == false) {
                    continue;
                }

                if (entity as MySafeZone != null)
                {
                    entity.Close();
                }
            }
        }

        public static string DetermineFactionFromEntityBlocks(MyCubeGrid grid)
        {
            var factionCount = new Dictionary<string, int>();
            foreach(IMySlimBlock slim in grid.GetBlocks())
            {
                if (slim.FatBlock == null) {
                    continue;
                }

                var block = (IMyCubeBlock) slim.FatBlock;
                if (block == null) {
                    continue;
                }

                var factionTag = block.GetOwnerFactionTag();
                if (factionTag == "") {
                    continue;
                }

                if (!factionCount.ContainsKey(factionTag)) {
                    factionCount.Add(factionTag, 0);
                }
                factionCount[factionTag]++;

            }

            var highestValue = factionCount.FirstOrDefault();
            var lowestValue = factionCount.FirstOrDefault();
            foreach(var count in factionCount) {
                if (count.Value > highestValue.Value) {
                    highestValue = count;
                }
                if (count.Value < lowestValue.Value ) {
                    lowestValue = count;
                }
            }

            if (highestValue.Key != lowestValue.Key &&
                highestValue.Value == lowestValue.Value) {
                return "";
            }
            return highestValue.Key;
        }

        public static MySafeZone CreateSafezone(string name, MyPositionAndOrientation pos)
        {
            var ob = new MyObjectBuilder_SafeZone();
            ob.PositionAndOrientation = pos;
            ob.PersistentFlags = MyPersistentEntityFlags2.InScene;

            ob.Factions = new long[] { };
            ob.AccessTypeFactions        = MySafeZoneAccess.Blacklist;
            ob.AccessTypePlayers         = MySafeZoneAccess.Blacklist;
            ob.AccessTypeGrids           = MySafeZoneAccess.Blacklist;
            ob.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
            ob.Shape = MySafeZoneShape.Sphere;
            ob.Radius = (float) 30;
            ob.Enabled = true;
            ob.DisplayName = name;
            ob.ModelColor = VRageMath.Color.Transparent.ToVector3();

            ob.AllowedActions = CastProhibit(MySessionComponentSafeZones.AllowedActions,
                MySafeZoneAction.Damage   |
                MySafeZoneAction.Shooting |
                MySafeZoneAction.Welding  |
                MySafeZoneAction.Grinding |
                MySafeZoneAction.LandingGearLock
            );

            return MyEntities.CreateFromObjectBuilderAndAdd(ob, true) as MySafeZone;
        }
    }
}