using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    [CustomEntity("SkinModHelper/SkinSwapTrigger")]
    public class SkinSwapTrigger : Trigger {
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        private readonly string skinId;
        private readonly bool revertOnLeave;

        private readonly bool playerVariant;
        private readonly bool otherselfVariant;
        private readonly bool silhouetteVariant;

        private string[] oldskinId = new string[3];
        public SkinSwapTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
            skinId = data.Attr("skinId", DEFAULT);
            revertOnLeave = data.Bool("revertOnLeave", false);

            playerVariant = data.Bool("playerVariant", true);
            otherselfVariant = data.Bool("otherselfVariant", true);
            silhouetteVariant = data.Bool("silhouetteVariant", false);

            if (string.IsNullOrEmpty(skinId)) {
                skinId = "Null";
            } else if (skinId.EndsWith("_NB") && skinConfigs.ContainsKey(skinId.Remove(-1, 3))) {
                skinId = skinId.Remove(-1, 3);
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            if (skinConfigs.ContainsKey(skinId) || skinId == DEFAULT) {
                swapSkin(skinId);
            } else if (skinId == "Null")  {
                swapSkin(null);
            } else {
                Logger.Log(LogLevel.Warn, "SkinModHelper/SkinSwapTrigger", $"Tried to swap to unknown SkinID: {skinId}");
                return;
            }
            PlayerSkinSystem.RefreshPlayerSpriteMode(player);
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (revertOnLeave) {
                revertSkin();
                PlayerSkinSystem.RefreshPlayerSpriteMode(player);
            }
        }
        public override void SceneEnd(Scene scene) {
            if (revertOnLeave && PlayerIsInside) {
                revertSkin();
            }
            base.SceneEnd(scene);
        }

        private void swapSkin(string newId) {
            if (playerVariant) {
                oldskinId[0] = Session.SelectedPlayerSkin;
                Session.SelectedPlayerSkin = newId;
            }
            if (otherselfVariant) {
                oldskinId[1] = Session.SelectedOtherSelfSkin;
                Session.SelectedOtherSelfSkin = newId;
            }
            if (silhouetteVariant) {
                oldskinId[2] = Session.SelectedSilhouetteSkin;
                Session.SelectedSilhouetteSkin = newId;
            }
        }
        private void revertSkin() {
            if (playerVariant) {
                Session.SelectedPlayerSkin = oldskinId[0];
            }
            if (otherselfVariant) {
                Session.SelectedOtherSelfSkin = oldskinId[1];
            }
            if (silhouetteVariant) {
                Session.SelectedSilhouetteSkin = oldskinId[2];
            }
        }
    }
}