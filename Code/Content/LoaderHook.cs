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

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public class LoaderHook {
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            On.Celeste.LevelLoader.ctor += on_LevelLoader_ctor;

            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread += GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup += OuiFileSelectSlotSetupHook;
        }

        public static void Unload() {
            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread -= GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup -= OuiFileSelectSlotSetupHook;
        }

        //-----------------------------Loader-----------------------------
        private static void on_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
            orig(self, session, startPosition);
            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.Character_ID)) {

                    try {
                        PlayerSprite.CreateFramesMetadata(config.Character_ID);
                    } catch (Exception e) {
                        throw new Exception($"Character_ID '{config.Character_ID}' does not exist in default Sprites.xml", e);
                    }
                }
            }
        }
        private static void GameLoaderLoadThreadHook(On.Celeste.GameLoader.orig_LoadThread orig, GameLoader self) {
            orig(self);
            RecordSpriteBanks_Start();

            string skinName = GetPlayerSkin();
            if (skinName != null) {
                Player_Skinid_verify = skinConfigs[skinName].hashValues;
            }
        }


        // Wait until the main sprite bank is created, then combine with our skin mod banks
        private static void LevelLoaderLoadingThreadHook(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {

            //at this Hooking time, The level data has not established, cannot get Default backpack state of Level 
            if (Settings.Backpack == SkinModHelperSettings.BackpackMode.Off || Settings.Backpack == SkinModHelperSettings.BackpackMode.On) {
                backpackOn = Settings.Backpack != SkinModHelperSettings.BackpackMode.Off;
            }

            string skinName = GetPlayerSkin();
            if (skinName != null) {
                Player_Skinid_verify = skinConfigs[skinName].hashValues;
                if (!backpackOn && GetPlayerSkin("_NB") != null) {
                    Player_Skinid_verify = skinConfigs[skinName + "_NB"].hashValues;
                }
            }
            Xmls_refresh = null;
            SpecificSprite_LoopReload();
            orig(self);
        }




        private static void OuiFileSelectSlotSetupHook(On.Celeste.OuiFileSelectSlot.orig_Setup orig, OuiFileSelectSlot self) {
            if (self.FileSlot == 0) {
                Xmls_refresh = null;
                SpecificSprite_LoopReload();

                foreach (string SpriteID in SpriteSkins_records.Keys) {
                    SpriteSkin_record[SpriteID] = null;
                }
                foreach (string SpriteID in PortraitsSkins_records.Keys) {
                    PortraitsSkin_record[SpriteID] = null;
                }

                if (SaveFilePortraits) {
                    //Reload the SpriteID registration code of "SaveFilePortraits"
                    Logger.Log("SkinModHelper", $"SaveFilePortraits reload start");
                    SaveFilePortraits_Reload();
                }
            }
            orig(self);
        }
        // ---SaveFilePortraits---
        private static void SaveFilePortraits_Reload() {
            List<Tuple<string, string>> On_ExistingPortraits = new List<Tuple<string, string>>();

            Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "SaveFilePortraits").First().GetType().Assembly;
            Type SaveFilePortraitsModule = assembly.GetType("Celeste.Mod.SaveFilePortraits.SaveFilePortraitsModule");

            var ExistingPortraits = SaveFilePortraitsModule.GetField("ExistingPortraits", BindingFlags.Public | BindingFlags.Static);
            ExistingPortraits.SetValue(ExistingPortraits, On_ExistingPortraits);

            On_ExistingPortraits.Clear();
            List<string> Sources_record = new();

            foreach (string portrait in GFX.PortraitsSpriteBank.SpriteData.Keys) {

                SpriteData sprite = GFX.PortraitsSpriteBank.SpriteData[portrait];
                string SourcesPath = sprite.Sources[0].Path;

                if (!Sources_record.Contains(SourcesPath)) {
                    Sources_record.Add(SourcesPath);

                    foreach (string animation in sprite.Sprite.Animations.Keys) {
                        if (animation.StartsWith("idle_") && !animation.Substring(5).Contains("_")
                            && sprite.Sprite.Animations[animation].Frames[0].Height <= 200 && sprite.Sprite.Animations[animation].Frames[0].Width <= 200) {
                            On_ExistingPortraits.Add(new Tuple<string, string>(portrait, animation));
                        }
                    }
                } else {
                    //Logger.Log(LogLevel.Info, "SkinModHelper", $"maybe SkinModHelper made some Sources same of ID, will stop them re-register to SaveFilePortraits");
                }
            }
            Logger.Log("SaveFilePortraits", $"Found {On_ExistingPortraits.Count} portraits to pick from.");
        }
    }
}