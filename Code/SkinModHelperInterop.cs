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
        
        public static void SetColorGrade(Sprite to, MTexture mTexture) {
            DynamicData spriteData = DynamicData.For(to);

            spriteData.Set("ColorGrade_Path", mTexture?.AtlasPath);
            spriteData.Set("ColorGrade_Atlas", mTexture?.Atlas);
        }
        public static void CopyColorGrades(Sprite from, Sprite to) {
            SyncColorGrade(to, from);
        }

        #region Legacy
        public static void SessionSet_PlayerSkin(string newSkinId) {
            SkinModHelperModule.SessionSet_PlayerSkin(newSkinId);
        }
        public static void SessionSet_SilhouetteSkin(string newSkinId) {
            SkinModHelperModule.SessionSet_SilhouetteSkin(newSkinId);
        }
        public static void SessionSet_GeneralSkin(string newSkinId, bool? OnOff) {
            SkinModHelperModule.SessionSet_GeneralSkin(newSkinId, OnOff);
        }
        #endregion
    }
}
