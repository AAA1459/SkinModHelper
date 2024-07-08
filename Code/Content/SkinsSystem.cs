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
using System.Diagnostics;
using Celeste.Mod.Meta;

using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public class SkinsSystem {
        #region

        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;
        public static SkinModHelperSettings smh_Settings => Settings;
        public static SkinModHelperSession smh_Session => Session;

        public static RespriteBank Reskin_SpriteBank = new("Sprites.xml", "SpritesXml", "Sprite");
        public static ReportraitsBank Reskin_PortraitsBank = new("Portraits.xml", "PortraitsXml", "Portraits");
        public static nonBankReskin OtherSpriteSkins = new("OtherExtra", "Other");

        public static Dictionary<string, SkinModHelperConfig> skinConfigs = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<int, string> skinname_hashcache = new();

        public static Dictionary<string, SkinModHelperConfig> OtherskinConfigs = new(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, SkinModHelperOldConfig> OtherskinOldConfig = new(StringComparer.OrdinalIgnoreCase);

        public static void Load() {
            Everest.Content.OnUpdate += EverestContentUpdateHook;

            On.Celeste.Player.Update += PlayerUpdateHook;
            doneHooks.Add(new Hook(typeof(Sprite).GetMethod("Render", BindingFlags.Public | BindingFlags.Instance),
                                   typeof(SkinsSystem).GetMethod("SpriteRenderHook", BindingFlags.NonPublic | BindingFlags.Static)));

            On.Monocle.SpriteBank.Create += SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn += SpriteBankCreateOnHook;

            doneHooks.Add(new Hook(typeof(Atlas).GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance),
                                   typeof(SkinsSystem).GetMethod("Atlas_GetItemHook", BindingFlags.NonPublic | BindingFlags.Static)));
            On.Monocle.Atlas.GetAtlasSubtextures += GetAtlasSubtexturesHook;
            On.Monocle.Sprite.ctor_Atlas_string += SpriteCtorAtlasStringHook;
        }

        public static void Unload() {
            Everest.Content.OnUpdate -= EverestContentUpdateHook;

            On.Celeste.Player.Update -= PlayerUpdateHook;

            On.Monocle.SpriteBank.Create -= SpriteBankCreateHook;
            On.Monocle.SpriteBank.CreateOn -= SpriteBankCreateOnHook;
            On.Monocle.Atlas.GetAtlasSubtextures -= GetAtlasSubtexturesHook;
            On.Monocle.Sprite.ctor_Atlas_string -= SpriteCtorAtlasStringHook;
        }

        public static void LoadContent(bool firstLoad) {
        }
        public static List<List<MTexture>> loadingTextures = new();

        #endregion
        #region
        public static readonly int MAX_HAIRLENGTH = 99;
        public static readonly string playercipher = "_+";

        public static readonly string DEFAULT = "Default";
        public static readonly string ORIGINAL = "Original";
        public static readonly string LockedToPlayer = "LockedToPlayer";

        public static readonly MethodInfo CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static int Player_Skinid_verify;

        public static bool? actualBackpack;
        public static bool backpackOn = true;

        /// <summary> 0-Default, 1-Invert, 2-Off, 3-On </summary>
        public static int backpackSetting = 0;
        public static Regex RGB_Regex = new Regex(@"^[a-fA-F0-9]{6}$");

        public static bool build_warning = true;
        public static List<string> FailedXml_record = new();
        public static Dictionary<string, SpriteBank> Xml_records = new();

        /// <summary> Similar to GFX.FxColorGrading, But indexing new color on colorGrade only based the rgb color of the texture source. </summary>
        public static Effect FxColorGrading_SMH;
        #endregion
        #region

        /// <summary> 
        /// Invoke when after SkinRefresh.  both values respectively as Xmls_refresh and inGame 
        /// </summary>
        public static Action<bool, bool> afterSkinRefresh;

        /// <summary> 
        /// Invoke when before SkinRefresh.  both values respectively as Xmls_refresh and inGame 
        /// </summary>
        public static Action<bool, bool> beforeSkinRefresh;

        #endregion

        //-----------------------------Build Skins-----------------------------
        #region
        private static void EverestContentUpdateHook(ModAsset oldAsset, ModAsset newAsset) {
            if (newAsset != null) {
                if (newAsset.PathVirtual.StartsWith("SkinModHelperConfig")) {
                    ConfigInsert(newAsset);
                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"If the new skins's content does not load, please enter the save slot menu to refresh it");
                }
            }
        }
        public static void ReloadSettings() {
            Logger.Log(LogLevel.Info, "SkinModHelper", $"Skins loading... Settings Initializing...");

            skinConfigs.Clear();
            OtherskinConfigs.Clear();
            OtherskinOldConfig.Clear();
            Instance.LoadSettings();
            build_warning = true;

            foreach (ModContent mod in Everest.Content.Mods) {
                if (mod.Map.TryGetValue("SkinModHelperConfig", out ModAsset configAsset)) {
                    ConfigInsert(configAsset);
                }
            }
            if (Settings.SelectedPlayerSkin == null) {
                Settings.SelectedPlayerSkin = DEFAULT;
            }
            if (Settings.SelectedSilhouetteSkin == null) {
                Settings.SelectedSilhouetteSkin = DEFAULT;
            }
        }
        public static void ConfigInsert(ModAsset asset) {
            if (asset.Type != typeof(AssetTypeYaml))
                return;
            skinname_hashcache.Clear();

            #region // nonPlus skin config
            if (LoadConfigFile<SkinModHelperOldConfig>(asset, out var old_config)) {
                if (string.IsNullOrEmpty(old_config.SkinId) || old_config.SkinId.EndsWith("_")) {
                    Logger.Log(LogLevel.Error, "SkinModHelper", $"Invalid skin name '{old_config.SkinId}', will not register.");
                    return;
                }
                SkinModHelperConfig config = new(old_config);

                if (config.SkinName == DEFAULT || config.SkinName == ORIGINAL || config.SkinName == LockedToPlayer) {
                    Logger.Log(LogLevel.Error, "SkinModHelper", $"skin name '{config.SkinName}' has been taken.");
                    return;
                }
                if (string.IsNullOrEmpty(config.Mod))
                    config.Mod = asset.Source.Name;

                if (OtherskinOldConfig.ContainsKey(config.SkinName)) {
                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Re-registered old-ver general skin: {config.SkinName}");
                } else if (OtherskinConfigs.ContainsKey(config.SkinName)) {
                    Logger.Log(LogLevel.Error, "SkinModHelper", $"skin name '{config.SkinName}' has been taken.");
                    return;
                } else {
                    Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered old-ver general skin: {config.SkinName}");
                }
                OtherskinConfigs[config.SkinName] = config;
                OtherskinOldConfig[config.SkinName] = old_config;
                return;
            }
            #endregion

            if (!LoadConfigFile<List<SkinModHelperConfig>>(asset, out var configs) || configs.Count < 1)
                return;

            #region // skin config
            foreach (SkinModHelperConfig config in configs) {
                if (string.IsNullOrEmpty(config.SkinName) || config.SkinName.EndsWith("_")) {
                    Logger.Log(LogLevel.Error, "SkinModHelper", $"Invalid skin name '{config.SkinName}', will not register.");
                    continue;

                } else if (config.SkinName == DEFAULT || config.SkinName == ORIGINAL || config.SkinName == LockedToPlayer) {
                    Logger.Log(LogLevel.Warn, "SkinModHelper", $"skin name '{config.SkinName}' has been taken.");
                    continue;
                }
                if (string.IsNullOrEmpty(config.Mod))
                    config.Mod = asset.Source.Name;

                //---------------------GeneralSkin------------------------#
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                    if (!OtherskinConfigs.ContainsKey(config.SkinName)) {
                        var logLevel = config.General_List == false ? LogLevel.Debug : LogLevel.Info;
                        Logger.Log(logLevel, "SkinModHelper", $"Registered new general skin: {config.SkinName}");
                    } else
                        Logger.Log(LogLevel.Debug, "SkinModHelper", $"Re-registered general skin: {config.SkinName}");

                    OtherskinOldConfig.Remove(config.SkinName);
                    OtherskinConfigs[config.SkinName] = config;
                }
                //--------------------------------------------------------#
                //---------------------PlayerSkin-------------------------
                if (!string.IsNullOrEmpty(config.Character_ID)) {
                    if (string.IsNullOrEmpty(config.hashSeed)) { config.hashSeed = config.SkinName; }
                    config.hashValues = getHash(config.hashSeed) + 1;

                    //----------------JungleLantern---------------
                    if ((config.SkinName.EndsWith("_lantern_NB") || config.SkinName.EndsWith("_lantern"))) {
                        config.JungleLanternMode = true;
                        if (config.Silhouette_List || config.Player_List) {
                            Logger.Log(LogLevel.Warn, "SkinModHelper", $"'{config.SkinName}' this name will affect the gameplay of junglehelper, it should't appear in the options.");
                        }
                        config.Player_List = config.Silhouette_List = false;
                    }
                    //--------------------------------------------#

                    string name = GetPlayerSkinName(config.hashValues);
                    if (name == null || name == config.SkinName) {
                        string s = "   ";
                        for (int i = config.SkinName.Length; i < 32; s += " ", i++) { }
                        if (name != config.SkinName)
                            Logger.Log(LogLevel.Info, "SkinModHelper", $"Registered new player skin: {config.SkinName}{s}{config.hashValues}");
                        else
                            Logger.Log(LogLevel.Debug, "SkinModHelper", $"Re-registered player skin: {config.SkinName}{s}{config.hashValues}");

                        skinConfigs[config.SkinName] = config;
                    } else {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"player skin '{config.SkinName}' and '{name}' happened hash value conflict! cannot registered.");
                    }
                }
                //--------------------------------------------------------#
            }
            #endregion
        }

        private static int getHash(string hash_send) {
            if (hash_send == null) {
                throw new Exception("null hash send");
            }
            int hashValue;

            unchecked {
                int num = 352654597;
                int num_2 = num;

                for (int i = 0; i < hash_send.Length; i += 2) {
                    num = ((num << 5) + num) ^ hash_send[i];
                    if (i == hash_send.Length - 1) { break; }
                    num_2 = ((num_2 << 5) + num_2) ^ hash_send[i + 1];
                }
                hashValue = num + (num_2 * 1566083941);
            }

            if (hashValue < 0) { hashValue += (1 << 31); }
            return hashValue;
        }
        #endregion

        //-----------------------------Sprite Banks / Skin Xmls-----------------------------
        #region
        private static Sprite SpriteBankCreateHook(On.Monocle.SpriteBank.orig_Create orig, SpriteBank self, string id) {

            if (RespriteBankModule.SearchInstance(self, out var skinbank)) {
                string newId = skinbank.GetCurrentSkin(id);
                if (self.Has(newId))
                    id = newId;
            }
            Sprite sprite = orig(self, id);
            if (sprite != null) {
                DynamicData.For(sprite).Set("smh_spriteName", id);
                DynamicData.For(sprite).Set("smh_spriteData", self.SpriteData[id]);
            }
            return sprite;
        }
        private static Sprite SpriteBankCreateOnHook(On.Monocle.SpriteBank.orig_CreateOn orig, SpriteBank self, Sprite sprite, string id) {
            if (sprite.Entity is OuiFileSelectSlot && SaveFilePortraits)
                return orig(self, sprite, id);

            if (RespriteBankModule.SearchInstance(self, out var skinbank)) {
                string newId = skinbank.GetCurrentSkin(id);
                if (self.Has(newId))
                    id = newId;
                if (sprite is PlayerSprite playerSprite)
                    playerSprite.spriteName = id;
            }
            sprite = orig(self, sprite, id);
            if (sprite != null) {
                DynamicData.For(sprite).Set("smh_spriteName", id);
                DynamicData.For(sprite).Set("smh_spriteData", self.SpriteData[id]);
            }
            return sprite;
        }
        #endregion

        #region
        public static void RespriteBank_Reload() {
            var RespriteBanks = RespriteBankModule.ManagedInstance();
            foreach (var instance in RespriteBanks) {
                instance.ClearRecord();
            }

            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (GFX.SpriteBank.SpriteData.ContainsKey(config.Character_ID)) {
                    PlayerSprite.CreateFramesMetadata(config.Character_ID);
                } else if (build_warning) {
                    throw new Exception($"[SkinModHelper] '{config.Character_ID}' does not exist in Graphics/Sprites.xml");
                }
            }
            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_Path)) {
                    string skinId = config.SkinName;
                    string dir = config.OtherSprite_Path;

                    foreach (var instance in RespriteBanks) {
                        instance.DoRecord(config.SkinName, dir, playercipher);
                        instance.DoCombine(config.SkinName, dir, playercipher);
                    }
                }
            }
            foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                if (!string.IsNullOrEmpty(config.OtherSprite_ExPath)) {
                    string skinId = config.SkinName;
                    string dir = config.OtherSprite_ExPath;

                    foreach (var instance in RespriteBanks) {
                        instance.DoRecord(skinId, dir);
                        instance.DoCombine(skinId, dir);
                    }
                }
            }
        }
        public static SpriteBank BuildBank(SpriteBank origBank, string skinId, string xmlPath) {
            string dir = xmlPath.Remove(xmlPath.LastIndexOf("/"));
            if (xmlPath == null)
                return null;

            if (Xml_records.TryGetValue(xmlPath, out SpriteBank newBank))
                return newBank;

            else if (FailedXml_record.Contains(dir) || FailedXml_record.Contains(xmlPath))
                return null;

            else if (!AssetExists<AssetTypeDirectory>(dir)) {
                FailedXml_record.Add(dir);
                Logger.Log(LogLevel.Error, "SkinModHelper", $"The xmls directory of '{skinId}' does not exist: {dir}");
                return null;

            } else if (AssetExists<AssetTypeXml>(xmlPath)) {
                try {
                    SpriteBank newBank_2 = new SpriteBank(origBank?.Atlas ?? GFX.Game, xmlPath);
                    return Xml_records[xmlPath] = newBank_2;
                } catch (Exception e) {
                    Logger.Log(LogLevel.Error, "SkinModHelper", $"The {xmlPath.Replace(dir + "/", "")} of '{skinId}' build failed! \n {xmlPath}: {e.Message}");
                }
            }
            FailedXml_record.Add(xmlPath);
            return null;
        }
        #endregion

        //-----------------------------Other Sprite-----------------------------
        #region
        public static void UpdateParticles() {
            // well... Atlas_GetItemHook will help we update this.
            FlyFeather.P_Collect.Source = GFX.Game["particles/feather"];
            FlyFeather.P_Boost.Source = GFX.Game["particles/feather"];
        }
        public static string RedirectPathToBackpack(Atlas atlas, string path) {
            if (atlas == MTN.Mountain) {
                if (path == "marker/runNoBackpack" && (backpackSetting == 3 || backpackSetting == 1)) {
                    path = "marker/runBackpack";
                } else if (path == "marker/runBackpack" && (backpackSetting == 2 || backpackSetting == 1)) {
                    path = "marker/runNoBackpack";
                }
            }
            return path;
        }
        #endregion

        #region
        private static MTexture Atlas_GetItemHook(Func<Atlas, string, MTexture> orig, Atlas self, string path) {
            path = OtherSpriteSkins.GetSkinWithPath(self, path, false);

            return orig(self, path);
        }

        private static List<MTexture> GetAtlasSubtexturesHook(On.Monocle.Atlas.orig_GetAtlasSubtextures orig, Atlas self, string path) {
            if (self == OVR.Atlas && path == "loading/" && loadingTextures.Count > 0) {
                return loadingTextures[new Random().Next() % loadingTextures.Count];
            }

            path = RedirectPathToBackpack(self, path);
            path = OtherSpriteSkins.GetSkinWithPath(self, path, true);
            return orig(self, path);
        }
        private static void SpriteCtorAtlasStringHook(On.Monocle.Sprite.orig_ctor_Atlas_string orig, Sprite self, Atlas atlas, string path) {
            path = RedirectPathToBackpack(atlas, path);
            path = OtherSpriteSkins.GetSkinWithPath(atlas, path, true);
            orig(self, atlas, path);
        }
        #endregion

        //-----------------------------Skins Refresh-----------------------------
        #region
        public static void RefreshSkins(bool Xmls_refresh, bool inGame = true) {
            beforeSkinRefresh?.Invoke(Xmls_refresh, inGame);
            if (!inGame) {
                Player_Skinid_verify = 0;

                string skinName = GetPlayerSkin();
                if (skinName != null) {
                    Player_Skinid_verify = skinConfigs[skinName].hashValues;
                }
            }

            if (Xmls_refresh) {
                LogLevel logLevel = Logger.GetLogLevel("Atlas");
                if (!build_warning)
                    Logger.SetLogLevel("Atlas", LogLevel.Error);

                Xml_records.Clear();
                FailedXml_record.Clear();
                RespriteBank_Reload();

                build_warning = false;
                Logger.SetLogLevel("Atlas", logLevel);
            }
            RefreshSkinValues(null, inGame);
            afterSkinRefresh?.Invoke(Xmls_refresh, inGame);
        }

        private static void PlayerUpdateHook(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);

            // PandorasBox have an function that can generate the second player entity, we don't want to detect it.
            if (Engine.Scene?.Tracker.GetEntity<Player>() != self) {
                return;
            }

            int player_skinid_verify = 0;
            if (GetPlayerSkinName((int)self.Sprite.Mode) != null) {
                player_skinid_verify = (int)self.Sprite.Mode;
            }

            if (Player_Skinid_verify != player_skinid_verify) {
                Player_Skinid_verify = player_skinid_verify;
                RefreshSkins(false);
            }
        }

        #endregion

        //-----------------------------Customize-----------------------------
        #region
        private static void SpriteRenderHook(Action<Sprite> orig, Sprite self) {
            if (self.Active && self.Entity != null) {
                // this line also invoke EntityTweaks.
                CharacterConfig config = CharacterConfig.For(self);

                if (config.HoldableFacingFlipable) {
                    Holdable holdable = self.Entity.Get<Holdable>();
                    if (holdable != null) {

                        DynamicData entityData = DynamicData.For(self.Entity);
                        Vector2 speed = holdable.Holder?.Speed ?? holdable.GetSpeed();

                        if (Math.Abs(speed.X) < 15f) {
                            if (entityData.TryGet("smh_facingBack", out bool? front))
                                if (front == false ? self.Scale.X < 0f : self.Scale.X > 0f)
                                    self.Scale.X *= -1f;

                        } else if ((speed.X > 0f && self.Scale.X < 0f) || (speed.X < 0f && self.Scale.X > 0f)) {
                            self.Scale.X *= -1f;
                            entityData.Set("smh_facingBack", self.Scale.X < 0f);
                        }
                    }
                }
            }
            orig(self);
        }
        #endregion

        //-----------------------------Method-----------------------------
        #region
        /// <summary> 
        /// Copies the animations of origSprite that newSprite missing to newSprite.
        /// </summary>
        public static void PatchSprite(Sprite origSprite, Sprite newSprite) {
            Dictionary<string, Sprite.Animation> newAnims = newSprite.Animations;

            // Shallow copy... sometimes new animations get added mid-update?
            Dictionary<string, Sprite.Animation> oldAnims = new(origSprite.Animations);
            foreach (KeyValuePair<string, Sprite.Animation> animEntry in oldAnims) {
                string origAnimId = animEntry.Key;
                Sprite.Animation origAnim = animEntry.Value;
                if (!newAnims.ContainsKey(origAnimId)) {
                    newAnims[origAnimId] = origAnim;
                }
            }
        }

        public static bool LoadConfigFile<T>(ModAsset skinConfigYaml, out T t) {
            return skinConfigYaml.TryDeserialize(out t);
        }
        public static T searchSkinConfig<T>(string FilePath) {
            if (Everest.Content.Map.TryGetValue(FilePath, out ModAsset configAsset) && configAsset.Type == typeof(AssetTypeYaml)) {
                return configAsset.Deserialize<T>();
            }
            return default(T);
        }
        public static float GetAlpha(Color c) {
            return c.A == 0 ? 0f : c.A / 255f;
        }
        /// <summary> 
        /// A method similar to Color.Multiply, but ignore alpha value
        /// </summary>
        public static Color ColorBlend(Color c1, object obj) {
            if (obj is Color c2 && c2.A != 0) {
                // Restore c2's brightness when as 100% opacity, and assume its brightness if as c1's opacity.
                c2 = c2 * (255f / c2.A) * GetAlpha(c1);
                return new Color(c1.R * c2.R / 255, c1.G * c2.G / 255, c1.B * c2.B / 255, c1.A);
            } else if (obj is float f) {
                if (f > 1f)
                    return Color.Lerp(c1, Color.White, f--);
                else
                    return Color.Lerp(Color.Black, c1, f);
            }
            return c1;
        }
        /// <returns> 
        /// return false if target's RGB over sample
        /// </returns>
        public static bool ColorSplitter(Color target, Color sample, out Color? value) {
            value = null;
            if (target.A == 0 || sample.A == 0) {
                return false;
            }
            target = target * (255f / target.A);
            sample = sample * (255f / sample.A);
            if (target.R > sample.R || target.G > sample.G || target.B > sample.B) {
                return false;
            } else if (target == sample) {
                value = Color.White;
                return true;
            }
            int R = sample.R == 0 ? 0 : target.R * 255 / sample.R;
            int G = sample.G == 0 ? 0 : target.G * 255 / sample.G;
            int B = sample.B == 0 ? 0 : target.B * 255 / sample.B;
            value = new Color(R, G, B);
            return true;
        }
        #endregion
        #region
        public static string getAnimationRootPath(object type) {
            if (type is Sprite sprite) {
                var data = DynamicData.For(sprite).Get<SpriteData>("smh_spriteData");
                if (data != null) {
                    return data.Sources[0].OverridePath ?? data.Sources[0].Path;
                }
                type = (sprite.Has("idle") ? sprite.GetFrame("idle", 0) : sprite.Texture ?? sprite.Animations?.First().Value.Frames[0]).ToString();
            } else if (type is Image image) {
                type = image.Texture.ToString();
            } else {
                type = type.ToString();
            }

            if (type is string path && path != null && path.LastIndexOf("/") >= 0) {
                return path.Remove(path.LastIndexOf("/") + 1);
            }
            return "";
        }
        public static string getAnimationRootPath(Sprite sprite, string id) {
            return sprite.Has(id) ? getAnimationRootPath(sprite.GetFrame(id, 0)) : getAnimationRootPath(sprite);
        }
        public static string getAnimationRootPath(object type, out string returnValue) {
            return returnValue = getAnimationRootPath(type);
        }
        #endregion
        #region
        public static FieldInfo GetFieldPlus(Type type, string name) {
            FieldInfo field = null;
            while (field == null && type != null) {
                field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                // some mods entities works based on vanilla entities, but mods entity possible don't have theis own field.
                type = type.BaseType;
            }
            return field;
        }
        public static T GetFieldPlus<T>(object obj, string name) {
            FieldInfo field = GetFieldPlus(obj.GetType(), name);
            if (field != null && field.GetValue(obj) is T value) {
                return value;
            }
            return default;
        }

        public static bool AssetExists<T>(string path, Atlas atlas = null) {
            if (atlas != null) {
                path = atlas.DataPath + "/" + path;
            }
            if (path.LastIndexOf(".") >= 0) {
                path = path.Remove(path.LastIndexOf("."));
            }
            return Everest.Content.TryGet<T>(path, out ModAsset asset);
        }
        public static bool SpriteExt_TryPlay(Sprite sprite, string id, bool restart = false) {
            if (sprite.Has(id)) {
                sprite.Play(id, restart);
                return true;
            }
            return false;
        }
        public static Stopwatch stopwatch;
        public static void StartDelayTiming() {
            if (stopwatch == null)
                stopwatch = Stopwatch.StartNew();
            else
                stopwatch.Restart();
        }
        public static void OutputDelayTiming() {
            stopwatch.Stop();
            Logger.Log(LogLevel.Info, "SkinModHelper", $"delay: {stopwatch.ElapsedTicks} ticks");
        }

        public static string GetNumberFormat(int number, int ofDigits = 2) {
            string str;
            for (str = number.ToString(); str.Length < ofDigits; str = "0" + str) { }
            return str;
        }

        #endregion
        #region 
        /// <summary> 
        /// Find out if specified textures-set exists under the sprite's inherited path or own path
        /// </summary>
        public static bool GetTexturesOnSprite(Image sprite, string filename, out List<MTexture> textures) {
            textures = null;
            var data = DynamicData.For(sprite).Get<SpriteData>("smh_spriteData");
            if (data == null) {
                string path = getAnimationRootPath(sprite) + filename;
                if (GFX.Game.HasAtlasSubtextures(path)) {
                    textures = GFX.Game.GetAtlasSubtextures(path);
                }
                return textures != null;
            }
            if (data.Atlas == null) {
                return false;
            }
            foreach (SpriteDataSource source in data.Sources) {
                if (!string.IsNullOrEmpty(source.OverridePath) && data.Atlas.HasAtlasSubtextures(source.OverridePath + filename)) {
                    textures = data.Atlas.GetAtlasSubtextures(source.OverridePath + filename);
                    return true;
                }
                if (data.Atlas.HasAtlasSubtextures(source.Path + filename)) {
                    textures = data.Atlas.GetAtlasSubtextures(source.Path + filename);
                    return true;
                }
            }
            return false;
        }
        /// <summary> 
        /// Find out if specified texture exists under the sprite's inherited path or own path
        /// </summary>
        public static bool GetTextureOnSprite(Image sprite, string filename, out MTexture texture) {
            texture = null;
            var data = DynamicData.For(sprite).Get<SpriteData>("smh_spriteData");
            if (data == null) {
                string path = getAnimationRootPath(sprite) + filename;
                if (GFX.Game.Has(path)) {
                    texture = GFX.Game[path];
                }
                return texture != null;
            }
            if (data.Atlas == null) {
                return false;
            }
            foreach (SpriteDataSource source in data.Sources) {
                if (!string.IsNullOrEmpty(source.OverridePath) && data.Atlas.Has(source.OverridePath + filename)) {
                    texture = data.Atlas[source.OverridePath + filename];
                    return true;
                }
                if (data.Atlas.Has(source.Path + filename)) {
                    texture = data.Atlas[source.Path + filename];
                    return true;
                }
            }
            return false;
        }
        /// <summary> 
        /// Find out if specified assets exists under the sprite's inherited path or own path
        /// </summary>
        public static ModAsset GetAssetOnSprite<T>(Image sprite, string filename) {
            var data = DynamicData.For(sprite).Get<SpriteData>("smh_spriteData");
            if (data == null) {
                if (Everest.Content.TryGet(GFX.Game.DataPath + "/" + getAnimationRootPath(sprite) + filename, out var asset) && asset.Type == typeof(T))
                    return asset;
                return null;
            }
            if (data.Atlas == null) {
                return null;
            }
            string path = data.Atlas.DataPath;
            if (!string.IsNullOrEmpty(path)) { path += '/'; }
            foreach (SpriteDataSource source in data.Sources) {
                if (!string.IsNullOrEmpty(source.OverridePath) && Everest.Content.TryGet(path + source.OverridePath + filename, out var asset2) && asset2.Type == typeof(T)) {
                    return asset2;
                }
                if (Everest.Content.TryGet(path + source.Path + filename, out asset2) && asset2.Type == typeof(T)) {
                    return asset2;
                }
            }
            return null;
        }
        public static T GetConfigOnSprite<T>(Image sprite, string filename) {
            ModAsset asset = GetAssetOnSprite<AssetTypeYaml>(sprite, filename);
            if (asset != null) {
                return asset.Deserialize<T>();
            }
            return default(T);
        }
        #endregion
    }
}