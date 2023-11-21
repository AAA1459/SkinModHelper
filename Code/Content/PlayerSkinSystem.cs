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
            IL.Celeste.Player.Render += PlayerRenderIlHook_Sprite;

            On.Celeste.PlayerHair.Render += PlayerHairRenderHook;
            On.Celeste.PlayerHair.Update += PlayerHairUpdateHook;

            On.Celeste.PlayerSprite.Render += OnPlayerSpriteRender_ColorGrade;
            On.Celeste.PlayerHair.Render += PlayerHairRenderHook_ColorGrade;

            On.Celeste.PlayerHair.GetHairColor += PlayerHairGetHairColorHook;
            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;

            IL.Celeste.Player.UpdateHair += patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate += patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor += patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame += patch_SpriteMode_Silhouette;

            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("TempleFallCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), TempleFallCoroutineILHook));

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
            IL.Celeste.Player.Render -= PlayerRenderIlHook_Sprite;

            On.Celeste.PlayerHair.Render -= PlayerHairRenderHook;
            On.Celeste.PlayerHair.Update -= PlayerHairUpdateHook;

            On.Celeste.PlayerSprite.Render -= OnPlayerSpriteRender_ColorGrade;
            On.Celeste.PlayerHair.Render -= PlayerHairRenderHook_ColorGrade;

            On.Celeste.PlayerHair.GetHairColor -= PlayerHairGetHairColorHook;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;

            IL.Celeste.Player.UpdateHair -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor -= patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame -= patch_SpriteMode_Silhouette;
        }

        //-----------------------------PlayerSprite-----------------------------
        private static Sprite SpriteBankCreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            // Prevent mode's non-vanilla value causing the game Error
            if (sprite is PlayerSprite && id == "") {
                return null;
            }
            return orig(self, sprite, id);
        }
        private static void on_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode) {
            Level level = Engine.Scene as Level ?? (Engine.Scene as LevelLoader)?.Level;

            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self);
            bool isGhost = mode < 0;

            if (!isGhost && level != null) {
                backpackOn = Settings.Backpack == SkinModHelperSettings.BackpackMode.On ||
                    (Settings.Backpack == SkinModHelperSettings.BackpackMode.Default && level.Session.Inventory.Backpack) ||
                    (Settings.Backpack == SkinModHelperSettings.BackpackMode.Invert && !level.Session.Inventory.Backpack);
            }

            string hash_object = null;
            if (!isGhost && (mode == PlayerSpriteMode.Madeline || mode == PlayerSpriteMode.MadelineNoBackpack || mode == PlayerSpriteMode.MadelineAsBadeline)) {
                hash_object = GetPlayerSkin();

            } else if (!isGhost && mode == PlayerSpriteMode.Playback) {
                hash_object = GetSilhouetteSkin();

            } else if (!isGhost && (mode == (PlayerSpriteMode)444482 || mode == (PlayerSpriteMode)444483)) {
                hash_object = GetPlayerSkin("_lantern");
                if (hash_object == GetPlayerSkin()) { hash_object = null; }

            } else if (isGhost) {
                selfData["isGhost"] = true;
            }


            if (hash_object != null) {
                hash_object = !backpackOn ? GetPlayerSkin("_NB", hash_object) : hash_object;

                mode = (PlayerSpriteMode)skinConfigs[hash_object].hashValues;
            } else if (!backpackOn && mode == PlayerSpriteMode.Madeline) {
                mode = PlayerSpriteMode.MadelineNoBackpack;
            } else if (backpackOn && mode == PlayerSpriteMode.MadelineNoBackpack) {
                mode = PlayerSpriteMode.Madeline;
            }
            orig(self, mode);
            Logger.Log(LogLevel.Info, "SkinModHelper", $"PlayerModeValue: {mode}");

            int requestMode = (int)(isGhost ? (1 << 31) + mode : mode);

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (requestMode == config.hashValues) {
                    string id = config.Character_ID;
                    selfData["spriteName"] = id;
                    GFX.SpriteBank.CreateOn(self, id);
                }
            }
            if (isGhost && selfData.Get<string>("spriteName") == "") {
                Logger.Log(LogLevel.Info, "SkinModHelper", $"someone else in CelesteNet uses a Skin-Mod that you don't have");
                string id = "player";
                if (!level.Session.Inventory.Backpack) {
                    id = "player_no_backpack";
                    selfData["spriteName"] = id;
                } else {
                    id = "player";
                    selfData["spriteName"] = id;
                }
                GFX.SpriteBank.CreateOn(self, id);
                return;
            }

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (config.JungleLanternMode == true && (requestMode == config.hashValues)) {

                    // replay the "idle" sprite to make it apply immediately.
                    self.Play("idle", restart: true);

                    // when the look up animation finishes, rewind it to frame 7: this way we are getting 7-11 playing in a loop.
                    self.OnFinish = anim => {
                        if (anim == "lookUp") {
                            self.Play("lookUp", restart: true);
                            self.SetAnimationFrame(5);
                        }
                    };
                }
            }
        }





        //-----------------------------Player-----------------------------

        private static void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
            orig(self, applyGravity);
            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self.Sprite);

            // We need this code, for sure make PlayerSilhouette can working.
            bool? HairFlashing = (bool?)new DynData<PlayerSprite>(self.Sprite)["HairFlashing"];
            Dictionary<int, List<Color>> HairColors = (Dictionary<int, List<Color>>)selfData["HairColors"];

            int? dashCount = GetDashCount(self);
            if (dashCount != null && HairColors != null && (self.Hair.Color != Color.White || HairFlashing == false)) {
                self.Hair.Color = HairColors[100][(int)dashCount];
            }
        }
        private static int PlayerStartDashHook(On.Celeste.Player.orig_StartDash orig, Player self) {
            new DynData<PlayerSprite>(self.Sprite)["TrailDashCount"] = Math.Max(Math.Min(self.Dashes - 1, MAX_DASHES), 0);
            return orig(self);
        }
        private static Color PlayerGetTrailColorHook(On.Celeste.Player.orig_GetTrailColor orig, Player self, bool wasDashB) {
            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self.Sprite);
            int dashCount = selfData["TrailDashCount"] != null ? (int)selfData["TrailDashCount"] : Math.Max(Math.Min(self.Dashes, MAX_DASHES), 0);

            Dictionary<int, List<Color>> HairColors = (Dictionary<int, List<Color>>)selfData["HairColors"];
            if (HairColors != null) {
                return HairColors[100][dashCount];
            }
            return orig(self, wasDashB);
        }
        public static void PlayerDashUpdateIlHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld<Player>("P_DashA") || instr.MatchLdsfld<Player>("P_DashB") || instr.MatchLdsfld<Player>("P_DashBadB"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<ParticleType, Player, ParticleType>>((orig, self) => {
                    DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self.Sprite);

                    int dashCount = selfData["TrailDashCount"] != null ? (int)selfData["TrailDashCount"] : Math.Max(Math.Min(self.Dashes, MAX_DASHES), 0);
                    Dictionary<int, List<Color>> HairColors = (Dictionary<int, List<Color>>)selfData["HairColors"];
                    if (HairColors != null) {
                        orig = new(orig);

                        orig.Color = HairColors[100][dashCount];
                        orig.Color2 = Color.Multiply(orig.Color, 1.4f);
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
                    string ConfigPath = $"Graphics/Atlases/Gameplay/{getAnimationRootPath(player.Sprite)}skinConfig/" + "CharacterConfig";

                    CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>(ConfigPath);
                    if (ModeConfig != null) {
                        if (ModeConfig.SilhouetteMode == true) {
                            player.Hair.Color = Color.Lerp(player.Hair.Color, Color.White, 1 / 4f);
                            color = player.Hair.Color;
                        } else if (ModeConfig.SilhouetteMode == false) {
                            color = Color.Red;
                        }
                    }
                    return color;
                });
            }

            // jump to the usage of the White-color / Null-color
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
                Logger.Log("SkinModHelper", $"Patching silhouette color at {cursor.Index} in IL code for Player.Render()");
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Color, Player, Color>>((orig, self) => {
                    string ConfigPath = $"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "CharacterConfig";

                    CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>(ConfigPath);
                    if (ModeConfig != null) {
                        if (ModeConfig.SilhouetteMode == true) {
                            return self.Hair.Color;
                        } else if (ModeConfig.SilhouetteMode == false) {
                            return Color.White;
                        }
                    }
                    return orig;
                });
            }
        }
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

                    if (GFX.Game.HasAtlasSubtexturesAt(spritePath, 0)) {
                        return spritePath;
                    }
                    return orig;
                });
            }
        }

        //-----------------------------ColorGrade-----------------------------
        private static void OnPlayerSpriteRender_ColorGrade(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self);

            int? get_dashCount = null;
            if (self.Entity is Player player) {
                if (player.MaxDashes <= 0 && player.Dashes < 2) {
                    get_dashCount = 1;
                } else {
                    get_dashCount = Math.Max(Math.Min(player.Dashes, MAX_DASHES), 0);
                }
            } else {
                get_dashCount = GetDashCount(self);
            }

            // Save colorgrade in typeof(PlayerSprite).
            // For make typeof(PlayerDeadBody) inherited typeof(Player)'s colorgrade, or similar situations.
            string colorGrade_Path = (string)selfData["ColorGrade_Path"];

            Atlas atlas = GFX.Game;
            if (get_dashCount != null) {
                colorGrade_Path = $"{getAnimationRootPath(self)}ColorGrading/dash";

                //Check if config from v0.7 Before---
                string spriteName = (string)new DynData<PlayerSprite>(self)["spriteName"];
                foreach (string key in OtherskinOldConfig.Keys) {
                    if (spriteName.EndsWith($"{key}")) {
                        atlas = GFX.ColorGrades;
                        colorGrade_Path = $"{OtherskinConfigs[key].OtherSprite_ExPath}/dash";
                        break;
                    }
                }
                //---

                int dashCount = Math.Max(Math.Min((int)get_dashCount, MAX_DASHES), 0);

                while (dashCount > 2 && !GFX.Game.Has($"{colorGrade_Path}{dashCount}")) {
                    dashCount--;
                }
                colorGrade_Path = $"{colorGrade_Path}{dashCount}";
                selfData["ColorGrade_Path"] = colorGrade_Path;
            }

            if (colorGrade_Path != null && atlas.Has(colorGrade_Path)) {
                Effect colorGradeEffect = GFX.FxColorGrading;
                colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                Engine.Graphics.GraphicsDevice.Textures[1] = atlas[colorGrade_Path].Texture.Texture_Safe;

                DynData<SpriteBatch> spriteData = new DynData<SpriteBatch>(Draw.SpriteBatch);
                Matrix matrix = (Matrix)spriteData["transformMatrix"];

                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                orig(self);
                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
                return;
            }
            orig(self);
        }
        private static void PlayerHairRenderHook_ColorGrade(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            Atlas atlas = GFX.Game;
            string colorGrade_Path = (string)new DynData<PlayerSprite>(self.Sprite)["ColorGrade_Path"];

            //Check if config from v0.7 Before---
            string spriteName = (string)new DynData<PlayerSprite>(self.Sprite)["spriteName"];
            foreach (string key in OtherskinOldConfig.Keys) {
                if (spriteName.EndsWith($"{key}")) {
                    atlas = GFX.ColorGrades;
                    break;
                }
            }

            if (colorGrade_Path != null && atlas.Has(colorGrade_Path)) {
                Effect colorGradeEffect = GFX.FxColorGrading;
                colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                Engine.Graphics.GraphicsDevice.Textures[1] = atlas[colorGrade_Path].Texture.Texture_Safe;

                DynData<SpriteBatch> spriteData = new DynData<SpriteBatch>(Draw.SpriteBatch);
                Matrix matrix = (Matrix)spriteData["transformMatrix"];

                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                orig(self);
                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
                return;
            }
            orig(self);
        }

        //-----------------------------PlayerHair-----------------------------
        private static void PlayerHairUpdateHook(On.Celeste.PlayerHair.orig_Update orig, PlayerHair self) {
            new DynData<PlayerHair>(self)["SMH_OncePerFrame"] = null;

            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self.Sprite);
            if (selfData["HairLength"] != null && self.Sprite.HairCount != (int)selfData["HairLength"]) { self.Sprite.HairCount = (int)selfData["HairLength"]; }

            orig(self);
        }

        private static void PlayerHairRenderHook(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {

            // We want hair get config in rendering before. but rendering run multiple times in one frame, so...
            if (new DynData<PlayerHair>(self)["SMH_OncePerFrame"] == null) {
                DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self.Sprite);

                //Check if config from v0.7 Before---
                string spriteName = (string)selfData["spriteName"];
                foreach (string key in OtherskinOldConfig.Keys) {
                    if (spriteName.EndsWith($"{key}")) {
                        selfData["HairColors"] = SkinModHelperOldConfig.BuildHairColors(OtherskinOldConfig[key]);
                        selfData["HairFlashing"] = false;

                        orig(self);
                        new DynData<PlayerHair>(self)["SMH_OncePerFrame"] = true;
                        return;
                    }
                }
                //---

                string rootPath = getAnimationRootPath(self.Sprite);
                HairConfig hairConfig = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "HairConfig");
                CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "CharacterConfig");

                if (self.Sprite.Mode == (PlayerSpriteMode)2 && ModeConfig != null && ModeConfig.BadelineMode == null) {
                    ModeConfig.BadelineMode = true;
                }

                if (hairConfig != null && hairConfig.OutlineColor != null && new Regex(@"^[a-fA-F0-9]{6}$").IsMatch(hairConfig.OutlineColor)) {
                    self.Border = Calc.HexToColor(hairConfig.OutlineColor);
                } else {
                    self.Border = Color.Black;
                }

                if (self.Entity is not PlayerPlayback) {
                    int number_search = 0;
                    while (number_search < MAX_DASHES && !GFX.Game.Has($"{rootPath}ColorGrading/dash{number_search}")) {
                        number_search++;
                    }
                    bool Build_switch = GFX.Game.Has($"{rootPath}ColorGrading/dash{number_search}");

                    if (hairConfig != null && hairConfig.HairFlash != null) {
                        selfData["HairFlashing"] = hairConfig.HairFlash == true;
                        Build_switch = hairConfig.HairFlash == true ? Build_switch : true;
                    }

                    if ((hairConfig != null && hairConfig.HairColors != null) || Build_switch) {
                        selfData["HairColors"] = HairConfig.BuildHairColors(hairConfig, ModeConfig);
                    } else {
                        selfData["HairColors"] = null;
                    }
                }
                int? get_dashCount = GetDashCount(self);

                if (self.Entity is not Player) {
                    // We need this code, for sure make badeline as silhouette can working.
                    Dictionary<int, List<Color>> HairColors = (Dictionary<int, List<Color>>)selfData["HairColors"];
                    if (HairColors != null && ModeConfig != null && get_dashCount != null) {
                        if (ModeConfig.SilhouetteMode == true) {
                            self.Sprite.Color = HairColors[100][Math.Max(Math.Min((int)get_dashCount, MAX_DASHES), 0)];
                        } else if (ModeConfig.SilhouetteMode == false) {
                            self.Sprite.Color = Color.White;
                        }
                    }
                }
                orig(self);

                int? HairLength = HairConfig.GetHairLength(hairConfig, get_dashCount);
                if (self.Entity is Player player) {
                    if (player.StateMachine.State == Player.StStarFly) {
                        HairLength = HairConfig.GetHairLength(hairConfig, -1);
                    } else if (player.StateMachine.State == Player.StRedDash) {
                        HairLength = null;
                    }
                }
                if (HairLength != null) {
                    selfData["HairLength"] = HairLength;
                }

                new DynData<PlayerHair>(self)["SMH_OncePerFrame"] = true;
                return;
            }
            orig(self);
        }

        private static MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index) {

            string spritePath = getAnimationRootPath(self.Sprite);

            //Check if config from v0.7 Before---
            string spriteName = (string)new DynData<PlayerSprite>(self.Sprite)["spriteName"];
            foreach (string key in OtherskinOldConfig.Keys) {
                if (spriteName.EndsWith($"{key}")) {
                    spritePath = $"{OtherskinConfigs[key].OtherSprite_ExPath}/characters/player/";
                    break;
                }
            }
            //---

            if (index == 0) {
                spritePath = spritePath + "bangs";
            } else {
                spritePath = spritePath + "hair";
            }

            if (GFX.Game.HasAtlasSubtexturesAt(spritePath, 0)) {
                List<MTexture> newhair = GFX.Game.GetAtlasSubtextures(spritePath);
                spriteName = $"{(newhair.Count > self.Sprite.HairFrame ? newhair[self.Sprite.HairFrame] : newhair[0])}";

                if (index != 0) {
                    if (GFX.Game.Has($"{spriteName}_{index - self.Sprite.HairCount}")) {
                        //Set the texture for hair of each section from back to front.
                        spriteName = $"{spriteName}_{index - self.Sprite.HairCount}";

                    } else if (GFX.Game.Has($"{spriteName}_{index}")) {
                        //Set the texture for hair of each section.
                        spriteName = $"{spriteName}_{index}";
                    }
                }
                return GFX.Game[spriteName];
            }
            return orig(self, index);
        }
        private static Color PlayerHairGetHairColorHook(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self.Sprite);

            int? get_dashCount = GetDashCount(self);
            bool? HairFlashing = (bool?)selfData["HairFlashing"];
            Dictionary<int, List<Color>> HairColors = (Dictionary<int, List<Color>>)selfData["HairColors"];

            if (get_dashCount != null && ((HairColors != null && self.Color != Color.White) || HairFlashing == false)) {
                int dashCount = Math.Max(Math.Min((int)get_dashCount, MAX_DASHES), 0);
                int Index = HairColors.ContainsKey(index - self.Sprite.HairCount) ? (index - self.Sprite.HairCount) : HairColors.ContainsKey(index) ? index : 100;

                return HairColors[Index][dashCount];
            }
            return orig(self, index);
        }

        //-----------------------------PlayerSpriteMode-----------------------------

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
                    string ConfigPath = $"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "CharacterConfig";

                    CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>(ConfigPath);
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
        private static void patch_SpriteMode_Silhouette(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<PlayerSpriteMode, PlayerPlayback, PlayerSpriteMode>>((orig, self) => {
                    string ConfigPath = $"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "CharacterConfig";

                    CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>(ConfigPath);
                    if (ModeConfig != null) {
                        if (ModeConfig.SilhouetteMode == true) {
                            return (PlayerSpriteMode)4;
                        } else if (ModeConfig.SilhouetteMode == false) {
                            return 0;
                        }
                    }
                    return orig;
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

        //-----------------------------Method-----------------------------
        public static void RefreshPlayerSpriteMode(string SkinName = null, int dashCount = 1) {
            if (Engine.Scene is not Level) {
                return;
            }
            Player player = Engine.Scene.Tracker?.GetEntity<Player>();
            if (player == null) {
                return;
            }

            Player_Skinid_verify = 0;
            if (SkinName != null && skinConfigs.ContainsKey(SkinName)) {

                Player_Skinid_verify = skinConfigs[SkinName].hashValues;
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
                get_dashCount = (int)new DynData<BadelineOldsite>(badelineOldsite)["index"];
            } else if (type is Player player) {
                if (player.StateMachine.State != Player.StStarFly) {
                    if (player.MaxDashes <= 0 && player.Dashes < 2) {
                        get_dashCount = 1;
                    } else {
                        get_dashCount = Math.Max(Math.Min(player.Dashes, MAX_DASHES), 0);
                    }
                }
            } else if (type is PlayerPlayback) {
                get_dashCount = null;
            } else if (get_dashCount == null) {
                var Dashes = type.GetType().GetField("Dashes");
                get_dashCount = Dashes != null ? (int?)Dashes.GetValue(type) : null;
            }
            return get_dashCount;
        }
    }
}