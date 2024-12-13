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
                    config.attached = target;
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
                    config.attached = target;
                    config.SourcePath = rootPath;

                    if (GetTexturesOnSprite(target.Sprite, "bangs", out var textures) && textures[0].ToString() != "characters/player/bangs00")
                        config.new_bangs = textures;
                    if (GetTexturesOnSprite(target.Sprite, "hair", out var textures2) && textures2[0].ToString() != "characters/player/hair00")
                        config.new_hairs = textures2;

                    config.InitAttrsWithDashes();
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
        private PlayerHair attached;
        private Entity lastEntity;
        private ModAsset Source;
        private string SourcePath;

        private int _HairLengthsMaxNum = 2;
        private int _HairScalesMaxNum = 2;

        private List<SkinModHelperOldConfig.HairColor> oldHairColors;

        public List<MTexture> new_bangs;
        public List<MTexture> new_hairs;

        [YamlIgnore]
        public Dictionary<int, List<Color>> ActualHairColors;
        [YamlIgnore]
        public Dictionary<int, int> ActualHairLengths;

        /// <summary>The <see cref="Vector2"/> here mean both root and end scales, not x,y.</summary>
        [YamlIgnore]
        public Dictionary<(int, int?), Vector2> ActualHairScales;
        #endregion
        
        #region Configurable values
        public string OutlineColor { get; set; }
        public bool HairFlash { get; set; } = true;

        public bool HairFlipable { get; set; }
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
            public string Scale { get; set; }

            public List<SegmentAttr> SegmentAttrs { get; set; }
            public class SegmentAttr {
                public int Segment { get; set; }
                public float? Scale { get; set; }
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

        #region InitAttrsWithDashes
        public void InitAttrsWithDashes() {
            int ColorsMaxNum = 2;
            Dictionary<int, Color> Colors = new();
            Dictionary<int, int> Lengths = new();

            // The Vector2 here mean both root and end scales, not x,y.
            Dictionary<(int, int?), Vector2> Scales = new();

            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                if (attr.Dashes >= 0 && attr.Color != null && RGB_Regex.IsMatch(attr.Color)) {
                    Colors[attr.Dashes] = Calc.HexToColor(attr.Color);
                    if (ColorsMaxNum < attr.Dashes)
                        ColorsMaxNum = attr.Dashes;
                }
                if (attr.Length != null && attr.Length.Value > 0) {
                    Lengths[attr.Dashes] = Math.Min(attr.Length.Value, MAX_HAIRLENGTH);
                    if (attr.Dashes > _HairLengthsMaxNum) { _HairLengthsMaxNum = attr.Dashes; }
                }
                if (attr.Scale != null) {
                    string[] scales = attr.Scale.Split(',', 2, StringSplitOptions.TrimEntries);
                    if (float.TryParse(scales[0], out float scale)) {
                        if (scales.Length < 2 || !float.TryParse(scales[1], out float scale2)) {
                            scale2 = scale;
                        }
                        Scales[(attr.Dashes, null)] = new(scale, scale2);
                        if (attr.SegmentAttrs != null) {
                            foreach (SegmentAttr attr2 in attr.SegmentAttrs) {
                                if (attr2.Scale is float f) {
                                    Scales[(attr.Dashes, attr2.Segment)] = new(f, f);
                                }
                            }
                        }
                        if (attr.Dashes > _HairScalesMaxNum) { _HairScalesMaxNum = attr.Dashes; }
                    }
                }
            }
            bool ForceColors = HairFlash == false || AssetExists<AssetTypeDirectory>(GFX.Game.RelativeDataPath + getAnimationRootPath(attached.Sprite, "idle") + "ColorGrading");
            if ((Colors.Count > 0 || ForceColors) && !(smh_Settings.PlayerSkinHairColorsDisabled && attached.Entity is Player))
                HairColorsProcess(Colors, ColorsMaxNum);
            if (Lengths.Count > 0 && !(smh_Settings.PlayerSkinHairLengthsDisabled && attached.Entity is Player))
                ActualHairLengths = Lengths;
            if (Scales.Count > 0)
                ActualHairScales = Scales;
        }

        #region ProcessHairColors...
        public void HairColorsProcess(Dictionary<int, Color> Colors, int maxCount) {
            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[maxCount + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };
            foreach (var keyValue in Colors) {
                GeneratedHairColors[keyValue.Key] = keyValue.Value;
            }

            // 0~99 as specify-segment Hair's color.
            // -100~-1 as reverse-order of hair.
            Dictionary<int, List<Color>> hairColors = new() {
                [100] = GeneratedHairColors // 100 as each-segment Hair's Default color, or as Player's Dash Color and Silhouette color.
            };
            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                if (!Colors.ContainsKey(attr.Dashes)) {
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
                    if (!Colors.ContainsKey(i)) {
                        hairColor[i] = hairColor[i - 1];
                    }
                }
            }
            ActualHairColors = hairColors;
        }
        #endregion

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
            if (ActualHairColors == null || dashes < 0) {
                color = new();
                return false;
            }
            var colors = ActualHairColors[100];
            color = colors[Calc.Clamp(dashes, 0, colors.Count - 1)];
            return true;
        }
        public bool Safe_GetHairColor(int index, int dashes, out Color color) {
            if (ActualHairColors == null || dashes < 0) {
                color = new();
                return false;
            }
            if (!ActualHairColors.TryGetValue(index - attached.Sprite.HairCount, out var colors) && !ActualHairColors.TryGetValue(index, out colors)) {
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
        public bool GetHairScale(int index, int dashes, out Vector2 scale) {
            if (ActualHairScales == null || index == 0) {
                scale = Vector2.Zero;
                return false;
            }
            dashes = Math.Min(_HairScalesMaxNum, dashes);
            while (dashes > 2 && !ActualHairScales.ContainsKey((dashes, null))) {
                dashes--;
            }
            if (ActualHairScales.TryGetValue((dashes, null), out Vector2 float2)) {
                if (ActualHairScales.TryGetValue((dashes, index - attached.Sprite.HairCount), out Vector2 float2b) || ActualHairScales.TryGetValue((dashes, index), out float2b)) {
                    float2 = float2b;
                }
                // float2.X mean the root scale, float2.Y mean the end scale.
                float num = float2.X + ((float)index / (float)(attached.Sprite.HairCount - 1)) * (float2.Y - float2.X);
                scale = new Vector2(num * Math.Abs(attached.Sprite.Scale.X), num);
                return true;
            }
            scale = Vector2.Zero;
            return false;
        }
        #endregion
    }
}
