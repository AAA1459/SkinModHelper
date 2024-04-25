using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using FMOD.Studio;
using System;
using MonoMod.Utils;
using System.Linq;

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




        public string OtherSprite_Path { get; set; }
        public string OtherSprite_ExPath { get; set; }


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
    public class CharacterConfig {
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
        public string SourcePath;
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
                config.SourcePath = rootPath;
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

        public Dictionary<int, List<Color>> ActualHairColors;
        #endregion

        //-----------------------------Initialization-----------------------------
        #region
        public static HairConfig For(PlayerHair target) {
            DynamicData selfData = DynamicData.For(target);
            HairConfig config = selfData.Get<HairConfig>("smh_hairConfig");

            string rootPath = getAnimationRootPath(target.Sprite);

            if (config == null || config.SourcePath != rootPath) {
                if (target.Entity is Player && OldConfigCheck(target.Sprite, out string isOld)) {
                    config = new();
                    config.oldHairColors = OtherskinOldConfig[isOld].HairColors ?? new();
                    config.HairFlash = false;
                    config.Old_BuildHairColors();
                } else {
                    config = searchSkinConfig<HairConfig>($"Graphics/Atlases/Gameplay/{rootPath}skinConfig/" + "HairConfig") ?? new();

                    if (config.HairColors != null || config.HairFlash == false || AssetExists<AssetTypeDirectory>($"{rootPath}ColorGrading", GFX.Game))
                        config.BuildHairColors();
                }
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
        #endregion
        #region
        public void Old_BuildHairColors() {
            List<bool> changed = new(new bool[MAX_DASHES + 1]);
            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };
            if (oldHairColors != null) {
                foreach (SkinModHelperOldConfig.HairColor hairColor in oldHairColors) {
                    Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");
                    if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColorRegex.IsMatch(hairColor.Color)) {
                        GeneratedHairColors[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                        changed[hairColor.Dashes] = true;
                    }
                }
            }
            // Fill upper dash range with the last customized dash color
            for (int i = 3; i <= MAX_DASHES; i++) {
                if (!changed[i]) {
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

        public int? GetHairLength(int? DashCount) {
            if (DashCount == null || HairLengths == null) {
                return null;
            }
            int? HairLength = null;

            DashCount = Math.Max(Math.Min((int)DashCount, MAX_DASHES), -1);
            // -1 for when player into flyFeathers state.

            int getCount = 1;
            foreach (HairLength hairLength in HairLengths) {
                if (DashCount == hairLength.Dashes) {
                    HairLength = hairLength.Length;
                    break;
                } else if (DashCount > 2 && DashCount > hairLength.Dashes && hairLength.Dashes > getCount) {
                    // Autofill HairLength if DashCount over config setted
                    getCount = hairLength.Dashes;
                    HairLength = hairLength.Length;
                }
            }
            if (HairLength != null) {
                HairLength = Math.Max(Math.Min((int)HairLength, MAX_HAIRLENGTH), 1);
            }
            return HairLength;
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
