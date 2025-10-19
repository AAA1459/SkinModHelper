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
        internal const string _DynamicDataKey = "smh_hairConfig";
        internal const string _ConfigName = "skinConfig/HairConfig";

        // Some special states that can attach HairAttrs with it than dashes.
        public const int FeatherIndex = -1;

        // the hair segment. and some others segment...
        public const int GeneralSegment = 100;
        public const int TrailSegment = 101;
        public const int DashPtclSegment = 102;
        public const int OutlineSegment = -101;
        public const int HairFlashSegment = -102;

        public static Color C_EmptyS = new(255, 255, 255, 0);

        public HairConfig() {
        }
        public static HairConfig For(PlayerHair target) {
            DynamicData selfData = DynamicData.For(target);
            HairConfig config = selfData.Get<HairConfig>(_DynamicDataKey);

            string rootPath = getAnimationRootPath(target.Sprite);

            if (config == null || config.SourcePath != rootPath) {

                if (OldConfigCheck(target.Sprite, out string isOld)) {
                    config = new();
                    config.attached = target;
                    config.SourcePath = rootPath;

                    string hairPath = $"{OtherskinConfigs[isOld].OtherSprite_ExPath}/characters/player/";
                    if (GFX.Game.HasAtlasSubtextures(hairPath + "bangs")) {
                        config.new_bangs = GFX.Game.GetAtlasSubtextures(hairPath + "bangs");
                    }

                    if (GFX.Game.HasAtlasSubtextures(hairPath + "hair"))
                        config.new_hairs = GFX.Game.GetAtlasSubtextures(hairPath + "hair");

                    if (target.Entity is Player) {
                        config.oldHairColors = OtherskinOldConfig[isOld].HairColors ?? new();
                        config.HairFlash = false;
                        config.Old_BuildHairColors();
                    }
                } else {
                    ModAsset asset = GetAssetOnSprite<AssetTypeYaml>(target.Sprite, _ConfigName);
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
                target.Border = RGBA_IsMatch(config.OutlineColor) ? Calc.HexToColorWithAlpha(config.OutlineColor) : Color.Black;
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
        private int _HairColorsMaxNum = 2;

        private List<SkinModHelperOldConfig.HairColor> oldHairColors;

        public List<MTexture> new_bangs;
        public List<MTexture> new_hairs;

        [YamlIgnore]
        public Dictionary<(int, int), Color> ActualHairColors;
        // public Dictionary<int, List<Color>> ActualHairColors;


        [YamlIgnore]
        public Dictionary<int, int> ActualHairLengths;
        [YamlIgnore]
        public bool ColorsActive = true;
        [YamlIgnore]
        public bool LengthsActive = true;
        public bool HairFlashing { get => HairFlash && lastEntity is Player player && player.Dashes != 0 && player.hairFlashTimer > 0f; }

        /// <summary>The <see cref="Vector2"/> here mean both root and end scales, not x,y.</summary>
        [YamlIgnore]
        public Dictionary<(int, int), Vector2> ActualHairScales;
        #endregion

        #region Configurable values
        public string OutlineColor { get; set; }

        public bool HairFlash { get; set; } = true;
        public int? HairFloatingDashCount { get; set; }
        public enum HairFlipModes { None, SyncBangs, FacingBangs, FacingPrevHair }
        public HairFlipModes HairFlipMode { get; set; } = HairFlipModes.None;

        [YamlMember(Alias = "BangsOrigin")]
        public string _BangsOrigin {
            get => null; set { BangsOrigin = StringToVector2(value); }
        }
        [YamlMember(Alias = "HairOrigin")]
        public string _HairOrigin {
            get => null; set { HairOrigin = StringToVector2(value); }
        }
        [YamlIgnore]
        public Vector2 BangsOrigin = new Vector2(5f, 5f);
        [YamlIgnore]
        public Vector2 HairOrigin = new Vector2(5f, 5f);


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
        public string BangsOffset {
            get => null; set {
                BangsOrigin += StringToVector2(value);
            }
        }
        public string HairOffset {
            get => null; set {
                HairOrigin += StringToVector2(value);
            }
        }
        #endregion

        #region InitAttrsWithDashes
        public void InitAttrsWithDashes() {

            void InitHairColor() {
                ActualHairColors ??= new Dictionary<(int, int), Color>() {
                    [(0, GeneralSegment)] = _ZeroDashesColor,
                    [(1, GeneralSegment)] = _OneDashesColor,
                    [(2, GeneralSegment)] = _TwoDashesColor
                };
            }
            if (HairFlash == false || AssetExists<AssetTypeDirectory>(GFX.Game.RelativeDataPath + getAnimationRootPath(attached.Sprite, "idle") + "ColorGrading"))
                InitHairColor();


            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                #region ProcessHairColors
                bool isC_EmptyS;
                if ((isC_EmptyS = attr.Color == "orig") || RGB_IsMatch(attr.Color)) {
                    InitHairColor();

                    ActualHairColors[(attr.Dashes, GeneralSegment)] = isC_EmptyS ? C_EmptyS : Calc.HexToColor(attr.Color);

                    if (attr.SegmentAttrs != null)
                        foreach (var c_attr in attr.SegmentAttrs ?? new()) {
                            if (c_attr.Segment != GeneralSegment && ((isC_EmptyS = c_attr.Color == "orig") || RGB_IsMatch(c_attr.Color))) {
                                ActualHairColors[(attr.Dashes, c_attr.Segment)] = isC_EmptyS ? C_EmptyS : Calc.HexToColor(c_attr.Color);
                            }
                        };
                    if (_HairColorsMaxNum < attr.Dashes) { _HairColorsMaxNum = attr.Dashes; }
                }
                #endregion

                #region ProcessHairLengths
                if (attr.Length != null) {
                    ActualHairLengths ??= new();

                    ActualHairLengths[attr.Dashes] = Math.Clamp(attr.Length.Value, 1, MAX_HAIRLENGTH);
                    if (attr.Dashes > _HairLengthsMaxNum) { _HairLengthsMaxNum = attr.Dashes; }
                }
                #endregion

                #region ProcessHairScales
                if (attr.Scale != null) {
                    string[] scales = attr.Scale.Split(',', 2, StringSplitOptions.TrimEntries);
                    if (float.TryParse(scales[0], out float scale)) {
                        ActualHairScales ??= new();

                        if (scales.Length < 2 || !float.TryParse(scales[1], out float scale2)) {
                            scale2 = scale;
                        }
                        // The Vector2 here mean both root and end scales, not x,y.
                        ActualHairScales[(attr.Dashes, GeneralSegment)] = new(scale, scale2);

                        if (attr.SegmentAttrs != null)
                            foreach (var c_attr in attr.SegmentAttrs ?? new()) {
                                if (c_attr.Scale is float f) {
                                    ActualHairScales[(attr.Dashes, c_attr.Segment)] = new(f, f);
                                }
                            };
                        if (attr.Dashes > _HairScalesMaxNum) { _HairScalesMaxNum = attr.Dashes; }
                    }
                }
                #endregion
            }
        }
        public static readonly Color _ZeroDashesColor = Calc.HexToColor("44B7FF");
        public static readonly Color _OneDashesColor = Calc.HexToColor("AC3232");
        public static readonly Color _TwoDashesColor = Calc.HexToColor("FF6DEF");
        #endregion

        #region Build Old Skins Hair Colors
        public void Old_BuildHairColors() {
            ActualHairColors = new Dictionary<(int, int), Color>() {
                [(0, GeneralSegment)] = _ZeroDashesColor,
                [(1, GeneralSegment)] = _OneDashesColor,
                [(2, GeneralSegment)] = _TwoDashesColor
            };

            if (oldHairColors != null) {
                for (int i = 0; i < oldHairColors.Count; i++) {
                    SkinModHelperOldConfig.HairColor hairColor = oldHairColors[i];
                    if (hairColor.Dashes >= 0 && RGB_IsMatch(hairColor.Color)) {
                        ActualHairColors[(hairColor.Dashes, GeneralSegment)] = Calc.HexToColor(hairColor.Color);

                        if (_HairColorsMaxNum < hairColor.Dashes)
                            _HairColorsMaxNum = hairColor.Dashes;
                    }
                }
            }
        }
        #endregion

        #region Method
        public bool Safe_GetHairColor(int dashes, out Color color) {
            if (ActualHairColors == null || !ColorsActive) {
                color = new();
                return false;
            }
            dashes = Math.Min(_HairColorsMaxNum, dashes);
        loop:
            if (ActualHairColors.TryGetValue((dashes, GeneralSegment), out color)) {
                return color != C_EmptyS;

            } else if (dashes <= 0) {
                return false;
            }
            dashes--;
            goto loop;
        }
        public bool Safe_GetHairColor(int index, int dashes, out Color color) {
            if (ActualHairColors == null || !ColorsActive) {
                color = new();
                return false;
            }
            dashes = Math.Min(_HairColorsMaxNum, dashes);
        loop:
            if (ActualHairColors.TryGetValue((dashes, GeneralSegment), out color)) {
                if ((index < GeneralSegment && ActualHairColors.TryGetValue((dashes, index - attached.Sprite.HairCount), out Color color2))
                    || ActualHairColors.TryGetValue((dashes, index), out color2)) {
                    color = color2;
                }
                return color != C_EmptyS;

            } else if (dashes <= 2) {
                return false;
            }
            dashes--;
            goto loop;
        }
        public bool GetHairColorWithSpecified(int index, int dashes, out Color color) {
            if (ActualHairColors == null || !ColorsActive) {
                color = new();
                return false;
            }
            dashes = Math.Min(_HairColorsMaxNum, dashes);
        loop:
            if (ActualHairColors.TryGetValue((dashes, index), out color)) {
                return color != C_EmptyS;

            } else if (dashes <= 2) {
                return false;
            }
            dashes--;
            goto loop;
        }

        public int? GetHairLength(int? get_dashes) {
            if (!LengthsActive || get_dashes == null || ActualHairLengths == null) {
                return null;
            }
            // dashes is -1 for when player into flyFeathers state.
            int dashes = Math.Min(_HairLengthsMaxNum, get_dashes.Value);
        loop:
            if (ActualHairLengths.TryGetValue(dashes, out var length)) {
                return length;
            } else if (dashes <= 2) {
                return null;
            }
            dashes--;
            goto loop;
        }
        public bool GetHairScale(int index, int dashes, out Vector2 scale) {
            if (ActualHairScales == null || index == 0) {
                scale = Vector2.Zero;
                return false;
            }
            dashes = Math.Min(_HairScalesMaxNum, dashes);
        loop:
            if (ActualHairScales.TryGetValue((dashes, GeneralSegment), out scale)) {
                if (ActualHairScales.TryGetValue((dashes, index - attached.Sprite.HairCount), out Vector2 vector) || ActualHairScales.TryGetValue((dashes, index), out vector)) {
                    scale = vector;
                }
                // float2.X mean the root scale, float2.Y mean the end scale.
                float num = scale.Y + (1f - (float)index / (float)(attached.Sprite.HairCount)) * (scale.X - scale.Y);
                scale = new Vector2(float.Round(num * Math.Abs(attached.Sprite.Scale.X), 2), float.Round(num, 2));
                return true;

            } else if (dashes <= 2) {
                return false;
            }
            dashes--;
            goto loop;
        }

        public Vector2 FlipHair(Vector2 scale, int index) {
            if (index > 0) {
                switch (HairFlipMode) {
                    case HairFlipModes.SyncBangs:
                        scale.X *= (float)attached.Facing;
                        break;
                    case HairFlipModes.FacingBangs:
                        float f = attached.Nodes[index].X - attached.Nodes[0].X;
                        scale.X *= (f < 0f ? 1 : -1);
                        break;
                    case HairFlipModes.FacingPrevHair:
                        f = attached.Nodes[index].X - attached.Nodes[index - 1].X;
                        scale.X *= (f < 0f ? 1 : -1);
                        break;
                    default:
                        break;
                }
            }
            return scale;
        }
        #endregion
    }
}
