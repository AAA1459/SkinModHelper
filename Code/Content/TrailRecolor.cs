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
    public class TrailRecolor {
        #region

        public static void Load() {
            On.Celeste.TrailManager.Add_Entity_Color_float_bool_bool += onTrailManager_Add_ECFBB;
            On.Celeste.TrailManager.Add_Entity_Vector2_Color_float += onTrailManager_Add_EV2CF;
        }

        public static void Unload() {
        }
        #endregion

        //-----------------------------Hooks-----------------------------

        private static void onTrailManager_Add_ECFBB(On.Celeste.TrailManager.orig_Add_Entity_Color_float_bool_bool orig,  Entity entity, Color color, float duration, bool frozenUpdate, bool useRawDeltaTime) {
            color = TrailsRecolor(entity, color);
            orig(entity, color, duration, frozenUpdate, useRawDeltaTime);
        }
        private static void onTrailManager_Add_EV2CF(On.Celeste.TrailManager.orig_Add_Entity_Vector2_Color_float orig, Entity entity, Vector2 scale, Color color, float duration) {
            color = TrailsRecolor(entity, color);
            orig(entity, scale, color, duration);
        }

        public static Color TrailsRecolor(Entity entity, Color color) {

            PlayerHair hair = entity.Get<PlayerHair>();
            if (hair != null && hair.Sprite?.Mode != PlayerSpriteMode.Badeline) {
                return color;
                // Exclude players and silhouette.
            }

            Sprite sprite = hair?.Sprite ?? GetFieldPlus<Sprite>(entity, "Sprite") ?? GetFieldPlus<Sprite>(entity, "sprite");
            if (sprite != null) {

                // --- observable entity objects ---
                // badeline =>         BadelineOldsite, BadelineDummy.
                // badeline_boss  =>   FinalBoss.
                // badelineBoost  =>   BadelineBoost.
                // oshiro_boss  =>     AngryOshiro.
                // bird  =>            BirdNPC, FlingBird.
                // seeker  =>          Seeker, PlayerSeeker.

                string rootPath = getAnimationRootPath(sprite);
                string TrailsColor = (searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "CharacterConfig") ?? new()).TrailsColor;

                if (TrailsColor != null && new Regex(@"^[a-fA-F0-9]{6}$").IsMatch(TrailsColor)) {
                    return Calc.HexToColor(TrailsColor);
                } else if (TrailsColor == "HairColor" && hair != null) {
                    return hair.Color;
                }
            }
            return color;
        }
    }
}