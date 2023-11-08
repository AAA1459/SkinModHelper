using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;

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
    }



    public class HairConfig {
        public string OutlineColor { get; set; }
        public bool? HairFlash { get; set; }

        public List<HairColor> HairColors { get; set; }
        public class HairColor {
            public int Dashes { get; set; }
            public string Color { get; set; }
        }

        public static List<Color> BuildHairColors(HairConfig build_object, CharacterConfig ModeConfig = null) {

            List<bool> changed = new(new bool[MAX_DASHES + 1]);

            // Default colors taken from vanilla
            List<Color> GeneratedHairColors = new List<Color>(new Color[MAX_DASHES + 1]) {
                [0] = Calc.HexToColor("44B7FF"),
                [1] = ModeConfig != null && ModeConfig.BadelineMode == true ? Calc.HexToColor("9B3FB5") : Calc.HexToColor("AC3232"),
                [2] = Calc.HexToColor("FF6DEF")
            };

            if (build_object != null && build_object.HairColors != null) {
                foreach (HairColor hairColor in build_object.HairColors) {

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
            return GeneratedHairColors;
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

        public static List<Color> BuildHairColors(SkinModHelperOldConfig config) {

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
            return GeneratedHairColors;
        }
    }
}
