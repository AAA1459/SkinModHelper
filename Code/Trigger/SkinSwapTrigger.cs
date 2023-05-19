using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.SkinModHelper {
    [CustomEntity("SkinModHelper/SkinSwapTrigger")]
    internal class SkinSwapTrigger : Trigger {
        private readonly string skinId;
        private readonly bool revertOnLeave;


        private string oldskinId;
        public SkinSwapTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
            skinId = data.Attr("skinId", SkinModHelperModule.DEFAULT);
            revertOnLeave = data.Bool("revertOnLeave", false);

            if (string.IsNullOrEmpty(skinId)) {
                skinId = "Null";
            } else if (skinId.EndsWith("_NB") && SkinModHelperModule.skinConfigs.ContainsKey(skinId.Remove(-1, 3))) {
                skinId = skinId.Remove(-1, 3);
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            oldskinId = SkinModHelperModule.Session.SessionPlayerSkin;

            string hash_object = skinId;
            if (SkinModHelperModule.skinConfigs.ContainsKey(skinId) || skinId == SkinModHelperModule.DEFAULT) {
                SkinModHelperModule.Session.SessionPlayerSkin = hash_object;
            } else if (skinId == "Null")  {
                SkinModHelperModule.Session.SessionPlayerSkin = null;
            } else {
                Logger.Log(LogLevel.Warn, "SkinModHelper/SkinSwapTrigger", $"Tried to swap to unknown SkinID: {skinId}");
                return;
            }

            if (!SkinModHelperModule.backpackOn && SkinModHelperModule.skinConfigs.ContainsKey(hash_object + "_NB")) {
                hash_object = hash_object + "_NB";
            }
            SkinModHelperModule.RefreshPlayerSpriteMode(hash_object);
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (revertOnLeave) {
                SkinModHelperModule.Session.SessionPlayerSkin = oldskinId;

                string hash_object = oldskinId;
                if (!SkinModHelperModule.backpackOn && SkinModHelperModule.skinConfigs.ContainsKey(hash_object + "_NB")) {
                    hash_object = hash_object + "_NB";
                }
                SkinModHelperModule.RefreshPlayerSpriteMode(hash_object);
            }
        }
    }
}