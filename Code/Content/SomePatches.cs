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
using System.Xml;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public class SomePatches {
        #region
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        public static void Load() {
            On.Celeste.DeathEffect.Render += DeathEffectRenderHook;
            On.Celeste.DeathEffect.Draw += DeathEffectDrawHook;

            using (new DetourContext() { Before = { "*" } }) { // Give those hook lowest priority.
                On.Monocle.Sprite.Play += PlayerSpritePlayHook;
                On.Monocle.Sprite.SetAnimationFrame += SpriteSetAnimationFrameHook;
            }
            On.Celeste.Player.SuperJump += PlayerSuperJumpHook;
            On.Celeste.Player.SuperWallJump += PlayerSuperWallJumpHook;
            IL.Celeste.Player.Render += PlayerRenderIlHook_Sprite;

            IL.Celeste.FancyText.Parse += ilFancyTextParse;
            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("TempleFallCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), TempleFallCoroutineILHook));

            if (OrigSkinModHelper_loaded) {
                int tracking_numbers = 0;
                try {
                    Logger.Log(LogLevel.Verbose, "SkinModHelper", $"SkinModHelperPlus trying interruption the code of orig SkinModHelper.");

                    Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "SkinModHelper").First().GetType().Assembly;
                    Type OldModule = assembly.GetType("SkinModHelper.Module.SkinModHelperModule");

                    tracking_numbers++;
                    doneHooks.Add(new Hook(OldModule.GetMethod("ReloadSettings", BindingFlags.NonPublic | BindingFlags.Instance),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_1", BindingFlags.NonPublic | BindingFlags.Static)));
                    tracking_numbers++;
                    doneHooks.Add(new Hook(OldModule.GetMethod("UniqueSkinSelected", BindingFlags.Public | BindingFlags.Static),
                                         typeof(SomePatches).GetMethod("EmptyBlocks_0_boolen", BindingFlags.NonPublic | BindingFlags.Static)));
                    tracking_numbers++;
                    doneHooks.Add(new Hook(OldModule.GetMethod("CreateModMenuSection", BindingFlags.Public | BindingFlags.Instance),
                                         typeof(SomePatches).GetMethod("modOptions_EmptyBlocks", BindingFlags.NonPublic | BindingFlags.Static)));
                } catch (Exception e) {
                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"SkinModHelperPlus trying interruption the code of orig SkinModHelper, but it failed in the hook No.{tracking_numbers}.\n {e}\n");
                }
            }
        }

        public static void Unload() {
            On.Celeste.DeathEffect.Render -= DeathEffectRenderHook;
            On.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;

            On.Monocle.Sprite.Play -= PlayerSpritePlayHook;

            On.Celeste.Player.SuperJump -= PlayerSuperJumpHook;
            On.Celeste.Player.SuperWallJump -= PlayerSuperWallJumpHook;
            IL.Celeste.Player.Render -= PlayerRenderIlHook_Sprite;

            IL.Celeste.FancyText.Parse -= ilFancyTextParse;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;

            On.Monocle.Sprite.SetAnimationFrame -= SpriteSetAnimationFrameHook;
        }

        #endregion

        //-----------------------------Portraits-----------------------------
        #region

        // Relinking portrait skin's textbox and sfx, instead of just changing the portrait self.
        private static void ilFancyTextParse(ILContext il) {
            ILCursor cursor = new(il);

            // This is more universal than the old hook, can works to the choice prompts of lua cutscenes.
            // But cannot refresh timely when in a dialogue.
            if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStfld<FancyText.Portrait>("Sprite"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {
                    string skinId = Reskin_PortraitsBank.GetCurrentSkin("portrait_" + orig);
                    if (GFX.PortraitsSpriteBank.Has(skinId))
                        orig = skinId.Substring(9);
                    return orig;
                });
            }

        }

        // This one requires hook - for some reason they implemented a tiny version of the Textbox class that behaves differently
        private static void CampfireQuestionHook(ILContext il) {
            ILCursor cursor = new(il);
            // Move to the last occurrence of this
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchIsinst<FancyText.Portrait>())) {
            }

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("_ask"),
                instr => instr.MatchCall(out MethodReference method) && method.Name == "Concat")) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {
                    return ReplaceTextboxPath(orig);
                });
            }
        }

        // ilFancyTextParse makes textbox_ask path funky, so correct to our real path or revert to vanilla for prevent crashes
        private static string ReplaceTextboxPath(string textboxPath) {

            string PortraitId = $"portrait_{textboxPath.Remove(textboxPath.LastIndexOf("_ask")).Replace("textbox/", "")}"; // "textbox/[skin id]_ask"

            if (GFX.PortraitsSpriteBank.Has(PortraitId)) {
                string SourcesPath = GFX.PortraitsSpriteBank.SpriteData[PortraitId].Sources[0].XML.Attr("textbox");

                textboxPath = SourcesPath == null ? "textbox/madeline_ask" : $"textbox/{SourcesPath}_ask";
            }

            if (!GFX.Portraits.Has(textboxPath)) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Requested texture that does not exist: {textboxPath}");
                textboxPath = "textbox/madeline_ask";
            }
            return textboxPath;
        }

        #endregion

        //-----------------------------Players-----------------------------
        #region
        private static void PlayerSuperWallJumpHook(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir) {
            orig(self, dir);
            if (!self.Sprite.CurrentAnimationID.Contains("dreamDashOut") && !SpriteExt_TryPlay(self.Sprite, "wallBounce")) {
                SpriteExt_TryPlay(self.Sprite, "jumpCrazy");
            }
        }
        private static void PlayerSuperJumpHook(On.Celeste.Player.orig_SuperJump orig, Player self) {
            bool hyper = self.Ducking;
            orig(self);
            if (!self.Sprite.CurrentAnimationID.Contains("dreamDashOut") && !SpriteExt_TryPlay(self.Sprite, hyper ? "jumpHyper" : "jumpSuper")) {
                SpriteExt_TryPlay(self.Sprite, "jumpCrazy");
            }
        }

        private static MethodInfo Player_SwimCheck = typeof(Player).GetMethod("SwimCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void PlayerSpritePlayHook(On.Monocle.Sprite.orig_Play orig, Sprite self, string id, bool restart = false, bool randomizeFrame = false) {
            string origID = id;

            if (self.Entity is Player player) {
                #region Animations modify and extended

                if (!restart && self.LastAnimationID != null) {
                    DynamicData playerData = DynamicData.For(player);
                    bool SwimCheck = player.Collidable ? (bool)Player_SwimCheck.Invoke(player, null) : false;

                    if (id == "walk" && player.Holding != null) {
                        // Patched on when player running in cutscene and carrying something.
                        id = "runSlow_carry";

                    } else if (id == "dash" && SwimCheck && self.Has("swimDash")) {
                        id = "swimDash";

                    } else if (id == "duck" && player.DashAttacking == true) {
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


                    if (origID == "dreamDashOut") {
                        // This requires some special fixes...
                        if (self.CurrentAnimationID.Contains(id)) {
                            origID = id;
                        }
                        if (self.LastAnimationID.Contains(id)) {
                            DynamicData.For(self).Set("LastAnimationID", origID);
                            return;
                        }
                    } else if ((origID != id || id == "duck" || id == "lookUp") && self.LastAnimationID.Contains(id)) {
                        // Make sure that The orig animations will not be forced replay or The new animations will not be forced cancel.
                        return;
                    } else if (origID == "runStumble") {
                        return;
                    } else if (self.LastAnimationID.Contains("jumpCrazy")) {
                        if ((origID == "jumpFast" || origID == "fallSlow" || origID == "runFast" || origID == "runWind") &&
                            (!playerData.Get<bool>("onGround") || !player.OnGround())) {
                            return;
                        }
                    } else if (self.LastAnimationID.Contains("jumpHyper") || self.LastAnimationID.Contains("jumpSuper")) {

                        if ((origID == "jumpFast" || origID == "fallFast" || origID == "runFast" || origID == "runWind" || (origID == "duck" && player.StartedDashing == false) || origID == "idle" || origID == "jumpSlow")
                            && (!player.OnGround() || !playerData.Get<bool>("wasOnGround"))
                            && (Math.Abs(player.Speed.X) > 110f
                               || (playerData.Get<float>("wallSpeedRetentionTimer") > 0f && Math.Abs(playerData.Get<float>("wallSpeedRetained")) > 110f))) {
                            return;
                        }

                    } else if (self.LastAnimationID.Contains("wallBounce")) {
                        if ((origID == "jumpFast" || origID == "jumpSlow" || origID == "fallSlow" || origID == "fallFast") &&
                            (!playerData.Get<bool>("onGround"))) {
                            return;
                        }
                    }
                }
                #endregion
            }

            if (self.Entity is Player || self.Entity is PlayerDeadBody) {
                DynamicData selfData = DynamicData.For(self);

                string spriteName_orig = selfData.Get<string>("spriteName_orig");
                if (spriteName_orig != null) {
                    GFX.SpriteBank.CreateOn(self, spriteName_orig);
                    selfData.Set("spriteName_orig", null);
                }

                if (self.Has(id)) {
                    orig(self, id, restart, randomizeFrame);
                    if (origID == "startStarFly") {
                        selfData.Set("CurrentAnimationID", origID);
                    }
                    return;
                } else {
                    string spriteName = selfData.Get<string>("spriteName");
                    if (spriteName != "") {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"'{spriteName}' missing animation: {id}");
                    }

                    if (GFX.SpriteBank.SpriteData["player"].Sprite.Animations.TryGetValue(id, out Sprite.Animation anim) ||
                        GFX.SpriteBank.SpriteData["player_no_backpack"].Sprite.Animations.TryGetValue(id, out anim)) {

                        self.Animations[id] = anim;
                        if (GFX.SpriteBank.Has(spriteName))
                            GFX.SpriteBank.SpriteData[spriteName].Sprite.Animations[id] = anim;
                    }
                    return;
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

                    // This hook position runs only when player.Sprite.CurrentAnimationID are "startStarFly", So we can indexing the textures directly.
                    string spritePath = getAnimationRootPath(self.Sprite.Texture) + "startStarFlyWhite";

                    if (self.Holding != null && GFX.Game.HasAtlasSubtexturesAt($"{spritePath}_carry", 0)) {
                        return $"{spritePath}_carry";
                    }
                    if (GFX.Game.HasAtlasSubtexturesAt(spritePath, 0)) {
                        return spritePath;
                    }
                    DynamicData selfData = DynamicData.For(self);
                    if (!selfData.TryGet("SMH_DisposableLog_bsaofsdlk", out string ddd)) {
                        selfData.Set("SMH_DisposableLog_bsaofsdlk", "");
                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"Requested texture that does not exist: {spritePath}");
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
                throw new Exception($"[SkinModHelper_LogPatch] '{getAnimationRootPath(self)}'--'{(string.IsNullOrEmpty(self.CurrentAnimationID) ? "null" : self.CurrentAnimationID)}'s frame {frame} does not exist!", e);
            }
        }
        #endregion

        //-----------------------------Death Effect-----------------------------
        #region
        private static void DeathEffectRenderHook(On.Celeste.DeathEffect.orig_Render orig, DeathEffect self) {

            DynamicData selfData = DynamicData.For(self);
            bool? deathAnimating = selfData.Get<bool?>("deathAnimating");

            if (!selfData.TryGet<MTexture>("mTexture", out var texture) && self.Entity != null) {
                texture = null;

                var sprite = selfData.Get<Sprite>("sprite") ?? self.Entity.Get<Sprite>();
                if (sprite != null) {
                    float alpha = GetAlpha(sprite.Color);
                    if (alpha < 1f && self.Color.A == 1f) { self.Color = self.Color * alpha; }

                    if (sprite.Has("deathExAnim")) {
                        InsertDeathAnimation(self, sprite, "deathExAnim");
                    }
                    string scolor = CharacterConfig.For(sprite).DeathParticleColor;

                    if (scolor != null && RGB_Regex.IsMatch(scolor)) {
                        self.Color = Calc.HexToColor(scolor) * GetAlpha(self.Color);
                    }

                    if (GetTextureOnSprite(sprite, "death_particle", out var texture2))
                        texture = texture2;
                }
                if (self.Entity is PlayerDeadBody) {
                    string overridePath = OtherSpriteSkins.GetSkinWithPath(GFX.Game, "death_particle");
                    if (overridePath != "death_particle") {
                        texture = GFX.Game[overridePath];
                    }
                }
                selfData.Set("mTexture", texture);
            }

            if (deathAnimating == true) {
                self.Percent = 0.0f;
            } else if (self.Entity != null) {
                DeathEffectNewDraw(self.Entity.Position + self.Position, self.Color, self.Percent, texture);
            }
        }
        public static void DeathEffectNewDraw(Vector2 position, Color color, float ease, MTexture mTexture = null) {
            float alpha = GetAlpha(color);
            if (alpha <= 0f)
                return;
            mTexture ??= GFX.Game["characters/player/hair00"];

            Color outline = Color.Black * alpha;
            Color color2 = (Math.Floor(ease * 10f) % 2.0 == 0.0) ? color : Color.White * alpha;
            float num = (ease < 0.5f) ? (0.5f + ease) : Ease.CubeOut(1f - (ease - 0.5f) * 2f);

            for (int i = 0; i < 8; i++) {
                Vector2 value = Calc.AngleToVector(((float)i / 8f + ease * 0.25f) * ((float)Math.PI * 2f), Ease.CubeOut(ease) * 24f);
                mTexture.DrawCentered(position + value + new Vector2(-1f, 0f), outline, new Vector2(num, num));
                mTexture.DrawCentered(position + value + new Vector2(1f, 0f), outline, new Vector2(num, num));
                mTexture.DrawCentered(position + value + new Vector2(0f, -1f), outline, new Vector2(num, num));
                mTexture.DrawCentered(position + value + new Vector2(0f, 1f), outline, new Vector2(num, num));
            }

            for (int j = 0; j < 8; j++) {
                Vector2 value2 = Calc.AngleToVector(((float)j / 8f + ease * 0.25f) * ((float)Math.PI * 2f), Ease.CubeOut(ease) * 24f);
                mTexture.DrawCentered(position + value2, color2, new Vector2(num, num));
            }
        }

        public static void InsertDeathAnimation(DeathEffect self, Sprite sprite, string id = "deathExAnim") {
            Entity entity = new(self.Entity.Position);

            // Clone the animation, At least make sure it's playing speed doesn't different in some case.
            object clone = new Sprite(null, null);
            if (clone is not Sprite deathAnim) {
                return;
            }
            SkinModHelperInterop.CopyColorGrades(sprite, deathAnim);

            deathAnim.ClearAnimations();
            PatchSprite(sprite, deathAnim);
            if (sprite.Scale.X < 0f && deathAnim.Has(id + "_Alt")) {
                id = id + "_Alt"; // Mirror animation if entity facing left-side?
            }

            deathAnim.Scale = new(1f, 1f);
            deathAnim.Justify = new(0.5f, 0.5f); // Center the texture.
            deathAnim.Visible = deathAnim.Active = true;
            deathAnim.Color = sprite.Color;

            entity.Add(deathAnim);
            Scene scene = self.Entity.Scene;
            scene.Add(entity);
            entity.Depth = -1000000;

            // Make sure animation playing for player pause retry.
            if (self.Entity is PlayerDeadBody || scene[Tags.PauseUpdate].Contains(self.Entity)) {
                scene[Tags.PauseUpdate].Add(entity);
            }

            deathAnim.Play(id);
            DynamicData data = DynamicData.For(self);
            data.Set("deathAnimating", true);

            deathAnim.OnFinish = anim => {
                deathAnim.Visible = false;
                entity.RemoveSelf();
                if (self != null)
                    data.Set("deathAnimating", false);
            };
        }
        #endregion
        #region
        // Although in "DeathEffectRenderHook", we blocked the original method. but only Player will still run this...
        private static void DeathEffectDrawHook(On.Celeste.DeathEffect.orig_Draw orig, Vector2 position, Color color, float ease) {
            Entity entity = null;
            foreach (Player player in (Engine.Scene as Level)?.Tracker?.GetEntities<Player>()) {
                if (player.Center + player.deadOffset == position) {
                    entity = player;
                    break;
                }
            }
            MTexture texture = null;
            Sprite sprite = entity?.Get<PlayerSprite>() ?? entity?.Get<Sprite>();
            if (sprite != null) {
                float alpha = GetAlpha(sprite.Color);
                if (alpha < 1f && color.A == 255)
                    color = color * alpha;

                string scolor = CharacterConfig.For(sprite).DeathParticleColor;

                if (scolor != null && RGB_Regex.IsMatch(scolor)) {
                    color = Calc.HexToColor(scolor) * GetAlpha(color);
                }
                if (GetTextureOnSprite(sprite, "death_particle", out var texture2))
                    texture = texture2;

                if (entity is Player) {
                    string overridePath = OtherSpriteSkins.GetSkinWithPath(GFX.Game, "death_particle");
                    if (overridePath != "death_particle") {
                        texture = GFX.Game[overridePath];
                    }
                }
            }
            DeathEffectNewDraw(position, color, ease, texture);
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
        private static void EmptyBlocks_1(object obj) { }
        private static bool EmptyBlocks_0_boolen() { return false; }
        private static void modOptions_EmptyBlocks(Action<EverestModule, TextMenu, bool, EventInstance> orig, EverestModule self, TextMenu menu, bool inGame, EventInstance snapshot) {
        }
        #endregion
    }
}