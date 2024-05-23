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

namespace Celeste.Mod.SkinModHelper {

    //-----------------------------MainConfig-----------------------------
    #region
    public class SkinModHelperConfig {
        #region
        public SkinModHelperConfig() {
        }
        public SkinModHelperConfig(SkinModHelperOldConfig old_config) : this() {
            SkinName = old_config.SkinId;
            SkinDialogKey = old_config.SkinDialogKey ?? SkinName;
            OtherSprite_ExPath = old_config.SkinId.Replace('_', '/');
        }
        #endregion

        //-----------------------------Values-----------------------------
        #region
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
        #endregion
        #region
        public int hashValues = -1;
        #endregion
    }
    #endregion

    //-----------------------------CharacterConfig-----------------------------
    #region
    public class CharacterConfig : Object {
        public CharacterConfig() {
        }

        //-----------------------------Values-----------------------------
        #region
        public bool? BadelineMode { get; set; }
        public bool? SilhouetteMode { get; set; }
        public string LowStaminaFlashColor { get; set; }
        public bool? LowStaminaFlashHair { get; set; }

        public string TrailsColor { get; set; }
        public string DeathParticleColor { get; set; }



        #endregion
        #region
        public Image Target;
        public string SourcePath;

        public bool TweaksTEST;
        public List<Tweak> EntityTweaks { get; set; }
        public class Tweak {
            public string Name { get; set; }
            public string Value { get; set; }

            public bool subTEST;
            public List<Tweak> subTweaks { get; set; }
        }
        #endregion

        //-----------------------------Initialization-----------------------------
        #region
        public static CharacterConfig For(Image target) {
            DynamicData selfData = DynamicData.For(target);
            CharacterConfig config = selfData.Get<CharacterConfig>("smh_characterConfig");

            string rootPath = getAnimationRootPath(target);

            if (config == null || config.SourcePath != rootPath) {
                config = searchSkinConfig<CharacterConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "CharacterConfig") ?? new();

                if (target is PlayerSprite playerSprite) {
                    config.ModeInitialize(playerSprite.Mode);
                }
                config.Target = target;
                config.SourcePath = rootPath;

                if (config.EntityTweaks != null) {
                    config.ValuesTweak(target.Entity, config.EntityTweaks, config.TweaksTEST);
                }
                selfData.Set("smh_characterConfig", config);
            }
            return config;
        }
        public void ModeInitialize(PlayerSpriteMode mode) {
            if (BadelineMode == null) {
                BadelineMode = mode == (PlayerSpriteMode)2 || mode == (PlayerSpriteMode)3;
            }
            if (SilhouetteMode == null) {
                SilhouetteMode = mode == (PlayerSpriteMode)4;
            }
        }
        #endregion
        #region
        private static List<Type> NotCloneList = new List<Type>() {
            typeof(Image)
        };
        public void ValuesTweak(object obj, List<Tweak> tweaks, bool TEST = false) {
            if (obj != null) {
                Type type = obj.GetType();
                if (TEST) {
                    string log = $"{SourcePath}skinConfig/CharacterConfig TEST on {type}:";
                    Type type2 = type;
                    while (type2 != null) {
                        var fs = type2.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList() ?? new();
                        foreach (var f in fs)
                            log = log + "\n" + f;
                        type2 = type2.BaseType;
                    }
                    Logger.Log(LogLevel.Info, "SkinModHelper", log);
                }

                foreach (Tweak t in tweaks) {
                    FieldInfo f = GetFieldPlus(type, t.Name);
                    if (f == null) {
                        Logger.Log(TEST ? LogLevel.Warn : LogLevel.Debug, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n Not found the Instance Field: {type}.{t.Name}");
                    } else {
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
                                    (v as Image).Texture = GFX.Game[SourcePath + t.Value];
                                    continue;
                                }

                                if (f.FieldType == typeof(MTexture)) {
                                    v = GFX.Game[SourcePath + t.Value];
                                } else if (f.FieldType == typeof(Color)) {
                                    v = Calc.HexToColorWithAlpha(t.Value);
                                } else {
                                    v = Convert.ChangeType(t.Value, f.FieldType);
                                }
                                f.SetValue(obj, v);
                            } catch (Exception e) {
                                Logger.Log(LogLevel.Error, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n '{type}.{t.Name}': \n   {e.Message}");
                                v = v2;
                                f.SetValue(obj, v);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
    #endregion

    //-----------------------------HairConfig-----------------------------
    #region
    public class HairConfig {
        #region
        public HairConfig() {
        }
        #endregion

        //-----------------------------Values-----------------------------
        #region
        public string OutlineColor { get; set; }
        public bool HairFlash { get; set; } = true;
        public int? HairFloatingDashCount { get; set; }

        public List<HairColor> HairColors { get; set; }
        public class HairColor {
            public int Dashes { get; set; }
            public string Color { get; set; }
            public List<SegmentsColor> SegmentsColors { get; set; }
            public class SegmentsColor {
                public int Segment { get; set; }
                public string Color { get; set; }
            }
        }

        public List<HairLength> HairLengths { get; set; }
        public class HairLength {
            public int Dashes { get; set; }
            public int Length { get; set; }
        }
        #endregion
        #region
        public string SourcePath;
        public List<SkinModHelperOldConfig.HairColor> oldHairColors;

        public List<MTexture> new_bangs;
        public List<MTexture> new_hairs;

        public Dictionary<int, List<Color>> ActualHairColors;
        public Dictionary<int, int> ActualHairLengths;
        #endregion

        //-----------------------------Initialization-----------------------------
        #region
        public static HairConfig For(PlayerHair target) {
            DynamicData selfData = DynamicData.For(target);
            HairConfig config = selfData.Get<HairConfig>("smh_hairConfig");

            string rootPath = getAnimationRootPath(target.Sprite);

            if (config == null || config.SourcePath != rootPath) {
                string hairPath = rootPath;
                if (OldConfigCheck(target.Sprite, out string isOld)) {
                    config = new();
                    hairPath = $"{OtherskinConfigs[isOld].OtherSprite_ExPath}/characters/player/";

                    if (target.Entity is Player) {
                        config.oldHairColors = OtherskinOldConfig[isOld].HairColors ?? new();
                        config.HairFlash = false;
                        config.Old_BuildHairColors();
                    }
                } else {
                    config = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "HairConfig") ?? new();

                    if (config.HairColors != null || config.HairFlash == false || AssetExists<AssetTypeDirectory>($"{rootPath}ColorGrading", GFX.Game))
                        config.BuildHairColors();
                    config.BuildHairLengths();
                }

                if (GFX.Game.HasAtlasSubtextures(hairPath + "bangs"))
                    config.new_bangs = GFX.Game.GetAtlasSubtextures(hairPath + "bangs");
                if (GFX.Game.HasAtlasSubtextures(hairPath + "hair"))
                    config.new_hairs = GFX.Game.GetAtlasSubtextures(hairPath + "hair");

                config.SourcePath = rootPath;
                selfData.Set("smh_hairConfig", config);
            }
            return config;
        }

        #endregion
        #region
        public void BuildHairColors() {
            Dictionary<int, Color> changed = new();
            Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");

            int maxCount = 2;
            if (this.HairColors != null) {
                foreach (HairColor hairColor in this.HairColors) {
                    if (hairColor.Dashes >= 0 && hairColorRegex.IsMatch(hairColor.Color)) {
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
            Dictionary<int, List<Color>> HairColors = new() {
                [100] = GeneratedHairColors // 100 as each-segment Hair's Default color, or as Player's Dash Color and Silhouette color.
            };
            if (this.HairColors != null) {
                foreach (HairColor hairColor in this.HairColors) {
                    if (hairColor.SegmentsColors != null && changed.ContainsKey(hairColor.Dashes)) {
                        foreach (HairColor.SegmentsColor SegmentColor in hairColor.SegmentsColors) {

                            if (SegmentColor.Segment <= MAX_HAIRLENGTH && hairColorRegex.IsMatch(SegmentColor.Color)) {
                                if (!HairColors.ContainsKey(SegmentColor.Segment)) {
                                    HairColors[SegmentColor.Segment] = new(GeneratedHairColors); // i never knew this work like a the variable or entity of static,  clone it.
                                }
                                HairColors[SegmentColor.Segment][hairColor.Dashes] = Calc.HexToColor(SegmentColor.Color);
                            }
                        }
                    }
                }
            }
            foreach (List<Color> hairColor in HairColors.Values) {
                // Fill upper dash range with the last customized dash color
                for (int i = 3; i < hairColor.Count; i++) {
                    if (!changed.ContainsKey(i)) {
                        hairColor[i] = hairColor[i - 1];
                    }
                }
            }
            ActualHairColors = HairColors;
        }

        public void BuildHairLengths() {
            if (this.HairLengths == null) {
                return;
            }
            Dictionary<int, int> HairLengths = new();

            foreach (HairLength hairLength in this.HairLengths) {
                HairLengths[hairLength.Dashes] = Math.Max(Math.Min(hairLength.Length, MAX_HAIRLENGTH), 1);
            }

            ActualHairLengths = HairLengths;
        }
        #endregion
        #region
        public void Old_BuildHairColors() {
            Dictionary<int, Color> changed = new();
            Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");

            int maxCount = 2;
            if (oldHairColors != null) {
                foreach (SkinModHelperOldConfig.HairColor hairColor in oldHairColors) {
                    if (hairColor.Dashes >= 0 && hairColorRegex.IsMatch(hairColor.Color)) {
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

        //-----------------------------Method-----------------------------
        #region
        public bool Safe_GetHairColor(int index, int dashes, out Color color) {
            if (ActualHairColors == null) {
                color = new();
                return false;
            }
            if (!ActualHairColors.TryGetValue(index, out var colors)) {
                colors = ActualHairColors[100];
            }
            dashes = Math.Max(Math.Min(dashes, colors.Count - 1), 0);
            color = colors[dashes];
            return true;
        }

        public int? GetHairLength(int? get_dashes) {
            if (get_dashes == null || ActualHairLengths == null) {
                return null;
            }
            // dashes is -1 for when player into flyFeathers state.
            int dashes = (int)get_dashes;
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

    //-----------------------------OldConfig-----------------------------
    #region
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
