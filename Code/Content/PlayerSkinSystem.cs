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

namespace Celeste.Mod.SkinModHelper {
    public class PlayerSkinSystem {
        #region
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public static void Load() {
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOn;
            On.Celeste.PlayerSprite.ctor += on_PlayerSprite_ctor;

            On.Celeste.Player.UpdateHair += PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor += PlayerGetTrailColorHook;
            On.Celeste.Player.StartDash += PlayerStartDashHook;
            IL.Celeste.Player.DashUpdate += PlayerDashUpdateIlHook;

            IL.Celeste.Player.Render += PlayerRenderIlHook_Color;
            
            On.Monocle.Image.Render += OnImageRender_ColorGrade;
            On.Celeste.PlayerHair.Render += PlayerHairRenderHook_ColorGrade;
            On.Celeste.Lookout.Update += LookoutUpdateHook_ColorGrade;
            On.Celeste.Payphone.Update += PayphoneUpdateHook_ColorGrade;

            On.Celeste.PlayerHair.Render += PlayerHairRenderHook;
            On.Celeste.PlayerSprite.Render += PlayerSpriteRenderHook;
            On.Celeste.PlayerHair.Update += PlayerHairUpdateHook;

            On.Celeste.PlayerHair.GetHairColor += PlayerHairGetHairColorHook;
            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;

            IL.Celeste.Player.UpdateHair += patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate += patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor += patch_SpriteMode_Badeline;

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("orig_Update", BindingFlags.Public | BindingFlags.Instance), ilPlayerOrig_Update));

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("<.ctor>b__280_2", BindingFlags.NonPublic | BindingFlags.Instance), patch_SpriteMode_BackPack));
            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("<.ctor>b__280_1", BindingFlags.NonPublic | BindingFlags.Instance), patch_SpriteMode_BackPack));

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

            On.Celeste.Player.UpdateHair -= PlayerUpdateHairHook;
            On.Celeste.Player.StartDash -= PlayerStartDashHook;
            On.Celeste.Player.GetTrailColor -= PlayerGetTrailColorHook;
            IL.Celeste.Player.DashUpdate -= PlayerDashUpdateIlHook;

            IL.Celeste.Player.Render -= PlayerRenderIlHook_Color;

            On.Celeste.PlayerHair.Render -= PlayerHairRenderHook;
            On.Celeste.PlayerSprite.Render -= PlayerSpriteRenderHook;
            On.Celeste.PlayerHair.Update -= PlayerHairUpdateHook;

            On.Monocle.Image.Render -= OnImageRender_ColorGrade;
            On.Celeste.PlayerHair.Render -= PlayerHairRenderHook_ColorGrade;

            On.Celeste.PlayerHair.GetHairColor -= PlayerHairGetHairColorHook;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;
            On.Celeste.Lookout.Update -= LookoutUpdateHook_ColorGrade;
            On.Celeste.Payphone.Update -= PayphoneUpdateHook_ColorGrade;

            IL.Celeste.Player.UpdateHair -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor -= patch_SpriteMode_Badeline;
        }
        #endregion

        //-----------------------------PlayerSprite-----------------------------
        #region
        private static Sprite SpriteBankCreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            // Prevent mode's non-vanilla value causing the game Error
            if (sprite is PlayerSprite && id == "") {
                return null;
            }
            return orig(self, sprite, id);
        }
        private static void on_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode) {
            Level level = Engine.Scene as Level ?? (Engine.Scene as LevelLoader)?.Level;

            DynamicData selfData = DynamicData.For(self);
            bool isGhost = mode < 0;

            if (!isGhost && level != null) {
                backpackOn = backpackSetting == 3 || (backpackSetting == 0 && level.Session.Inventory.Backpack) || (backpackSetting == 1 && !level.Session.Inventory.Backpack);
            }

            string hash_object = null;
            if (isGhost) {
                selfData.Set("isGhost", true);
            } else if (mode == PlayerSpriteMode.Madeline || mode == PlayerSpriteMode.MadelineNoBackpack || mode == PlayerSpriteMode.MadelineAsBadeline) {
                hash_object = GetPlayerSkin();

            } else if (mode == PlayerSpriteMode.Playback) {
                hash_object = GetSilhouetteSkin();

            } else if (mode == (PlayerSpriteMode)444482 || mode == (PlayerSpriteMode)444483) {
                hash_object = GetPlayerSkin("_lantern");
                if (hash_object == GetPlayerSkin())
                    hash_object = null;
            }


            if (hash_object != null) {
                mode = (PlayerSpriteMode)skinConfigs[!backpackOn ? GetPlayerSkin("_NB", hash_object) : hash_object].hashValues;
            } else if (!backpackOn && mode == PlayerSpriteMode.Madeline) {
                mode = PlayerSpriteMode.MadelineNoBackpack;
            } else if (backpackOn && mode == PlayerSpriteMode.MadelineNoBackpack) {
                mode = PlayerSpriteMode.Madeline;
            }
            orig(self, mode);
            int requestMode = (int)(isGhost ? (1 << 31) + mode : mode);

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (requestMode == config.hashValues) {
                    string id = config.Character_ID;
                    selfData.Set("spriteName", id);
                    GFX.SpriteBank.CreateOn(self, id);

                    if (config.JungleLanternMode == true) {
                        // replay the "idle" sprite to make it apply immediately.
                        self.Play("idle", restart: true);

                        // when the look up animation finishes, rewind it to frame 7: this way we are getting 7-11 playing in a loop.
                        self.OnFinish += anim => {
                            if (anim == "lookUp") {
                                self.Play("lookUp", restart: true);
                                self.SetAnimationFrame(5);
                            }
                        };
                    }
                    break;
                }
            }

            if (isGhost && selfData.Get<string>("spriteName") == "") {
                Logger.Log(LogLevel.Info, "SkinModHelper", $"Someone in CelesteNet uses skin mod '{requestMode}' which you don't have");
                #region
                string id = "player";
                if (!level.Session.Inventory.Backpack) {
                    selfData.Set("spriteName", id = "player_no_backpack");
                } else {
                    selfData.Set("spriteName", id = "player");
                }
                GFX.SpriteBank.CreateOn(self, id);
                #endregion 

            } else if (isGhost) {
                Logger.Log(LogLevel.Verbose, "SkinModHelper", $"GhostModeValue: {requestMode}");
            } else {
                Logger.Log(LogLevel.Debug, "SkinModHelper", $"PlayerModeValue: {requestMode}");
            }
        }

        #endregion

        //-----------------------------Player-----------------------------
        #region

        private static void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
            orig(self, applyGravity);

            HairConfig hairConfig = HairConfig.For(self.Hair);

            // We need this code, for sure make PlayerSilhouette can working.
            int? dashCount = GetDashCount(self);
            if (dashCount != null && (self.Hair.Color != Color.White || hairConfig.HairFlash == false) && hairConfig.Safe_GetHairColor(100, (int)dashCount, out Color color)) {
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

            if (hairConfig.Safe_GetHairColor(100, dashCount, out Color color))
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

                    if (hairConfig.Safe_GetHairColor(100, dashCount, out Color color)) {
                        orig = new(orig);
                        orig.Color = color;
                        orig.Color2 = Color.Multiply(orig.Color, 1.4f);
                    }
                    return orig;
                });
            }
        }
        #endregion

        #region
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

                    DynamicData selfData = DynamicData.For(player.Hair);
                    CharacterConfig ModeConfig = CharacterConfig.For(player.Sprite);
                    
                    object backup = null;
                    if (ModeConfig.LowStaminaFlashColor != null && new Regex(@"^[a-fA-F0-9]{6}$").IsMatch(ModeConfig.LowStaminaFlashColor)) {
                        backup = color = Calc.HexToColor(ModeConfig.LowStaminaFlashColor);
                        if (ModeConfig.SilhouetteMode == true) {
                            color = ColorBlend(player.Hair.Color, color);
                        }
                    } else if (ModeConfig.SilhouetteMode == true) {
                        color = ColorBlend(player.Hair.Color, (backup = 0.5f));
                    }

                    if (ModeConfig.LowStaminaFlashHair == true || (ModeConfig.SilhouetteMode == true)) {
                        selfData.Set("HairColorGrading", backup ?? color);
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
        #endregion

        //-----------------------------ColorGrade-----------------------------
        #region
        private static void PlayerHairRenderHook_ColorGrade(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            DynamicData selfData = DynamicData.For(self.Sprite);

            // Save colorgrade in typeof(Image).
            // For make typeof(PlayerDeadBody) inherited typeof(Player)'s colorgrade, or similar situations.
            Atlas atlas = selfData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;
            string colorGrade_Path = selfData.Get<string>("ColorGrade_Path");

            if (colorGrade_Path == null) {
                colorGrade_Path = $"{getAnimationRootPath(self.Sprite)}ColorGrading/dash";

                //Check if config from v0.7 Before---
                if (self.Entity is Player && OldConfigCheck(self.Sprite, out string isOld)) {
                    atlas = GFX.ColorGrades;
                    colorGrade_Path = $"{OtherskinConfigs[isOld].OtherSprite_ExPath}/dash";
                }
                //---
                selfData.Set("ColorGrade_Path", colorGrade_Path);
            }

            int? get_dashCount;
            if (self.Entity is Player player) {
                int lastDashes = DynamicData.For(player).Get<int>("lastDashes");
                if (player.MaxDashes <= 0 && lastDashes < 2) {
                    get_dashCount = 1;
                } else {
                    get_dashCount = Math.Max(lastDashes, 0);
                }
            } else {
                get_dashCount = GetDashCount(self);
            }

            if (self.Color == Color.White && atlas.Has(getAnimationRootPath(colorGrade_Path, out string value) + "flash")) {
                selfData.Set("ColorGrade_Atlas", atlas);
                selfData.Set("ColorGrade_Path", colorGrade_Path = value + "flash");

            } else if (get_dashCount != null) {
                colorGrade_Path = getAnimationRootPath(colorGrade_Path) + "dash";
                
                int dashCount = Math.Max((int)get_dashCount, 0);
                while (dashCount > 2 && !atlas.Has(colorGrade_Path + dashCount)) {
                    dashCount--;
                }
                selfData.Set("ColorGrade_Atlas", atlas);
                selfData.Set("ColorGrade_Path", colorGrade_Path = colorGrade_Path + dashCount);
            }

            if (colorGrade_Path != null && atlas.Has(colorGrade_Path)) {
                Effect colorGradeEffect = FxColorGrading_SMH;
                colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                Engine.Graphics.GraphicsDevice.Textures[1] = atlas[colorGrade_Path].Texture.Texture_Safe;

                DynamicData spriteData = DynamicData.For(Draw.SpriteBatch);
                Matrix matrix = spriteData.Get<Matrix>("transformMatrix");

                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                orig(self);
                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
                return;
            }
            orig(self);
        }
        private static void OnImageRender_ColorGrade(On.Monocle.Image.orig_Render orig, Image self) {
            if (self is Sprite sprite) {
                // filter non-Sprite type, and don't use DynData for minimizing lag...
                DynamicData selfData = DynamicData.For(sprite);

                string colorGrade_Path = selfData.Get<string>("ColorGrade_Path");

                if (colorGrade_Path != null) {
                    Atlas atlas = selfData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;

                    if (atlas.Has(colorGrade_Path)) {
                        Effect colorGradeEffect = FxColorGrading_SMH;
                        colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                        Engine.Graphics.GraphicsDevice.Textures[1] = atlas[colorGrade_Path].Texture.Texture_Safe;

                        DynamicData spriteData = DynamicData.For(Draw.SpriteBatch);
                        Matrix matrix = spriteData.Get<Matrix>("transformMatrix");

                        GameplayRenderer.End();
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                        orig(self);
                        GameplayRenderer.End();
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
                        return;
                    }
                }
            }
            orig(self);
        }
        #endregion
        #region
        private static void LookoutUpdateHook_ColorGrade(On.Celeste.Lookout.orig_Update orig, Lookout self) {
            Player player = Engine.Scene?.Tracker.GetEntity<Player>();
            SkinModHelperInterop.CopyColorGrades(player?.Sprite, DynamicData.For(self).Get<Sprite>("sprite"));
            orig(self);
        }
        private static void PayphoneUpdateHook_ColorGrade(On.Celeste.Payphone.orig_Update orig, Payphone self) {
            // player's dashes is usually fixed at 1 for payphone cutscenes... so probably this no real works.
            Player player = Engine.Scene?.Tracker.GetEntity<Player>();
            SkinModHelperInterop.CopyColorGrades(player?.Sprite, self.Sprite);
            orig(self);
        }
        #endregion

        //-----------------------------PlayerSprite-----------------------------
        #region
        private static void PlayerSpriteRenderHook(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            DynamicData selfData = DynamicData.For(self);

            // rendering run multiple times in one frame, but we don't need to do much. so...
            if (selfData.Get("SMH_OncePerFrame") == null) {
                if (self.Entity is not Player && selfData.Get("isGhost") == null && self.Entity is not PlayerDeadBody) {

                    PlayerHair hair = self.Entity?.Get<PlayerHair>();
                    if (hair?.Sprite == self) {
                        CharacterConfig ModeConfig = CharacterConfig.For(self);

                        if (ModeConfig.SilhouetteMode == true) {
                            self.Color = hair.Color * hair.Alpha;

                        } else if (self.Color == hair.Color * hair.Alpha) {
                            self.Color = Color.White * hair.Alpha;
                        }
                    }
                }
                selfData.Set("SMH_OncePerFrame", true);
            }
            orig(self);
        }
        #endregion

        //-----------------------------PlayerHair-----------------------------
        #region
        private static void PlayerHairUpdateHook(On.Celeste.PlayerHair.orig_Update orig, PlayerHair self) {
            DynamicData selfData = DynamicData.For(self);
            selfData.Set("SMH_OncePerFrame", null);
            DynamicData.For(self.Sprite).Set("SMH_OncePerFrame", null);

            selfData.Set("HairColorGrading", null);
            if (selfData.TryGet("smh_HairLength", out int? length) && length != null) {
                self.Sprite.HairCount = (int)length;
            }

            if (self.Entity is Player player && player.StateMachine.State == 14) {
                HairConfig hairConfig = HairConfig.For(self);

                if (hairConfig.Safe_GetHairColor(100, (int)GetDashCount(player), out Color color))
                    self.Color = color;
            }
            orig(self);
        }

        private static void PlayerHairRenderHook(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            DynamicData selfData = DynamicData.For(self);

            // We want hair get config in rendering before. but rendering run multiple times in one frame, so...
            if (selfData.Get("SMH_OncePerFrame") == null) {
                #region
                HairConfig hairConfig = HairConfig.For(self);
                CharacterConfig ModeConfig = CharacterConfig.For(self.Sprite);

                if (hairConfig.OutlineColor != null && new Regex(@"^[a-fA-F0-9]{6}$").IsMatch(hairConfig.OutlineColor)) {
                    self.Border = Calc.HexToColor(hairConfig.OutlineColor);
                } else {
                    self.Border = Color.Black;
                }
                self.Border = ColorBlend(self.Border, selfData.Get("HairColorGrading"));

                int? get_dashCount = GetDashCount(self);

                if (ModeConfig.SilhouetteMode == true)
                    self.Border = ColorBlend(self.Border, self.Color);
                orig(self);


                int? HairLength = hairConfig.GetHairLength(get_dashCount);
                if (self.Entity is Player player) {
                    if (player.StateMachine.State == Player.StStarFly) {
                        HairLength = hairConfig.GetHairLength(-1);
                    } else if (player.StateMachine.State == Player.StRedDash) {
                        HairLength = null;
                    }
                }
                selfData.Set("smh_HairLength", HairLength);

                #endregion
                selfData.Set("SMH_OncePerFrame", true);
                return;
            }
            orig(self);
        }

        private static MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index) {

            HairConfig config = HairConfig.For(self);
            string spritePath = config.SourcePath;
            string detectPath = getAnimationRootPath(self.Sprite.Texture);

            if (detectPath.StartsWith("characters/player_no_backpack/") || detectPath.StartsWith("characters/player/")
                || detectPath.StartsWith("characters/player_badeline/") || detectPath.StartsWith("characters/player_playback/") || detectPath.StartsWith("characters/badeline/")) {
                if (GFX.Game.HasAtlasSubtexturesAt(spritePath + "bangs", 0) && !detectPath.StartsWith(spritePath)
                    && DynamicData.For(self).Get("SMH_DisposableLog_aPhggdddd") == null && DynamicData.For(self.Sprite).Get("isGhost") == null) {

                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Avoid the possible invisible hair texture work to vanilla characters...");
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
            DynamicData selfData = DynamicData.For(self);
            HairConfig hairConfig = HairConfig.For(self);

            int? get_dashCount = GetDashCount(self);

            if (hairConfig.ActualHairColors != null && get_dashCount != null && (self.Color != Color.White || hairConfig.HairFlash == false)) {
                int Index = hairConfig.ActualHairColors.ContainsKey(index - self.Sprite.HairCount) ? index - self.Sprite.HairCount : index;

                if (hairConfig.Safe_GetHairColor(Index, (int)get_dashCount, out Color color)) {

                    if (index == 0)
                        self.Color = color;
                    return ColorBlend(color * self.Alpha, selfData.Get("HairColorGrading"));
                }
            }
            return ColorBlend(orig(self, index), selfData.Get("HairColorGrading"));
        }
        #endregion

        //-----------------------------PlayerSpriteMode-----------------------------
        #region
        private static void SetPlayerSpriteMode(PlayerSpriteMode? mode) {
            if (Engine.Scene is Level level) {
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null) {

                    if (mode == null) {
                        mode = player.DefaultSpriteMode;
                    }
                    if (player.Active) {
                        player.ResetSpriteNextFrame((PlayerSpriteMode)mode);
                    } else {
                        player.ResetSprite((PlayerSpriteMode)mode);
                    }
                }
            }
        }

        private static void patch_SpriteMode_Badeline(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<PlayerSpriteMode, Player, PlayerSpriteMode>>((orig, self) => {

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
        private static void patch_SpriteMode_BackPack(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.EmitDelegate<Func<PlayerSpriteMode, PlayerSpriteMode>>((orig) => {
                    return orig = (PlayerSpriteMode)(actualBackpack((int)orig) ? 0 : 1);
                });
            }
        }
        // ---JungleHelper---
        public static bool HasLantern(PlayerSpriteMode mode) {
            if (mode == (PlayerSpriteMode)444482 || mode == (PlayerSpriteMode)444483) {
                return true;
            }
            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (config.JungleLanternMode == true) {
                    if (mode == (PlayerSpriteMode)config.hashValues) {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        //-----------------------------Method-----------------------------
        #region
        public static void RefreshPlayerSpriteMode(string SkinName = null, int dashCount = 1) {
            if (Engine.Scene is not Level) {
                return;
            }
            Player player = Engine.Scene.Tracker?.GetEntity<Player>();
            if (player == null) {
                return;
            }

            if (SkinName != null && skinConfigs.ContainsKey(SkinName)) {
                SetPlayerSpriteMode((PlayerSpriteMode)skinConfigs[SkinName].hashValues);

            } else if (SaveData.Instance != null && SaveData.Instance.Assists.PlayAsBadeline) {
                SetPlayerSpriteMode(PlayerSpriteMode.MadelineAsBadeline);
            } else {
                SetPlayerSpriteMode(null);
            }
        }
        public static int? GetDashCount(object type) {
            int? get_dashCount = null;
            if (type is Component component) {
                if (component is PlayerHair playerHair) {
                    component = playerHair.Sprite;
                }
                if (component is PlayerSprite sprite && sprite.Mode == (PlayerSpriteMode)2) {
                    get_dashCount = 0;
                }
                type = component.Entity;
            }

            if (type is BadelineOldsite badelineOldsite) {
                get_dashCount = DynamicData.For(badelineOldsite).Get<int>("index");
            } else if (type is Player player) {
                if (player.StateMachine.State != Player.StStarFly) {
                    int lastDashes = DynamicData.For(player).Get<int>("lastDashes");
                    if (lastDashes == 0 && player.MaxDashes <= 0) {
                        get_dashCount = 1;
                    } else {
                        get_dashCount = Math.Max(lastDashes, 0);
                    }
                }
            } else if (type is PlayerPlayback) {
                get_dashCount = null;
            }
            return get_dashCount;
        }
        public static bool OldConfigCheck(PlayerSprite sprite, out string key) {
            string spriteName = DynamicData.For(sprite).Get<string>("spriteName");
            foreach (string _key in OtherskinOldConfig.Keys) {
                if (spriteName.EndsWith($"{_key}")) {
                    key = _key;
                    return true;
                }
            }
            key = null;
            return false;
        }
        public static bool actualBackpack(int mode) {
            string skinName = GetPlayerSkinName(mode);
            return !(skinName?.EndsWith("_NB") ?? mode == 1 || mode == 4);
        }
        public static int GetStartedDashingCount(Player player) {
            int? dashCount = DynamicData.For(player).Get<int?>("TrailDashCount");
            if (dashCount == null)
                return SetStartedDashingCount(player);
            return (int)dashCount;
        }
        public static int SetStartedDashingCount(Player player, int? count = null) {
            int dashCount = Math.Max(count ?? player.Dashes - 1, 0);
            DynamicData.For(player).Set("TrailDashCount", dashCount);
            return dashCount;
        }
        #endregion
    }
}