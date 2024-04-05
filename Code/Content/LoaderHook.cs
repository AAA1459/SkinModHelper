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
        #region
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            IL.Celeste.LevelLoader.ctor += il_LevelLoader_ctor;
            On.Celeste.OverworldLoader.LoadThread += OverworldLoaderLoadThreadHook;

            On.Celeste.GameLoader.LoadThread += GameLoaderLoadThreadHook;

            On.Celeste.OuiFileSelectSlot.Setup += OuiFileSelectSlotSetupHook;
            doneILHooks.Add(new ILHook(typeof(OuiFileSelect).GetMethod("Enter", BindingFlags.Public | BindingFlags.Instance).GetStateMachineTarget(), OuiFileSelectEnterILHook));
        }

        public static void Unload() {
            IL.Celeste.LevelLoader.ctor -= il_LevelLoader_ctor;
            On.Celeste.OverworldLoader.LoadThread -= OverworldLoaderLoadThreadHook;

            On.Celeste.GameLoader.LoadThread -= GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup -= OuiFileSelectSlotSetupHook;
        }

        #endregion

        //-----------------------------Loader-----------------------------
        #region
        // loading if game starts.
        private static void GameLoaderLoadThreadHook(On.Celeste.GameLoader.orig_LoadThread orig, GameLoader self) {
            ReloadSettings();
            orig(self);
            // Don't put the methods under orig(self), it crashes the game randomly in FNA...
        }

        // loading if enter the maps.
        /*
        private static void on_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
            orig(self, session, startPosition);
            // Methods moved to il_LevelLoader_ctor, some extreme testing told me the hook is not safe if its here.
        } 
        */
        private static void il_LevelLoader_ctor(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // do ILHook instead of usually hooking to orig under,
            // for prevent this process interfering with other processes, then makes the atlas-warning or anything become enough to make level loading fails.
            // and... this only happens to a very small number of people, semi-randomly, so it's not obvious.
            cursor.GotoNext(MoveType.After, instr => instr.MatchStsfld(typeof(GFX), "PortraitsSpriteBank"));
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<Session>>((session) => {

                if (session != null) {
                    backpackOn = backpackSetting == 3 || (backpackSetting == 0 && session.Inventory.Backpack) || (backpackSetting == 1 && !session.Inventory.Backpack);
                }
                Player_Skinid_verify = 0;

                string hash_object = GetPlayerSkin();
                if (hash_object != null) {
                    Player_Skinid_verify = skinConfigs[!backpackOn ? GetPlayerSkin("_NB", hash_object) : hash_object].hashValues;
                }
                RefreshSkins(true);
            });
        }


        // loading if overworld loads or exit maps.
        private static void OverworldLoaderLoadThreadHook(On.Celeste.OverworldLoader.orig_LoadThread orig, OverworldLoader self) {
            RefreshSkins(true, false);
            orig(self);
        }
        #endregion

        #region
        // loading if save file menu be first time enter when before overworld reloaded.
        private static void OuiFileSelectSlotSetupHook(On.Celeste.OuiFileSelectSlot.orig_Setup orig, OuiFileSelectSlot self) {
            if (self.FileSlot == 0) {
                RefreshSkins(true, false);

                if (SaveFilePortraits) {
                    // return the madeline's portrait to orig if SaveFilePortraits installed
                    foreach (string SpriteID in PortraitsSkins_records.Keys) {
                        PortraitsSkin_record[SpriteID] = null;
                    }

                    // Reload the SpriteID registration code of "SaveFilePortraits"
                    Logger.Log("SkinModHelper", $"SaveFilePortraits reload start");
                    SaveFilePortraits_Reload();
                }
            }
            slots_tracking[self.FileSlot] = self;
            orig(self);
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
        #endregion
    }
}