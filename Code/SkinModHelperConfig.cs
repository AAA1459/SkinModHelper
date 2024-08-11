using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using FMOD.Studio;
using System;
using MonoMod.Utils;
using System.Linq;
using System.IO;
using System.Reflection;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.PlayerSkinSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;
using AsmResolver.IO;
using static Celeste.Mod.SkinModHelper.CharacterConfig;

namespace Celeste.Mod.SkinModHelper {
    #region SkinModHelperConfig
    public class SkinModHelperConfig {
        #region Ctor
        public SkinModHelperConfig() {
        }
        public SkinModHelperConfig(SkinModHelperOldConfig old_config) : this() {
            SkinName = old_config.SkinId;
            SkinDialogKey = old_config.SkinDialogKey ?? SkinName;
            OtherSprite_ExPath = old_config.SkinId.Replace('_', '/');
        }
        #endregion

        #region Values
        public string SkinName { get; set; }
        public bool Player_List { get; set; }
        public bool Silhouette_List { get; set; }
        public bool? General_List { get; set; }


        public bool JungleLanternMode = false;
        public string Character_ID { get; set; }


        public string OtherSprite_Path {
            set {
                if (value != null) {
                    value = value.Replace("\\", "/");
                    if (value.EndsWith("/"))
                        value = value.Remove(value.Length - 1);
                }
                _OtherSprite_Path = value;
            }
            get { return _OtherSprite_Path; }
        }
        private string _OtherSprite_Path;

        public string OtherSprite_ExPath {
            set {
                if (value != null) {
                    value = value.Replace("\\", "/");
                    if (value.EndsWith("/"))
                        value = value.Remove(value.Length - 1);
                }
                _OtherSprite_ExPath = value;
            }
            get { return _OtherSprite_ExPath; }
        }
        private string _OtherSprite_ExPath;


        public string SkinDialogKey { get; set; }
        public string hashSeed { get; set; }
        public string Mod { get; set; }

        public int hashValues = -1;
        #endregion
    }
    #endregion

    #region CharacterConfig
    public class CharacterConfig {
        #region Ctor / Initialization
        public CharacterConfig() {
        }

        public static CharacterConfig For(Image target) {
            DynamicData selfData = DynamicData.For(target);
            CharacterConfig config = selfData.Get<CharacterConfig>("smh_characterConfig");

            string rootPath = getAnimationRootPath(target);

            if (config == null || config.SourcePath != rootPath) {
                ModAsset asset = GetAssetOnSprite<AssetTypeYaml>(target, "skinConfig/CharacterConfig");
                config = AssetIntoConfig<CharacterConfig>(asset) ?? new();
                config.Source = asset;
                config.Target = target;
                config.SourcePath = rootPath;

                if (target is PlayerSprite playerSprite)
                    config.ModeInitialize(playerSprite.Mode);

                selfData.Set("smh_characterConfig", config);
            }
            if (target.Entity != config.lastEntity) {
                config.lastEntity = target.Entity;
                if (config.EntityTweaks != null && target is Sprite)
                    // Avoid multiple EntityTweaks works, make sure this target is the first of its entity. 
                    if (target == target.Entity?.Get<Sprite>())
                        config.ValuesTweak(target.Entity, config.EntityTweaks, config.TweaksTEST);
            }
            return config;
        }
        public void ModeInitialize(PlayerSpriteMode mode) {
            BadelineMode ??= mode == (PlayerSpriteMode)2 || mode == (PlayerSpriteMode)3;
            SilhouetteMode ??= mode == (PlayerSpriteMode)4;
        }
        #endregion

        #region Values
        public bool? BadelineMode { get; set; }
        public bool? SilhouetteMode { get; set; }

        public string LowStaminaFlashColor { get; set; }
        public bool LowStaminaFlashHair { get; set; }
        public bool HoldableFacingFlipable { get; set; }

        public string TrailsColor { get; set; }
        public string DeathParticleColor { get; set; }


        #endregion

        #region Other Values 
        public Image Target;
        public Entity lastEntity;
        public ModAsset Source;
        public string SourcePath;


        public bool TweaksTEST;
        public List<Tweak> EntityTweaks { get; set; }
        public class Tweak {
            public string Name { get; set; }
            public string Value { get; set; }
            public string LimitOnType { get; set; }

            public bool subTEST;
            public List<Tweak> subTweaks { get; set; }
        }
        #endregion

        #region EntityTweaks Method
        private static List<Type> NotCloneList = new List<Type>() {
            typeof(Image)
        };
        public void ValuesTweak(object obj, List<Tweak> tweaks, bool TEST = false) {
            if (obj == null) {
                return;
            }
            Type type = obj.GetType();
            if (TEST) {
                string log = $"{SourcePath}skinConfig/CharacterConfig TEST on {type}:";
                Type type2 = type;
                while (type2 != null) {
                    FieldInfo[] fs = type2.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fs != null) {
                        for (int i = 0; i < fs.Length; i++) {
                            FieldInfo f = fs[i];
                            log = log + "\n" + (f.FieldType.IsEnum ? "IsEnum " : "") + f;
                        }
                    }
                    type2 = type2.BaseType;
                }
                Logger.Log(LogLevel.Info, "SkinModHelper", log);
            }

            for (int i = 0; i < tweaks.Count; i++) {
                Tweak t = tweaks[i];
                if (t.LimitOnType != null) {
                    bool match = true;
                    Type type2 = type;
                    while (t.LimitOnType != type2.FullName) {
                        if (match = t.LimitOnType == type2?.FullName)
                            break;
                        if ((type2 = type2.BaseType) == null)
                            break;
                    }
                    if (!match)
                        continue;
                }
                FieldInfo f = GetFieldPlus(type, t.Name);
                if (f == null) {
                    Logger.Log(TEST ? LogLevel.Warn : LogLevel.Info, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n Not found the Instance Field: {type}.{t.Name}");
                    continue;
                }

                object v = f.GetValue(obj);
                if (t.subTweaks != null) {
                    // Clone it first before modify e.g ParticleType.
                    if (!NotCloneList.Contains(f.FieldType)) {
                        v = CloneMethod.Invoke(v, null);
                    }

                    ValuesTweak(v, t.subTweaks, t.subTEST);
                    f.SetValue(obj, v);
                    continue;
                }

                if (t.Value != null) {
                    object v2 = v;
                    try {
                        // Check field type instead of value, for works even field's value is null.
                        if (f.FieldType == typeof(Sprite)) {
                            GFX.SpriteBank.CreateOn(v as Sprite, t.Value);
                            continue;
                        }
                        if (f.FieldType == typeof(Image)) {
                            if (GetTextureOnSprite(Target, t.Value, out var texture))
                                (v as Image).Texture = texture;
                            else
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n texture {SourcePath}{t.Value} does not exist");
                            continue;
                        }

                        if (f.FieldType == typeof(MTexture)) {
                            if (GetTextureOnSprite(Target, t.Value, out var texture2))
                                v = texture2;
                            else
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n texture {SourcePath}{t.Value} does not exist");
                        } else if (f.FieldType == typeof(Color)) {
                            v = Calc.HexToColorWithAlpha(t.Value);
                        } else if (f.FieldType.IsEnum) {
                            if (int.TryParse(t.Value, out int v3)) // string value cannot convert to enum, but int value can.
                                v = v3;
                            else
                                Logger.Log(LogLevel.Error, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n '{f.FieldType} {type}.{t.Name}' IsEnum, but its new value is not number");
                        } else
                            v = Convert.ChangeType(t.Value, f.FieldType);

                        f.SetValue(obj, v);
                    } catch (Exception e) {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n '{f.FieldType} {type}.{t.Name}': \n   {e.Message}");
                        v = v2;
                        f.SetValue(obj, v);
                    }
                }
            }
        }
        #endregion
    }
    #endregion

    #region HairConfig
    public class HairConfig {
        #region Ctor / Initialization
        public HairConfig() { }
        public static HairConfig For(PlayerHair target) {
            DynamicData selfData = DynamicData.For(target);
            HairConfig config = selfData.Get<HairConfig>("smh_hairConfig");

            string rootPath = getAnimationRootPath(target.Sprite);

            if (config == null || config.SourcePath != rootPath) {

                if (OldConfigCheck(target.Sprite, out string isOld)) {
                    config = new();
                    config.Target = target;
                    config.SourcePath = rootPath;

                    string hairPath = $"{OtherskinConfigs[isOld].OtherSprite_ExPath}/characters/player/";
                    if (GFX.Game.HasAtlasSubtextures(hairPath + "bangs"))
                        config.new_bangs = GFX.Game.GetAtlasSubtextures(hairPath + "bangs");
                    if (GFX.Game.HasAtlasSubtextures(hairPath + "hair"))
                        config.new_hairs = GFX.Game.GetAtlasSubtextures(hairPath + "hair");

                    if (target.Entity is Player) {
                        config.oldHairColors = OtherskinOldConfig[isOld].HairColors ?? new();
                        config.HairFlash = false;
                        if (!SkinsSystem.Settings.PlayerSkinHairColorsDisabled)
                            config.Old_BuildHairColors();
                    }
                } else {
                    ModAsset asset = GetAssetOnSprite<AssetTypeYaml>(target.Sprite, "skinConfig/HairConfig");
                    config = AssetIntoConfig<HairConfig>(asset) ?? new();
                    config.Source = asset;
                    config.Target = target;
                    config.SourcePath = rootPath;

                    if (GetTexturesOnSprite(target.Sprite, "bangs", out var textures) && textures[0].ToString() != "characters/player/bangs00")
                        config.new_bangs = textures;
                    if (GetTexturesOnSprite(target.Sprite, "hair", out var textures2) && textures2[0].ToString() != "characters/player/hair00")
                        config.new_hairs = textures2;

                    if (!(SkinsSystem.Settings.PlayerSkinHairColorsDisabled && target.Entity is Player)) {
                        bool ForceGenerated = config.HairFlash == false || AssetExists<AssetTypeDirectory>(getAnimationRootPath(target.Sprite, "idle") + "ColorGrading", GFX.Game);
                        config.BuildHairColors(ForceGenerated);
                    }
                    if (!(SkinsSystem.Settings.PlayerSkinHairLengthsDisabled && target.Entity is Player)) {
                        config.BuildHairLengths();
                    }
                }
                selfData.Set("smh_hairConfig", config);
            }
            if (target.Entity != config.lastEntity) {
                config.lastEntity = target.Entity;
            }
            return config;
        }

        #endregion

        #region Values
        public string OutlineColor { get; set; }
        public bool HairFlash { get; set; } = true;
        public int? HairFloatingDashCount { get; set; }

        public string iHairColors { get; set; }
        public List<HairColor> HairColors { get; set; }
        public class HairColor {
            public int Dashes { get; set; }
            public string Color { get; set; }

            public string iSegmentsColors { get; set; }
            public List<SegmentsColor> SegmentsColors { get; set; }
            public class SegmentsColor {
                public int Segment { get; set; }
                public string Color { get; set; }
            }
        }

        public string iHairLengths { get; set; }
        public List<HairLength> HairLengths { get; set; }
        public class HairLength {
            public int Dashes { get; set; }
            public int Length { get; set; }
        }
        #endregion

        #region Other Values
        public PlayerHair Target;
        public Entity lastEntity;
        public ModAsset Source;
        public string SourcePath;
        public List<SkinModHelperOldConfig.HairColor> oldHairColors;

        public List<MTexture> new_bangs;
        public List<MTexture> new_hairs;

        public Dictionary<int, List<Color>> ActualHairColors;
        public Dictionary<int, int> ActualHairLengths;
        #endregion

        #region Build Hair Colors
        public void BuildHairColors(bool ForceGenerated) {
            Dictionary<int, Color> changed = new();

            int maxCount = 2;
            if (iHairColors != null) {
                string[] colors = iHairColors.Split('|', StringSplitOptions.TrimEntries);
                for (int i = 0; i < colors.Length; i++) {
                    if (colors[i] == "x")
                        continue;
                    if (RGB_Regex.IsMatch(colors[i])) {
                        changed[i] = Calc.HexToColor(colors[i]);
                    }
                }
                maxCount = Math.Max(colors.Length - 1, 2);
            } else if (HairColors == null && !ForceGenerated) {
                return;
            }
            if (HairColors != null) {
                for (int i = 0; i < HairColors.Count; i++) {
                    HairColor hairColor = HairColors[i];
                    if (hairColor.Dashes >= 0 && hairColor.Color != null && RGB_Regex.IsMatch(hairColor.Color)) {
                        changed[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                        if (maxCount < hairColor.Dashes)
                            maxCount = hairColor.Dashes;
                    }
                }
            }

            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[maxCount + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };
            foreach (var keyValue in changed) {
                GeneratedHairColors[keyValue.Key] = keyValue.Value;
            }

            // 0~99 as specify-segment Hair's color.
            // -100~-1 as reverse-order of hair.
            Dictionary<int, List<Color>> hairColors = new() {
                [100] = GeneratedHairColors // 100 as each-segment Hair's Default color, or as Player's Dash Color and Silhouette color.
            };
            if (HairColors != null) {
                for (int i = 0; i < HairColors.Count; i++) {
                    if (!changed.ContainsKey(HairColors[i].Dashes)) {
                        continue;
                    }
                    HairColor hairColor = HairColors[i];
                    if (hairColor.iSegmentsColors != null) {
                        string[] colors = hairColor.iSegmentsColors.Split('|', StringSplitOptions.TrimEntries);
                        for (int j = 0; j < colors.Length && j < MAX_HAIRLENGTH; j++) {
                            if (colors[j] == "x")
                                continue;
                            if (RGB_Regex.IsMatch(colors[j])) {
                                if (!hairColors.ContainsKey(j)) {
                                    hairColors[j] = new(GeneratedHairColors);
                                }
                                hairColors[j][hairColor.Dashes] = Calc.HexToColor(colors[j]);
                            }
                        }
                    }
                    if (hairColor.SegmentsColors != null) {
                        for (int j = 0; j < hairColor.SegmentsColors.Count; j++) {
                            HairColor.SegmentsColor SegmentColor = hairColor.SegmentsColors[j];
                            if (SegmentColor.Segment <= MAX_HAIRLENGTH && SegmentColor.Color != null && RGB_Regex.IsMatch(SegmentColor.Color)) {

                                if (!hairColors.ContainsKey(SegmentColor.Segment)) {
                                    hairColors[SegmentColor.Segment] = new(GeneratedHairColors); // i never knew this work like a the variable or entity of static,  clone it.
                                }
                                hairColors[SegmentColor.Segment][hairColor.Dashes] = Calc.HexToColor(SegmentColor.Color);
                            }
                        }
                    }
                }
            }
            foreach (List<Color> hairColor in hairColors.Values) {
                // Fill upper dash range with the last customized dash color
                for (int i = 3; i < hairColor.Count; i++) {
                    if (!changed.ContainsKey(i)) {
                        hairColor[i] = hairColor[i - 1];
                    }
                }
            }
            ActualHairColors = hairColors;
        }
        #endregion 
        #region Build Hair Lengths
        public void BuildHairLengths() {
            Dictionary<int, int> hairLengths = new();
            if (iHairLengths != null) {
                string[] lengths = iHairLengths.Split('|', StringSplitOptions.TrimEntries);
                for (int i = 0; i < lengths.Length; i++) {
                    if (lengths[i] == "x")
                        continue;
                    if (int.TryParse(lengths[i], out int length)) {
                        hairLengths[i] = Calc.Clamp(length, 1, MAX_HAIRLENGTH);
                    }
                }
            }
            if (HairLengths != null) {
                for (int i = 0; i < HairLengths.Count; i++) {
                    HairLength hairLength = HairLengths[i];
                    hairLengths[hairLength.Dashes] = Calc.Clamp(hairLength.Length, 1, MAX_HAIRLENGTH);
                }
            }
            if (hairLengths.Count < 1) {
                return;
            }
            ActualHairLengths = hairLengths;
        }
        #endregion

        #region Build Old Skins Hair Colors
        public void Old_BuildHairColors() {
            Dictionary<int, Color> changed = new();

            int maxCount = 2;
            if (oldHairColors != null) {
                for (int i = 0; i < oldHairColors.Count; i++) {
                    SkinModHelperOldConfig.HairColor hairColor = oldHairColors[i];
                    if (hairColor.Dashes >= 0 && RGB_Regex.IsMatch(hairColor.Color)) {
                        changed[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                        if (maxCount < hairColor.Dashes)
                            maxCount = hairColor.Dashes;
                    }
                }
            }

            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[maxCount + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };
            foreach (var keyValue in changed) {
                GeneratedHairColors[keyValue.Key] = keyValue.Value;
            }

            // Fill upper dash range with the last customized dash color
            for (int i = 3; i < GeneratedHairColors.Count; i++) {
                if (!changed.ContainsKey(i)) {
                    GeneratedHairColors[i] = GeneratedHairColors[i - 1];
                }
            }

            Dictionary<int, List<Color>> HairColors = new() {
                [100] = GeneratedHairColors
            };
            ActualHairColors = HairColors;
        }
        #endregion

        #region Method
        public bool Safe_GetHairColor(int index, int dashes, out Color color) {
            if (ActualHairColors == null) {
                color = new();
                return false;
            }
            if (!ActualHairColors.TryGetValue(index, out var colors)) {
                colors = ActualHairColors[100];
            }
            color = colors[Calc.Clamp(dashes, 0, colors.Count - 1)];
            return true;
        }

        public int? GetHairLength(int? get_dashes) {
            if (get_dashes == null || ActualHairLengths == null) {
                return null;
            }
            // dashes is -1 for when player into flyFeathers state.
            int dashes = get_dashes ?? 0;
            while (dashes > 2 && !ActualHairLengths.ContainsKey(dashes)) {
                dashes--;
            }
            if (ActualHairLengths.TryGetValue(dashes, out var length)) {
                return length;
            }
            return null;
        }
        #endregion
    }
    #endregion

    #region SkinModHelperOldConfig
    public class SkinModHelperOldConfig {
        public string SkinId { get; set; }
        public string SkinDialogKey { get; set; }
        public List<HairColor> HairColors { get; set; }

        public class HairColor {
            public int Dashes { get; set; }
            public string Color { get; set; }
        }

        public List<Color> GeneratedHairColors { get; set; }
    }
    #endregion
}
