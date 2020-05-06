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
    public class ConquestGameHelper
    {
        public static double deg2rad(int degree) {
            return degree * (double)Math.PI / 180;
        }

        public static int rad2deg(float rad) {
            return (int) (rad * 180 / Math.PI);
        }

        public static Vector3 ToHsvColor(VRageMath.Color color)
        {
            var hsvColor = color.ColorToHSV();
            return new Vector3(hsvColor.X, hsvColor.Y * 2f - 1f, hsvColor.Z * 2f - 1f);
        }

        public static VRageMath.Color ToColor(Vector3 hsv)
        {
            return new Vector3(hsv.X, (hsv.Y + 1f) / 2f, (hsv.Z + 1f) / 2f).HSVtoColor();
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

        public static MySafeZone CreateSafezone(string name, MyPositionAndOrientation pos)
        {
            var ob = new MyObjectBuilder_SafeZone();
            ob.PositionAndOrientation = pos;
            ob.PersistentFlags = MyPersistentEntityFlags2.InScene;
            ob.Factions = new long[] { };
            ob.AccessTypeFactions = MySafeZoneAccess.Whitelist;
            ob.Shape = MySafeZoneShape.Sphere;
            ob.Radius = (float) 30;
            ob.Enabled = true;
            ob.DisplayName = name;
            ob.ModelColor = VRageMath.Color.Transparent.ToVector3(); //VRageMath.Color.White.ToVector3();

            return MyEntities.CreateFromObjectBuilderAndAdd(ob, true) as MySafeZone;
        }
    }
}