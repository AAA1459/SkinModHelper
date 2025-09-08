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
    #region SkinModHelperConfig
    public class SkinModHelperConfig {
        #region Ctor
        public SkinModHelperConfig() {
        }
        public SkinModHelperConfig(SkinModHelperOldConfig old_config) : this() {
            SkinName = old_config.SkinId;
            SkinDialogKey = old_config.SkinDialogKey ?? SkinName;
            OtherSprite_ExPath = old_config.SkinId.Replace('_', '/');
        }
        #endregion

        #region Values
        public string SkinName { get; set; }
        public bool Player_List { get; set; }
        public bool Silhouette_List { get; set; }
        public bool? General_List { get; set; }

        [YamlIgnore]
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
        public string Mod { get; set; }

        [YamlIgnore]
        public int hashValues = -1;
        #endregion
    }
    #endregion

    #region SkinModHelperOldConfig
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
