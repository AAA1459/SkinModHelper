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

        public static HairConfig GetHairConfig(PlayerHair hair) {
            return HairConfig.For(hair);
        }
        public static CharacterConfig GetCharacterConfig(Image image) {
            return CharacterConfig.For(image);
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
            return GetPlayerSkinName(Player_Skinid_verify);
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
            if (HairConfig.For(hair).Safe_GetHairColor(index, dashes, out Color color)) {
                return color;
            }
            return null;
        }
        public static Color? GetHairColorWithSpecified(PlayerHair hair, int dashes, int index) {
            if (HairConfig.For(hair).GetHairColorWithSpecified(index, dashes, out Color color)) {
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

        /// <summary>
        /// type a texture path, atlas[texture]. to make it skinnable for SMH skins. <br/><br/>
        /// `isStatic` means whether the texture is not animated.<br/><br/>
        /// 
        /// `optionsId` means in the precisely skin choose menu of the advanced options. a options to onoff only the parts of skin that related to this.<br/>
        /// </summary>
        public static void AddSkinnableCompatibilityFor(Atlas atlas, string texture, bool isStatic, string optionsId) {

            var manager = SkinsSystem.OtherSpriteSkins;
            nonBankReskin.SpriteInfo.Add((optionsId, atlas, texture, isStatic));

            if (!Dialog.Has($"SkinModHelper_{manager.O_DescriptionPrefix}__{optionsId}")) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Added skinnable compatibility for [{atlas.RelativeDataPath}{texture}]... " +
                    $"but the dialogID [SkinModHelper_{manager.O_DescriptionPrefix}__{optionsId}] used for its options in 'Precisely skin choose' menu does not exist");

            } else {
                Logger.Log(LogLevel.Verbose, "SkinModHelper", $"Added skinnable compatibility for [{atlas.RelativeDataPath}{texture}]");
            }
        }


        /// <summary>
        /// Patch/Combines the animations of sprite from GFX.SpriteBank into player sprites.<br/><br/>
        /// 
        /// 'id' is a default sprite you want its animations to be combined into all player sprites.<br/><br/>
        /// 
        /// 'array' is used adds the difference animation to the difference player sprites. a sprite(first param)'s animations to be combined into the player sprite(second param).
        /// </summary>
        public static void AddPlayerSpritePatch(string id, (string, string)[] array) {
            Dictionary<string, string> dict = new(StringComparer.OrdinalIgnoreCase);
            foreach (var str in array) {
                dict[str.Item1] = str.Item2;
            }
            patchPlayerSprite_List.Add((id, dict));
        }

        /// <summary>
        /// Enable TintMaskWithHair from SMH for your sprite.    don't call this for player
        /// 
        /// mode 0 is red as the mask, 1 is green, 2 is blue. 3 is grayscale
        /// </summary>
        public static void TintMaskWith(Sprite sprite, int mode, Color color) {
            CharacterConfig character = CharacterConfig.For(sprite);

            character.TintMaskWithHair = true;
            character.MaskMode = (CharacterConfig.MaskModes)mode;
            character.effect_hairColor = color;

            character.RefreshConflict();
        }














        public static void SetColorGrade(Sprite to, MTexture mTexture) {
            CharacterConfig config_ofTo = CharacterConfig.For(to);

            config_ofTo.ColorGrade_Atlas = mTexture?.Atlas;
            config_ofTo.ColorGrade_Path = mTexture?.AtlasPath;
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
