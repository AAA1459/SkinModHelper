﻿using FMOD.Studio;
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
using Celeste.Mod.SkinModHelper.Interop;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperModule : EverestModule {
        #region Values
        public static SkinModHelperModule Instance;
        public override Type SettingsType => typeof(SkinModHelperSettings);
        public override Type SessionType => typeof(SkinModHelperSession);

        public static SkinModHelperSettings smh_Settings => Settings;
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;

        public static SkinModHelperSession smh_Session => Session;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static SkinModHelperUI InstanceUI;

        public static List<Hook> doneHooks = new List<Hook>();
        public static List<ILHook> doneILHooks = new List<ILHook>();
        #endregion

        #region Hooks
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
            SkinsSystem.Load();

            LoaderHook.Load();
            PlayerSkinSystem.Load();
            ObjectsHook.Load();
            SomePatches.Load();
            TrailRecolor.Load();

            SkinModHelperInterop.Load();
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

        #region UI
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

        #region LoadContent
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

        #region Setting Update
        public static void UpdatePlayerSkin(string newSkinId, bool inGame) {
            if (smh_Session != null) {
                SessionSet_PlayerSkin(null);
            }
            Settings.SelectedPlayerSkin = newSkinId;
            RefreshSkins(false);
        }
        public static void UpdateSilhouetteSkin(string newSkinId, bool inGame) {
            if (smh_Session != null) {
                smh_Session.SelectedSilhouetteSkin = null;
            }
            Settings.SelectedSilhouetteSkin = newSkinId;
            RefreshSkins(false);
        }
        public static void UpdateGeneralSkin(string SkinId, bool OnOff, bool inGame) {
            if (smh_Session != null) {
                smh_Session.ExtraXmlList.Remove(SkinId);
            }
            Settings.ExtraXmlList[SkinId] = OnOff;
            RefreshSkins(false);
        }
        #endregion

        #region Session Update
        public static void SessionSet_PlayerSkin(string newSkinId) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            } 
            if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
            }
            smh_Session.SelectedPlayerSkin = newSkinId;
            RefreshSkins(false);
        }
        public static void SessionSet_SilhouetteSkin(string newSkinId) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            }
            if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
            }
            smh_Session.SelectedSilhouetteSkin = newSkinId;
            RefreshSkins(false);
        }
        public static void SessionSet_GeneralSkin(string newSkin, bool? OnOff) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"The player is not in the level, cannot setting session!");
                return;
            } 
            if (GetGeneralSkin(newSkin) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"GeneralSkin '{newSkin}' does not exist!");
            }

            if (OnOff == null) {
                smh_Session.ExtraXmlList.Remove(newSkin);
            } else if (OnOff != null) {
                smh_Session.ExtraXmlList[newSkin] = OnOff.Value;
            }
            RefreshSkins(false);
        }
        #endregion

        #region Skins Refresh
        /// <summary> 
        /// Used cache for EnabledGeneralSkins. just set this value to <c>GetEnabledGeneralSkins()</c>, and remember to clear it later
        /// </summary>
        public static List<SkinModHelperConfig> _enabledGeneralSkins;
        public static void RefreshSkinValues(bool? Setting, bool inGame) {
            if (Setting != null) {
                Settings.FreeCollocations_OffOn = (bool)Setting;
            }
            _enabledGeneralSkins = GetEnabledGeneralSkins();
            foreach (var respriteBank in RespriteBankModule.InstanceList) {
                respriteBank.DoRefresh(inGame);
            }
            _enabledGeneralSkins = null;
            UpdateParticles();
        }
        #endregion

        #region Method #1

        /// <returns> 
        /// Return settings or specified PlayerSkin if it exist, or with suffix.
        /// </returns>
        public static string GetPlayerSkin(string skin_suffix = null, string skinName = null) {
            if (skinName == null) {
                skinName = Settings.SelectedPlayerSkin ?? "";
                if (Engine.Scene is Level or LevelLoader && smh_Session?.SelectedPlayerSkin != null) {
                    skinName = smh_Session.SelectedPlayerSkin;
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

        /// <returns> 
        /// Return SilhouetteSkin of settings if it exist, or with suffix.
        /// </returns>
        public static string GetSilhouetteSkin(string skin_suffix = null) {
            string skinName = Settings.SelectedSilhouetteSkin ?? "";
            if (Engine.Scene is Level or LevelLoader && smh_Session?.SelectedSilhouetteSkin != null) {
                skinName = smh_Session.SelectedSilhouetteSkin;
            }

            return GetPlayerSkin(skin_suffix, skinName);
        }

        /// <returns> 
        /// Return the enabled status of an GeneralSkin, return null if it does not exist.
        /// </returns>
        public static bool? GetGeneralSkin(string skinName) {
            if (!OtherskinConfigs.ContainsKey(skinName)) 
                return null;

            if (Engine.Scene is Level or LevelLoader && Session != null && Session.ExtraXmlList.TryGetValue(skinName, out bool boolen)) {
                return boolen;
            }
            if (Settings.ExtraXmlList.TryGetValue(skinName, out boolen))
                return boolen;
            return false;
        }

        #endregion
        #region Method #2
        public static List<SkinModHelperConfig> GetEnabledGeneralSkins() {
            if (OtherskinConfigs.Count > 0) {
                List<SkinModHelperConfig> configs = new();
                List<SkinModHelperConfig> delayToAdd = new();

                foreach (var config in OtherskinConfigs.Values) {
                    if (Engine.Scene is Level or LevelLoader && Session != null && Session.ExtraXmlList.TryGetValue(config.SkinName, out bool boolen)) {
                        if (boolen)
                            delayToAdd.Add(config);
                    } else if (Settings.ExtraXmlList.TryGetValue(config.SkinName, out boolen) && boolen) {
                        configs.Add(config);
                    }
                }
                foreach (var config in delayToAdd) {
                    configs.Add(config);
                }
                return configs;
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
