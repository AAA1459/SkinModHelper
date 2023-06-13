using FMOD.Studio;
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

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperModule : EverestModule {
        public static SkinModHelperModule Instance;
        public static readonly string DEFAULT = "Default";
        public static readonly string ORIGINAL = "Original";
        public static readonly int MAX_DASHES = 5;

        private static readonly List<string> spritesWithHair = new() {
            "player", "player_no_backpack", "badeline", "player_badeline", "player_playback"
        };

        public override Type SettingsType => typeof(SkinModHelperSettings);
        public override Type SessionType => typeof(SkinModHelperSession);


        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;




        public static SkinModHelperUI UI;

        public static Dictionary<string, SkinModHelperConfig> skinConfigs;
        public static Dictionary<string, SkinModHelperConfig> OtherskinConfigs;

        public static string Xmls_record;

        public static Dictionary<string, string> SpriteSkin_record;
        public static Dictionary<string, List<string>> SpriteSkins_records;

        public static Dictionary<string, string> PortraitsSkin_record;
        public static Dictionary<string, List<string>> PortraitsSkins_records;

        public static Dictionary<string, string> OtherSkin_record;
        public static Dictionary<string, List<string>> OtherSkins_records;

        private static List<ILHook> doneILHooks = new List<ILHook>();
        private static List<Hook> doneHooks = new List<Hook>();

        public SkinModHelperModule() {
            Instance = this;
            UI = new SkinModHelperUI();

            skinConfigs = new Dictionary<string, SkinModHelperConfig>();
            OtherskinConfigs = new Dictionary<string, SkinModHelperConfig>();

            SpriteSkin_record = new Dictionary<string, string>();
            SpriteSkins_records = new Dictionary<string, List<string>>();

            PortraitsSkin_record = new Dictionary<string, string>();
            PortraitsSkins_records = new Dictionary<string, List<string>>();

            OtherSkin_record = new Dictionary<string, string>();
            OtherSkins_records = new Dictionary<string, List<string>>();

            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "JungleHelper", Version = new Version(1, 0, 8) })) {
                JungleHelperInstalled = true;
            }
            if (Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "SaveFilePortraits", Version = new Version(1, 0, 0) })) {
                SaveFilePortraits = true;
            }
        }
        bool JungleHelperInstalled = false;
        bool SaveFilePortraits = false;

        public override void Load() {
            SkinModHelperInterop.Load();

            Everest.Content.OnUpdate += EverestContentUpdateHook;
            On.Celeste.LevelLoader.ctor += on_LevelLoader_ctor;

            On.Celeste.Player.UpdateHair += PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor += PlayerGetTrailColorHook;
            On.Celeste.Player.StartDash += PlayerStartDashHook;

            IL.Celeste.Player.UpdateHair += patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate += patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor += patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame += patch_SpriteMode_Silhouette;

            On.Celeste.Player.Update += PlayerUpdateHook;
            On.Celeste.PlayerSprite.ctor += on_PlayerSprite_ctor;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOn;

            On.Celeste.BadelineOldsite.ctor_Vector2_int += on_BadelineOldsite_ctor;
            On.Celeste.BadelineDummy.ctor += on_BadelineDummy_ctor;
            On.Celeste.DreamMirror.Added += DreamMirrorAddedHook;

            On.Celeste.Lookout.Interact += on_Lookout_Interact;
            IL.Celeste.Player.Render += PlayerRenderIlHook_Color;
            On.Celeste.PlayerSprite.Render += OnPlayerSpriteRender;
            
            IL.Celeste.Player.Render += PlayerRenderIlHook_Sprite;
            On.Celeste.PlayerHair.GetHairTexture += PlayerHairGetHairTextureHook;
            On.Monocle.Sprite.Play += PlayerSpritePlayHook;

            On.Monocle.SpriteBank.Create += SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOnHook;
            On.Monocle.Atlas.GetAtlasSubtextures += GetAtlasSubtexturesHook;

            On.Celeste.LevelLoader.LoadingThread += LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread += GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup += OuiFileSelectSlotSetupHook;

            On.Celeste.DeathEffect.Render += DeathEffectRenderHook;
            IL.Celeste.DeathEffect.Draw += DeathEffectDrawHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool += DreamBlockHook;

            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool += FlyFeatherHook;
            IL.Celeste.CS06_Campfire.Question.ctor += CampfireQuestionHook;
            IL.Celeste.MiniTextbox.ctor += SwapTextboxHook;

            doneILHooks.Add(new ILHook(typeof(Textbox).GetMethod("RunRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), SwapTextboxHook));
            doneILHooks.Add(new ILHook(typeof(Player).GetMethod("TempleFallCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), TempleFallCoroutineILHook));

            if (JungleHelperInstalled) {
                Assembly assembly = Everest.Modules.Where(m => m.Metadata?.Name == "JungleHelper").First().GetType().Assembly;
                Type EnforceSkinController = assembly.GetType("Celeste.Mod.JungleHelper.Entities.EnforceSkinController");

                doneHooks.Add(new Hook(EnforceSkinController.GetMethod("ChangePlayerSpriteMode", BindingFlags.Public | BindingFlags.Static),
                                       typeof(SkinModHelperModule).GetMethod("ChangePlayerSpriteMode", BindingFlags.Public | BindingFlags.Static)));

                doneHooks.Add(new Hook(EnforceSkinController.GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static),
                                       typeof(SkinModHelperModule).GetMethod("HasLantern", BindingFlags.Public | BindingFlags.Static)));
            }
        }


        public override void LoadContent(bool firstLoad) {
            base.LoadContent(firstLoad);
            ReloadSettings();
        }

        public override void Unload() {
            Everest.Content.OnUpdate -= EverestContentUpdateHook;

            On.Celeste.Player.UpdateHair -= PlayerUpdateHairHook;
            On.Celeste.Player.GetTrailColor -= PlayerGetTrailColorHook;
            On.Celeste.Player.StartDash -= PlayerStartDashHook;

            IL.Celeste.Player.UpdateHair -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.DashUpdate -= patch_SpriteMode_Badeline;
            IL.Celeste.Player.GetTrailColor -= patch_SpriteMode_Badeline;
            IL.Celeste.PlayerPlayback.SetFrame -= patch_SpriteMode_Silhouette;

            On.Celeste.Player.Update -= PlayerUpdateHook;
            On.Celeste.PlayerSprite.ctor -= on_PlayerSprite_ctor;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOn;

            On.Celeste.BadelineOldsite.ctor_Vector2_int -= on_BadelineOldsite_ctor;
            On.Celeste.BadelineDummy.ctor -= on_BadelineDummy_ctor;
            On.Celeste.DreamMirror.Added -= DreamMirrorAddedHook;

            On.Celeste.Lookout.Interact -= on_Lookout_Interact;
            IL.Celeste.Player.Render -= PlayerRenderIlHook_Color;
            On.Celeste.PlayerSprite.Render -= OnPlayerSpriteRender;

            IL.Celeste.Player.Render -= PlayerRenderIlHook_Sprite;
            On.Celeste.PlayerHair.GetHairTexture -= PlayerHairGetHairTextureHook;
            On.Monocle.Sprite.Play -= PlayerSpritePlayHook;

            On.Monocle.SpriteBank.Create -= SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOnHook;
            On.Monocle.Atlas.GetAtlasSubtextures -= GetAtlasSubtexturesHook;

            On.Celeste.LevelLoader.LoadingThread -= LevelLoaderLoadingThreadHook;
            On.Celeste.GameLoader.LoadThread -= GameLoaderLoadThreadHook;
            On.Celeste.OuiFileSelectSlot.Setup -= OuiFileSelectSlotSetupHook;

            On.Celeste.DeathEffect.Render -= DeathEffectRenderHook;
            IL.Celeste.DeathEffect.Draw -= DeathEffectDrawHook;
            IL.Celeste.DreamBlock.ctor_Vector2_float_float_Nullable1_bool_bool_bool -= DreamBlockHook;

            IL.Celeste.FlyFeather.ctor_Vector2_bool_bool -= FlyFeatherHook;
            IL.Celeste.CS06_Campfire.Question.ctor -= CampfireQuestionHook;
            IL.Celeste.MiniTextbox.ctor -= SwapTextboxHook;

            foreach (ILHook h in doneILHooks) {
                h.Dispose();
            }
            doneILHooks.Clear();
            foreach (Hook h in doneHooks) {
                h.Dispose();
            }
            doneHooks.Clear();
        }

        public void SpecificSprite_LoopReload() {
            string skinId = XmlCombineValue();
            if (Xmls_record != skinId) {
                Xmls_record = skinId;

                UpdateParticles();
                foreach (string SpriteID in SpriteSkins_records.Keys) {
                    SpriteSkin_record[SpriteID] = null;
                }
                foreach (string SpriteID in PortraitsSkins_records.Keys) {
                    PortraitsSkin_record[SpriteID] = null;
                }

                bool Selected = false;
                foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                    Selected = Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName];

                    if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                        string spritesXmlPath = $"Graphics/{config.OtherSprite_ExPath}/Sprites.xml";
                        string portraitsXmlPath = $"Graphics/{config.OtherSprite_ExPath}/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, config.SkinName, spritesXmlPath, Selected);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, config.SkinName, portraitsXmlPath, Selected);
                    }
                }
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    Selected = Player_Skinid_verify == config.hashValues;

                    if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {
                        string spritesXmlPath = $"Graphics/{config.OtherSprite_Path}/Sprites.xml";
                        string portraitsXmlPath = $"Graphics/{config.OtherSprite_Path}/Portraits.xml";

                        CombineSpriteBanks(GFX.SpriteBank, $"{config.hashValues}", spritesXmlPath, Selected);
                        CombineSpriteBanks(GFX.PortraitsSpriteBank, $"{config.hashValues}", portraitsXmlPath, Selected);
                    }
                }
            }
        }





        private void on_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
            orig(self, session, startPosition);
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.Character_ID)) {
                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Load {config.Character_ID}'s Metadata");
                    //Metadata can is null, but Character_ID can't non-existent , 
                    PlayerSprite.CreateFramesMetadata(config.Character_ID);
                }
            }
        }

        public static int Player_Skinid_verify;
        public static bool backpackOn = true;
        private void on_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode) {
            Level level = Engine.Scene as Level ?? (Engine.Scene as LevelLoader)?.Level;

            DynData<PlayerSprite> selfData = new DynData<PlayerSprite>(self);
            bool isGhost = mode < 0;

            if (!isGhost && level != null) {
                backpackOn = Settings.Backpack == SkinModHelperSettings.BackpackMode.On ||
                    (Settings.Backpack == SkinModHelperSettings.BackpackMode.Default && level.Session.Inventory.Backpack);
            }

            string hash_object = null;
            if (!isGhost && (mode == PlayerSpriteMode.Madeline || mode == PlayerSpriteMode.MadelineNoBackpack || mode == PlayerSpriteMode.MadelineAsBadeline)) {

                hash_object = Session.SessionPlayerSkin == null ? Settings.SelectedPlayerSkin : Session.SessionPlayerSkin;
            } else if (!isGhost && mode == PlayerSpriteMode.Playback) {

                hash_object = Session.SessionSilhouetteSkin == null ? Settings.SelectedSilhouetteSkin : Session.SessionSilhouetteSkin;
            } else if (isGhost) {
                selfData["isGhost"] = true;
            }


            if (hash_object != null) {
                if (skinConfigs.ContainsKey(hash_object)) {
                    mode = (PlayerSpriteMode)skinConfigs[hash_object].hashValues;

                    if (!backpackOn && skinConfigs.ContainsKey($"{hash_object}_NB")) {
                        mode = (PlayerSpriteMode)skinConfigs[$"{hash_object}_NB"].hashValues;
                    }
                }
            }
            orig(self, mode);
            Logger.Log(LogLevel.Info, "SkinModHelper", $"PlayerModeValue: {mode}");

            int requestMode = (int)(isGhost ? (1 << 31) + mode : mode);

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
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

            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
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


        private static Sprite SpriteBankCreateOn(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            // Prevent mode's non-vanilla value causing the game Error
            if (sprite is PlayerSprite && id == "") {
                return null;
            }
            return orig(self, sprite, id);
        }
        
        private void on_BadelineOldsite_ctor(On.Celeste.BadelineOldsite.orig_ctor_Vector2_int orig, BadelineOldsite self, Vector2 position, int index) {
            orig(self, position, index);

            int dashCount = Math.Max(Math.Min(index, MAX_DASHES), 0);
            
            HairConfig hairConfig = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "HairConfig");
            CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "CharacterConfig");


            if (ModeConfig != null && ModeConfig.BadelineMode == null) {
                ModeConfig.BadelineMode = true;
            }
            if (hairConfig != null && hairConfig.HairColors != null) {
                self.Hair.Color = HairConfig.BuildHairColors(hairConfig, ModeConfig)[dashCount];
            }

            if (ModeConfig != null) {
                if (ModeConfig.SilhouetteMode == true) {
                    self.Sprite.Color = self.Hair.Color;
                } else if (ModeConfig.SilhouetteMode == false) {
                    self.Sprite.Color = Color.White;
                }
            }
        }
        private void on_BadelineDummy_ctor(On.Celeste.BadelineDummy.orig_ctor orig, BadelineDummy self, Vector2 position) {
            orig(self, position);

            HairConfig hairConfig = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "HairConfig");
            CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{getAnimationRootPath(self.Sprite)}skinConfig/" + "CharacterConfig");

            if (hairConfig != null && hairConfig.HairColors != null) {
                self.Hair.Color = HairConfig.BuildHairColors(hairConfig)[0];
            }

            if (ModeConfig != null) {
                if (ModeConfig.SilhouetteMode == true) {
                    self.Sprite.Color = self.Hair.Color;
                } else if (ModeConfig.SilhouetteMode == false) {
                    self.Sprite.Color = Color.White;
                }
            }
        }
        private void DreamMirrorAddedHook(On.Celeste.DreamMirror.orig_Added orig, DreamMirror self, Scene scene) {
            orig(self, scene);

            if (!(bool)new DynData<DreamMirror>(self)["smashed"]) {

                PlayerHair reflectionHair = (PlayerHair)new DynData<DreamMirror>(self)["reflectionHair"];
                PlayerSprite reflectionSprite = (PlayerSprite)new DynData<DreamMirror>(self)["reflectionSprite"];

                HairConfig hairConfig = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{getAnimationRootPath(reflectionSprite)}skinConfig/" + "HairConfig");
                CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{getAnimationRootPath(reflectionSprite)}skinConfig/" + "CharacterConfig");

                if (hairConfig != null && hairConfig.HairColors != null) {
                    reflectionHair.Color = HairConfig.BuildHairColors(hairConfig)[0];
                }

                if (ModeConfig != null) {
                    if (ModeConfig.SilhouetteMode == true) {
                        reflectionSprite.Color = reflectionHair.Color;
                    } else if (ModeConfig.SilhouetteMode == false) {
                        reflectionSprite.Color = Color.White;
                    }
                }
                new DynData<PlayerSprite>(reflectionSprite)["ColorGrade_Path"] = $"{getAnimationRootPath(reflectionSprite)}ColorGrading/dash0";
            }
        }



        private void patch_SpriteMode_Badeline(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
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
        private void patch_SpriteMode_Silhouette(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<PlayerSprite>("get_Mode"))) {
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

        private static void on_Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player) {
            orig(self, player);
            if (Player_Skinid_verify != 0) {
                DynData<Lookout> selfData = new DynData<Lookout>(self);
                if (selfData.Get<string>("animPrefix") == "badeline_" || selfData.Get<string>("animPrefix") == "nobackpack_") {
                    selfData["animPrefix"] = "";
                }
            }
            return;
        }

        private void TempleFallCoroutineILHook(ILContext il) {
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

        private void PlayerUpdateHook(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);

            int dashCount = Math.Max(Math.Min(self.Dashes, MAX_DASHES), 0);
            bool MaxDashZero = self.MaxDashes <= 0;

            string rootPath = getAnimationRootPath(self.Sprite);
            int number_search = 0;
            while (number_search < MAX_DASHES && !GFX.Game.Has($"{rootPath}ColorGrading/dash{number_search}")) {
                number_search++;
            }
            bool has_ColorGrade = GFX.Game.Has($"{rootPath}ColorGrading/dash{number_search}");
            new DynData<Player>(self)["has_ColorGrade"] = has_ColorGrade;

            HairConfig hairConfig = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "HairConfig");
            CharacterConfig ModeConfig = searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "CharacterConfig");
            
            if ((hairConfig != null && hairConfig.HairColors != null) || has_ColorGrade) {
                new DynData<Player>(self)["HairColors"] = HairConfig.BuildHairColors(hairConfig, ModeConfig);
            } else {
                new DynData<Player>(self)["HairColors"] = null;
            }

            bool search_out = false;
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if ((int)self.Sprite.Mode == config.hashValues) {
                    Player_Skinid_verify = config.hashValues;
                    search_out = true;
                }
                if (search_out) { break; }
            }
            if (!search_out) { Player_Skinid_verify = 0; }
            SpecificSprite_LoopReload();
        }



        public enum VariantCategory {
            SkinFreeConfig, None
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            //UI.CreateMenu(menu, inGame);
            if (inGame) {
                new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: false, includeRandomizer: false, null, menu, inGame, forceEnabled: false);
                return;
            }
            new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, includeMasterSwitch: true, includeCategorySubmenus: true, includeRandomizer: true, delegate {
                OuiModOptions.Instance.Overworld.Goto<OuiModOptions>();
            }, menu, inGame, forceEnabled: false);
        }
        private void EverestContentUpdateHook(ModAsset oldAsset, ModAsset newAsset) {
            if (newAsset != null && newAsset.PathVirtual.StartsWith("SkinModHelperConfig")) {
                ReloadSettings();
            }
        }

        public void ReloadSettings() {
            skinConfigs.Clear();
            OtherskinConfigs.Clear();

            Instance.LoadSettings();

            foreach (ModContent mod in Everest.Content.Mods) {

                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml)) {
                    List<SkinModHelperConfig> configs = LoadConfigFile<List<SkinModHelperConfig>>(configAsset);

                    foreach (SkinModHelperConfig config in configs) {
                        Regex skinIdRegex = new(@"^[a-zA-Z0-9]+_[a-zA-Z0-9]+$");

                        if (string.IsNullOrEmpty(config.SkinName)) {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"Invalid skin name {config.SkinName}, will not register.");
                        }
                        if (OtherskinConfigs.ContainsKey(config.SkinName) || skinConfigs.ContainsKey(config.SkinName)) {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"Duplicate skin name {config.SkinName}, unregister the second {config.SkinName}");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                            Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new non-player skin: {config.SkinName}");
                            OtherskinConfigs.Add(config.SkinName, config);
                        }

                        if (!string.IsNullOrEmpty(config.Character_ID)) {

                            if (string.IsNullOrEmpty(config.hashSeed)) {
                                config.hashSeed = config.SkinName;
                            }
                            config.hashValues = getHash(config.hashSeed) + 1;

                            if (config.SkinName.EndsWith("_lantern_NB") || config.SkinName.EndsWith("_lantern")) {
                                config.JungleLanternMode = true;

                                if (config.Silhouette_List || config.Player_List) {
                                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"{config.SkinName} this name will affect the gameplay of JungleHelper, it should't appear in the options");
                                }
                                config.Silhouette_List = false;
                                config.Player_List = false;
                            }

                            if (!spritesWithHair.Contains(config.Character_ID)) {
                                spritesWithHair.Add(config.Character_ID);
                            }

                            Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new player skin: {config.SkinName} and {config.hashValues}");
                            skinConfigs.Add(config.SkinName, config);
                        }
                    }
                }
            }

            RecordSpriteBanks_Start();

            if (Settings.SelectedPlayerSkin == null || !skinConfigs.ContainsKey(Settings.SelectedPlayerSkin)) {
                Settings.SelectedPlayerSkin = DEFAULT;
            }
            if (Settings.SelectedSilhouetteSkin == null || !skinConfigs.ContainsKey(Settings.SelectedSilhouetteSkin)) {
                Settings.SelectedSilhouetteSkin = DEFAULT;
            }
        }
        private static int getHash(string hash_send) {
            int hashValue = hash_send.GetHashCode() >> 4;
            if (hashValue < 0) {
                hashValue += (1 << 31);
            }
            return hashValue;
        }

        private static T LoadConfigFile<T>(ModAsset skinConfigYaml) {
            return skinConfigYaml.Deserialize<T>();
        }

        private static T searchSkinConfig<T>(string FilePath) {
            foreach (ModContent mod in Everest.Content.Mods) {
                if (mod.Map.TryGetValue(FilePath, out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml)) {
                    return configAsset.Deserialize<T>();
                }
            }
            return default(T);
        }




        // ---Custom ColorGrade---
        private void OnPlayerSpriteRender(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            orig(self);
            int? get_dashCount;
            string colorGrade_Path = (string)new DynData<PlayerSprite>(self)["ColorGrade_Path"];

            if (self.Entity is BadelineOldsite badeline) {
                get_dashCount = (int)new DynData<BadelineOldsite>(badeline)["index"];
            } else if (self.Entity is BadelineDummy) {
                get_dashCount = 0;
            } else {
                var Dashes = self.Entity.GetType().GetField("Dashes");
                get_dashCount = Dashes != null ? (int?)Dashes.GetValue(self.Entity) : null;

                if (self.Entity is Player player && player.MaxDashes <= 0) {
                    get_dashCount = 1;
                }
            }
            
            if (get_dashCount != null) {
                colorGrade_Path = getAnimationRootPath(self);
                int dashCount = Math.Max(Math.Min((int)get_dashCount, MAX_DASHES), 0);

                while (dashCount > 2 && !GFX.Game.Has($"{colorGrade_Path}ColorGrading/dash{dashCount}")) {
                    dashCount--;
                }

                colorGrade_Path = $"{colorGrade_Path}ColorGrading/dash{dashCount}";
                new DynData<PlayerSprite>(self)["ColorGrade_Path"] = colorGrade_Path;
            }

            if (colorGrade_Path != null && GFX.Game.Has(colorGrade_Path)) {
                Effect colorGradeEffect = GFX.FxColorGrading;
                colorGradeEffect.CurrentTechnique = colorGradeEffect.Techniques["ColorGradeSingle"];
                Engine.Graphics.GraphicsDevice.Textures[1] = GFX.Game[colorGrade_Path].Texture.Texture_Safe;

                DynData<SpriteBatch> spriteData = new DynData<SpriteBatch>(Draw.SpriteBatch);
                Matrix matrix = (Matrix)spriteData["transformMatrix"];

                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, colorGradeEffect, matrix);
                orig(self);
                GameplayRenderer.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
            }
            return;
        }

        // ---Custom Dash Color---
        private void PlayerUpdateHairHook(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity) {
            orig(self, applyGravity);
            if (self.StateMachine.State == Player.StStarFly) {
                return;
            }

            int dashCount = Math.Max(Math.Min(self.Dashes, MAX_DASHES), 0);
            bool MaxDashZero = self.MaxDashes <= 0;

            bool? has_ColorGrade = (bool?)new DynData<Player>(self)["has_ColorGrade"];
            List<Color> HairColors = (List<Color>)new DynData<Player>(self)["HairColors"];
            if ((HairColors != null && self.Hair.Color != Color.White) || has_ColorGrade == true) {
                if (MaxDashZero) {
                    self.Hair.Color = HairColors[1];
                } else {
                    self.Hair.Color = HairColors[dashCount];
                }
            }
            return;
        }
        private Color PlayerGetTrailColorHook(On.Celeste.Player.orig_GetTrailColor orig, Player self, bool wasDashB) {
            int dashCount = new DynData<Player>(self)["TrailDashCount"] != null ? (int)new DynData<Player>(self)["TrailDashCount"] : Math.Max(Math.Min(self.Dashes, MAX_DASHES), 0);
            
            List<Color> HairColors = (List<Color>)new DynData<Player>(self)["HairColors"];
            if (HairColors != null) {
                return HairColors[dashCount];
            }
            return orig(self, wasDashB);
        }
        private int PlayerStartDashHook(On.Celeste.Player.orig_StartDash orig, Player self) {
            new DynData<Player>(self)["TrailDashCount"] = Math.Max(Math.Min(self.Dashes - 1, MAX_DASHES), 0);
            return orig(self);
        }


        private void PlayerRenderIlHook_Color(ILContext il) {
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



        // ---Specific Player Sprite---
        private void PlayerRenderIlHook_Sprite(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("characters/player/startStarFlyWhite"))) {
                Logger.Log("SkinModHelper", $"Changing startStarFlyWhite path at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, Player, string>>((orig, self) => {

                    string spritePath = getAnimationRootPath(self.Sprite, "startStarFly") + "startStarFlyWhite";
                    string number = "";
                    while (number != "00" && !GFX.Game.Has(spritePath + number)) {
                        number = number + "0";
                    }

                    if (GFX.Game.Has(spritePath + number)) {
                        return spritePath;
                    }
                    return orig;
                });
            }
        }
        
        private MTexture PlayerHairGetHairTextureHook(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index) {

            string spritePath = getAnimationRootPath(self.Sprite);
            if (index == 0) {
                string bangs = spritePath + "bangs";

                string number = "";
                while (number != "00" && !GFX.Game.Has(bangs + number)) {
                    number = number + "0";
                }

                if (GFX.Game.Has(bangs + number)) {
                    List<MTexture> newbangs = GFX.Game.GetAtlasSubtextures(bangs);
                    return newbangs.Count > self.Sprite.HairFrame ? newbangs[self.Sprite.HairFrame] : newbangs[0];
                }
            } else {
                string newhair = spritePath + "hair00";
                if (GFX.Game.Has(newhair)) {
                    return GFX.Game[newhair];
                }
            }
            return orig(self, index);
        }




        private void DeathEffectRenderHook(On.Celeste.DeathEffect.orig_Render orig, DeathEffect self) {

            string spritePath = (string)new DynData<DeathEffect>(self)["spritePath"];

            if (self.Entity != null && spritePath == null) {

                var Sprite = self.Entity.GetType().GetField("sprite", BindingFlags.NonPublic | BindingFlags.Instance);

                if (Sprite != null) {
                    Sprite On_sprite = Sprite.GetValue(self.Entity) as Sprite;
                    spritePath = getAnimationRootPath(On_sprite) + "death_particle";

                    new DynData<DeathEffect>(self)["spritePath"] = spritePath;
                }
            }
            if (self.Entity != null) {
                DeathEffectNewDraw(self.Entity.Position + self.Position, self.Color, self.Percent, spritePath);
            }
        }
        public static void DeathEffectNewDraw(Vector2 position, Color color, float ease, string spritePath = "") {

            spritePath = (spritePath == null || !GFX.Game.Has(spritePath)) ? "characters/player/hair00" : spritePath;

            string SpriteID = "death_particle";
            if (spritePath == "characters/player/hair00" && OtherSkins_records.ContainsKey(SpriteID)) {
                Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                string SkinId = getOtherSkin_ReskinPath(GFX.Game, "death_particle", SpriteID, OtherSkin_record[SpriteID]);

                spritePath = SkinId == "death_particle" ? spritePath : SkinId;
            }

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
        private void DeathEffectDrawHook(ILContext il) {
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
                            Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                            string SkinId = getOtherSkin_ReskinPath(GFX.Game, "death_particle", SpriteID, OtherSkin_record[SpriteID]);

                            spritePath = SkinId == "death_particle" ? spritePath : SkinId;
                        }
                    }
                    return spritePath;
                });
            }
        }



        // ---Other Sprite---
        private static List<MTexture> GetAtlasSubtexturesHook(On.Monocle.Atlas.orig_GetAtlasSubtextures orig, Atlas self, string path) {

            string new_path = null;
            bool number_search = false;
            if (path == "marker/runNoBackpack" || path == "marker/Fall" || path == "marker/runBackpack") {
                new_path = path;
                number_search = true;
            }

            if (new_path != null) {
                path = GetReskinPath(self, new_path, true, false, Player_Skinid_verify, number_search);
            }
            return orig(self, path);
        }


        private Sprite SpriteBankCreateOnHook(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            string newId = id;
            if (self == GFX.SpriteBank) {
                if (SpriteSkin_record.ContainsKey(id)) {
                    newId = id + SpriteSkin_record[id];
                }
            }

            if (self.SpriteData.ContainsKey(newId)) {
                id = newId;
                if (sprite is PlayerSprite) {
                    new DynData<PlayerSprite>((PlayerSprite)sprite)["spriteName"] = id;
                }
            }
            return orig(self, sprite, id);
        }

        private Sprite SpriteBankCreateHook(On.Monocle.SpriteBank.orig_Create orig, SpriteBank self, string id) {
            string newId = id;
            if (self == GFX.SpriteBank) {
                if (SpriteSkin_record.ContainsKey(id)) {
                    newId = id + SpriteSkin_record[id];
                }
            } else if (self == GFX.PortraitsSpriteBank) {
                if (PortraitsSkin_record.ContainsKey(id)) {
                    newId = id + PortraitsSkin_record[id];
                }
            }

            if (self.SpriteData.ContainsKey(newId)) {
                id = newId;
            }
            return orig(self, id);
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


        private void PlayerSpritePlayHook(On.Monocle.Sprite.orig_Play orig, Sprite self, string id, bool restart = false, bool randomizeFrame = false) {
            
            if (self is PlayerSprite) {
                DynData<PlayerSprite> selfData = new DynData<PlayerSprite>((PlayerSprite)self);
                if (selfData["spriteName_orig"] != null) {
                    selfData["spriteName_orig"] = null;
                    GFX.SpriteBank.CreateOn(self, (string)selfData["spriteName"]);
                }

                try {
                    orig(self, id, restart, randomizeFrame);
                    return;
                } catch (Exception e) {
                    if (selfData["spriteName_orig"] == null && selfData.Get<string>("spriteName") != "") {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"{selfData["spriteName"]} skin missing animation: {id}");
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


        // ---Load part---
        private void GameLoaderLoadThreadHook(On.Celeste.GameLoader.orig_LoadThread orig, GameLoader self) {
            orig(self);
            RecordSpriteBanks_Start();


            if (UniqueSkinSelected()) {
                Player_Skinid_verify = skinConfigs[Settings.SelectedPlayerSkin].hashValues;
            }
        }


        // Wait until the main sprite bank is created, then combine with our skin mod banks
        private void LevelLoaderLoadingThreadHook(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {

            //at this Hooking time, The level data has not established, cannot get Default backpack state of Level 
            if (Settings.Backpack != SkinModHelperSettings.BackpackMode.Default) {
                backpackOn = Settings.Backpack != SkinModHelperSettings.BackpackMode.Off;
            }

            if (UniqueSkinSelected()) {
                Player_Skinid_verify = skinConfigs[Settings.SelectedPlayerSkin].hashValues;
                if (!backpackOn && UniqueSkinSelected("_NB")) {
                    Player_Skinid_verify = skinConfigs[$"{Settings.SelectedPlayerSkin}_NB"].hashValues;
                }
            }

            Xmls_record = null;
            SpecificSprite_LoopReload();
            orig(self);
        }

        private void OuiFileSelectSlotSetupHook(On.Celeste.OuiFileSelectSlot.orig_Setup orig, OuiFileSelectSlot self) {
            if (self.FileSlot == 0) {
                Xmls_record = null;
                SpecificSprite_LoopReload();

                foreach (string SpriteID in SpriteSkins_records.Keys) {
                    SpriteSkin_record[SpriteID] = null;
                }

                if (SaveFilePortraits) {
                    foreach (string SpriteID in PortraitsSkins_records.Keys) {
                        PortraitsSkin_record[SpriteID] = null;
                    }

                    //Reload the SpriteID registration code of "SaveFilePortraits"
                    Logger.Log("SkinModHelper", $"SaveFilePortraits reload start");
                    SaveFilePortraits_Reload();
                }
            }
            orig(self);
        }


        private static string XmlCombineValue() {
            int sort = 0;
            string identifier = "";
            foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                if (Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName]) {
                    if (sort == 0) {
                        identifier = $"{sort}_{config.SkinName}";
                        sort++;
                    } else {
                        identifier = $"{identifier}, {sort}_{config.SkinName}";
                        sort++;
                    }
                }
            }
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (sort == 0) {
                    identifier = $"{sort}_{Player_Skinid_verify}";
                    sort++;
                } else {
                    identifier = $"{identifier}, {sort}_{Player_Skinid_verify}";
                    sort++;
                }
            }

            if (identifier != "") {
                //Logger.Log(LogLevel.Verbose, "SkinModHelper", $"SpriteBank identifier: {identifier}");
                return $"_{identifier}";
            }
            return null;
        }



        private static void UpdateParticles() {
            FlyFeather.P_Collect.Source = GFX.Game["particles/feather"];
            FlyFeather.P_Boost.Source = GFX.Game["particles/feather"];

            string CustomPath = "particles/feather";

            string SpriteID = "feather_particles";
            if (OtherSkins_records.ContainsKey(SpriteID)) {
                Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                CustomPath = getOtherSkin_ReskinPath(GFX.Game, "particles/feather", SpriteID, OtherSkin_record[SpriteID]);
            }

            if (CustomPath != null) {
                FlyFeather.P_Collect.Source = GFX.Game[CustomPath];
                FlyFeather.P_Boost.Source = GFX.Game[CustomPath];
            }
        }


        private void DreamBlockHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/dreamblock/particles"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    string SpriteID = "dreamblock_particles";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/dreamblock/particles", SpriteID, OtherSkin_record[SpriteID]);
                    }
                    return "objects/dreamblock/particles";
                });
            }
        }

        private void FlyFeatherHook(ILContext il) {
            ILCursor cursor = new(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("objects/flyFeather/outline"))) {
                cursor.EmitDelegate<Func<string, string>>((orig) => {

                    UpdateParticles();
                    string SpriteID = "feather_outline";
                    if (OtherSkins_records.ContainsKey(SpriteID)) {
                        Update_FreeCollocations_OtherExtra(SpriteID, null, true, true);
                        return getOtherSkin_ReskinPath(GFX.Game, "objects/flyFeather/outline", SpriteID, OtherSkin_record[SpriteID]);
                    }
                    return "objects/flyFeather/outline";
                });
            }
        }



        private static string GetReskinPath(Atlas atlas, string orig, bool N_Path, bool Ex_Path, int mode, bool number_search = false) {
            string number = "";
            string CustomPath = null;
            if (mode != 0) {
                foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                    if (mode == config.hashValues) {
                        if (N_Path && !string.IsNullOrEmpty(config.OtherSprite_Path)) {
                            CustomPath = $"{config.OtherSprite_Path}/{orig}";
                        }

                        if (CustomPath != null) {
                            while (number_search && number != "00000" && !atlas.Has(CustomPath + number)) {
                                number = number + "0";
                            }
                            if (atlas.Has(CustomPath + number)) {
                                return CustomPath;
                            }
                        }
                    }
                }
            }

            if (Ex_Path) {
                CustomPath = null;
                foreach (SkinModHelperConfig config in SkinModHelperModule.OtherskinConfigs.Values) {
                    if (Settings.ExtraXmlList.ContainsKey(config.SkinName)) {
                        if (Settings.ExtraXmlList[config.SkinName] && !string.IsNullOrEmpty(config.OtherSprite_ExPath)) {

                            number = "";
                            while (number_search && number != "00000" && !atlas.Has($"{config.OtherSprite_ExPath}/{orig}{number}")) {
                                number = number + "0";
                            }
                            if (atlas.Has($"{config.OtherSprite_ExPath}/{orig}{number}")) {
                                CustomPath = $"{config.OtherSprite_ExPath}/{orig}";
                            }
                        }
                    }
                }

                if (CustomPath != null) {
                    return atlas.Has(CustomPath + number) ? CustomPath : orig;
                }
            }
            return orig;
        }


        private void SwapTextboxHook(ILContext il) {
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
        private void CampfireQuestionHook(ILContext il) {
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


        // Combine skin mod XML with a vanilla sprite bank
        private void CombineSpriteBanks(SpriteBank origBank, string skinId, string xmlPath, bool Selected) {
            SpriteBank newBank = BuildBank(origBank, skinId, xmlPath);
            if (newBank == null) {
                return;
            }

            // For each overridden sprite, patch it and add it to the original bank with a unique identifier
            foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData) {
                string spriteId = spriteDataEntry.Key;
                SpriteData newSpriteData = spriteDataEntry.Value;

                if (origBank.SpriteData.TryGetValue(spriteId, out SpriteData origSpriteData)) {
                    PatchSprite(origSpriteData.Sprite, newSpriteData.Sprite);

                    string newSpriteId = spriteId + skinId;
                    origBank.SpriteData[newSpriteId] = newSpriteData;

                    if (origBank == GFX.SpriteBank && !string.IsNullOrEmpty(skinId)) {
                        if (Settings.FreeCollocations_OffOn) {
                            if (!Settings.FreeCollocations_Sprites.ContainsKey(spriteId) || Settings.FreeCollocations_Sprites[spriteId] == DEFAULT) {
                                if (Selected) {
                                    SpriteSkin_record[spriteId] = skinId;
                                }
                            } else if (Settings.FreeCollocations_Sprites[spriteId] == skinId) {
                                SpriteSkin_record[spriteId] = skinId;
                            } else {
                                SpriteSkin_record[spriteId] = null;
                            }
                        } else if (Selected) {
                            SpriteSkin_record[spriteId] = skinId;
                        }

                        if (spritesWithHair.Contains(spriteId)) {
                            PlayerSprite.CreateFramesMetadata(newSpriteId);
                        }
                    } else if (origBank == GFX.PortraitsSpriteBank && !string.IsNullOrEmpty(skinId)) {
                        if (Settings.FreeCollocations_OffOn) {
                            if (!Settings.FreeCollocations_Portraits.ContainsKey(spriteId) || Settings.FreeCollocations_Portraits[spriteId] == DEFAULT) {
                                if (Selected) {
                                    PortraitsSkin_record[spriteId] = skinId;
                                }
                            } else if (Settings.FreeCollocations_Portraits[spriteId] == skinId) {
                                PortraitsSkin_record[spriteId] = skinId;
                            } else {
                                PortraitsSkin_record[spriteId] = null;
                            }
                        } else if (Selected) {
                            PortraitsSkin_record[spriteId] = skinId;
                        }
                    }
                }
            }
        }

        private void RecordSpriteBanks_Start() {
            SpriteSkins_records.Clear();
            PortraitsSkins_records.Clear();
            OtherSkins_records.Clear();

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {

                    string spritesXmlPath = $"Graphics/{config.OtherSprite_Path}/Sprites.xml";
                    string portraitsXmlPath = $"Graphics/{config.OtherSprite_Path}/Portraits.xml";

                    RecordSpriteBanks(GFX.SpriteBank, DEFAULT, spritesXmlPath);
                    RecordSpriteBanks(GFX.PortraitsSpriteBank, DEFAULT, portraitsXmlPath);

                    if (GFX.Game.Has(config.OtherSprite_Path + "/death_particle")) {
                        RecordSpriteBanks(null, DEFAULT, null, "death_particle");
                    }
                    if (GFX.Game.Has(config.OtherSprite_Path + "/objects/dreamblock/particles")) {
                        RecordSpriteBanks(null, DEFAULT, null, "dreamblock_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_Path + "/particles/feather")) {
                        RecordSpriteBanks(null, DEFAULT, null, "feather_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_Path + "/objects/flyFeather/outline")) {
                        RecordSpriteBanks(null, DEFAULT, null, "feather_outline");
                    }
                }
            }

            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {

                    string spritesXmlPath = $"Graphics/{config.OtherSprite_ExPath}/Sprites.xml";
                    string portraitsXmlPath = $"Graphics/{config.OtherSprite_ExPath}/Portraits.xml";

                    RecordSpriteBanks(GFX.SpriteBank, config.SkinName, spritesXmlPath);
                    RecordSpriteBanks(GFX.PortraitsSpriteBank, config.SkinName, portraitsXmlPath);

                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/death_particle")) {
                        RecordSpriteBanks(null, config.SkinName, null, "death_particle");
                    }
                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/objects/dreamblock/particles")) {
                        RecordSpriteBanks(null, config.SkinName, null, "dreamblock_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/particles/feather")) {
                        RecordSpriteBanks(null, config.SkinName, null, "feather_particles");
                    }
                    if (GFX.Game.Has(config.OtherSprite_ExPath + "/objects/flyFeather/outline")) {
                        RecordSpriteBanks(null, config.SkinName, null, "feather_outline");
                    }
                }
            }
        }
        private void RecordSpriteBanks(SpriteBank origBank, string skinId, string xmlPath, string otherSkin = null) {
            if (otherSkin == null) {
                SpriteBank newBank = BuildBank(origBank, skinId, xmlPath);
                if (newBank == null) {
                    return;
                }

                foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData) {
                    string spriteId = spriteDataEntry.Key;
                    if (!string.IsNullOrEmpty(skinId)) {
                        if (origBank == GFX.SpriteBank && origBank.SpriteData.ContainsKey(spriteId)) {

                            if (!SpriteSkins_records.ContainsKey(spriteId)) {
                                SpriteSkins_records.Add(spriteId, new());
                            }
                            if (skinId != DEFAULT && !SpriteSkins_records[spriteId].Contains(skinId)) {
                                SpriteSkins_records[spriteId].Add(skinId);
                            }
                        } else if (origBank == GFX.PortraitsSpriteBank && origBank.SpriteData.ContainsKey(spriteId)) {

                            if (!PortraitsSkins_records.ContainsKey(spriteId)) {
                                PortraitsSkins_records.Add(spriteId, new());
                            }
                            if (skinId != DEFAULT && !PortraitsSkins_records[spriteId].Contains(skinId)) {
                                PortraitsSkins_records[spriteId].Add(skinId);
                            }
                        }
                    }
                }
            } else {
                string spriteId = otherSkin;
                if (!OtherSkins_records.ContainsKey(spriteId)) {
                    OtherSkins_records.Add(spriteId, new());
                }
                if (skinId != DEFAULT && !OtherSkins_records[spriteId].Contains(skinId)) {
                    OtherSkins_records[spriteId].Add(skinId);
                }
            }
        }






        private SpriteBank BuildBank(SpriteBank origBank, string skinId, string xmlPath) {
            try {
                SpriteBank newBank = new(origBank.Atlas, xmlPath);
                return newBank;
            } catch (Exception e) {
                return null;
            }
        }

        // Add any missing vanilla animations to an overridden sprite
        private void PatchSprite(Sprite origSprite, Sprite newSprite) {
            Dictionary<string, Sprite.Animation> newAnims = newSprite.GetAnimations();

            // Shallow copy... sometimes new animations get added mid-update?
            Dictionary<string, Sprite.Animation> oldAnims = new(origSprite.GetAnimations());
            foreach (KeyValuePair<string, Sprite.Animation> animEntry in oldAnims) {
                string origAnimId = animEntry.Key;
                Sprite.Animation origAnim = animEntry.Value;
                if (!newAnims.ContainsKey(origAnimId)) {
                    newAnims[origAnimId] = origAnim;
                }
            }
        }


        public static string getAnimationRootPath(Sprite sprite, string animationID = "idle") {

            if (sprite is PlayerSprite) {
                string spriteName = (string)new DynData<PlayerSprite>((PlayerSprite)sprite)["spriteName"];
                if (GFX.SpriteBank.SpriteData.ContainsKey(spriteName)) {

                    SpriteData spriteData = GFX.SpriteBank.SpriteData[spriteName];

                    if (!string.IsNullOrEmpty(spriteData.Sources[0].OverridePath)) {
                        return spriteData.Sources[0].OverridePath;
                    } else {;
                        return spriteData.Sources[0].Path;
                    }
                }
            }

            string RootPath = $"{(sprite.Has(animationID) ? sprite.GetFrame(animationID, 0) : sprite.Texture)}";
            return RootPath.Remove(RootPath.LastIndexOf("/") + 1);
        }



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





        public static void UpdateSkin(string newSkinId, bool inGame = false) {
            if (Session != null) {
                Session.SessionPlayerSkin = null;
            }

            Settings.SelectedPlayerSkin = newSkinId;
            RefreshPlayerSpriteMode();
            if (!inGame) {
                if (skinConfigs.ContainsKey(newSkinId)) {
                    Player_Skinid_verify = skinConfigs[newSkinId].hashValues;
                } else {
                    Player_Skinid_verify = 0;
                }
            }
        }
        public static void UpdateSilhouetteSkin(string newSkinId) {
            if (Session != null) {
                Session.SessionSilhouetteSkin = null;
            }

            Settings.SelectedSilhouetteSkin = newSkinId;
        }
        public static void UpdateExtraXml(string SkinId, bool OnOff) {
            if (Session != null && Session.SessionExtraXml.ContainsKey(SkinId)) {
                Session.SessionExtraXml.Remove(SkinId);
            }

            Settings.ExtraXmlList[SkinId] = OnOff;
        }

        public static void Update_FreeCollocations_OnOff(bool OnOff, bool inGame) {
            Settings.FreeCollocations_OffOn = OnOff;

            foreach (string SpriteID in SpriteSkins_records.Keys) {
                Update_FreeCollocations_Sprites(SpriteID, null, inGame, true);
            }
            foreach (string SpriteID in PortraitsSkins_records.Keys) {
                Update_FreeCollocations_Portraits(SpriteID, null, inGame, true);
            }
            foreach (string SpriteID in OtherSkins_records.Keys) {
                Update_FreeCollocations_OtherExtra(SpriteID, null, inGame, true);
            }
        }

        public static void Update_FreeCollocations_Sprites(string SpriteID, string SkinId, bool inGame, bool OnOff = false) {
            if (!OnOff) {
                Settings.FreeCollocations_Sprites[SpriteID] = SkinId;
            }

            if (!Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !Settings.FreeCollocations_Sprites.ContainsKey(SpriteID) || Settings.FreeCollocations_Sprites[SpriteID] == DEFAULT) {
                SpriteSkin_record[SpriteID] = getSkinDefaultValues(GFX.SpriteBank, SpriteID);
            } else {
                SpriteSkin_record[SpriteID] = Settings.FreeCollocations_Sprites[SpriteID];
            }
        }

        public static void Update_FreeCollocations_Portraits(string SpriteID, string SkinId, bool inGame, bool OnOff = false) {
            if (!OnOff) {
                Settings.FreeCollocations_Portraits[SpriteID] = SkinId;
            }

            if (!Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !Settings.FreeCollocations_Portraits.ContainsKey(SpriteID) || Settings.FreeCollocations_Portraits[SpriteID] == DEFAULT) {
                PortraitsSkin_record[SpriteID] = getSkinDefaultValues(GFX.PortraitsSpriteBank, SpriteID);
            } else {
                PortraitsSkin_record[SpriteID] = Settings.FreeCollocations_Portraits[SpriteID];
            }
        }
        public static void Update_FreeCollocations_OtherExtra(string SpriteID, string SkinId, bool inGame, bool OnOff = false) {
            if (!OnOff) {
                Settings.FreeCollocations_OtherExtra[SpriteID] = SkinId;
            }

            if (!Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !Settings.FreeCollocations_OtherExtra.ContainsKey(SpriteID) || Settings.FreeCollocations_OtherExtra[SpriteID] == DEFAULT) {
                OtherSkin_record[SpriteID] = DEFAULT;
            } else {
                OtherSkin_record[SpriteID] = Settings.FreeCollocations_OtherExtra[SpriteID];
            }
        }









        public static string getSkinDefaultValues(SpriteBank selfBank, string SpriteID) {
            if (selfBank.Has(SpriteID + $"{Player_Skinid_verify}")) {
                return $"{Player_Skinid_verify}";
            }
            string SkinID = null;
            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if ((selfBank == GFX.SpriteBank && SpriteSkins_records[SpriteID].Contains(config.SkinName)) ||
                    (selfBank == GFX.PortraitsSpriteBank && PortraitsSkins_records[SpriteID].Contains(config.SkinName))) {
                    if (Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName]) {
                        SkinID = config.SkinName;
                    }
                }
            }
            //Logger.Log(LogLevel.Warn, "SkinModHelper", $"SkinDefaultValues: {SkinID}");
            return SkinID;
        }


        public static string getOtherSkin_ReskinPath(Atlas atlas, string origPath, string SpriteID, string SkinId, bool number_search = false) {
            string number = "";
            string CustomPath = null;
            bool Default = !Settings.FreeCollocations_OffOn || SkinId == DEFAULT || !OtherSkin_record.ContainsKey(SpriteID) || OtherSkin_record[SpriteID] == DEFAULT;
            if (Default) {
                foreach (SkinModHelperConfig config in skinConfigs.Values) {
                    if (Player_Skinid_verify == config.hashValues) {
                        if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {
                            CustomPath = $"{config.OtherSprite_Path}/{origPath}";
                        }

                        if (CustomPath != null) {
                            while (number_search && number != "00000" && !atlas.Has(CustomPath + number)) {
                                number = number + "0";
                            }
                            if (atlas.Has(CustomPath + number)) {
                                return CustomPath;
                            }
                        }
                    }
                }
            }
            CustomPath = null;
            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                    if (SkinId == config.SkinName ||
                       (Default && Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName])) {

                        number = "";
                        while (number_search && number != "00000" && !atlas.Has($"{config.OtherSprite_ExPath}/{origPath}{number}")) {
                            number = number + "0";
                        }
                        if (atlas.Has($"{config.OtherSprite_ExPath}/{origPath}{number}")) {
                            CustomPath = $"{config.OtherSprite_ExPath}/{origPath}";
                        }
                    }
                }
            }
            return atlas.Has(CustomPath + number) ? CustomPath : origPath;
        }




        public static bool UniqueSkinSelected(string skin_suffix = null) {

            string skin_name = Settings.SelectedPlayerSkin + skin_suffix;
            return Settings.SelectedPlayerSkin != null && Settings.SelectedPlayerSkin != DEFAULT && skinConfigs.ContainsKey(skin_name);
        }
        public static bool UniqueSilhouetteSelected(string skin_suffix = null) {

            string skin_name = Settings.SelectedSilhouetteSkin + skin_suffix;
            return Settings.SelectedSilhouetteSkin != null && Settings.SelectedSilhouetteSkin != DEFAULT && skinConfigs.ContainsKey(skin_name);
        }











        public static void SetPlayerSpriteMode(PlayerSpriteMode? mode) {
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


        // ---JungleHelper---
        public static bool HasLantern(PlayerSpriteMode mode) {
            if (mode == (PlayerSpriteMode)444482 || mode == (PlayerSpriteMode)444483) {
                return true;
            }
            foreach (SkinModHelperConfig config in SkinModHelperModule.skinConfigs.Values) {
                if (config.JungleLanternMode == true) {
                    if (mode == (PlayerSpriteMode)config.hashValues) {
                        return true;
                    }
                }
            }
            return false;
        }


        public static void ChangePlayerSpriteMode(Player player, bool hasLantern) {
            PlayerSpriteMode mode;

            if (hasLantern) {
                mode = SaveData.Instance.Assists.PlayAsBadeline ? (PlayerSpriteMode)444483 : (PlayerSpriteMode)444482;

                string hash_object = $"{(Session.SessionPlayerSkin == null ? Settings.SelectedPlayerSkin : Session.SessionPlayerSkin)}_lantern";

                if (skinConfigs.ContainsKey(hash_object)) {

                    if (!skinConfigs[hash_object].JungleLanternMode) {
                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"{hash_object} unset JungleLanternMode to true, will cancel this jungle-jump");
                    } else {
                        if (!backpackOn && skinConfigs.ContainsKey($"{hash_object}_NB")) {
                            if (!skinConfigs[$"{hash_object}_NB"].JungleLanternMode) {
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{$"{hash_object}_NB"} unset JungleLanternMode to true, will jungle-jump to {hash_object}");
                            } else {
                                hash_object = $"{hash_object}_NB";
                            }
                        }
                        Player_Skinid_verify = skinConfigs[hash_object].hashValues;
                        mode = (PlayerSpriteMode)skinConfigs[hash_object].hashValues;
                    }
                }
            } else {
                mode = SaveData.Instance.Assists.PlayAsBadeline ? PlayerSpriteMode.MadelineAsBadeline : player.DefaultSpriteMode;
            }

            if (player.Active) {
                player.ResetSpriteNextFrame(mode);
            } else {
                player.ResetSprite(mode);
            }
        }
    }
}