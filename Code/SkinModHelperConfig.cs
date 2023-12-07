using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using FMOD.Studio;
using System;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public class SkinModHelperConfig {

        public SkinModHelperConfig() {
        }
        public SkinModHelperConfig(SkinModHelperOldConfig old_config) {
            SkinName = old_config.SkinId;
            SkinDialogKey = old_config.SkinDialogKey;
            OtherSprite_ExPath = old_config.SkinId.Replace('_', '/');
        }

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

        public int hashValues;
    }



    public class CharacterConfig {
        public bool? BadelineMode { get; set; }
        public bool? SilhouetteMode { get; set; }
        public string LowStaminaFlashColor { get; set; }
        public bool? LowStaminaFlashHair { get; set; }
    }



    public class HairConfig {
        public string OutlineColor { get; set; }
        public bool? HairFlash { get; set; }

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

        public static Dictionary<int, List<Color>> BuildHairColors(HairConfig build_object, CharacterConfig ModeConfig = null) {
            List<bool> changed = new(new bool[MAX_DASHES + 1]);
            Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");

            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = ModeConfig != null && ModeConfig.BadelineMode == true ? Calc.HexToColor("9B3FB5") : Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };

            if (build_object != null && build_object.HairColors != null) {
                foreach (HairColor hairColor in build_object.HairColors) {
                    if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColorRegex.IsMatch(hairColor.Color)) {
                        GeneratedHairColors[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                        changed[hairColor.Dashes] = true;
                    }
                }
            }

            Dictionary<int, List<Color>> HairColors = new();
            // 0~99 as specify-segment Hair's color.
            // -100~-1 as reverse-order of hair.
            HairColors[100] = GeneratedHairColors; // 100 as each-segment Hair's Default color, or as Player's Dash Color and Silhouette color.

            if (build_object != null && build_object.HairColors != null) {
                foreach (HairColor hairColor in build_object.HairColors) {
                    if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColor.SegmentsColors != null) {

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
                for (int i = 3; i <= MAX_DASHES; i++) {
                    if (!changed[i]) {
                        hairColor[i] = hairColor[i - 1];
                    }
                }
            }

            return HairColors;
        }

        public static int? GetHairLength(HairConfig build_object, int? DashCount) {
            if (DashCount == null || build_object == null) {
                return null;
            }
            int? HairLength = null;

            DashCount = Math.Max(Math.Min((int)DashCount, MAX_DASHES), -1);
            // -1 for when player into flyFeathers state.

            if (build_object.HairLengths != null) {
                foreach (HairLength hairLength in build_object.HairLengths) {
                    if (DashCount == hairLength.Dashes) {
                        HairLength = hairLength.Length;
                        break;
                    } else if (DashCount > 2 && hairLength.Dashes > 1 && DashCount > hairLength.Dashes) {
                        // Autofill HairLength if DashCount over config setted
                        HairLength = hairLength.Length;
                    }
                }
            }
            if (HairLength != null) {
                HairLength = Math.Max(Math.Min((int)HairLength, MAX_HAIRLENGTH), 1);
            }
            return HairLength;
        }
    }


    public class SkinModHelperOldConfig {
        public string SkinId { get; set; }
        public string SkinDialogKey { get; set; }
        public List<HairColor> HairColors { get; set; }

        public class HairColor {
            public int Dashes { get; set; }
            public string Color { get; set; }
        }

        public List<Color> GeneratedHairColors { get; set; }

        public static Dictionary<int, List<Color>> BuildHairColors(SkinModHelperOldConfig config) {
            List<bool> changed = new(new bool[MAX_DASHES + 1]);

            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };

            if (config.HairColors != null) {
                foreach (HairColor hairColor in config.HairColors) {
                    Regex hairColorRegex = new(@"^[a-fA-F0-9]{6}$");
                    if (hairColor.Dashes >= 0 && hairColor.Dashes <= MAX_DASHES && hairColorRegex.IsMatch(hairColor.Color)) {
                        GeneratedHairColors[hairColor.Dashes] = Calc.HexToColor(hairColor.Color);
                        changed[hairColor.Dashes] = true;
                    } else {
                        Logger.Log(LogLevel.Warn, "SkinModHelper", $"Invalid hair color or dash count values provided for {config.SkinId}.");
                    }
                }
            }
            // Fill upper dash range with the last customized dash color
            for (int i = 3; i <= MAX_DASHES; i++) {
                if (!changed[i]) {
                    GeneratedHairColors[i] = GeneratedHairColors[i - 1];
                }
            }

            Dictionary<int, List<Color>> HairColors = new();
            HairColors[100] = GeneratedHairColors;

            return HairColors;
        }
    }
}
