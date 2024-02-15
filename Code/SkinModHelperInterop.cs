using MonoMod.ModInterop;

namespace Celeste.Mod.SkinModHelper {
    [ModExportName("SkinModHelperPlus")]
    public static class SkinModHelperInterop {
        internal static void Load() {
            typeof(SkinModHelperInterop).ModInterop();
        }

        public static void SessionSet_PlayerSkin(string newSkinId) {
            SkinModHelperModule.SessionSet_PlayerSkin(newSkinId);
        }
        public static void SessionSet_SilhouetteSkin(string newSkinId) {
            SkinModHelperModule.SessionSet_SilhouetteSkin(newSkinId);
        }
        public static void SessionSet_GeneralSkin(string newSkinId, bool? OnOff) {
            SkinModHelperModule.SessionSet_GeneralSkin(newSkinId, OnOff);
        }
    }
}
