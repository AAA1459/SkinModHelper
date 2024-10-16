﻿using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Celeste.Mod.SkinModHelper {

    public class SkinModHelperSession : EverestModuleSession {
        public string SelectedPlayerSkin { get; set; }

        public string SelectedSilhouetteSkin { get; set; }

        public Dictionary<string, bool> ExtraXmlList {
            get => _ExtraXmlList;
            // When reading the save it loses the comparator... so create the new with the comparator.
            set => _ExtraXmlList = new(value, StringComparer.OrdinalIgnoreCase);
        }
        [YamlIgnore]
        private Dictionary<string, bool> _ExtraXmlList = new(StringComparer.OrdinalIgnoreCase);

        public string Player_animPrefixAddOn { get; set; }



        [YamlIgnore]
        public Dictionary<string, string> SpriteSkin_record = new(StringComparer.OrdinalIgnoreCase);
        [YamlIgnore]
        public Dictionary<string, string> PortraitsSkin_record = new(StringComparer.OrdinalIgnoreCase);
        [YamlIgnore]
        public Dictionary<string, string> OtherSkin_record = new(StringComparer.OrdinalIgnoreCase);
    }
}