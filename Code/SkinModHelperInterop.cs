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

        /// <summary> 
        /// <para> Copy the ColorGrades of source to target. </para>
        /// <para> if tracking is true, so send an delegate to doing this when source's frame change.</para>
        /// </summary>
        public static void CopyColorGrades(Monocle.Sprite source, Monocle.Sprite target, bool tracking = false) {
            MonoMod.Utils.DynData<Monocle.Sprite> sourceData = new(source);
            MonoMod.Utils.DynData<Monocle.Sprite> targetData = new(target);

            targetData["ColorGrade_Path"] = sourceData.Get<string>("ColorGrade_Path");
            targetData["ColorGrade_Atlas"] = sourceData.Get<Monocle.Atlas>("ColorGrade_Atlas");
            
            if (tracking) {
                source.OnFrameChange += frame => {
                    targetData["ColorGrade_Path"] = sourceData.Get<string>("ColorGrade_Path");
                    targetData["ColorGrade_Atlas"] = sourceData.Get<Monocle.Atlas>("ColorGrade_Atlas");
                };
            }
        }
    }
}
