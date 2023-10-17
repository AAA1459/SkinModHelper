using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    [CustomEntity("SkinModHelper/SkinSwapTrigger")]
    internal class SkinSwapTrigger : Trigger {
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        private readonly string skinId;
        private readonly bool revertOnLeave;


        private string oldskinId;
        public SkinSwapTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
            skinId = data.Attr("skinId", DEFAULT);
            revertOnLeave = data.Bool("revertOnLeave", false);

            if (string.IsNullOrEmpty(skinId)) {
                skinId = "Null";
            } else if (skinId.EndsWith("_NB") && skinConfigs.ContainsKey(skinId.Remove(-1, 3))) {
                skinId = skinId.Remove(-1, 3);
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            oldskinId = Session.SessionPlayerSkin;

            string hash_object = skinId;
            if (skinConfigs.ContainsKey(skinId) || skinId == DEFAULT) {
                Session.SessionPlayerSkin = hash_object;
            } else if (skinId == "Null")  {
                Session.SessionPlayerSkin = null;
            } else {
                Logger.Log(LogLevel.Warn, "SkinModHelper/SkinSwapTrigger", $"Tried to swap to unknown SkinID: {skinId}");
                return;
            }

            PlayerSkinSystem.RefreshPlayerSpriteMode();
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (revertOnLeave) {
                Session.SessionPlayerSkin = oldskinId;

                PlayerSkinSystem.RefreshPlayerSpriteMode();
            }
        }
    }
}