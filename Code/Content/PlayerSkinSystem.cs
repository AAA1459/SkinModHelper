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
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;
using AsmResolver.PE.File;

namespace Celeste.Mod.SkinModHelper {
    public static class PlayerSkinSystem {
        #region Hooks

        public static void Load() {
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOn;
            On.Celeste.PlayerSprite.ctor += on_PlayerSprite_ctor;

            using (new DetourContext() { Before = { "*" } }) {
                On.Celeste.PlayerSprite.CreateFramesMetadata += on_PlayerSprite_CreateFramesMetadata;
            }

            using (new DetourContext() { After = { "*" } }) { // btw fixes silhouette color for DJMapHelper's MaxDashesTrigger
                On.Celeste.Player.Update += PlayerUpdateHook;
            }
            On.Celeste.Player.UpdateHair += PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor += PlayerGetTrailColorHook;
            On.Celeste.Player.StartDash += PlayerStartDashHook;
            IL.Celeste.Player.DashUpdate += PlayerDashUpdateIlHook;

            IL.Celeste.Player.Render += PlayerRenderIlHook_Color;
            On.Celeste.PlayerHair.Render += PlayerHairRenderHook;
            On.Celeste.PlayerSprite.Render += PlayerSpriteRenderHook;
            On.Celeste.PlayerDeadBody.ctor += PlayerDeadBodyCtor;

            On.Celeste.PlayerHair.Render += PlayerHairRenderHook_ColorGrade;
            doneHooks.Add(new Hook(typeof(Sprite).GetMethod("Render", BindingFlags.Public | BindingFlags.Instance), SpriteRenderHook_ColorGrade));

            On.Celeste.Lookout.Update += LookoutUpdateHook_ColorGrade;
            On.Celeste.Payphone.Update += PayphoneUpdateHook_ColorGrade;

            On.Celeste.PlayerHair.Update += PlayerHairUpdateHook;
            On.Celeste.PlayerHair.AfterUpdate += on_PlayerHair_AfterUpdate;

            On.Celeste.PlayerHair.GetHairColor += PlayerHairGetHairColorHook;
            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;
            On.Celeste.PlayerHair.GetHairScale += PlayerHairGetHairScaleHook;

            IL.Celeste.Player.UpdateHair += patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate += patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor += patch_SpriteMode_Badeline;

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("orig_Update", BindingFlags.Public | BindingFlags.Instance), ilPlayerOrig_Update));

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("<.ctor>b__280_1", BindingFlags.NonPublic | BindingFlags.Instance), ilPlayer_b__280_1));
            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("<.ctor>b__280_2", BindingFlags.NonPublic | BindingFlags.Instance), ilPlayer_b__280_2));

            if (JungleHelperInstalled) {
                Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "JungleHelper").First().GetType().Assembly;
                Type EnforceSkinController = assembly.GetType("Celeste.Mod.JungleHelper.Entities.EnforceSkinController");

                // I want use ILHook for this, But i don't know How to do.
                doneHooks.Add(new Hook(EnforceSkinController.GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static),
                                       typeof(PlayerSkinSystem).GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static)));
            }
        }

        public static void Unload() {
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOn;
            On.Celeste.PlayerSprite.ctor -= on_PlayerSprite_ctor;
            On.Celeste.PlayerSprite.CreateFramesMetadata -= on_PlayerSprite_CreateFramesMetadata;

            On.Celeste.Player.Update -= PlayerUpdateHook;
            On.Celeste.Player.UpdateHair -= PlayerUpdateHairHook;
            On.Celeste.Player.StartDash -= PlayerStartDashHook;
            On.Celeste.Player.GetTrailColor -= PlayerGetTrailColorHook;
            IL.Celeste.Player.DashUpdate -= PlayerDashUpdateIlHook;

            IL.Celeste.Player.Render -= PlayerRenderIlHook_Color;
            On.Celeste.PlayerHair.Render -= PlayerHairRenderHook;
            On.Celeste.PlayerSprite.Render -= PlayerSpriteRenderHook;
            On.Celeste.PlayerDeadBody.ctor -= PlayerDeadBodyCtor;

            On.Celeste.PlayerHair.Render -= PlayerHairRenderHook_ColorGrade;
            On.Celeste.Lookout.Update -= LookoutUpdateHook_ColorGrade;
            On.Celeste.Payphone.Update -= PayphoneUpdateHook_ColorGrade;

            On.Celeste.PlayerHair.Update -= PlayerHairUpdateHook;
            On.Celeste.PlayerHair.AfterUpdate -= on_PlayerHair_AfterUpdate;

            On.Celeste.PlayerHair.GetHairColor -= PlayerHairGetHairColorHook;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;
            On.Celeste.PlayerHair.GetHairScale -= PlayerHairGetHairScaleHook;

            IL.Celeste.Player.UpdateHair -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor -= patch_SpriteMode_Badeline;
        }
        #endregion

        #region PlayerSprite Ctor
        private static Sprite SpriteBankCreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            // Prevent mode's non-vanilla value causing the game Error
            if (sprite is PlayerSprite && id == "") {
                return null;
            }
            return orig(self, sprite, id);
        }
        private static void on_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode) {
            Level level = Engine.Scene as Level ?? (Engine.Scene as LevelLoader)?.Level;

            bool isGhost = mode < 0;

            if (!isGhost && level != null) {
                backpackOn = backpackSetting == 3 || (backpackSetting == 0 && level.Session.Inventory.Backpack) || (backpackSetting == 1 && !level.Session.Inventory.Backpack);
            }
            if (isGhost) {
                DynamicData.For(self).Set("isGhost", true);
            } else {
                string hash_object = null;
                switch (mode) {
                    case PlayerSpriteMode.Madeline:
                    case PlayerSpriteMode.MadelineNoBackpack:
                        hash_object = GetPlayerSkin();
                        break;
                    case PlayerSpriteMode.MadelineAsBadeline:
                        hash_object = GetOtherselfSkin();
                        break;
                    case PlayerSpriteMode.Playback:
                        hash_object = GetSilhouetteSkin();
                        break;
                    case (PlayerSpriteMode)444482:
                        if ((hash_object = GetPlayerSkin("_lantern")) == GetPlayerSkin())
                            hash_object = null;
                        break;
                    case (PlayerSpriteMode)444483:
                        if ((hash_object = GetOtherselfSkin("_lantern")) == GetOtherselfSkin())
                            hash_object = null;
                        break;
                    default:
                        break;
                }
                if (hash_object != null) {
                    mode = (PlayerSpriteMode)skinConfigs[!backpackOn ? GetPlayerSkin("_NB", hash_object) : hash_object].hashValues;
                } else if (!backpackOn && mode == PlayerSpriteMode.Madeline) {
                    mode = PlayerSpriteMode.MadelineNoBackpack;
                } else if (backpackOn && mode == PlayerSpriteMode.MadelineNoBackpack) {
                    mode = PlayerSpriteMode.Madeline;
                }
            }

            orig(self, mode);
            int requestMode = (int)(isGhost ? (1 << 31) + mode : mode);

            string name = GetPlayerSkinName(requestMode);
            if (name != null && skinConfigs.TryGetValue(name, out var config)) {
                GFX.SpriteBank.CreateOn(self, self.spriteName = config.Character_ID);

                if (config.JungleLanternMode == true) {
                    self.Play("idle", restart: true); // replay the "idle" sprite to make it apply immediately.
                    self.OnFinish += anim => { // when the look up animation finishes, rewind it to frame 7: this way we are getting 7-11 playing in a loop.
                        if (anim == "lookUp") {
                            self.Play("lookUp", restart: true);
                            self.SetAnimationFrame(5);
                        }
                    };
                }
            }

            if (isGhost && self.spriteName == "") {
                Logger.Log(LogLevel.Debug, "SkinModHelper", $"Someone in CelesteNet uses skin mod '{requestMode}' which you don't have");
                GFX.SpriteBank.CreateOn(self, self.spriteName = (level == null || level.Session.Inventory.Backpack ? "player" : "player_no_backpack"));
            } else if (isGhost) {
                Logger.Log(LogLevel.Verbose, "SkinModHelper", $"GhostModeValue: {requestMode}");
            } else {
                Logger.Log(LogLevel.Verbose, "SkinModHelper", $"PlayerModeValue: {requestMode}");
            }
        }

        private static void on_PlayerSprite_CreateFramesMetadata(On.Celeste.PlayerSprite.orig_CreateFramesMetadata orig, string id) {
            orig(id);
            if (id == "player") {
                IDHasHairMetadate.Clear();
            }
            IDHasHairMetadate.Add(id);
            if (GFX.SpriteBank.SpriteData.TryGetValue("SkinModHelper_PlayerAnimFill", out var fills)) {
                PatchSprite(fills.Sprite, GFX.SpriteBank.SpriteData[id].Sprite);
            }
        }
        #endregion

        #region Player On

        private static void PlayerUpdateHook(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);
            if (self.Sprite != null) {
                DynamicData.For(self.Sprite).Set("smh_AnimPrefix", smh_Session?.Player_animPrefixAddOn);
            }

            // in there, DJMapHelper's MaxDashesTrigger setting OverrideHairColor for 0 dashes blue hair, let's reset it to skin's 0 dashes color.
            if (self.OverrideHairColor != Player.UsedHairColor) {
                return;
            }
            HairConfig hairConfig = HairConfig.For(self.Hair);
            int? dashCount = GetDashCount(self, self.Sprite);
            if (dashCount != null && (self.Hair.Color != Color.White || hairConfig.HairFlash == false) && hairConfig.Safe_GetHairColor((int)dashCount, out Color color)) {
                self.Hair.Color = color;
            }
        }
        private static void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
            orig(self, applyGravity);

            HairConfig hairConfig = HairConfig.For(self.Hair);
            int? dashCount = GetDashCount(self, self.Sprite);
            if (dashCount != null && (self.Hair.Color != Color.White || hairConfig.HairFlash == false) && hairConfig.Safe_GetHairColor((int)dashCount, out Color color)) {
                self.Hair.Color = color;
            }
        }

        private static int PlayerStartDashHook(On.Celeste.Player.orig_StartDash orig, Player self) {
            SetStartedDashingCount(self);
            return orig(self);
        }
        private static Color PlayerGetTrailColorHook(On.Celeste.Player.orig_GetTrailColor orig, Player self, bool wasDashB) {
            HairConfig hairConfig = HairConfig.For(self.Hair);
            int dashCount = GetStartedDashingCount(self);

            if (hairConfig.Safe_GetHairColor(dashCount, out Color color))
                return color;
            return orig(self, wasDashB);
        }
        public static void PlayerDashUpdateIlHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Player>("P_DashA") || instr.MatchLdsfld<Player>("P_DashB") || instr.MatchLdsfld<Player>("P_DashBadB"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<ParticleType, Player, ParticleType>>((orig, self) => {

                    HairConfig hairConfig = HairConfig.For(self.Hair);
                    int dashCount = GetStartedDashingCount(self);

                    if (hairConfig.Safe_GetHairColor(dashCount, out Color color)) {
                        orig = new(orig);
                        orig.Color = color;
                        orig.Color2 = Color.Lerp(color, Color.White, 0.4f);
                    }
                    return orig;
                });
            }
        }
        #endregion

        #region PlayerDeadBody
        private static void PlayerDeadBodyCtor(On.Celeste.PlayerDeadBody.orig_ctor orig, PlayerDeadBody self, Player player, Vector2 direction) {
            Color? color = null;
            if (CharacterConfig.For(player.Sprite).SilhouetteMode == true) {
                color = player.Sprite.Color;
            }
            orig(self, player, direction);
            if (color.HasValue) {
                player.Sprite.Color = color.Value;
            }
        }

        #endregion

        #region Player IL
        private static void ilPlayerOrig_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("Dashes"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<int, Player, int>>((orig, player) => {

                    HairConfig config = HairConfig.For(player.Hair);
                    if (config.HairFloatingDashCount != null) {
                        return config.HairFloatingDashCount < 0 || Math.Max(player.Dashes, 0) < config.HairFloatingDashCount ? 0 : 2;
                    }
                    return orig;
                });
            }
        }

        private static void PlayerRenderIlHook_Color(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump to the usage of the Red color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_Red"))) {
                Logger.Log("SkinModHelper", $"Patching silhouette hair color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((color, player) => {

                    CharacterConfig ModeConfig = CharacterConfig.For(player.Sprite);
                    
                    object backup = null;
                    if (ModeConfig.LowStaminaFlashColor != null && RGB_Regex.IsMatch(ModeConfig.LowStaminaFlashColor)) {
                        backup = color = Calc.HexToColor(ModeConfig.LowStaminaFlashColor);
                        if (ModeConfig.SilhouetteMode == true) {
                            color = ColorBlend(player.Hair.Color, color);
                        }
                    } else if (ModeConfig.SilhouetteMode == true) {
                        color = ColorBlend(player.Hair.Color, (backup = 0.4f));
                    }

                    if (ModeConfig.LowStaminaFlashHair || (ModeConfig.SilhouetteMode == true)) {
                        DynamicData.For(player.Hair).Set("HairColorGrading", backup ?? color);
                    }
                    return color;
                });
            }

            // jump to the usage of the White-color / Null-color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
                Logger.Log("SkinModHelper", $"Patching silhouette color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((orig, self) => {
                    if (CharacterConfig.For(self.Sprite).SilhouetteMode == true) {
                        return self.Hair.Color;
                    }
                    return orig;
                });
            }
        }

        private static void ilPlayer_b__280_2(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            float optionsWeight(float orig, Player p) => CharacterConfig.For(p.Sprite).IdleOptionsWeight ?? orig;
            PlayerSpriteMode _patchSpriteMode_NB_V(PlayerSpriteMode orig, Player p) => (actualBackpack((int)orig) || CharacterConfig.For(p.Sprite).IdleWarmOptions != null) ? 0 : orig;
            Chooser<string> chooserC(Chooser<string> orig, Player p) => CharacterConfig.For(p.Sprite).IdleColdOptions ?? orig;
            Chooser<string> chooserW(Chooser<string> orig, Player p) => CharacterConfig.For(p.Sprite).IdleWarmOptions ?? orig;

            if (cursor.TryGotoNext(MoveType.After, instr => instr.Match(OpCodes.Ldc_R4, 0.2f))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(optionsWeight);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(_patchSpriteMode_NB_V);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Player>("idleColdOptions"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(chooserC);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Player>("idleWarmOptions"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(chooserW);
            }
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Player>("idleNoBackpackOptions"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(chooserC);
            }
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate(_patchSpriteMode_NB);
            }

        }
        private static void ilPlayer_b__280_1(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate(_patchSpriteMode_NB);
            }
        }
        private static PlayerSpriteMode _patchSpriteMode_NB(PlayerSpriteMode mode) => (PlayerSpriteMode)(actualBackpack((int)mode) ? 0 : 1);
        #endregion

        #region ColorGrade
        private static Dictionary<string, int> _ColorGradeMaxNum = new();
        private static void PlayerHairRenderHook_ColorGrade(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            DynamicData selfData = DynamicData.For(self.Sprite);

            // Save colorgrade in typeof(Image).
            // For make typeof(PlayerDeadBody) inherited typeof(Player)'s colorgrade, or similar situations.
            Atlas atlas = selfData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;
            string colorGrade_Path = selfData.Get<string>("ColorGrade_Path");
            if (!self.Active)
                goto goto_one;

            #region
            if (!selfData.TryGet("ColorGrade_Path", out colorGrade_Path)) {
                colorGrade_Path = getAnimationRootPath(self.Sprite, "idle") + "ColorGrading/";

                //Check if config from v0.7 Before---
                if (self.Entity is Player && OldConfigCheck(self.Sprite, out string isOld)) {
                    atlas = GFX.ColorGrades;
                    colorGrade_Path = OtherskinConfigs[isOld].OtherSprite_ExPath + '/';
                }
                //---

                selfData.Set("ColorGrade_Atlas", atlas);
                selfData.Set("ColorGrade_Path", colorGrade_Path);
            }

            string dir = getAnimationRootPath(colorGrade_Path);
            string fullDir = atlas.RelativeDataPath + dir;
            if (!_ColorGradeMaxNum.TryGetValue(fullDir, out int maxNum)) {
                if (AssetExists<AssetTypeDirectory>(fullDir, out ModAsset dir2)) {
                    maxNum = 2;
                    foreach (ModAsset child in dir2.Children) {
                        if (child.Type == typeof(Texture2D) && child.PathVirtual.StartsWith(fullDir + "dash", StringComparison.CurrentCultureIgnoreCase) && int.TryParse(child.PathVirtual.Substring(fullDir.Length + 4), out int i) && i > maxNum)
                            maxNum = i;
                    }
                }
                _ColorGradeMaxNum[fullDir] = maxNum;
            }
            if (maxNum == 0)
                goto goto_one;

            int? get_dashCount;
            if (self.Entity is Player player) {
                if (player.OverrideHairColor == Player.UsedHairColor)
                    get_dashCount = 0;
                else if (player.MaxDashes <= 0 && player.lastDashes < 2)
                    get_dashCount = 1;
                else
                    get_dashCount = Math.Max(player.lastDashes, 0);
            } else if (selfData.Get("isGhost") != null && GetFieldPlus(self.Entity, "Dashes", out int dashes)){
                get_dashCount = dashes;
                Log(LogLevel.Verbose, $"Got the dashes {dashes} of ghost for colorgrade");
            } else {
                get_dashCount = self.Entity is PlayerDeadBody ? null : GetDashCount(self.Entity, self.Sprite);
            }

            if (self.Color == Color.White && atlas.Has(dir + "flash")) {
                selfData.Set("ColorGrade_Path", colorGrade_Path = dir + "flash");

            } else if (get_dashCount != null) {
                colorGrade_Path = dir + "dash";
                int dashCount = Calc.Clamp(get_dashCount.Value, 0, maxNum);
                while (dashCount > 2 && !atlas.Has(colorGrade_Path + dashCount)) {
                    dashCount--;
                }
                selfData.Set("ColorGrade_Path", colorGrade_Path += dashCount);
            }
        #endregion
            goto_one:

            if (colorGrade_Path != null && atlas.Has(colorGrade_Path)) {
                Effect colorGradeEffect = FxColorGrading_SMH;
                colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                Engine.Graphics.GraphicsDevice.Textures[1] = atlas[colorGrade_Path].Texture.Texture_Safe;

                Matrix matrix = DynamicData.For(Draw.SpriteBatch).Get<Matrix>("transformMatrix");

                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                orig(self);
                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
                return;
            }
            orig(self);
        }
        private static void SpriteRenderHook_ColorGrade(Action<Sprite> orig, Sprite self) {
            DynamicData selfData = DynamicData.For(self);
            string colorGrade_Path = selfData.Get<string>("ColorGrade_Path");

            if (colorGrade_Path != null) {
                Atlas atlas = selfData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;

                if (atlas.Has(colorGrade_Path)) {
                    Effect colorGradeEffect = FxColorGrading_SMH;
                    colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                    Engine.Graphics.GraphicsDevice.Textures[1] = atlas[colorGrade_Path].Texture.Texture_Safe;

                    Matrix matrix = DynamicData.For(Draw.SpriteBatch).Get<Matrix>("transformMatrix");

                    GameplayRenderer.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                    orig(self);
                    GameplayRenderer.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
                    return;
                }
            }
            orig(self);
        }
        #endregion

        #region ColorGrade Other
        private static void LookoutUpdateHook_ColorGrade(On.Celeste.Lookout.orig_Update orig, Lookout self) {
            Player player = Engine.Scene?.Tracker.GetEntity<Player>();
            SyncColorGrade(self.sprite, player?.Sprite);
            orig(self);
        }
        private static void PayphoneUpdateHook_ColorGrade(On.Celeste.Payphone.orig_Update orig, Payphone self) {
            // player's dashes is usually fixed at 1 for payphone cutscenes... so probably this no real works.
            Player player = Engine.Scene?.Tracker.GetEntity<Player>();
            SyncColorGrade(self.Sprite, player?.Sprite);
            orig(self);
        }
        #endregion

        #region PlayerSprite Color
        private static void PlayerSpriteRenderHook(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            if (self.Entity is not (Player or PlayerDeadBody) && DynamicData.For(self).Get("isGhost") == null) {

                PlayerHair hair = self.Entity?.Get<PlayerHair>();
                if (hair?.Sprite == self) {
                    CharacterConfig ModeConfig = CharacterConfig.For(self);

                    if (ModeConfig.SilhouetteMode == true)
                        self.Color = hair.Color * hair.Alpha;
                    else if (self.Color == hair.Color * hair.Alpha)
                        self.Color = Color.White * hair.Alpha;
                }
            }
            orig(self);
        }
        #endregion

        #region PlayerHair
        private static void on_PlayerHair_AfterUpdate(On.Celeste.PlayerHair.orig_AfterUpdate orig, PlayerHair self) {
            // Hair fills its nodes basen on HairCount at AfterUpdate instead of Render... For safety, modify HairCount here.
            if (DynamicData.For(self).TryGet("smh_HairLength", out int? length) && length != null)
                self.Sprite.HairCount = (int)length;

            orig(self);
        }
        private static void PlayerHairUpdateHook(On.Celeste.PlayerHair.orig_Update orig, PlayerHair self) {
            DynamicData.For(self).Set("HairColorGrading", null);

            if (self.Entity is Player player && player.StateMachine.State == 14) {
                int? dashes = GetDashCount(player, self.Sprite);
                if (dashes != null && HairConfig.For(self).Safe_GetHairColor((int)dashes, out Color color))
                    self.Color = color;
            }
            orig(self);
        }

        private static void PlayerHairRenderHook(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            if (self.Active) {
                DynamicData selfData = DynamicData.For(self);
                HairConfig hairConfig = HairConfig.For(self);

                int? get_dashCount = GetDashCount(self.Entity, self.Sprite);
                if (get_dashCount != null && (self.Entity is not Player || self.Color != Color.White || hairConfig.HairFlash == false) && hairConfig.Safe_GetHairColor((int)get_dashCount, out Color color))
                    self.Color = color;

                Color border = self.Border;
                self.Border = ColorBlend(self.Border, selfData.Get("HairColorGrading"));
                if (CharacterConfig.For(self.Sprite).SilhouetteMode == true) {
                    self.Border = ColorBlend(self.Border, self.Color);
                }
                orig(self);
                self.Border = border;

                int? HairLength = hairConfig.GetHairLength(get_dashCount);
                if (self.Entity is Player player && player.StateMachine.State == Player.StRedDash) {
                    HairLength = null;
                }
                selfData.Set("smh_HairLength", HairLength);
                return;
            }
            orig(self);
        }

        private static MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index) {

            HairConfig config = HairConfig.For(self);
            if (config.new_bangs != null && VanillaCharacterTextures.Contains($"{self.Sprite.Texture}")) {
                if (DynamicData.For(self).Get("SMH_DisposableLog_aPhggdddd") == null) {
                    Logger.Log(LogLevel.Debug, "SkinModHelper", $"Avoid the {self.Sprite.spriteName}'s bangs texture work on vanilla characters texture...");
                    DynamicData.For(self).Set("SMH_DisposableLog_aPhggdddd", "");
                }
                return orig(self, index);
            }

            if (index == 0) {
                if (config.new_bangs != null) {
                    return config.new_bangs.Count > self.Sprite.HairFrame ? config.new_bangs[self.Sprite.HairFrame] : config.new_bangs[0];
                }
            } else if (config.new_hairs != null) {
                MTexture hair = config.new_hairs.Count > self.Sprite.HairFrame ? config.new_hairs[self.Sprite.HairFrame] : config.new_hairs[0];

                int ri = index - self.Sprite.HairCount;
                if (GFX.Game.Has($"{hair}_{ri}"))
                    return GFX.Game[$"{hair}_{ri}"]; //Set the texture for hair of each section from back to front.
                else if (GFX.Game.Has($"{hair}_{index}"))
                    return GFX.Game[$"{hair}_{index}"]; //Set the texture for hair of each section.
                else
                    return hair;
            }
            return orig(self, index);
        }
        private static Color PlayerHairGetHairColorHook(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            HairConfig hairConfig = HairConfig.For(self);
            int? dashes = GetDashCount(self.Entity, self.Sprite);

            if (hairConfig.ActualHairColors != null && dashes != null && (self.Entity is not Player || self.Color != Color.White || hairConfig.HairFlash == false)) {
                if (hairConfig.Safe_GetHairColor(index, (int)dashes, out Color color)) {
                    return ColorBlend(color * self.Alpha, DynamicData.For(self).Get("HairColorGrading"));
                }
            }
            return ColorBlend(orig(self, index), DynamicData.For(self).Get("HairColorGrading"));
        }
        private static Vector2 PlayerHairGetHairScaleHook(On.Celeste.PlayerHair.orig_GetHairScale orig, PlayerHair self, int index) {
            HairConfig hairConfig = HairConfig.For(self);
            int? dashes = GetDashCount(self.Entity, self.Sprite);

            if (dashes != null && hairConfig.GetHairScale(index, dashes.Value, out Vector2 scale)) {
                return scale;
            }
            return orig(self, index);
        }
        #endregion

        #region PlayerSpriteMode
        private static void SetPlayerSpriteMode(Player player, PlayerSpriteMode? mode) {
            player ??= _Player;
            if (player == null) {
                return;
            }
            mode ??= player.DefaultSpriteMode;
            if (player.Active) {
                player.ResetSpriteNextFrame(mode.Value);
            } else {
                player.ResetSprite(mode.Value);
            }
        }


        private static void patch_SpriteMode_Badeline(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(static PlayerSpriteMode (PlayerSpriteMode orig, Player self) => {
                    CharacterConfig ModeConfig = CharacterConfig.For(self.Sprite);
                    if (ModeConfig != null) {
                        if (ModeConfig.BadelineMode == true) {
                            return (PlayerSpriteMode)3;
                        } else if (ModeConfig.BadelineMode == false) {
                            return 0;
                        }
                    }
                    return orig;
                });
            }
        }

        public static bool HasLantern(Func<PlayerSpriteMode, bool> orig, PlayerSpriteMode mode) {
            bool boolen = orig(mode);
            if (boolen) {
                return boolen;
            }

            string skinName = GetPlayerSkinName((int)mode);
            if (skinName != null && skinConfigs[skinName].JungleLanternMode) {
                return true;
            }
            return boolen;
        }
        #endregion

        #region Method
        public static void RefreshPlayerSpriteMode() {
            if (Engine.Scene is not Level) {
                return;
            }
            if (SaveData.Instance?.Assists.PlayAsBadeline == true) {
                SetPlayerSpriteMode(_Player, PlayerSpriteMode.MadelineAsBadeline);
            } else {
                SetPlayerSpriteMode(_Player, null);
            }
        }
        public static void RefreshPlayerSpriteMode(Player player) {
            if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineAsBadeline || SaveData.Instance?.Assists.PlayAsBadeline == true)
                SetPlayerSpriteMode(player, PlayerSpriteMode.MadelineAsBadeline);
            else
                SetPlayerSpriteMode(player, null);
        }

        public static int? GetDashCount(Entity entity, PlayerSprite sprite) {
            switch (entity) {
                case BadelineOldsite badelineOldsite:
                    return badelineOldsite.index;
                case Player player:
                    if (player.StateMachine.State == Player.StStarFly)
                        return HairConfig.FeatherIndexInAttrs;

                    // DJMapHelper's MaxDashesTrigger setting OverrideHairColor for 0 dashes blue hair, so let's skin also get 0 dashes.
                    if (player.OverrideHairColor == Player.UsedHairColor)
                        return 0;
                    if (player.lastDashes == 0 && player.MaxDashes <= 0)
                        return 1;
                    return Math.Max(player.lastDashes, 0);
                case PlayerDeadBody playerBody:
                    Player player2 = playerBody.player;
                    if (player2.StateMachine.State == Player.StStarFly)
                        return HairConfig.FeatherIndexInAttrs;
                    if (player2.OverrideHairColor == Player.UsedHairColor)
                        return 0;
                    if (player2.lastDashes == 0 && player2.MaxDashes <= 0)
                        return 1;
                    return Math.Max(player2.lastDashes, 0);
                case PlayerPlayback:
                    return null;
            }
            return sprite.Mode == (PlayerSpriteMode)2 ? 0 : null;
        }


        private static Dictionary<string, string> oldskinname_cache = new();
        public static bool OldConfigCheck(PlayerSprite sprite, out string key) {
            string spriteName = sprite.spriteName;
            if (oldskinname_cache.TryGetValue(spriteName, out key)) {
                return key != null;
            }
            return (oldskinname_cache[spriteName] = key = OtherskinOldConfig.Keys.FirstOrDefault(key2 => spriteName.EndsWith(key2))) != null;
        }

        public static bool actualBackpack(int mode) {
            return !(GetPlayerSkinName(mode)?.EndsWith("_NB") ?? mode == 1 || mode == 4);
        }
        public static int GetStartedDashingCount(Player player) {
            return DynamicData.For(player).Get<int?>("TrailDashCount") ?? SetStartedDashingCount(player);
        }
        public static int SetStartedDashingCount(Player player, int? count = null) {
            int dashCount = Math.Max(count ?? player.Dashes - 1, 0);
            DynamicData.For(player).Set("TrailDashCount", dashCount);
            return dashCount;
        }
        #endregion
    }
}