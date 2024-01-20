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

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperModule : EverestModule {
        public static SkinModHelperModule Instance;

        public override Type SettingsType => typeof(SkinModHelperSettings);
        public override Type SessionType => typeof(SkinModHelperSession);
        

        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static SkinModHelperUI UI;



        public static List<ILHook> doneILHooks = new List<ILHook>();
        public static List<Hook> doneHooks = new List<Hook>();

        //-----------------------------
        public static bool JungleHelperInstalled = false;
        public static bool SaveFilePortraits = false;
        public static bool OrigSkinModHelper_loaded = false;
        public static bool MaddieHelpingHandInstalled = false;

        public SkinModHelperModule() {
            Instance = this;
            UI = new SkinModHelperUI();

            JungleHelperInstalled = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "JungleHelper", Version = new Version(1, 0, 8) });
            SaveFilePortraits = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "SaveFilePortraits", Version = new Version(1, 0, 0) });
            OrigSkinModHelper_loaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "SkinModHelper", Version = new Version(0, 0, 0) });
        }

        public override void Load() {
            SkinModHelperInterop.Load();
            SkinsSystem.Load();

            LoaderHook.Load();
            PlayerSkinSystem.Load();
            ObjectsHook.Load();
            SomePatches.Load();
        }
        public override void Initialize() {
            base.Initialize();
            MaddieHelpingHandInstalled = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "MaxHelpingHand", Version = new Version(1, 17, 3) });
            SomePatches.LazyLoad();
        }

        public override void Unload() {
            SkinsSystem.Unload();

            LoaderHook.Unload();
            PlayerSkinSystem.Unload();
            ObjectsHook.Unload();
            SomePatches.Unload();

            foreach (ILHook h in doneILHooks) {
                h.Dispose();
            }
            doneILHooks.Clear();
            foreach (Hook h in doneHooks) {
                h.Dispose();
            }
            doneHooks.Clear();
        }
        //-----------------------------
        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);
        }
        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            //UI.CreateMenu(menu, inGame);
            if (inGame) {
                new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: false, includeRandomizer: false, null, menu, inGame, forceEnabled: false);
                return;
            }
            new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: true, includeRandomizer: true, delegate {
                OuiModOptions.Instance.Overworld.Goto<OuiModOptions>();
            }, menu, inGame, forceEnabled: false);
        }



        //-----------------------------Setting update-----------------------------
        public static void UpdateSkin(string newSkinId, bool inGame = false) {
            if (Session != null) {
                Session.SessionPlayerSkin = null;
            }
            Settings.SelectedPlayerSkin = newSkinId;
            if (inGame) {
                PlayerSkinSystem.RefreshPlayerSpriteMode();
            } else {
                RefreshSkins(false, inGame);
            }
        }
        public static void UpdateSilhouetteSkin(string newSkinId, bool inGame) {
            if (Session != null) {
                Session.SessionSilhouetteSkin = null;
            }

            Settings.SelectedSilhouetteSkin = newSkinId;
        }
        public static void UpdateExtraXml(string SkinId, bool OnOff, bool inGame) {
            if (Session != null && Session.SessionExtraXml.ContainsKey(SkinId)) {
                Session.SessionExtraXml.Remove(SkinId);
            }
            Settings.ExtraXmlList[SkinId] = OnOff;
            RefreshSkins(false, inGame);
        }

        //-----------------------------FreeCollocations Update / Skins Refresh-----------------------------
        public static void RefreshSkinValues(bool? Setting, bool inGame) {
            if (Setting != null) {
                Settings.FreeCollocations_OffOn = (bool)Setting;
            }

            foreach (string SpriteID in SpriteSkins_records.Keys) {
                RefreshSkinValues_Sprites(SpriteID, null, inGame, false);
            }
            foreach (string SpriteID in PortraitsSkins_records.Keys) {
                RefreshSkinValues_Portraits(SpriteID, null, inGame, false);
            }
            foreach (string SpriteID in OtherSkins_records.Keys) {
                RefreshSkinValues_OtherExtra(SpriteID, null, inGame, false);
            }
            UpdateParticles();
        }

        public static string RefreshSkinValues_Sprites(string SpriteID, string SkinId, bool inGame, bool Setting = true) {
            if (Setting) {
                Settings.FreeCollocations_Sprites[SpriteID] = SkinId;
            }
            var value = Settings.FreeCollocations_Sprites;


            bool boolen = SkinId == DEFAULT || SkinId == LockedToPlayer
                          || !value.ContainsKey(SpriteID) || value[SpriteID] == DEFAULT || value[SpriteID] == LockedToPlayer;
            if (!Settings.FreeCollocations_OffOn || boolen) {
                SpriteSkin_record[SpriteID] = getSkinDefaultValues(GFX.SpriteBank, SpriteID);
            } else {
                SpriteSkin_record[SpriteID] = value[SpriteID];
            }
            return SpriteSkin_record[SpriteID];
        }

        public static string RefreshSkinValues_Portraits(string SpriteID, string SkinId, bool inGame, bool Setting = true) {
            if (Setting) {
                Settings.FreeCollocations_Portraits[SpriteID] = SkinId;
            }

            var value = Settings.FreeCollocations_Portraits;


            bool boolen = SkinId == DEFAULT || SkinId == LockedToPlayer 
                          || !value.ContainsKey(SpriteID) || value[SpriteID] == DEFAULT || value[SpriteID] == LockedToPlayer;
            if (!Settings.FreeCollocations_OffOn || boolen) {
                PortraitsSkin_record[SpriteID] = getSkinDefaultValues(GFX.PortraitsSpriteBank, SpriteID);
            } else {
                PortraitsSkin_record[SpriteID] = value[SpriteID];
            }
            return PortraitsSkin_record[SpriteID];
        }
        public static string RefreshSkinValues_OtherExtra(string SpriteID, string SkinId, bool inGame, bool Setting = true) {
            if (Setting) {
                Settings.FreeCollocations_OtherExtra[SpriteID] = SkinId;
            }
            var value = Settings.FreeCollocations_OtherExtra;


            bool boolen = SkinId == DEFAULT || !value.ContainsKey(SpriteID) || value[SpriteID] == DEFAULT;
            if (!Settings.FreeCollocations_OffOn || boolen) {
                OtherSkin_record[SpriteID] = DEFAULT;
            } else {
                OtherSkin_record[SpriteID] = value[SpriteID];
            }
            return OtherSkin_record[SpriteID];
        }



        //-----------------------------Method-----------------------------
        public static string GetPlayerSkin(string skin_suffix = null, string skinName = null) {
            if (skinName == null) {
                skinName = Settings.SelectedPlayerSkin ?? "";
                if (Session != null && Session.SessionPlayerSkin != null) {
                    skinName = Session.SessionPlayerSkin;
                }
            }

            if (skinConfigs.ContainsKey(skinName + skin_suffix)) {
                return skinName + skin_suffix;
            } else if (skinConfigs.ContainsKey(skinName)) {
                return skinName;
            } else {
                return null;
            }
        }
        public static string GetSilhouetteSkin(string skin_suffix = null) {
            string skinName = Settings.SelectedSilhouetteSkin ?? "";
            if (Session != null && Session.SessionSilhouetteSkin != null) {
                skinName = Session.SessionSilhouetteSkin;
            }
            return GetPlayerSkin(skin_suffix, skinName);
        }
        public static string GetPlayerSkinName(int hashValues = -1) {
            if (hashValues < 0) { hashValues = Player_Skinid_verify; }
            try {
                return skinConfigs.Values.Where(config => config.hashValues == hashValues).First().SkinName;
            } catch {
                return null;
            }
        }
    }
}
