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
    public static class LoaderHook {
        #region Hooks
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            On.Celeste.GameLoader.Begin += GameLoaderBeginHook;
            On.Celeste.OverworldLoader.LoadThread += OverworldLoaderLoadThreadHook;
            
            doneHooks.Add(new Hook(typeof(LevelLoader).GetMethod("orig_ctor", BindingFlags.Public | BindingFlags.Instance),
                                   typeof(LoaderHook).GetMethod("on_LevelLoader_origctor", BindingFlags.NonPublic | BindingFlags.Static)));

            On.Celeste.OuiFileSelectSlot.Setup += OuiFileSelectSlotSetupHook;
            doneILHooks.Add(new ILHook(typeof(OuiFileSelect).GetMethod("Enter", BindingFlags.Public | BindingFlags.Instance).GetStateMachineTarget(), OuiFileSelectEnterILHook));
        }

        public static void Unload() {
            On.Celeste.GameLoader.Begin -= GameLoaderBeginHook;
            On.Celeste.OverworldLoader.LoadThread -= OverworldLoaderLoadThreadHook;

            On.Celeste.OuiFileSelectSlot.Setup -= OuiFileSelectSlotSetupHook;
        }

        #endregion
        
        #region Loader
        // loading if game starts.
        private static void GameLoaderBeginHook(On.Celeste.GameLoader.orig_Begin orig, GameLoader self) {
            ReloadSettings();

            loadingTextures.Add(OVR.Atlas.GetAtlasSubtextures("loading/"));
            List<string> paths = GetAllConfigsSpritePath();
            foreach (string path in paths)
                if (OVR.Atlas.HasAtlasSubtextures(path + "/loading/")) {
                    loadingTextures.Add(OVR.Atlas.GetAtlasSubtextures(path + "/loading/"));
                }

            orig(self);
            // Placing the method under orig will result in multi-threaded parallelism here.
        }

        // loading if enter the maps.
        private static void on_LevelLoader_origctor(Action<LevelLoader, Session, Vector2?> orig, LevelLoader self, Session session, Vector2? startPosition) {
            if (session != null) {
                backpackOn = backpackSetting == 3 || (backpackSetting == 0 && session.Inventory.Backpack) || (backpackSetting == 1 && !session.Inventory.Backpack);
            }
            Player_Skinid_verify = 0;

            string hash_object = GetPlayerSkin();
            if (hash_object != null) {
                Player_Skinid_verify = skinConfigs[!backpackOn ? GetPlayerSkin("_NB", hash_object) : hash_object].hashValues;
            }
            RefreshSkins(true);
            orig(self, session, startPosition);
            // Placing the method under orig will result in multi-threaded parallelism here.
        }


        // loading if overworld loads or exit maps.
        private static void OverworldLoaderLoadThreadHook(On.Celeste.OverworldLoader.orig_LoadThread orig, OverworldLoader self) {
            RefreshSkins(true, false);
            orig(self);
        }
        #endregion

        #region OuiFileSelect
        // loading if save file menu be first time enter when before overworld reloaded.
        private static void OuiFileSelectSlotSetupHook(On.Celeste.OuiFileSelectSlot.orig_Setup orig, OuiFileSelectSlot self) {
            if (self.FileSlot == 0) {
                RefreshSkins(true, false);

                if (SaveFilePortraits) {
                    Logger.Log("SkinModHelper", $"SaveFilePortraits reload start");
                    SaveFilePortraits_Reload();
                }
            }
            if (SaveFilePortraits) {
                Reskin_PortraitsBank.Active = false;
            }
            slots_tracking[self.FileSlot] = self;
            orig(self);
            Reskin_PortraitsBank.Active = true;
        }
        private static Dictionary<int, OuiFileSelectSlot> slots_tracking = new();
        private static void OuiFileSelectEnterILHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(MoveType.AfterLabel, instr => instr.MatchStfld<OuiFileSelect>("SlotSelected"));
            cursor.EmitDelegate<Action>(() => {
                // Make sure the slots's portrait reloading when everytime open save menu.
                if (OuiFileSelect.Loaded) {
                    foreach (OuiFileSelectSlot slot in new List<OuiFileSelectSlot>(slots_tracking.Values)) {
                        typeof(OuiFileSelectSlot).GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(slot, null);
                    }
                }
            });
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

                foreach (string animation in sprite.Sprite.Animations.Keys) {
                    if (animation.StartsWith("idle_") && !animation.Substring(5).Contains("_")) {
                        MTexture texture = sprite.Sprite.Animations[animation].Frames[0];
                        if (texture.Height <= 200 && texture.Width <= 200 && !Sources_record.Contains(texture.ToString())) {
                            Sources_record.Add($"{sprite.Sprite.GetFrame(animation, 0)}");
                            On_ExistingPortraits.Add(new Tuple<string, string>(portrait, animation));
                        }
                    }
                }
            }
            Logger.Log("SaveFilePortraits", $"Found {On_ExistingPortraits.Count} portraits to pick from.");
        }
        #endregion
    }
}