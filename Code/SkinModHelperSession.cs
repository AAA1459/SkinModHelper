﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Mod.SkinModHelper {

    public class SkinModHelperSession : EverestModuleSession {
        public string SelectedPlayerSkin { get; set; }

        public string SelectedSilhouetteSkin { get; set; }

        public Dictionary<string, bool> ExtraXmlList { get; set; } = new();


        public Dictionary<string, string> SpriteSkin_record = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> PortraitsSkin_record = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> OtherSkin_record = new(StringComparer.OrdinalIgnoreCase);


        public string Player_animPrefixAddOn { get; set; }
    }
}