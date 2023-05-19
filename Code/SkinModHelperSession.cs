using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Mod.SkinModHelper {

    public class SkinModHelperSession : EverestModuleSession {


        public string SessionPlayerSkin { get; set; } = null;
        public string SessionSilhouetteSkin { get; set; } = null;
        public Dictionary<string, bool> SessionExtraXml { get; set; } = new();

        public static Dictionary<string, List<string>> SpriteSkins_session { get; set; } = new Dictionary<string, List<string>>();

        public static Dictionary<string, List<string>> PortraitsSkins_session { get; set; } = new Dictionary<string, List<string>>();

        public static Dictionary<string, List<string>> OtherSkins_session { get; set; } = new Dictionary<string, List<string>>();
    }
}
