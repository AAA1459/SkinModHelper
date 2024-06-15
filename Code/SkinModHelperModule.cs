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
        #region
        public static SkinModHelperModule Instance;
        public override Type SettingsType => typeof(SkinModHelperSettings);
        public override Type SessionType => typeof(SkinModHelperSession);
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        public static SkinModHelperUI InstanceUI;

        public static List<Hook> doneHooks = new List<Hook>();
        public static List<ILHook> doneILHooks = new List<ILHook>();
        #endregion

        //-----------------------------Hooks-----------------------------
        #region
        public static bool JungleHelperInstalled = false;
        public static bool SaveFilePortraits = false;
        public static bool OrigSkinModHelper_loaded = false;
        public static bool MaddieHelpingHandInstalled = false;

        public SkinModHelperModule() {
            Instance = this;
            InstanceUI = new SkinModHelperUI();

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
            TrailRecolor.Load();
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
            TrailRecolor.Unload();

            foreach (ILHook h in doneILHooks) {
                h.Dispose();
            }
            doneILHooks.Clear();
            foreach (Hook h in doneHooks) {
                h.Dispose();
            }
            doneHooks.Clear();
        }
        #endregion

        //-----------------------------UI-----------------------------
        #region
        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            //UI.CreateMenu(menu, inGame);
            if (inGame) {
                InstanceUI.CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: false, includeRandomizer: false, null, menu, inGame, forceEnabled: false);
                return;
            }
            InstanceUI.CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: true, includeRandomizer: true, delegate {
                OuiModOptions.Instance.Overworld.Goto<OuiModOptions>();
            }, menu, inGame, forceEnabled: false);
        }
        #endregion

        //-----------------------------Somethings-----------------------------
        #region
        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);

            IGraphicsDeviceService graphicsDeviceService =
                Engine.Instance.Content.ServiceProvider
                .GetService(typeof(IGraphicsDeviceService))
                as IGraphicsDeviceService;

            ModAsset asset = Everest.Content.Get("Effects/SkinModHelperShader.cso", true);
            FxColorGrading_SMH = new Effect(graphicsDeviceService.GraphicsDevice, asset.Data);

            SkinsSystem.LoadContent(firstLoad);
        }
        #endregion

        //-----------------------------Setting update-----------------------------
        #region
        public static void UpdatePlayerSkin(string newSkinId, bool inGame) {
            if (Session != null) {
                SessionSet_PlayerSkin(null);
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
                SessionSet_SilhouetteSkin(null);
            }
            Settings.SelectedSilhouetteSkin = newSkinId;
        }
        public static void UpdateGeneralSkin(string SkinId, bool OnOff, bool inGame) {
            if (Session != null) {
                SessionSet_GeneralSkin(SkinId, null);
            }
            Settings.ExtraXmlList[SkinId] = OnOff;
            RefreshSkins(false, inGame);
        }
        #endregion

        //-----------------------------Session update-----------------------------
        #region
        public static void SessionSet_PlayerSkin(string newSkinId) {
            if (Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"The player is not in the level, cannot setting session!");
                return;
            } else if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
                return;
            }
            Session.SelectedPlayerSkin = newSkinId;
            PlayerSkinSystem.RefreshPlayerSpriteMode();
        }
        public static void SessionSet_SilhouetteSkin(string newSkinId) {
            if (Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"The player is not in the level, cannot setting session!");
                return;
            } else if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
                return;
            }
            Session.SelectedSilhouetteSkin = newSkinId;
        }
        public static void SessionSet_GeneralSkin(string newSkin, bool? OnOff) {
            if (Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"The player is not in the level, cannot setting session!");
                return;
            } else if (GetGeneralSkin(newSkin) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"GeneralSkin '{newSkin}' does not exist!");
                return;
            }

            if (OnOff == null && Session.ExtraXmlList.ContainsKey(newSkin)) {
                Session.ExtraXmlList.Remove(newSkin);
            } else if (OnOff != null){
                Session.ExtraXmlList[newSkin] = OnOff == true;
            }
        }
        #endregion

        //-----------------------------Skins Refresh-----------------------------
        #region
        public static void RefreshSkinValues(bool? Setting, bool inGame) {
            if (Setting != null) {
                Settings.FreeCollocations_OffOn = (bool)Setting;
            }

            foreach (var respriteBank in RespriteBankModule.InstanceList) {
                respriteBank.DoRefresh(inGame);
            }
            UpdateParticles();
        }
        #endregion

        //-----------------------------Method----------------------------- 
        #region

        /// <returns> 
        /// Return settings or specified PlayerSkin if it exist, or with suffix.
        /// </returns>
        public static string GetPlayerSkin(string skin_suffix = null, string skinName = null) {
            if (skinName == null) {
                skinName = Session?.SelectedPlayerSkin ?? Settings.SelectedPlayerSkin ?? "";
            }

            if (skinConfigs.ContainsKey(skinName + skin_suffix)) {
                return skinName + skin_suffix;
            } else if (skinConfigs.ContainsKey(skinName)) {
                return skinName;
            } else {
                return null;
            }
        }

        /// <returns> 
        /// Return SilhouetteSkin of settings if it exist, or with suffix.
        /// </returns>
        public static string GetSilhouetteSkin(string skin_suffix = null) {
            string skinName = Session?.SelectedSilhouetteSkin ?? Settings.SelectedSilhouetteSkin ?? "";

            return GetPlayerSkin(skin_suffix, skinName);
        }

        /// <returns> 
        /// Return the enabled status of an GeneralSkin, return null if it does not exist.
        /// </returns>
        public static bool? GetGeneralSkin(string skinName) {
            if (!OtherskinConfigs.ContainsKey(skinName)) {
                return null;
            }
            if (Session?.ExtraXmlList.ContainsKey(skinName) == true) {
                return Session.ExtraXmlList[skinName];
            }
            if (Settings.ExtraXmlList.ContainsKey(skinName)) {
                return Settings.ExtraXmlList[skinName];
            }
            
            return false;
        }

        #endregion
        #region
        public static List<SkinModHelperConfig> GetEnabledGeneralSkins() {
            if (OtherskinConfigs.Count > 0) {
                return OtherskinConfigs.Values.Where(config => GetGeneralSkin(config.SkinName) == true).ToList();
            }
            return new();
        }

        /// <summary> 
        /// A method to get PlayerSkin's name based on it's hashValue. The hash defaults as player's current skin
        /// </summary>
        public static string GetPlayerSkinName(int hashValues = -1) {
            if (hashValues < 0) {
                hashValues = Player_Skinid_verify;
            }
            if (skinname_hashcache.TryGetValue(hashValues, out string name)) {
                return name;
            }
            return skinname_hashcache[hashValues] = skinConfigs.Values.FirstOrDefault(config => config.hashValues == hashValues)?.SkinName;
        }

        public static List<string> GetAllConfigsSpritePath() {
            List<string> paths = new();
            foreach (SkinModHelperConfig config in skinConfigs.Values)
                if (!string.IsNullOrEmpty(config.OtherSprite_Path) && !paths.Contains(config.OtherSprite_Path))
                    paths.Add(config.OtherSprite_Path);
            foreach (SkinModHelperConfig config in OtherskinConfigs.Values)
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath) && !paths.Contains(config.OtherSprite_ExPath))
                    paths.Add(config.OtherSprite_ExPath);
            return paths;
        }
        #endregion
    }
}
