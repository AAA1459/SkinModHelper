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
using static Celeste.Mod.SkinModHelper.HairConfig.AttrWithDashes;

namespace Celeste.Mod.SkinModHelper {
    public class HairConfig {
        #region Ctor / Initialization
        public const int FeatherIndexInAttrs = -1;
        public const int GeneralSegmentIndex = 100;
        public HairConfig() { 
        }
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
                        if (!smh_Settings.PlayerSkinHairColorsDisabled)
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

                    if (!(smh_Settings.PlayerSkinHairColorsDisabled && target.Entity is Player)) {
                        bool ForceGenerated = config.HairFlash == false || AssetExists<AssetTypeDirectory>(GFX.Game.RelativeDataPath + getAnimationRootPath(target.Sprite, "idle") + "ColorGrading");
                        config.BuildHairColors(ForceGenerated);
                    }
                    if (!(smh_Settings.PlayerSkinHairLengthsDisabled && target.Entity is Player)) {
                        config.BuildHairLengths();
                    }
                }
                target.Border = config.OutlineColor != null && RGB_Regex.IsMatch(config.OutlineColor) ? Calc.HexToColor(config.OutlineColor) : Color.Black;
                selfData.Set("smh_hairConfig", config);
            }
            if (target.Entity != config.lastEntity) {
                config.lastEntity = target.Entity;
            }
            return config;
        }

        #endregion

        #region Values
        private PlayerHair Target;
        private Entity lastEntity;
        private ModAsset Source;
        private string SourcePath;

        private List<SkinModHelperOldConfig.HairColor> oldHairColors;

        public List<MTexture> new_bangs;
        public List<MTexture> new_hairs;

        [YamlIgnore]
        public Dictionary<int, List<Color>> ActualHairColors;

        private int _HairLengthsMaxNum = 2;
        [YamlIgnore]
        public Dictionary<int, int> ActualHairLengths;
        #endregion

        #region Configurable values
        public string OutlineColor { get; set; }
        public bool HairFlash { get; set; } = true;
        public int? HairFloatingDashCount { get; set; }

        public List<AttrWithDashes> HairAttrWithDashes {
            get => null; // Just for deserialization to recognize this property, don't use it here
            set {
                foreach (var attr in value) {
                    if (_HairAttrWithDashes.TryGetValue(attr.Dashes, out var attr2)) {
                        attr.Color ??= attr2.Color;
                        attr.Length ??= attr2.Length;
                    }
                    _HairAttrWithDashes[attr.Dashes] = attr;
                }
            }
        }
        [YamlIgnore]
        public Dictionary<int, AttrWithDashes> _HairAttrWithDashes = new();

        public class AttrWithDashes {
            public AttrWithDashes() { } // Exists to sure deserialization works, but don't use this in anywhere.
            public AttrWithDashes(int dashes) { Dashes = dashes; }
            public int Dashes { get; set; }
            public string Color { get; set; }
            public int? Length { get; set; }

            public List<SegmentAttr> SegmentAttrs { get; set; }
            public class SegmentAttr {
                public int Segment { get; set; }
                public string Color { get; set; }
            }

            #region backward compatibility
            public string iSegmentsColors {
                get => null; // Just for deserialization to recognize this property, don't use it here
                set {
                    SegmentAttrs = new();
                    string[] colors = value.Split('|', StringSplitOptions.TrimEntries);
                    for (int i = 0; i < colors.Length; i++) {
                        if (colors[i] == "x")
                            continue;
                        SegmentAttrs.Add(new SegmentAttr() { Segment = i, Color = colors[i] });
                    }
                }
            }
            public List<SegmentAttr> SegmentsColors { get => SegmentAttrs; set => SegmentAttrs = value; } // name for backward compatibility
            #endregion
        }
        #endregion
        #region backward compatibility
        public List<AttrWithDashes> HairColors { get => HairAttrWithDashes; set => HairAttrWithDashes = value; }
        public string iHairColors {
            get => null; // Just for deserialization to recognize this property, don't use it here
            set {
                string[] colors = value.Split('|', StringSplitOptions.TrimEntries);
                for (int i = 0; i < colors.Length; i++) {
                    if (colors[i] == "x")
                        continue;
                    if (!_HairAttrWithDashes.ContainsKey(i))
                        _HairAttrWithDashes[i] = new(i);
                    _HairAttrWithDashes[i].Color = colors[i];
                }
            }
        }
        public string iHairLengths {
            get => null; // Just for deserialization to recognize this property, don't use it here
            set {
                string[] lengths = value.Split('|', StringSplitOptions.TrimEntries);
                for (int i = 0; i < lengths.Length; i++) {
                    if (lengths[i] == "x" || !int.TryParse(lengths[i], out int length) || length < 1)
                        continue;
                    if (!_HairAttrWithDashes.ContainsKey(i))
                        _HairAttrWithDashes[i] = new(i);
                    _HairAttrWithDashes[i].Length = length;
                }
            }
        }
        public List<HairLength> HairLengths {
            get => null; // Just for deserialization to recognize this property, don't use it here
            set {
                foreach (var item in value) {
                    if (!_HairAttrWithDashes.ContainsKey(item.Dashes))
                        _HairAttrWithDashes[item.Dashes] = new(item.Dashes);
                    _HairAttrWithDashes[item.Dashes].Length = item.Length;
                }
            }
        }
        public class HairLength {
            public int Dashes { get; set; }
            public int Length { get; set; }
        }

        #endregion

        #region Build Hair Colors
        public void BuildHairColors(bool ForceGenerated) {
            int maxCount = 2;
            Dictionary<int, Color> changed = new();
            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                if (attr.Dashes >= 0 && attr.Color != null && RGB_Regex.IsMatch(attr.Color)) {
                    changed[attr.Dashes] = Calc.HexToColor(attr.Color);
                    if (maxCount < attr.Dashes)
                        maxCount = attr.Dashes;
                }
            }
            if (changed.Count == 0 && !ForceGenerated) {
                return;
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
            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                if (!changed.ContainsKey(attr.Dashes)) {
                    continue;
                }
                if (attr.SegmentAttrs != null) {
                    foreach (SegmentAttr attr2 in attr.SegmentAttrs) {
                        if (attr2.Segment <= MAX_HAIRLENGTH && attr2.Color != null && RGB_Regex.IsMatch(attr2.Color)) {

                            if (!hairColors.ContainsKey(attr2.Segment)) {
                                hairColors[attr2.Segment] = new(GeneratedHairColors); // i never knew this work like a the variable or entity of static,  clone it.
                            }
                            hairColors[attr2.Segment][attr.Dashes] = Calc.HexToColor(attr2.Color);
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
            if (_HairAttrWithDashes.Count == 0) {
                return;
            }
            Dictionary<int, int> hairLengths = new();
            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                if (!attr.Length.HasValue || attr.Length.Value < 1)
                    continue;
                hairLengths[attr.Dashes] = Math.Min(attr.Length.Value, MAX_HAIRLENGTH);
                if (attr.Dashes > _HairLengthsMaxNum)
                    _HairLengthsMaxNum = attr.Dashes;
            }
            if (hairLengths.Count == 0) {
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
        public bool Safe_GetHairColor(int dashes, out Color color) {
            if (ActualHairColors == null) {
                color = new();
                return false;
            }
            var colors = ActualHairColors[100];
            color = colors[Calc.Clamp(dashes, 0, colors.Count - 1)];
            return true;
        }
        public bool Safe_GetHairColor(int index, int revIndex, int dashes, out Color color) {
            if (ActualHairColors == null) {
                color = new();
                return false;
            }
            if (!ActualHairColors.TryGetValue(revIndex, out var colors) && !ActualHairColors.TryGetValue(index, out colors)) {
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
            int dashes = Math.Min(_HairLengthsMaxNum, get_dashes.Value);
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
}
