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
    public class SomePatches {
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            On.Celeste.DeathEffect.Render += DeathEffectRenderHook;
            IL.Celeste.DeathEffect.Draw += DeathEffectDrawHook;

            On.Monocle.Sprite.Play += PlayerSpritePlayHook;
            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            IL.Celeste.MiniTextbox.ctor += SwapTextboxHook;

            doneILHooks.Add(new ILHook(typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), SwapTextboxHook));

            if (OrigSkinModHelper_loaded) {
                try {
                    Logger.Log(LogLevel.Verbose, "SkinModHelper", $"SkinModHelperPlus trying interruption the code of orig SkinModHelper.");

                    Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "SkinModHelper").First().GetType().Assembly;
                    Type OldModule = assembly.GetType("SkinModHelper.Module.SkinModHelperModule");

                    doneHooks.Add(new Hook(OldModule.GetMethod("ReloadSettings", BindingFlags.NonPublic | BindingFlags.Instance),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_1", BindingFlags.NonPublic | BindingFlags.Instance), OldModule));

                    doneHooks.Add(new Hook(OldModule.GetMethod("CreateModMenuSection", BindingFlags.Public | BindingFlags.Instance),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_4", BindingFlags.Public | BindingFlags.Instance), OldModule));

                    //OldModule.GetMethod("Unload", BindingFlags.Public | BindingFlags.Instance).Invoke(OldModule, new object[] { OldModule });
                } catch (Exception e) {
                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"SkinModHelperPlus trying interruption the code of orig SkinModHelper, but it failed.");
                }
            }
        }

        public static void Unload() {
            On.Celeste.DeathEffect.Render -= DeathEffectRenderHook;
            IL.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;

            On.Monocle.Sprite.Play -= PlayerSpritePlayHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            IL.Celeste.MiniTextbox.ctor -= SwapTextboxHook;
        }


        //-----------------------------Portraits-----------------------------
        private static void SwapTextboxHook(ILContext il) {
            ILCursor cursor = new(il);
            // Move to the last occurence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) {
            }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true) {
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((orig) => {
                    return ReplacePortraitPath(orig);
                });
            }
        }

        // This one requires double hook - for some reason they implemented a tiny version of the Textbox class that behaves differently
        private static void CampfireQuestionHook(ILContext il) {
            ILCursor cursor = new(il);
            // Move to the last occurrence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) {
            }
            // Make sure nothing went wrong
            if (cursor.Prev?.MatchIsinst<FancyText.Portrait>() == true) {
                cursor.EmitDelegate<Func<FancyText.Portrait, FancyText.Portrait>>((orig) => {
                    return ReplacePortraitPath(orig);
                });
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("_ask"),
                instr => instr.MatchCall(out MethodReference method) && method.Name == "Concat")) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {
                    return ReplaceTextboxPath(orig);
                });
            }
        }

        // ReplacePortraitPath makes textbox path funky, so correct to our real path or revert to vanilla if it does not exist
        private static string ReplaceTextboxPath(string textboxPath) {

            string PortraitId = "portrait_" + textboxPath.Split('_')[0].Replace("textbox/", ""); // "textbox/[skin id]_ask"

            if (GFX.PortraitsSpriteBank.Has(PortraitId)) {
                string SourcesPath = GFX.PortraitsSpriteBank.SpriteData[PortraitId].Sources[0].XML.Attr("textbox");

                textboxPath = SourcesPath == null ? "textbox/madeline_ask" : $"textbox/{SourcesPath}_ask";
                if (!GFX.Portraits.Has(textboxPath)) {
                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"Requested texture that does not exist: {textboxPath}");
                    textboxPath = "textbox/madeline_ask";
                }
            }

            return textboxPath;
        }
        private static FancyText.Portrait ReplacePortraitPath(FancyText.Portrait portrait) {

            string skinId = portrait.SpriteId;

            foreach (string SpriteId in PortraitsSkin_record.Keys) {
                //Ignore case of string
                if (string.Compare(SpriteId, skinId, true) == 0) {
                    skinId = SpriteId + PortraitsSkin_record[SpriteId];
                }
            }

            if (GFX.PortraitsSpriteBank.Has(skinId)) {
                portrait.Sprite = skinId.Replace("portrait_", "");
            }
            return portrait;
        }

        //-----------------------------Sprites-----------------------------
        private static void PlayerSpritePlayHook(On.Monocle.Sprite.orig_Play orig, Sprite self, string id, bool restart = false, bool randomizeFrame = false) {
            if (self is PlayerSprite) {
                if (id == "duck" && self.LastAnimationID == "duck") {
                    //Duck's animation frames keep replaying? Blocks it!
                    return;
                } else if (id == "lookUp" && self.LastAnimationID.StartsWith("lookUp")) {
                    return;
                }

                DynData<PlayerSprite> selfData = new DynData<PlayerSprite>((PlayerSprite)self);
                if (selfData["spriteName_orig"] != null) {
                    GFX.SpriteBank.CreateOn(self, (string)selfData["spriteName_orig"]);
                    selfData["spriteName_orig"] = null;
                }

                try {
                    orig(self, id, restart, randomizeFrame);
                    return;
                } catch {
                    if (selfData["spriteName_orig"] == null && selfData.Get<string>("spriteName") != "") {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"'{selfData["spriteName"]}' missing animation: {id}");
                        selfData["spriteName_orig"] = selfData["spriteName"];
                    }

                    if (GFX.SpriteBank.SpriteData["player"].Sprite.Animations.ContainsKey(id)) {
                        GFX.SpriteBank.CreateOn(self, "player");
                    } else if (GFX.SpriteBank.SpriteData["player_no_backpack"].Sprite.Animations.ContainsKey(id)) {
                        GFX.SpriteBank.CreateOn(self, "player_no_backpack");
                    }
                }
            }
            orig(self, id, restart, randomizeFrame);
        }


        //-----------------------------Death Effect-----------------------------
        private static void DeathEffectRenderHook(On.Celeste.DeathEffect.orig_Render orig, DeathEffect self) {

            string spritePath = (string)new DynData<DeathEffect>(self)["spritePath"];

            if (self.Entity != null && spritePath == null) {

                var Sprite = self.Entity.GetType().GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance);
                if (Sprite != null) {
                    Sprite On_sprite = Sprite.GetValue(self.Entity) as Sprite;
                    spritePath = getAnimationRootPath(On_sprite) + "death_particle";
                }

                if (self.Entity is PlayerDeadBody) {
                    string SpriteID = "death_particle";
                        if (OtherSkins_records.ContainsKey(SpriteID)) {
                            RefreshSkinValues_OtherExtra(SpriteID, null, true, false);
                            string overridePath = getOtherSkin_ReskinPath(GFX.Game, "death_particle", SpriteID, OtherSkin_record[SpriteID]);

                            spritePath = overridePath == "death_particle" ? spritePath : overridePath;
                        }
                }
                new DynData<DeathEffect>(self)["spritePath"] = spritePath;
            }
            if (self.Entity != null) {
                DeathEffectNewDraw(self.Entity.Position + self.Position, self.Color, self.Percent, spritePath);
            }
        }
        public static void DeathEffectNewDraw(Vector2 position, Color color, float ease, string spritePath = "") {
            spritePath = (spritePath == null || !GFX.Game.Has(spritePath)) ? "characters/player/hair00" : spritePath;

            Color color2 = (Math.Floor(ease * 10f) % 2.0 == 0.0) ? color : Color.White;
            MTexture mTexture = GFX.Game[spritePath];
            float num = (ease < 0.5f) ? (0.5f + ease) : Ease.CubeOut(1f - (ease - 0.5f) * 2f);
            for (int i = 0; i < 8; i++) {
                Vector2 value = Calc.AngleToVector(((float)i / 8f + ease * 0.25f) * ((float)Math.PI * 2f), Ease.CubeOut(ease) * 24f);
                mTexture.DrawCentered(position + value + new Vector2(-1f, 0f), Color.Black, new Vector2(num, num));
                mTexture.DrawCentered(position + value + new Vector2(1f, 0f), Color.Black, new Vector2(num, num));
                mTexture.DrawCentered(position + value + new Vector2(0f, -1f), Color.Black, new Vector2(num, num));
                mTexture.DrawCentered(position + value + new Vector2(0f, 1f), Color.Black, new Vector2(num, num));
            }

            for (int j = 0; j < 8; j++) {
                Vector2 value2 = Calc.AngleToVector(((float)j / 8f + ease * 0.25f) * ((float)Math.PI * 2f), Ease.CubeOut(ease) * 24f);
                mTexture.DrawCentered(position + value2, color2, new Vector2(num, num));
            }
        }

        // Although in "DeathEffectRenderHook", we blocked the original method. but only Player will still run this Hook
        private static void DeathEffectDrawHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/hair00"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string spritePath = orig;
                    if (Engine.Scene is Level) {
                        Sprite On_sprite = Engine.Scene.Tracker?.GetEntity<Player>().Sprite;
                        spritePath = getAnimationRootPath(On_sprite) + "death_particle";

                        spritePath = !GFX.Game.Has(spritePath) ? orig : spritePath;
                    }

                    if (spritePath == orig) {
                        string SpriteID = "death_particle";
                        if (OtherSkins_records.ContainsKey(SpriteID)) {
                            RefreshSkinValues_OtherExtra(SpriteID, null, true, false);
                            string SkinId = getOtherSkin_ReskinPath(GFX.Game, "death_particle", SpriteID, OtherSkin_record[SpriteID]);

                            spritePath = SkinId == "death_particle" ? spritePath : SkinId;
                        }
                    }
                    return spritePath;
                });
            }
        }

        // Maybe... maybe maybe...
        private void EmptyBlocks_1(object obj) { }
        public void EmptyBlocks_4(object obj, object obj_2, object obj_3, object obj_4) { }
    }
}