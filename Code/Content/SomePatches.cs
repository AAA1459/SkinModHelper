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
        #region
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        public static void Load() {
            On.Celeste.DeathEffect.Render += DeathEffectRenderHook;
            IL.Celeste.DeathEffect.Draw += DeathEffectDrawHook;

            On.Monocle.Sprite.Play += PlayerSpritePlayHook;
            On.Celeste.Player.SuperJump += PlayerSuperJumpHook;
            On.Celeste.Player.SuperWallJump += PlayerSuperWallJumpHook;
            IL.Celeste.Player.Render += PlayerRenderIlHook_Sprite;

            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            IL.Celeste.MiniTextbox.ctor += SwapTextboxHook;

            On.Monocle.Sprite.SetAnimationFrame += SpriteSetAnimationFrameHook;

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("TempleFallCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), TempleFallCoroutineILHook));
            doneILHooks.Add(new ILHook(typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), SwapTextboxHook));

            if (OrigSkinModHelper_loaded) {
                int tracking_numbers = 0;
                try {
                    Logger.Log(LogLevel.Verbose, "SkinModHelper", $"SkinModHelperPlus trying interruption the code of orig SkinModHelper.");

                    Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "SkinModHelper").First().GetType().Assembly;
                    Type OldModule = assembly.GetType("SkinModHelper.Module.SkinModHelperModule");

                    tracking_numbers++;
                    doneHooks.Add(new Hook(OldModule.GetMethod("ReloadSettings", BindingFlags.NonPublic | BindingFlags.Instance),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_1", BindingFlags.NonPublic | BindingFlags.Instance), OldModule));
                    tracking_numbers++;
                    doneHooks.Add(new Hook(OldModule.GetMethod("UniqueSkinSelected", BindingFlags.Public | BindingFlags.Static),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_0_boolen", BindingFlags.Public | BindingFlags.Static)));

                    tracking_numbers++;  // There are some issues with this unimportant hook that only happen to EverestCore, So let's put it last.
                    doneHooks.Add(new Hook(OldModule.GetMethod("CreateModMenuSection", BindingFlags.Public | BindingFlags.Instance),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_4", BindingFlags.Public | BindingFlags.Instance), OldModule));
                } catch (Exception e) {
                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"SkinModHelperPlus trying interruption the code of orig SkinModHelper, but it failed in the hook No.{tracking_numbers}.\n {e}\n");
                }
            }
        }

        public static void Unload() {
            On.Celeste.DeathEffect.Render -= DeathEffectRenderHook;
            IL.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;

            On.Monocle.Sprite.Play -= PlayerSpritePlayHook;

            On.Celeste.Player.SuperJump -= PlayerSuperJumpHook;
            On.Celeste.Player.SuperWallJump -= PlayerSuperWallJumpHook;
            IL.Celeste.Player.Render -= PlayerRenderIlHook_Sprite;

            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            IL.Celeste.MiniTextbox.ctor -= SwapTextboxHook;

            On.Monocle.Sprite.SetAnimationFrame -= SpriteSetAnimationFrameHook;
        }

        #endregion

        //-----------------------------Portraits-----------------------------
        #region
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

        #endregion

        //-----------------------------Players-----------------------------
        #region
        private static void PlayerSuperWallJumpHook(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
            orig(self, dir);
            if (!self.Sprite.CurrentAnimationID.Contains("dreamDashOut")
                && self.Sprite.Has("jumpCrazy")) { self.Sprite.Play("jumpCrazy"); }
        }
        private static void PlayerSuperJumpHook(On.Celeste.Player.orig_SuperJump orig, Player self) {
            orig(self);
            if (!self.Sprite.CurrentAnimationID.Contains("dreamDashOut")
                && self.Sprite.Has("jumpCrazy")) { self.Sprite.Play("jumpCrazy"); }
        }

        private static void PlayerSpritePlayHook(On.Monocle.Sprite.orig_Play orig, Sprite self, string id, bool restart = false, bool randomizeFrame = false) {

            if (self.Entity is Player player) {
                DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(player.Sprite);

                string origID = id;

                #region Animations modify and extended
                if (self.LastAnimationID != null) {
                    bool SwimCheck = (bool)typeof(Player).GetMethod("SwimCheck", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);

                    if (id == "walk" && player.Holding != null) {
                        // Patched on when player running in cutscene and carrying something.
                        id = "runSlow_carry";

                    } else if (id == "dash" && SwimCheck && self.Has("swimDash")) {
                        id = "swimDash";

                    } else if (id == "duck" && player.DashAttacking) {
                        if (SwimCheck && self.Has("swimDashCrouch")) {
                            id = "swimDashCrouch";

                        } else if (self.Has("dashCrouch")) {
                            id = "dashCrouch";
                        }
                    }

                    // Universal code... if you are theo smuggle enthusiast...
                    if (player.Holding != null && !id.EndsWith("_carry") && self.Has($"{id}_carry")) {
                        id = $"{id}_carry";
                    }


                    if (origID != id && origID == "dreamDashOut") {
                        // This requires some special fixes...
                        if (self.CurrentAnimationID.Contains(id)) {
                            origID = id;
                        }
                        if (self.LastAnimationID.Contains(id)) {
                            new DynData<Sprite>(self)["LastAnimationID"] = origID;
                            return;
                        }
                    }
                    else if ((origID != id || id == "lookUp" || id == "duck") && self.LastAnimationID.Contains(id)) {
                        // Make sure that The orig animations will not be forced replay or The new animations will not be forced cancel.
                        return;
                    } else if (self.LastAnimationID.Contains("jumpCrazy") && (id == "jumpFast" || id == "runFast")) {
                        return;
                    }
                }
                #endregion

                if (selfData["spriteName_orig"] != null) {
                    GFX.SpriteBank.CreateOn(self, (string)selfData["spriteName_orig"]);
                    selfData["spriteName_orig"] = null;
                }

                try {
                    orig(self, id, restart, randomizeFrame);
                    if (origID != id && origID == "dreamDashOut") {

                    }
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

        #endregion
        #region
        private static void TempleFallCoroutineILHook(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("idle"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {
                    if (Player_Skinid_verify != 0) {
                        return "fallPose";
                    }
                    return orig;
                });
            }
        }
        private static void PlayerRenderIlHook_Sprite(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/startStarFlyWhite"))) {
                Logger.Log("SkinModHelper", $"Changing startStarFlyWhite path at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, Player, string>>((orig, self) => {
                    string spritePath = getAnimationRootPath(self.Sprite) + "startStarFlyWhite";

                    if (self.Holding != null && self.Sprite.Has("startStarFly_carry") && GFX.Game.HasAtlasSubtexturesAt($"{spritePath}_carry", 0)) {
                        return $"{spritePath}_carry";
                    } else if (GFX.Game.HasAtlasSubtexturesAt(spritePath, 0)) {
                        return spritePath;
                    }
                    return orig;
                });
            }
        }
        #endregion

        //-----------------------------Log Patch-----------------------------
        #region
        private static void SpriteSetAnimationFrameHook(On.Monocle.Sprite.orig_SetAnimationFrame orig, Sprite self, int frame) {
            try {
                orig(self, frame);
            } catch (Exception e) {
                throw new Exception($"[SkinModHelper_LogPatch] '{getAnimationRootPath(self)}'--'{self.CurrentAnimationID}'s frame {frame} does not exist!", e);
            }
        }

        #endregion

        //-----------------------------Death Effect-----------------------------
        #region
        private static void DeathEffectRenderHook(On.Celeste.DeathEffect.orig_Render orig, DeathEffect self) {

            string spritePath = (string)new DynData<DeathEffect>(self)["spritePath"];

            if (self.Entity != null && spritePath == null) {

                var Sprite = GetFieldPlus(self.Entity.GetType(), "sprite");
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
        #endregion

        //-----------------------------Lazy Load-----------------------------
        #region
        public static void LazyLoad() {
            if (MaddieHelpingHandInstalled) {
                Logger.Log(LogLevel.Info, "SkinModHelper", $"interruption MaddieHelpingHand's silhouettes render, Let SkinModHelperPlus own render it");

                using (new DetourContext() { Before = { "*" } }) { // Make those hook to take precedence over the same hook that ExtendedVariants
                    Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "MaxHelpingHand").First().GetType().Assembly;
                    Type madelineSilhouetteTrigger = assembly.GetType("Celeste.Mod.MaxHelpingHand.Triggers.MadelineSilhouetteTrigger");

                    doneILHooks.Add(new ILHook(madelineSilhouetteTrigger.GetNestedType("<>c", BindingFlags.NonPublic)
                                .GetMethod("<patchPlayerRender>b__4_1", BindingFlags.NonPublic | BindingFlags.Instance), hookMadelineIsSilhouette));
                    doneILHooks.Add(new ILHook(madelineSilhouetteTrigger.GetNestedType("<>c", BindingFlags.NonPublic)
                        .GetMethod("<patchPlayerRender>b__4_3", BindingFlags.NonPublic | BindingFlags.Instance), hookMadelineIsSilhouette));
                }
            }
        }
        #endregion

        //-----------------------------Hook MaddieHelpingHand / MaxHelpingHand-----------------------------
        #region
        private static void hookMadelineIsSilhouette(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Callvirt && (instr.Operand as MethodReference).Name == "get_MadelineIsSilhouette")) {
                cursor.EmitDelegate<Func<bool, bool>>(orig => {
                    return false;
                });
            }
        }
        #endregion


        #region
        // Maybe... maybe maybe...
        public static bool EmptyBlocks_0_boolen() { return false; }
        private void EmptyBlocks_1(object obj) { }
        public void EmptyBlocks_4(object obj, object obj_2, object obj_3, object obj_4) { }
        public void EmptyBlocks_3(object obj, object obj_2, object obj_3) { }
        #endregion
    }
}