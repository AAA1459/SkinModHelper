using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using Celeste.Mod.UI;
using System.Xml;
using System.Linq;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public static class TrailRecolor {
        #region Hooks
        public static void Load() {
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool += onTrailManager_Add_V2IV2CIFBB;
        }

        public static void Unload() {
            On.Celeste.TrailManager.Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool -= onTrailManager_Add_V2IV2CIFBB;
        }

        private static TrailManager.Snapshot onTrailManager_Add_V2IV2CIFBB(On.Celeste.TrailManager.orig_Add_Vector2_Image_PlayerHair_Vector2_Color_int_float_bool_bool orig,
            Vector2 position, Image image, PlayerHair hair, Vector2 scale, Color color, int depth, float duration, bool frozenUpdate, bool useRawDeltaTime) {


            return orig(position, image, hair, scale, (TrailsRecolor(image, hair) ?? color), depth, duration, frozenUpdate, useRawDeltaTime);
        }

        public static Color? TrailsRecolor(Image sprite, PlayerHair hair) {
            if (hair != null && hair.Sprite?.Mode != PlayerSpriteMode.Badeline) {
                return null; // Exclude players and silhouette.
            }

            string TrailsColor = CharacterConfig.For(sprite).TrailsColor;

            if (RGB_IsMatch(TrailsColor))
                return Calc.HexToColor(TrailsColor);
            if (hair != null) {
                HairConfig config = HairConfig.For(hair);
                if (config.lastDashes is int dashes && config.Safe_GetHairColor((int)HairConfig.SpecialSegment.Trail, dashes, out Color color))
                return color;
            }
            return null;
        }
        #endregion
    }
}