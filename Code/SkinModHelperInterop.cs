using MonoMod.ModInterop;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Reflection;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.PlayerSkinSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper.Interop {

    [ModExportName("SkinModHelperPlus")]
    public static class SkinModHelperInterop {
        internal static void Load() {
            typeof(SkinModHelperInterop).ModInterop();
        }

        public static string GetHairConfig_DynamicDataKey() {
            return HairConfig._DynamicDataKey;
        }
        public static string GetCharacterConfig_DynamicDataKey() {
            return CharacterConfig._DynamicDataKey;
        }

        // We used some hooks to figure out which entity called ParticleSystem.Emit and reskin particle there. but it may not be accurate... This can be used to avoid it.
        /// <summary> Check and get if your static particles are modified in the skin of the specify entity </summary>
        public static bool ParticleReplace(ParticleType ptcl, Entity entity, out ParticleType ptcl2) {
            return ParticleModify.ParticleReplace(ptcl, entity, out ptcl2);
        }


        public static void SetHairConfigColor_Active(PlayerHair hair, bool onoff) {
            HairConfig.For(hair).ColorsActive = onoff;
        }
        public static void SetHairConfigLengths_Active(PlayerHair hair, bool onoff) {
            HairConfig.For(hair).LengthsActive = onoff;
        }


        public static string GetPlayerSkinNameForGlobal() {
            return SkinModHelperModule.GetPlayerSkinName(Player_Skinid_verify);
        }


        public static int GetDashTrailCount(Player player) {
            return GetStartedDashingCount(player);
        }
        public static int SetDashTrailCount(Player player, int count) {
            return SetStartedDashingCount(player, count);
        }
        public static Color? GetHairColor(PlayerHair hair, int dashes) {
            if (HairConfig.For(hair).Safe_GetHairColor(dashes, out Color color)) {
                return color;
            }
            return null;
        }
        public static Color? GetHairColor(PlayerHair hair, int dashes, int index) {
            if (HairConfig.For(hair).Safe_GetHairColor(dashes, index, out Color color)) {
                return color;
            }
            return null;
        }

        public static Color? GetDeathOrbColor(Player player) {
            string scolor = CharacterConfig.For(player.Sprite).DeathParticleColor;
            if (RGB_IsMatch(scolor)) {
                return Calc.HexToColor(scolor);
            }
            int? dashes = GetDashCount(player, player.Sprite);
            if (dashes != null && HairConfig.For(player.Hair).Safe_GetHairColor((int)dashes, out Color color2)) {
                return color2;
            }
            return null;
        }
        public static Color? GetDeathOrbColor(Entity entity) {
            string scolor = CharacterConfig.For(entity.Get<Sprite>()).DeathParticleColor;
            if (RGB_IsMatch(scolor)) {
                return Calc.HexToColor(scolor);
            }
            return null;
        }





















        public static void SetColorGrade(Sprite to, MTexture mTexture) {
            DynamicData spriteData = DynamicData.For(to);

            spriteData.Set("ColorGrade_Path", mTexture?.AtlasPath);
            spriteData.Set("ColorGrade_Atlas", mTexture?.Atlas);
        }
        public static void CopyColorGrades(Sprite from, Sprite to) {
            SyncColorGrade(to, from);
        }

        public static void SessionSet_PlayerSkin(string newSkinId) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            }
            smh_Session.SetPlayerSkin(newSkinId);
        }
        public static void SessionSet_SilhouetteSkin(string newSkinId) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            }
            smh_Session.SetSilhouetteSkin(newSkinId);
        }
        public static void SessionSet_OtherSelfSkin(string newSkinId) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            }
            smh_Session.SetOtherSelfSkin(newSkinId);
        }
        public static void SessionSet_GeneralSkin(string newSkinId, bool? OnOff) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            }
            smh_Session.SetGeneralSkin(newSkinId, OnOff);
        }
    }
}
