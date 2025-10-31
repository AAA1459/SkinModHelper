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
using System.Runtime.CompilerServices;
using System.Collections;

namespace Celeste.Mod.SkinModHelper {
    public class HairConfig {
        #region Ctor / Initialization
        internal const string _ConfigName = "skinConfig/HairConfig";

        private static ConditionalWeakTable<PlayerHair, HairConfig> _Instance = new();

        // Some special states that can attach HairAttrs with it than dashes.
        public const int FeatherIndex = -1;

        // the hair segment. and some others segment...
        public enum SpecialSegment {
            General = 100, Trail = 101, DashPtcl = 102,
            Outline = -101, Flash = -102
        }

        public static Color C_EmptyS = new(255, 255, 255, 0);

        public HairConfig() {
            OnHairUpdate = () => {
                HairColorGrading = null;

                if (attached.Entity is not Player) {
                    lastDashes = GetDashCount(attached.Entity, attached.Sprite);
                }
            };
        }
        public static HairConfig For(PlayerHair target) {
            string rootPath = getAnimationRootPath(target.Sprite);

            if (!_Instance.TryGetValue(target, out HairConfig config) || config.SourcePath != rootPath) {

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
                config.lastDashes = GetDashCount(target.Entity, target.Sprite);

                _Instance.AddOrUpdate(target, config);
            }
            if (target.Entity != config.lastEntity) {
                config.lastEntity = target.Entity;
            }
            return config;
        }
        #endregion

        #region Values
        public int? lastDashes;

        private PlayerHair attached;
        private Entity lastEntity;
        private ModAsset Source;
        private string SourcePath;

        // may be a float. may be a Color.
        public object HairColorGrading;
        public int lastHairCount;

        public List<MTexture> new_bangs;
        public List<MTexture> new_hairs;

        public Action OnHairUpdate;

        private int _AttrDasheslimit = 2;
        public Dictionary<(int, int), Color> ActualHairColors;
        public Dictionary<int, int> ActualHairLengths;
        /// <summary>The <see cref="Vector2"/> here mean both root and end scales, not x,y.</summary>
        public Dictionary<(int, int), Vector2> ActualHairScales;

        public Dictionary<(int, int), float> ActualHairSpeeds; // todo

        [YamlIgnore]
        public bool ColorsActive = true;
        [YamlIgnore]
        public bool LengthsActive = true;
        public bool HairFlashing = false;
        [YamlIgnore]
        public bool HasZeroDashFlash = false;


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
            public float? Speed { get; set; }

            public List<SegmentAttr> SegmentAttrs { get; set; } = new();
            public class SegmentAttr {

                [YamlMember(Alias = "Segment")]
                public string _Segment {
                    get => null; set {
                        if (int.TryParse(value, out int i))
                            Segment = i;
                        else if (Enum.TryParse<SpecialSegment>(value, true, out var result))
                            Segment = (int)result;
                    }
                }
                public int Segment { get; private set; }

                public float? Scale { get; set; }
                public string Color { get; set; }
                public float? Speed { get; set; }
            }

            #region backward compatibility
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
                    [(0, (int)SpecialSegment.General)] = _ZeroDashesColor,
                    [(1, (int)SpecialSegment.General)] = _OneDashesColor,
                    [(2, (int)SpecialSegment.General)] = _TwoDashesColor
                };
            }
            if (HairFlash == false || AssetExists<AssetTypeDirectory>(GFX.Game.RelativeDataPath + getAnimationRootPath(attached.Sprite, "idle") + "ColorGrading"))
                InitHairColor();


            foreach (AttrWithDashes attr in _HairAttrWithDashes.Values) {
                #region ProcessHairColors
                bool isValid = false;
                bool isC_EmptyS;
                foreach (var c_attr in attr.SegmentAttrs) {



                }
                if ((isC_EmptyS = attr.Color == "orig") || RGB_IsMatch(attr.Color)) {
                    InitHairColor();
                    ActualHairColors[(attr.Dashes, (int)SpecialSegment.General)] = isC_EmptyS ? C_EmptyS : Calc.HexToColor(attr.Color);

                    foreach (var c_attr in attr.SegmentAttrs) {
                        if (c_attr.Segment != (int)SpecialSegment.General && ((isC_EmptyS = c_attr.Color == "orig") || RGB_IsMatch(c_attr.Color))) {
                            ActualHairColors[(attr.Dashes, c_attr.Segment)] = isC_EmptyS ? C_EmptyS : Calc.HexToColor(c_attr.Color);
                        }
                    };
                    isValid = true;
                }
                #endregion

                #region ProcessHairLengths
                if (attr.Length != null) {
                    (ActualHairLengths ??= new())[attr.Dashes] = Math.Clamp(attr.Length.Value, 1, MAX_HAIRLENGTH);
                    isValid = true;
                }
                #endregion

                #region ProcessHairScales
                if (attr.Scale != null) {
                    string[] arr = attr.Scale.Split(',', 2, StringSplitOptions.TrimEntries);
                    if (float.TryParse(arr[0], out float scale)) {
                        ActualHairScales ??= new();

                        if (arr.Length < 2 || !float.TryParse(arr[1], out float scale2)) {
                            scale2 = scale;
                        }
                        // The Vector2 here mean both root and end scales, not x,y.
                        ActualHairScales[(attr.Dashes, (int)SpecialSegment.General)] = new(scale, scale2);

                        foreach (var c_attr in attr.SegmentAttrs) {
                            if (c_attr.Scale is float f) {
                                ActualHairScales[(attr.Dashes, c_attr.Segment)] = new(f, f);
                            }
                        };
                        isValid = true;
                    }
                }
                #endregion

                #region ProcessHairSpeeds
                if (attr.Speed is float speed) {
                    (ActualHairSpeeds ??= new())[(attr.Dashes, (int)SpecialSegment.General)] = speed;

                    foreach (var c_attr in attr.SegmentAttrs) {
                        if (c_attr.Speed is float c_speed) {
                            ActualHairSpeeds[(attr.Dashes, c_attr.Segment)] = c_speed;
                        }
                    };
                    isValid = true;
                }
                #endregion
                if (isValid && attr.Dashes > _AttrDasheslimit) { _AttrDasheslimit = attr.Dashes; }
            }
            HasZeroDashFlash = ActualHairColors?.ContainsKey((0, (int)SpecialSegment.Flash)) ?? false;
        }
        public static readonly Color _ZeroDashesColor = Calc.HexToColor("44B7FF");
        public static readonly Color _OneDashesColor = Calc.HexToColor("AC3232");
        public static readonly Color _TwoDashesColor = Calc.HexToColor("FF6DEF");
        #endregion

        #region Build Old Skins Hair Colors
        private List<SkinModHelperOldConfig.HairColor> oldHairColors;
        public void Old_BuildHairColors() {
            ActualHairColors = new Dictionary<(int, int), Color>() {
                [(0, (int)SpecialSegment.General)] = _ZeroDashesColor,
                [(1, (int)SpecialSegment.General)] = _OneDashesColor,
                [(2, (int)SpecialSegment.General)] = _TwoDashesColor
            };

            if (oldHairColors != null) {
                for (int i = 0; i < oldHairColors.Count; i++) {
                    SkinModHelperOldConfig.HairColor hairColor = oldHairColors[i];
                    if (hairColor.Dashes >= 0 && RGB_IsMatch(hairColor.Color)) {
                        ActualHairColors[(hairColor.Dashes, (int)SpecialSegment.General)] = Calc.HexToColor(hairColor.Color);

                        if (_AttrDasheslimit < hairColor.Dashes)
                            _AttrDasheslimit = hairColor.Dashes;
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
            dashes = Math.Min(_AttrDasheslimit, dashes);
        loop:
            if (ActualHairColors.TryGetValue((dashes, (int)SpecialSegment.General), out color)) {
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
            dashes = Math.Min(_AttrDasheslimit, dashes);
        loop:
            if (ActualHairColors.TryGetValue((dashes, (int)SpecialSegment.General), out color)) {
                if ((index < (int)SpecialSegment.General && ActualHairColors.TryGetValue((dashes, index - attached.Sprite.HairCount), out Color color2))
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
            dashes = Math.Min(_AttrDasheslimit, dashes);
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
            int dashes = Math.Min(_AttrDasheslimit, get_dashes.Value);
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
            dashes = Math.Min(_AttrDasheslimit, dashes);
        loop:
            if (ActualHairScales.TryGetValue((dashes, (int)SpecialSegment.General), out scale)) {
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
        public bool GetHairAttrValue<T>(Dictionary<int, T> onAttr, int dashes, out T value) {
            if (onAttr == null) {
                value = default;
                return false;
            }
            dashes = Math.Min(_AttrDasheslimit, dashes);
        loop:
            if (onAttr.TryGetValue(dashes, out value)) {
                return true;

            } else if (dashes <= 2) {
                return false;
            }
            dashes--;
            goto loop;
        }
        public bool GetHairAttrValue<T>(Dictionary<(int, int), T> onAttr, int index, int dashes, out T value) {
            if (onAttr == null) {
                value = default;
                return false;
            }
            dashes = Math.Min(_AttrDasheslimit, dashes);
        loop:
            if (onAttr.TryGetValue((index, dashes), out value)) {
                if (onAttr.TryGetValue((dashes, index - attached.Sprite.HairCount), out T value2) || onAttr.TryGetValue((dashes, index), out value2)) {
                    value = value2;
                }
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
