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
        /// <para> if tracking is true and source is Monocle.Sprite, so send an delegate to doing this when source's frame change.</para>
        /// </summary>
        public static void CopyColorGrades(Monocle.Image source, Monocle.Image target, bool tracking = false) {
            MonoMod.Utils.DynData<Monocle.Image> sourceData = new(source);
            MonoMod.Utils.DynData<Monocle.Image> targetData = new(target);

            targetData["ColorGrade_Path"] = sourceData.Get<string>("ColorGrade_Path");
            targetData["ColorGrade_Atlas"] = sourceData.Get<Monocle.Atlas>("ColorGrade_Atlas");
            
            if (tracking && source is Monocle.Sprite s2) {
                s2.OnFrameChange += frame => {
                    targetData["ColorGrade_Path"] = sourceData.Get<string>("ColorGrade_Path");
                    targetData["ColorGrade_Atlas"] = sourceData.Get<Monocle.Atlas>("ColorGrade_Atlas");
                };
            }
        }
    }
}
