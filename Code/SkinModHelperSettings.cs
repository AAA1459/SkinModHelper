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

        public enum BackpackMode { Default, Invert, Off, On }
        private BackpackMode backpack = BackpackMode.Default;

        public BackpackMode Backpack {
            get => backpack;
            set {
                backpack = value;
                PlayerSkinSystem.RefreshPlayerSpriteMode();
            }
        }


        [SettingIgnore]
        public string SelectedPlayerSkin { get; set; }


        [SettingIgnore]
        public string SelectedSilhouetteSkin { get; set; }


        [SettingIgnore]
        public Dictionary<string, bool> ExtraXmlList { get; set; } = new();


        [SettingIgnore]
        public bool FreeCollocations_OffOn { get; set; }

        [SettingIgnore]
        public Dictionary<string, string> FreeCollocations_Sprites { get; set; } = new();
        [SettingIgnore]
        public Dictionary<string, string> FreeCollocations_Portraits { get; set; } = new();
        [SettingIgnore]
        public Dictionary<string, string> FreeCollocations_OtherExtra { get; set; } = new();




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
