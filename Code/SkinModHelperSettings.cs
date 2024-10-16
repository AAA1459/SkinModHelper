using System;
using System.Collections.Generic;
using System.IO;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    [SettingName("SKIN_MOD_HELPER_SETTINGS_TITLE")]
    public class SkinModHelperSettings : EverestModuleSettings {

        [SettingIgnore] // Please modify it in the save file(.celeste file)
        public List<string> HideSkinsInOptions { get; set; } = new() {
            "If you loaded much PlayerSkins, and feels difficult to switch them,",
            "",
            "So you can write the skins key that you want ignore here.",
            "",
            "(This not affect those skin work in game)",
            "",
            "",
            ""
        };
        [SettingIgnore]
        public bool PlayerSkinGreatestPriority { get; set; } = false;
        [SettingIgnore]
        public bool PlayerSkinHairColorsDisabled { get; set; } = false;
        [SettingIgnore]
        public bool PlayerSkinHairLengthsDisabled { get; set; } = false;

        public enum BackpackMode { Default, Invert, Off, On }
        private BackpackMode backpack = BackpackMode.Default;

        public BackpackMode Backpack {
            get => backpack;
            set {
                backpack = value;
                backpackSetting = (int)value;
                PlayerSkinSystem.RefreshPlayerSpriteMode();
            }
        }


        [SettingIgnore]
        public string SelectedPlayerSkin { get; set; }


        [SettingIgnore]
        public bool SilhouetteVariantsWithOwnMenu { get; set; } = false;
        [SettingIgnore]
        public string SelectedSilhouetteSkin { get; set; }


        [SettingIgnore]
        public Dictionary<string, bool> ExtraXmlList {
            get => _ExtraXmlList;
            // When reading the save it loses the comparator... so create the new with the comparator.
            set => _ExtraXmlList = new(value, StringComparer.OrdinalIgnoreCase);
        }
        private Dictionary<string, bool> _ExtraXmlList = new(StringComparer.OrdinalIgnoreCase);


        [SettingIgnore]
        public bool FreeCollocations_OffOn { get; set; }

        [SettingIgnore]
        public Dictionary<string, string> FreeCollocations_Sprites {
            get => _FreeCollocations_Sprites; set => _FreeCollocations_Sprites = new(value, StringComparer.OrdinalIgnoreCase);
        }
        private Dictionary<string, string> _FreeCollocations_Sprites = new(StringComparer.OrdinalIgnoreCase);

        [SettingIgnore]
        public Dictionary<string, string> FreeCollocations_Portraits {
            get => _FreeCollocations_Portraits; set => _FreeCollocations_Portraits = new(value, StringComparer.OrdinalIgnoreCase);
        }
        private Dictionary<string, string> _FreeCollocations_Portraits = new(StringComparer.OrdinalIgnoreCase);

        [SettingIgnore]
        public Dictionary<string, string> FreeCollocations_OtherExtra {
            get => _FreeCollocations_OtherExtra; set => _FreeCollocations_OtherExtra = new(value, StringComparer.OrdinalIgnoreCase);
        }
        private Dictionary<string, string> _FreeCollocations_OtherExtra = new(StringComparer.OrdinalIgnoreCase);




        public void CreateBackpackEntry(TextMenu textMenu, bool inGame) {
            Array enumValues = Enum.GetValues(typeof(BackpackMode));
            Array.Sort((int[])enumValues);
            TextMenu.Item item = new TextMenu.Slider("SkinModHelper_options_Backpack".DialogClean(),
                    i => {
                        string enumName = enumValues.GetValue(i).ToString();
                        return $"SkinModHelper_options_{nameof(BackpackMode)}_{enumName}".DialogClean();
                    }, 0, enumValues.Length - 1, (int)Backpack)
                .Change(value => Backpack = (BackpackMode)value);

            if (SkinModHelperUI.Disabled(inGame)) {
                item.Disabled = true;
            }
            textMenu.Add(item);
        }
        public string SelectedSkinMod = null;
    }
}
